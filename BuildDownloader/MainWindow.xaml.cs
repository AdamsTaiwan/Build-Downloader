using System.Windows;

namespace BuildDownloader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly AppVM vm;

        public MainWindow()
        {
            this.vm = new AppVM
            {
                ui=this
            };
            this.DataContext = this.vm;

            InitializeComponent();
                        
            this.vm.InitUI();

            this.cbFeed.SelectionChanged += (s, e) => this.vm.FeedSelected(s);
            this.btnDownload.Click += (s, e) => this.vm.DownLoad();
            this.btnLoad.Click += (s, e) => this.vm.LoadSessions();
            this.btnBrowse.Click += (s, e) => this.vm.BrowseFolder();
            this.btnOpen.Click += (s, e) => this.vm.OpenFolder();
            this.tbSessionCode.TextChanged += (s, e) => this.vm.SessionCodeChanged(s);
            this.tbTitle.TextChanged += (s, e) => this.vm.TitelChanged(s);
            this.chkSlides.Click += (s, e) => this.vm.SlidesClicked(s);
            this.chkVideos.Click += (s, e) => this.vm.VideosClicked(s);
            this.btnClearFilters.Click += (s, e) => this.vm.ClearFilters();
            this.btnGetSlides.Click += (s, e) => this.vm.GetSlides();
            this.btnGetVideos.Click += (s, e) => this.vm.GetVideos();
            this.btnCreateMarkup.Click += (s, e) => this.vm.CreateForWeb();
            this.dgMain.SelectionChanged += (s, e) => this.vm.SelectionChanged(s);
            this.lbFields.SelectionChanged += (s,e) => this.vm.Fields_SelectionChanged(s);
        }

    }
}
