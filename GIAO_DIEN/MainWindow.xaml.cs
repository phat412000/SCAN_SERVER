using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using Microsoft.Win32;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Windows.Threading;
using Window = System.Windows.Window;
using System.Threading;
using Basler.Pylon;

using System.Net.WebSockets;

namespace GIAO_DIEN
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
 

        public MainWindow()
        {

            InitializeComponent();

           
            MainWindow1.Height = 648.4;
            MainWindow1.Width = 1103.2;

        }
        private void ChooseBarCodeMode_Click(object sender, RoutedEventArgs e)
        {
            UserControl s = new UserControl1();
            User.Content = s;
        }

        private void ChooseOpenfileMode_Click(object sender, RoutedEventArgs e)
        {
            WindowStart w = new WindowStart(String.Empty, String.Empty);
            w.Visibility = Visibility.Visible;

            // m.Visibility = Visibility.Hidden;

            this.Visibility = Visibility.Hidden;
            Console.WriteLine("aab");
        }

       

    }
}
