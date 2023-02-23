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
using Window = System.Windows.Window;
namespace GIAO_DIEN
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        int ButtonFile_Click_Mode = 0;
        string SelectImgPath;
        Mat SourceImg;
        public MainWindow()
        {

            InitializeComponent();
            ButtonFile_canvas.Visibility = Visibility.Hidden;


            //test.Background = imgBrush;
        }

        private void OpenFileButton1_Click(object sender, RoutedEventArgs e)
        {

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Select Image";
            openFileDialog.Filter = "Image files (*.png;*.jpeg;*jpg;*bmp)|*.png;*.jpeg;*jpg;*bmp|All files (*.*)|*.*";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            openFileDialog.FilterIndex = 1;
            //openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (openFileDialog.ShowDialog() == false)
            { MessageBox.Show("fail to show"); }
            if (openFileDialog.FileName != "")
            {
                SelectImgPath = openFileDialog.FileName;
                BitmapImage bitmap = new BitmapImage();
                SourceImg = Cv2.ImRead(SelectImgPath);
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(SelectImgPath);
                bitmap.EndInit();
                //ImgScreen.Width = bitmap.Width;
                //ImgScreen.Height = bitmap.Height;
                //ImgScreen_Canvas.Width = bitmap.Width;
                //ImgScreen_Canvas.Height = bitmap.Height;
                //Canvas_On_ImgScreen.Width = bitmap.Width;
                //Canvas_On_ImgScreen.Height = bitmap.Height;
                //ImgScroll.Width = bitmap.Width / 2;
                //ImgScroll.Height = bitmap.Height / 2;
                ImgScreen.Source = bitmap;
                //ImagePathTxt.Content = SelectImgPath;
                //img = bitmap;
                //img1 = BitmapImage2Bitmap(img);
            }
            ButtonFile_canvas.Visibility = Visibility.Hidden;
            ButtonFile_Click_Mode = 0;
            FileButton.Background = null;
            SolidColorBrush Foreground_color = new SolidColorBrush();
            Foreground_color.Color = Colors.White;
            FileButton.Foreground = Foreground_color;

        }

        private void FileButton_Click(object sender, RoutedEventArgs e)
        {
            ButtonFile_Click_Mode += 1;
            if (ButtonFile_Click_Mode == 1)
            {
                ButtonFile_canvas.Visibility = Visibility.Visible;
                SolidColorBrush color = new SolidColorBrush();
                color.Color = Colors.White;
                FileButton.Background = color;
                SolidColorBrush Foreground_color = new SolidColorBrush();
                Foreground_color.Color = Color.FromRgb(3, 102, 169);
                FileButton.Foreground = Foreground_color;
            }
            if (ButtonFile_Click_Mode == 2)
            {
                ButtonFile_canvas.Visibility = Visibility.Hidden;
                ButtonFile_Click_Mode = 0;
                FileButton.Background = null;
                SolidColorBrush Foreground_color = new SolidColorBrush();
                Foreground_color.Color = Colors.White;
                FileButton.Foreground = Foreground_color;
            }
        }
    }
}
