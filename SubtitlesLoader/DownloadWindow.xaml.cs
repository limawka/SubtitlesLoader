using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SubtitlesLoader
{
    /// <summary>
    /// Interaction logic for DownloadWindow.xaml
    /// </summary>
    public partial class DownloadWindow : Window
    {
        public DownloadWindow(string path)
        {
            InitializeComponent();
            this.result.Text = path;

            string html = "loading...";
            
            try
            {
                html = ApiServices.GetSubtitles($@"Z:\dane\Filmy\A Beautiful Mind (2001) [1080p]\A Beautiful Mind (2001) [1080p].mp4");
            }
            catch (Exception ex) { 
                html = ex.ToString();
            }
            
            result.Text = html;
            


        }

    }
}
