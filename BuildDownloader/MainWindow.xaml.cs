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
        }

    }
}
