using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            var v = "4.7.2";
            this.dsFeedList = FeedList.New();
            this.dsFeedList.ReadXml("FeedList.xml");
            this.DVFeed = new DataView(this.dsFeedList.Tables[0]);
            if (this.dsFeedList.Tables[0].Rows.Count > 0)
            {
#if NET472  //.Net 4.7.2 code here
                this.Title = $"{Res.DEFAULT_TITLE} (.Net {v})";
                LoadRow(this.dsFeedList.Tables[0].Rows[this.dsFeedList.Tables[0].Rows.Count - 1]);   //select last feed
#endif
#if NET     //.Net code here
                v = "8.x";
                this.Title = $"{Res.DEFAULT_TITLE} (.Net {v})";
                LoadRow(this.dsFeedList.Tables[0].Rows[^1]);   //select last feed
#endif   
            }
            else
            {
                LoadRow(null);
            }

            this.ds = BuildSet.New();
            foreach (DataColumn c in this.ds.Tables[0].Columns)
            {
                this.Fields.Add(c.ColumnName);
            }
            this.CanLoad = File.Exists(Path.Combine(this.outputPath, Res.SessionData));
        }

        private void LoadRow(DataRow r)
        {
            string type = Res.DEFAULT_TYPE;
            string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string url = Res.DEFAULT_URL;
            bool candownload = false;

            this.cr = r;

            if (r != null)
            {
                if (this.cr != null)
                {
                    this.FeedName = this.cr["name"].ToString();
                    type = this.cr["type"].ToString();
                    path = this.cr["saveto"].ToString();
                    url = this.cr["url"].ToString();
                    candownload = Convert.ToInt32(this.cr["closed"]) == 0;
                }
            }

            this.Type = Convert.ToInt32(type);
            this.OutputPath = path;
            this.slideExt = path.Contains("Ignite20") ? ".pdf" : ".pptx";
            this.URL = url;
            this.CanDownload = candownload;
        }



        internal void InitUI()
        {
            this.ui.tbTemplate.Text = File.ReadAllText(Res.ResourceFile);
            if (this.dsFeedList.Tables[0].Rows.Count > 0)
            {
                this.ui.cbFeed.SelectedIndex = this.dsFeedList.Tables[0].Rows.Count - 1;
            }

            this.ui.cbFeed.SelectionChanged += (s, e) => this.FeedSelected(s);
            this.ui.btnDownload.Click += (s, e) => this.DownLoad();
            this.ui.btnLoad.Click += (s, e) => this.LoadSessions();
            this.ui.btnBrowse.Click += (s, e) => this.BrowseFolder();
            this.ui.btnOpen.Click += (s, e) => this.OpenFolder();
            this.ui.tbSessionCode.TextChanged += (s, e) => this.SessionCodeChanged(s);
            this.ui.tbLang.TextChanged += (s, e) => this.LangLocaleChanged(s);
            this.ui.tbTitle.TextChanged += (s, e) => this.TitleChanged(s);
            this.ui.chkSlides.Click += (s, e) => this.SlidesClicked(s);
            this.ui.chkVideos.Click += (s, e) => this.VideosClicked(s);
            this.ui.btnClearFilters.Click += (s, e) => this.ClearFilters();
            this.ui.btnGetSlides.Click += (s, e) => this.GetSlides();
            this.ui.btnGetVideos.Click += (s, e) => this.GetVideos();
            this.ui.btnCreateMarkup.Click += (s, e) => this.CreateForWeb();
            this.ui.dgMain.SelectionChanged += (s, e) => this.SelectionChanged(s);
            this.ui.lbFields.SelectionChanged += (s, e) => this.Fields_SelectionChanged(s);
        }


        #region Fields
        internal MainWindow ui;

        private string FeedName = ""; 
        private int Type = 1; //1=Old session format, 2=Build2020 session format
        private string filterSessionCode = "";
        private string filterLangLocale = "";
        private string filterTitle = "";
        private string filterSlides = "";
        private string filterVideos = "";
        private string slideExt = "pptx";  //slide extension

        private DataSet dsFeedList = new DataSet("R");
        private DataSet ds = new DataSet("R");

        private DataRow cr; //current row
        private DataView dv = new DataView();
        #endregion


        #region INotify
        private string title = Res.DEFAULT_TITLE;
        public string Title
        {
            get => this.title;
            set => SetProperty(ref this.title, value);
        }

        private string status = "";
        public string Status
        {
            get => this.status;
            set => SetProperty(ref this.status, value);
        }

        private string url = Res.DEFAULT_URL;
        public string URL
        {
            get => this.url;
            set => SetProperty(ref this.url, value);
        }

        private string outputPath = "";
        public string OutputPath
        {
            get => this.outputPath;
            set => SetProperty(ref this.outputPath, value);
        }

        private bool canLoad = false;
        public bool CanLoad
        {
            get => this.canLoad;
            set => SetProperty(ref this.canLoad, value);
        }

        private bool canDownload = false;
        public bool CanDownload
        {
            get => this.canDownload;
            set => SetProperty(ref this.canDownload, value);
        }


        private ObservableCollection<string> fields = new ObservableCollection<string>();
        public ObservableCollection<string> Fields
        {
            get => this.fields;
            set => SetProperty(ref this.fields, value);
        }


        public DataView DV
        {
            get => this.dv;
            set => SetProperty(ref this.dv, value);
        }

        private DataView dvFeed = new DataView();
        public DataView DVFeed
        {
            get => this.dvFeed;
            set => SetProperty(ref this.dvFeed, value);
        }


        #endregion


        #region Methods
        internal void FeedSelected(object s)
        {
            ComboBox cbi;
            DataRowView drv;
            try
            {
                cbi = s as ComboBox;
                drv = cbi.SelectedItem as DataRowView;
                LoadRow(drv.Row);

            }
            catch (Exception ex)
            {
                this.Status = $"Error {ex.Message}";
            }
        }

        internal async void DownLoad()
        {
            var sw = new Stopwatch();
            int cnt;

            try
            {
                sw.Start();
                this.Status = "Downloading session data...";
                cnt = await DownloadSessions();
                this.DV = new DataView(this.ds.Tables["B"]);
                this.ui.tbLang.Text = "en-US";

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
            c.Timeout = TimeSpan.FromMinutes(5);

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

                    r = this.ds.Tables["B"].NewRow();

                    r["id"] = Guid.NewGuid().ToString();
                    r["sessionId"] = item.sessionId;
                    r["langLocale"] = item.langLocale;
                    r["sessionCode"] = item.sessionCode;
                    r["title"] = item.title;
                    r["sortRank"] = item.sortRank;
                    if (this.Type == 3)
                    {
                        r["level"] = getItemDetail(item.level);
                        r["sessionType"] = getItemDetail(item.sessionType);
                    }
                    else
                    {
                        r["level"] = item.level;
                        r["sessionType"] = item.sessionType;
                    }
                    r["sessionTypeId"] = item.sessionTypeId;
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

                    this.ds.Tables["B"].Rows.Add(r);

                    Trace.WriteLine($"INF {i} {item.sessionId}");
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"ERR {i} {item.sessionId} {ex.Message}");
                }
            }
            this.ds.WriteXmlSchema(Path.Combine(this.outputPath, Res.SessionSchema));
            this.ds.WriteXml(Path.Combine(this.outputPath, Res.SessionData));
            return i;
        }

        private string getItemDetail(dynamic item)
        {
            string l = item.displayValue;
            if (l != item.logicalValue.ToString())
            {
                l = $"{l} ({item.logicalValue})";
            }
            return l;
        }

        internal async void LoadSessions()
        {
            try
            {
                this.Status = "Loading sessions from file...";
                await Task.Run(() =>
                {
                    this.ds = BuildSet.New();
                    this.ds.ReadXml(Path.Combine(this.outputPath, Res.SessionData));
                });
                this.DV = new DataView(this.ds.Tables["B"]);
                this.ui.tbLang.Text = "en-US";
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

        internal void LangLocaleChanged(object s)
        {
            try
            {
                var ui = (TextBox)s;
                this.filterLangLocale = ui.Text;
                ApplyFilter();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        internal void TitleChanged(object s)
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
                    if (this.filterLangLocale.Length > 0)
                    {
                        sb.Append($"{delim}langLocale LIKE '%{this.filterLangLocale}%'");
                        delim = " AND ";
                    }
                    if (this.filterTitle.Length > 0)
                    {
                        sb.Append($"{delim}title LIKE '%{this.filterTitle}%'");
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
            this.ui.tbLang.Text = "";
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
            string tmp = "";    //for template processing
            string tmp2 = "";
            string slides;
            string[] lines;
            var sb = new StringBuilder();   //Text before template
            var sb2 = new StringBuilder();  //Text after template
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
                                    sb.AppendLine(line.Replace("{feed}", this.FeedName));
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
