using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;


namespace BuildDownloader
{
    public class AppVM : INotifyBase
    {
        public AppVM()
        {
            string type = Res.DEFAULT_TYPE;
            if (ConfigurationManager.AppSettings["DefaultType"]?.Length > 0)
            {
                type = ConfigurationManager.AppSettings["DefaultType"];
            }
            this.Type = Convert.ToInt32(type);

            string url = Res.DEFAULT_URL;
            if (ConfigurationManager.AppSettings["DefaultURL"]?.Length > 0)
            {
                url = ConfigurationManager.AppSettings["DefaultURL"];
            }
            this.URL = url;

            string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (ConfigurationManager.AppSettings["DefaultPath"]?.Length > 0)
            {
                path = ConfigurationManager.AppSettings["DefaultPath"];
            }
            this.OutputPath = path;
            this.slideExt = path.Contains("Ignite2020") ? ".pdf" : ".pptx";

            this.ds = BuildSet.New();
            foreach (DataColumn c in ds.Tables[0].Columns)
            {
                this.Fields.Add(c.ColumnName);
            }
            this.CanLoad = File.Exists(Path.Combine(this.outputPath, Res.SessionData));
        }


        internal void InitUI()
        {
            this.ui.tbTemplate.Text = File.ReadAllText(Res.ResourceFile);
        }


        #region Fields
        internal MainWindow ui;

        private int Type=1; //1=Old session format, 2=Build2020 session format
        private string filterSessionCode = "";
        private string filterTitle="";
        private string filterSlides = "";
        private string filterVideos = "";
        private string slideExt="pptx";  //slide extension

        private DataSet ds = new DataSet("R");

        private DataView dv = new DataView();

        #endregion


        #region INotify
        private string title = Res.DEFAULT_TITLE;
        public string Title
        {
            get { return this.title; }
            set { SetProperty(ref this.title, value); }
        }

        private string status = "";
        public string Status
        {
            get { return this.status; }
            set { SetProperty(ref this.status, value); }
        }

        private string url = Res.DEFAULT_URL;
        public string URL
        {
            get { return this.url; }
            set { SetProperty(ref this.url, value); }
        }

        private string outputPath = "";
        public string OutputPath
        {
            get { return this.outputPath; }
            set { SetProperty(ref this.outputPath, value); }
        }

        private bool canLoad = false;
        public bool CanLoad
        {
            get { return this.canLoad; }
            set { SetProperty(ref this.canLoad, value); }
        }


        private ObservableCollection<string> fields = new ObservableCollection<string>();
        public ObservableCollection<string> Fields
        {
            get { return this.fields; }
            set { SetProperty(ref this.fields, value); }
        }


        public DataView DV
        {
            get { return this.dv; }
            set { SetProperty(ref this.dv, value); }
        }

        #endregion


        #region Methods
        internal async void DownLoad()
        {
            var sw = new Stopwatch();
            int cnt;

            try
            {
                sw.Start();
                this.Status = "Downloading session data...";
                cnt=await DownloadSessions();
                this.DV = new DataView(this.ds.Tables["B"]);

                this.Status = $"Found {cnt} sessions in {sw.Elapsed}";
            }
            catch (Exception ex)
            {
                this.Status = $"Error {ex.Message}";
            }
        }

        internal async Task<int> DownloadSessions()
        {
            string json, json2;
            dynamic o;
            HttpClient c;
            int i = 0;
            bool hasSlides = false;
            bool hasVideo = false;
            bool hasChanged = false;
            string d;
            DataRow r;

            c = new HttpClient();

            json = await c.GetStringAsync(this.url);
            json2 = json.Replace("OMG", "").Replace("\\\"\\\"", "");        //Cleanup
            this.Status = " Processing session data...";
            o = Tool.JsonConvertToClass<dynamic>(json2);
            Tool.CreateFolder(this.outputPath);
            File.WriteAllText(Path.Combine(this.outputPath, Res.SessionJson), json2);
            this.ds = BuildSet.New();

            foreach (dynamic item in o)
            {
                i++;
                try
                {
                    hasSlides = item.slideDeck?.ToString().Length > 0;
                    if (item.downloadVideoLink != null)
                    {
                        hasVideo = item.downloadVideoLink.ToString().Length > 0;

                    }
                    if (item.description.ToString().Contains("â€™"))
                    {
                        //TODO: Find a way to remove unwanted characters
                        d = item.description.ToString().Replace("’", "'").Replace("â€™", "'");
                    }

                    r = ds.Tables["B"].NewRow();

                    r["sessionId"] = item.sessionId;
                    r["sessionCode"] = item.sessionCode;
                    r["title"] = item.title;
                    r["sortRank"] = item.sortRank;
                    r["level"] = item.level;
                    r["sessionTypeId"] = item.sessionTypeId;
                    r["sessionType"] = item.sessionType;
                    r["durationInMinutes"] = item.durationInMinutes;
                    r["lastUpdate"] = item.lastUpdate;
                    r["visibleInSessionListing"] = item.visibleInSessionListing;
                    r["slideDeck"] = item.slideDeck;

                    if (this.Type == 1)
                    {
                        r["downloadVideoLink"] = item.downloadVideoLink;
                        r["onDemandThumbnail"] = item.onDemandThumbnail;
                    }
                    r["captionFileLink"] = item.captionFileLink;
                    r["hasSlides"] = hasSlides;
                    r["hasVideo"] = hasVideo;
                    r["hasChanged"] = hasChanged;
                    r["desciption"] = item.description;

                    ds.Tables["B"].Rows.Add(r);

                    Trace.WriteLine($"INF {i} {item.sessionId}");
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"ERR {i} {item.sessionId} {ex.Message}");
                }
            }
            ds.WriteXmlSchema(Path.Combine(this.outputPath, Res.SessionSchema));
            ds.WriteXml(Path.Combine(this.outputPath, Res.SessionData));
            return i;
        }

        internal async void LoadSessions()
        {
            try
            {
                this.Status = "Loading sessions from file...";
                await Task.Run(() =>
                {
                    this.ds = BuildSet.New();
                    ds.ReadXml(Path.Combine(this.outputPath, Res.SessionData));
                });
                this.DV = new DataView(this.ds.Tables["B"]);
                this.Status = "";
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        internal void BrowseFolder()
        {
            try
            {
                this.OutputPath = Tool.BrowseForFolder(this.OutputPath, "Select Output Folder");
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }


        internal void OpenFolder()
        {
            string folder;
            try
            {
                folder = this.outputPath;
                if (folder.Length > 0)
                {
                    if (!Directory.Exists(folder))
                    {
                        if (MessageBox.Show($"Create folder {folder}?", "Folder Does Not Exist", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
                        {
                            try
                            {
                                Directory.CreateDirectory(folder);
                            }
                            catch (Exception ex)
                            {
                                ShowError(ex, "Create Folder Error");
                                return;
                            }
                        }
                    }
                    if (Directory.Exists(folder))
                    {
                        Process.Start("Explorer.exe", folder);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        internal void SessionCodeChanged(object s)
        {
            try
            {
                var ui = (TextBox)s;
                this.filterSessionCode = ui.Text;
                ApplyFilter();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        internal void TitelChanged(object s)
        {
            try
            {
                var ui = (TextBox)s;
                this.filterTitle = ui.Text;
                ApplyFilter();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }


        internal void SlidesClicked(object s)
        {
            try
            {
                var ui = (CheckBox)s;
                if (ui.IsChecked.Value)
                {
                    this.filterSlides = $"hasSlides=true";
                }
                else
                {
                    this.filterSlides = "";
                }
                ApplyFilter();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        internal void VideosClicked(object s)
        {
            try
            {
                var ui = (CheckBox)s;
                if (ui.IsChecked.Value)
                {
                    this.filterVideos = $"hasVideo=true";
                }
                else
                {
                    this.filterVideos = "";
                }
                ApplyFilter();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private async void ApplyFilter()
        {
            var sb = new StringBuilder();
            var delim = "";
            try
            {
                this.Status = "Applying filter...";
                await Task.Run(() =>
                {
                    if (this.filterSessionCode.Length > 0)
                    {
                        sb.Append($"sessionCode LIKE '%{this.filterSessionCode}%'");
                        delim = " AND ";
                    }
                    if (this.filterTitle.Length > 0)
                    {
                        sb.Append($"title LIKE '%{this.filterTitle}%'");
                        delim = " AND ";
                    }
                    if (this.filterSlides.Length > 0)
                    {
                        sb.Append($"{delim}{this.filterSlides}");
                        delim = " AND ";
                    }
                    if (this.filterVideos.Length > 0)
                    {
                        sb.Append($"{delim}{this.filterVideos}");
                        delim = " AND ";
                    }
                });

                Trace.WriteLine($"INF Filter {sb.ToString()}");
                this.DV.RowFilter = sb.ToString();
                this.Status = $"{this.dv.Count} sessions";
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        internal void ClearFilters()
        {
            this.ui.tbSessionCode.Text = "";
            this.ui.tbTitle.Text = "";
            this.ui.chkSlides.IsChecked = false;
            this.ui.chkVideos.IsChecked = false;
            this.filterSlides = "";
            this.filterVideos = "";
            ApplyFilter();
        }

        internal void SelectionChanged(object s)
        {
            try
            {
                var ui = (DataGrid)s;

            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        internal async void GetSlides()
        {
            int i = 0;
            int cnt = 0;
            var q = new Queue<Session>();
            Session s;
            string path;
            string toFile;
            var sw = new Stopwatch();

            sw.Start();
            path = Path.Combine(this.outputPath, "Media");
            Tool.CreateFolder(path);
            foreach (DataRowView r in this.ui.dgMain.SelectedItems)
            {
                if (Convert.ToBoolean(r["hasSlides"]))
                {
                    q.Enqueue(new Session
                    {
                        Code = r["sessionCode"].ToString(),
                        SlidesURL = r["slideDeck"].ToString()
                    });
                }
            }
            cnt = q.Count;
            if (cnt > 0)
            {
                i = 0;
                while (q.Count > 0)
                {
                    s = q.Dequeue();
                    try
                    {
                        i++;
                        this.Status = $" Downloading {i}/{cnt} {s.Code}{this.slideExt}....";
                        toFile = Path.Combine(path, $"{s.Code}{this.slideExt}");
                        await DownloadResource(s.SlidesURL, toFile);
                    }
                    catch (Exception ex2)
                    {
                        Trace.WriteLine($"ERR {s.Code} {ex2.Message}");
                    }
                }
                this.Status = $" Download completed in {sw.Elapsed}";
            }
        }

        internal async void GetVideos()
        {
            int i = 0;
            int cnt = 0;
            var q = new Queue<Session>();
            Session s;
            string path;
            string toFile;
            var sw = new Stopwatch();

            sw.Start();
            path = Path.Combine(this.outputPath, "Media");
            Tool.CreateFolder(path);
            foreach (DataRowView r in this.ui.dgMain.SelectedItems)
            {
                if (Convert.ToBoolean(r["hasVideo"]))
                {
                    q.Enqueue(new Session
                    {
                        Code = r["sessionCode"].ToString(),
                        VideoURL = r["downloadVideoLink"].ToString()
                    });
                }
            }
            cnt = q.Count;
            if (cnt > 0)
            {
                i = 0;
                while (q.Count > 0)
                {
                    s = q.Dequeue();
                    try
                    {
                        i++;
                        this.Status = $" Downloading {i}/{cnt} {s.Code}.mp4....";
                        toFile = Path.Combine(path, $"{s.Code}.mp4");
                        await DownloadResource(s.VideoURL, toFile);
                    }
                    catch (Exception ex2)
                    {
                        Trace.WriteLine($"ERR {s.Code} {ex2.Message}");
                    }
                }
                this.Status = $" Download completed in {sw.Elapsed}";
            }
        }

        private async Task DownloadResource(string requestUri, string toFile)
        {
            using (HttpClient c = new HttpClient())
            {
                var r = await c.GetStreamAsync(requestUri);
                using (var fs = new FileStream(toFile, FileMode.CreateNew))
                {
                    await r.CopyToAsync(fs);
                }
            }
        }


        public void Fields_SelectionChanged(object s)
        {
            try
            {
                var ui = (ListBox)s;
                Clipboard.SetText("{" + ui.SelectedItem.ToString() + "}");
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        internal void CreateForWeb()
        {
            string tmp = "";
            string tmp2 = "";
            string slides;
            string[] lines;
            var sb = new StringBuilder();
            var sb2 = new StringBuilder();
            int i = 0;
            var sw = new Stopwatch();
            try
            {
                sw.Start();
                var f = Res.ResourceFile;
                File.WriteAllText(f, this.ui.tbTemplate.Text);
                lines = File.ReadAllLines(f);
                foreach (var line in lines)
                {
                    if (line.ToLower().Contains("<template>"))
                    {
                        i = 1;
                    }
                    else
                    {
                        if (line.ToLower().Contains("</template>"))
                        {
                            i = 2;
                        }
                        else
                        {
                            switch (i)
                            {
                                case 0:
                                    sb.AppendLine(line);
                                    break;
                                case 1:
                                    tmp += $"{line}{Environment.NewLine}";
                                    break;
                                default:
                                    sb2.AppendLine(line);
                                    break;
                            }
                        }
                    }
                }
                foreach (DataRowView r in this.ui.dgMain.SelectedItems)
                {
                    tmp2 = tmp;
                    slides = "";
                    foreach (DataColumn c in this.ds.Tables[0].Columns)
                    {
                        tmp2 = tmp2.Replace("{" + c.ColumnName + "}", r[c.ColumnName].ToString());
                    }
                    if (Convert.ToBoolean(r["hasSlides"]))
                    {
                        slides = $@"Slides {r["slideDeck"]}<br/>{this.outputPath}\Media\{r["sessionCode"]}{this.slideExt}";
                    }
                    tmp2 = tmp2.Replace("[hasSlides]", slides);
                    tmp2 = tmp2.Replace(Environment.NewLine, "<br/>");
                    sb.Append(tmp2);
                }
                sb.Append(sb2.ToString());
                //this.ui.tbOutput.Text = sb.ToString();
                File.WriteAllText(Path.Combine(this.outputPath, Res.SessionHTML), sb.ToString());
                this.ui.web.NavigateToString(sb.ToString());
                this.Status = $"Completed in {sw.Elapsed}";
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }


        /// <summary>
        /// Temporary error handler
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="sender"></param>
        private void ShowError(Exception ex, [CallerMemberName] string sender = "")
        {
            MessageBox.Show(ex.Message, sender);
        }

        #endregion
    }
}
