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
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Hosting;
using Python.Runtime;

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
        Mat MatThreshold;
        Mat ImgAfterAddMask;
        Mat FinalImg;
        int ZoomInRatio;
        int ZoomOutRatio;
        bool AutoMode = false;
        bool ManualMode = false;


        

        //Stack<double> mystackValues = new Stack<double>();
        //Stack<string> mystackLabels = new Stack<string>();
        //Stack<double> mystackCurValues = new Stack<double>();
        //Stack<string> mystackCurLabels = new Stack<string>();

        Stack<BACKDATA> backStack = new Stack<BACKDATA>();
        Stack<BACKDATA> nextStack = new Stack<BACKDATA>();

        private double cropX = 0;
        private double cropY = 0;
        private double cropWidth = 0;
        private double cropHeight = 0;
        double centerCircleX = 0;
        double centerCircleY = 0;
        int XminRectangle = 0;
        int YminRectangle = 0;
        int XmaxRectangle = 0;
        int YmaxRectangle = 0;
        bool circleCheck = false;
        bool RectangleCheck = false;
        double radiusCircle = 0;
        double H, W = 0;
        System.Windows.Point dragClickup;
        System.Windows.Point dragClickdown;

        string curLabel;
        double curValue;
        string a;
        object b;
        bool tamThresh = true;
        bool tamLocal = true;
        bool tamH = true;
        bool tamS = true;
        bool tamV = true;
        double c;
        //int zoomX = 0;
        //  PixelDataConverter converter = new PixelDataConverter();
        private DispatcherTimer Timer1;
        private int time = 0;

        SocketHandler socketHandler;

        public MainWindow()
        {
            
            InitializeComponent();
            ButtonFile_canvas.Visibility = Visibility.Hidden;
            AutoScreen.Visibility = Visibility.Hidden;
            ManualScreen.Visibility = Visibility.Hidden;
            WelcomeScreen.Visibility = Visibility.Visible;

            //test.Background = imgBrush;

            socketHandler = new SocketHandler();
            socketHandler.Connect();

            Thresh_Slider.TickFrequency = 10;

            
        }



        private String generateFilename()
        {
            Random rand = new Random();

            // Choosing the size of string
            // Using Next() string
            int stringlen = rand.Next(4, 10);
            int randValue;
            string str = "";
            char letter;
            for (int i = 0; i < stringlen; i++)
            {

                // Generating a random number.
                randValue = rand.Next(0, 26);

                // Generating random character by converting
                // the random number into character.
                letter = System.Convert.ToChar(randValue + 65);

                // Appending the letter to string.
                str = str + letter;

            }
            return str;
        }

        private async void OpenFileButton1_Click(object sender, RoutedEventArgs e)
        {

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.RestoreDirectory = true;
            openFileDialog.Title = "Select Image";
            openFileDialog.Filter = "Image files (*.png;*.jpeg;*jpg;*bmp)|*.png;*.jpeg;*jpg;*bmp|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
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
                ImgScreen.Width = SourceImg.Width/6.5;
                ImgScreen.Height = SourceImg.Height/6.5;
                Total_Count_Value.Text = SourceImg.Width.ToString() + ", " + SourceImg.Height.ToString();
                //ImgScreen_Canvas.Width = bitmap.Width;
                //ImgScreen_Canvas.Height = bitmap.Height;
                Canvas_On_ImgScreen.Width = SourceImg.Width/6.5;
                Canvas_On_ImgScreen.Height = SourceImg.Height/6.5;
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

        private void Zoom_in_Click(object sender, RoutedEventArgs e)
        {
            ZoomInRatio += 1;
            ZoomOutRatio -= 1;
            Matrix mat = ImgScreen.RenderTransform.Value;
            Matrix matCanvas = ImgScreen_Canvas.RenderTransform.Value;
            mat.ScaleAtPrepend(1.1, 1.1, 0, 0);
            MatrixTransform mtf = new MatrixTransform(mat);
            ImgScreen.RenderTransform = mtf;
            Canvas_On_ImgScreen.RenderTransform = mtf;
            S_Slider_Value.Text = "in: " + ZoomInRatio.ToString();
            V_Slider_Value.Text = "out: " + ZoomOutRatio.ToString();
        }

        private void Zoom_out_Click(object sender, RoutedEventArgs e)
        {
            ZoomOutRatio += 1;
            ZoomInRatio -= 1;
            Matrix mat = ImgScreen.RenderTransform.Value;
            Matrix matCanvas = ImgScreen_Canvas.RenderTransform.Value;
            mat.ScaleAtPrepend(1/1.1, 1/1.1, 0, 0);
            MatrixTransform mtf = new MatrixTransform(mat);
            ImgScreen.RenderTransform = mtf;
            Canvas_On_ImgScreen.RenderTransform = mtf;
            S_Slider_Value.Text = "in: " + ZoomInRatio.ToString();
            V_Slider_Value.Text = "out: " + ZoomOutRatio.ToString();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void OpenfileBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Select Image";
            openFileDialog.Filter = "Image files (*.png;*.jpeg;*jpg;*bmp)|*.png;*.jpeg;*jpg;*bmp|All files (*.*)|*.*";
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
                ImgScreen.Width = SourceImg.Width/6.5;
                ImgScreen.Height = SourceImg.Height/6.5;
                Canvas_On_ImgScreen.Width = SourceImg.Width/6.5;
                Canvas_On_ImgScreen.Height = SourceImg.Height/6.5;
                
                ImgScreen.Source = bitmap;
                centerCircleX = SourceImg.Width / 2;
                centerCircleY = SourceImg.Height / 2;
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

        private void AutoBtn_Click(object sender, RoutedEventArgs e)
        {
            AutoMode = true;
            if (AutoMode == true)
            {
                AutoScreen.Visibility = Visibility.Visible;
                ManualScreen.Visibility = Visibility.Hidden;
                WelcomeScreen.Visibility = Visibility.Hidden;
                Thread checkCamConnectionThread = new Thread(CheckCamConnection);
                checkCamConnectionThread.Start();

            }
        }

        private void ManualBtn_Click(object sender, RoutedEventArgs e)
        {
            ManualMode = true;
            if (ManualMode == true)
            {
                AutoScreen.Visibility = Visibility.Hidden;
                ManualScreen.Visibility = Visibility.Visible;
                WelcomeScreen.Visibility = Visibility.Hidden;
                Thread checkCamConnectionThread = new Thread(CheckCamConnection);
                checkCamConnectionThread.Start();
            }
        }

        private void ExitAutoScreen_Click(object sender, RoutedEventArgs e)
        {
            AutoScreen.Visibility = Visibility.Hidden;
            ManualScreen.Visibility = Visibility.Hidden;
            WelcomeScreen.Visibility = Visibility.Visible;
        }

        private void ExitManualScreen_Click(object sender, RoutedEventArgs e)
        {
            //ManualMode = false;
            AutoScreen.Visibility = Visibility.Hidden;
            ManualScreen.Visibility = Visibility.Hidden;
            WelcomeScreen.Visibility = Visibility.Visible;
        }


        System.Windows.Point DragStartPoint, DragEndPoint, ObjectStartLocation;
        object ClickedObject;
        TextBlock textBlockSize = new TextBlock();
        public object BodyCount { get; private set; }
        public object MainCanvas { get; private set; }
        double Heightsize90;
        double Widthsize90;
        private void Size90mm_Checked(object sender, RoutedEventArgs e)
        {
            circleCheck = true;
            RectangleCheck = false;
            Heightsize90 = 3180/6.5;
            Widthsize90 = 3180/6.5;
            radiusCircle = 3180;
            size100mm.IsChecked = false;
            size150mm.IsChecked = false;
            size150x300mm.IsChecked = false;
            size200x300mm.IsChecked = false;
            centerCircleX = (SourceImg.Width / 2)/ 6.5;
            centerCircleY = SourceImg.Height / 2/ 6.5;
            Ellipse ACircle = new Ellipse()
            {
                Height = Heightsize90,
                Width = Widthsize90,
                Stroke = Brushes.LightGreen,
                StrokeThickness = 3,
                Fill = Brushes.Transparent
            };

            double x = (Canvas_On_ImgScreen.ActualWidth / 2) - (ACircle.Width / 2);
            double y = (Canvas_On_ImgScreen.ActualHeight / 2) - (ACircle.Height / 2);


            Canvas.SetLeft(ACircle, x);
            Canvas.SetTop(ACircle, y);
            ACircle.Tag = "C" + (Canvas_On_ImgScreen.Children.Count - 1).ToString();

            ACircle.PreviewMouseLeftButtonDown += ACircle_PreviewMouseLeftButtonDown;

            Canvas_On_ImgScreen.Children.Clear();
            Canvas_On_ImgScreen.Children.Add(ACircle);


            cropX = x;
            cropY = y;

            cropWidth = Heightsize90;
            cropHeight = Widthsize90;

            textBlockSize = new TextBlock();
            textBlockSize.Text = "D = 90mm";
            textBlockSize.Foreground = new SolidColorBrush(Colors.LightGreen);
            textBlockSize.FontSize = 16;
            textBlockSize.FontWeight = FontWeights.UltraBold;
            Canvas.SetLeft(textBlockSize, x + 80);
            Canvas.SetTop(textBlockSize, y - 25);
            Canvas_On_ImgScreen.Children.Add(textBlockSize);

        }


        private void Size100mm_Checked(object sender, RoutedEventArgs e)
        {
            circleCheck = true;
            RectangleCheck = false;
            float Heightsize100 = 300;
            float Widthsize100 = 300;
            radiusCircle = 300;
            size90mm.IsChecked = false;
            size150mm.IsChecked = false;
            size150x300mm.IsChecked = false;
            size200x300mm.IsChecked = false;
            centerCircleX = SourceImg.Width / 2;
            centerCircleY = SourceImg.Height / 2;
            Ellipse ACircle = new Ellipse()
            {
                Height = Heightsize100,
                Width = Widthsize100,
                Stroke = Brushes.Green,
                StrokeThickness = 3,
                Fill = Brushes.Transparent
            };

            double x = (Canvas_On_ImgScreen.ActualWidth / 2) - (ACircle.Width / 2);
            double y = (Canvas_On_ImgScreen.ActualHeight / 2) - (ACircle.Height / 2);


            Canvas.SetLeft(ACircle, x);
            Canvas.SetTop(ACircle, y);
            ACircle.Tag = "C" + (Canvas_On_ImgScreen.Children.Count - 1).ToString();

            ACircle.PreviewMouseLeftButtonDown += ACircle_PreviewMouseLeftButtonDown;

            Canvas_On_ImgScreen.Children.Clear();
            Canvas_On_ImgScreen.Children.Add(ACircle);

            cropX = x;
            cropY = y;

            cropWidth = Heightsize100;
            cropHeight = Widthsize100;
            textBlockSize = new TextBlock();
            textBlockSize.Text = "D = 100mm";
            textBlockSize.Foreground = new SolidColorBrush(Colors.LightGreen);
            textBlockSize.FontSize = 16;
            textBlockSize.FontWeight = FontWeights.UltraBold;
            Canvas.SetLeft(textBlockSize, x + 80);
            Canvas.SetTop(textBlockSize, y - 25);
            Canvas_On_ImgScreen.Children.Add(textBlockSize);
        }

        private void Size150mm_Checked(object sender, RoutedEventArgs e)
        {
            circleCheck = true;
            RectangleCheck = false;
            float Heightsize150 = 430;
            float Widthsize150 = 430;
            radiusCircle = 430;
            size100mm.IsChecked = false;
            size90mm.IsChecked = false;
            size150x300mm.IsChecked = false;
            size200x300mm.IsChecked = false;
            centerCircleX = SourceImg.Width / 2;
            centerCircleY = SourceImg.Height / 2;
            Ellipse ACircle = new Ellipse()
            {
                Height = Heightsize150,
                Width = Widthsize150,
                Stroke = Brushes.Green,
                StrokeThickness = 3,
                Fill = Brushes.Transparent
            };

            double x = (Canvas_On_ImgScreen.ActualWidth / 2) - (ACircle.Width / 2);
            double y = (Canvas_On_ImgScreen.ActualHeight / 2) - (ACircle.Height / 2);


            Canvas.SetLeft(ACircle, x);
            Canvas.SetTop(ACircle, y);
            ACircle.Tag = "C" + (Canvas_On_ImgScreen.Children.Count - 1).ToString();

            ACircle.PreviewMouseLeftButtonDown += ACircle_PreviewMouseLeftButtonDown;

            Canvas_On_ImgScreen.Children.Clear();
            Canvas_On_ImgScreen.Children.Add(ACircle);

            cropX = x;
            cropY = y;

            cropWidth = Heightsize150;
            cropHeight = Widthsize150;
            textBlockSize = new TextBlock();
            textBlockSize.Text = "D = 150mm";
            textBlockSize.Foreground = new SolidColorBrush(Colors.LightGreen);
            textBlockSize.FontSize = 16;
            textBlockSize.FontWeight = FontWeights.UltraBold;
            Canvas.SetLeft(textBlockSize, x + 80);
            Canvas.SetTop(textBlockSize, y - 25);
            Canvas_On_ImgScreen.Children.Add(textBlockSize);
        }
        private void ACircle_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Ellipse c = sender as Ellipse;
            DragStartPoint.X = e.GetPosition(this).X;
            DragStartPoint.Y = e.GetPosition(this).Y;


            ObjectStartLocation.X = Canvas.GetLeft(c);
            ObjectStartLocation.Y = Canvas.GetTop(c);

            ClickedObject = c;
        }

        private void Size150x300mm_Checked(object sender, RoutedEventArgs e)
        {
            circleCheck = false;
            RectangleCheck = true;
            size100mm.IsChecked = false;
            size150mm.IsChecked = false;
            size90mm.IsChecked = false;
            size200x300mm.IsChecked = false;
            H = 150;
            W = 300;

            Rectangle ARectangle = new Rectangle() { Height = 150, Width = 300, Stroke = Brushes.Green, StrokeThickness = 2, Fill = Brushes.Transparent };

            double x = (Canvas_On_ImgScreen.ActualWidth / 2) - (ARectangle.Width / 2);
            double y = (Canvas_On_ImgScreen.ActualHeight / 2) - (ARectangle.Width / 2);

            Canvas.SetLeft(ARectangle, x);
            Canvas.SetTop(ARectangle, y);
            XminRectangle = (int)x;
            YminRectangle = (int)y;
            rectangle = new OpenCvSharp.Rect
            {
                X = XminRectangle,
                Y = YminRectangle,
                Width = (int)W,
                Height = (int)H
            };
            ARectangle.Tag = "R" + (Canvas_On_ImgScreen.Children.Count - 1).ToString();

            ARectangle.PreviewMouseLeftButtonDown += ARectangle_PreviewMouseLeftButtonDown1;
            Canvas_On_ImgScreen.Children.Clear();
            Canvas_On_ImgScreen.Children.Add(ARectangle);
            textBlockSize = new TextBlock();
            textBlockSize.Text = "150x350mm";
            textBlockSize.Foreground = new SolidColorBrush(Colors.LightGreen);
            textBlockSize.FontSize = 16;
            textBlockSize.FontWeight = FontWeights.UltraBold;
            Canvas.SetLeft(textBlockSize, x);
            Canvas.SetTop(textBlockSize, y - 25);
            Canvas_On_ImgScreen.Children.Add(textBlockSize);
        }

        private void Size200x300mm_Checked(object sender, RoutedEventArgs e)
        {
            circleCheck = false;
            RectangleCheck = true;
            size100mm.IsChecked = false;
            size150mm.IsChecked = false;
            size150x300mm.IsChecked = false;
            size90mm.IsChecked = false;
            H = 200;
            W = 350;
            
            Rectangle ARectangle = new Rectangle() { Height = 200, Width = 350, Stroke = Brushes.Green, Fill = Brushes.Transparent, StrokeThickness = 2 };

            double x = (Canvas_On_ImgScreen.ActualWidth / 2) - (ARectangle.Width / 2);
            double y = (Canvas_On_ImgScreen.ActualHeight / 2) - (ARectangle.Width / 2);

            Canvas.SetLeft(ARectangle, x);
            Canvas.SetTop(ARectangle, y);
            XminRectangle = (int)x;
            YminRectangle = (int)y;
            rectangle = new OpenCvSharp.Rect
            {
                X = XminRectangle,
                Y = YminRectangle,
                Width = (int)W,
                Height = (int)H
            };
            ARectangle.Tag = "R" + (Canvas_On_ImgScreen.Children.Count - 1).ToString();

            ARectangle.PreviewMouseLeftButtonDown += ARectangle_PreviewMouseLeftButtonDown1; ;

            Canvas_On_ImgScreen.Children.Clear();
            Canvas_On_ImgScreen.Children.Add(ARectangle);
            textBlockSize = new TextBlock();
            textBlockSize.Text = "D = 200x350mm";
            textBlockSize.Foreground = new SolidColorBrush(Colors.LightGreen);
            textBlockSize.FontSize = 16;
            textBlockSize.FontWeight = FontWeights.UltraBold;
            Canvas.SetLeft(textBlockSize, x);
            Canvas.SetTop(textBlockSize, y - 25);
            Canvas_On_ImgScreen.Children.Add(textBlockSize);
        }


        private void ARectangle_PreviewMouseLeftButtonDown1(object sender, MouseButtonEventArgs e)
        {
            Total_Count_Value.Text = DragStartPoint.X.ToString();
            Rectangle r = sender as Rectangle;
            DragStartPoint.X = e.GetPosition(this).X;

            DragStartPoint.Y = e.GetPosition(this).Y;
            //XminRectangle = (int)DragStartPoint.X;
            //YminRectangle = (int)DragStartPoint.Y;
            //Console.WriteLine(DragStartPoint.X);

            ObjectStartLocation.X = Canvas.GetLeft(r);
            ObjectStartLocation.Y = Canvas.GetTop(r);

            ClickedObject = r;
        }


        private void Canvas_On_ImgScreen_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            //Camera_Check_TBlock.Text = "X: " + e.GetPosition(this).X.ToString() + "," + "Y: " + e.GetPosition(this).Y.ToString();
            if (ClickedObject == null)
                return;

            DragEndPoint.X = e.GetPosition(this).X;
            DragEndPoint.Y = e.GetPosition(this).Y;

            double deltaX = DragEndPoint.X - DragStartPoint.X;
            double deltaY = DragEndPoint.Y - DragStartPoint.Y;


            if (ClickedObject is Rectangle)
            {
                Rectangle r = ClickedObject as Rectangle;

                Canvas.SetLeft(r, ObjectStartLocation.X + deltaX);
                Canvas.SetTop(r, ObjectStartLocation.Y + deltaY);
                Canvas.SetLeft(textBlockSize, ObjectStartLocation.X + deltaX);
                Canvas.SetTop(textBlockSize, ObjectStartLocation.Y + deltaY -25);

            }
            else if (ClickedObject is Ellipse)
            {
                //EllipseGeometry c = ClickedObject as EllipseGeometry;
                Ellipse c = ClickedObject as Ellipse;

                Canvas.SetLeft(c, ObjectStartLocation.X + deltaX);
                Canvas.SetTop(c, ObjectStartLocation.Y + deltaY);
                Canvas.SetLeft(textBlockSize, ObjectStartLocation.X + deltaX + 80);
                Canvas.SetTop(textBlockSize, ObjectStartLocation.Y + deltaY - 25);
                cropX = ObjectStartLocation.X + deltaX;
                cropY = ObjectStartLocation.Y + deltaY;

                cropWidth = c.Width;
                cropHeight = c.Height;
                //centerCircleX = (int)(ObjectStartLocation.X + deltaX + radiusCircle);
                //centerCircleY = (int)(ObjectStartLocation.Y + deltaY + radiusCircle);
            }
            else
                return;
        }



        private void Canvas_On_ImgScreen_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            dragClickdown.X = e.GetPosition(ImgScreen).X;
            dragClickdown.Y = e.GetPosition(ImgScreen).Y;
            //Total_Count_Value.Text = dragClickdown.X.ToString();

        }
        OpenCvSharp.Rect rectangle = new OpenCvSharp.Rect();

        private void Canvas_On_ImgScreen_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            dragClickup.X = e.GetPosition(ImgScreen).X;
            dragClickup.Y = e.GetPosition(ImgScreen).Y;
            if (circleCheck == true)
            {

                centerCircleX = (int)(centerCircleX + (dragClickup.X - dragClickdown.X));
                centerCircleY = (int)(centerCircleY + (dragClickup.Y - dragClickdown.Y));
                //Total_Count_Value.Text = dragClickup.X.ToString() + "  " + dragClickup.Y.ToString();
                //H_Slider_Value.Text = dragClickup.X.ToString() + " - " + dragClickdown.X.ToString() + " = " + (dragClickup.X - dragClickdown.X).ToString();
            }
            XminRectangle = XminRectangle + (int)(dragClickup.X - dragClickdown.X);
            YminRectangle = YminRectangle + (int)(dragClickup.Y - dragClickdown.Y);
            ClickedObject = null;
            if (RectangleCheck == true)
            {
                rectangle = new OpenCvSharp.Rect
                {
                    X = XminRectangle,
                    Y = YminRectangle,
                    Width = (int)W,
                    Height = (int)H
                };
                //H_Slider_Value.Text = XminRectangle.ToString();
                //S_Slider_Value.Text = YminRectangle.ToString();
                //V_Slider_Value.Text = W.ToString() + " " + H.ToString();
            }
        }
        private void Canvas_On_ImgScreen_MouseMove(object sender, MouseEventArgs e)
        {
            teets.Text = "X: " + e.GetPosition(ImgScreen).X.ToString() + "," + "Y: " + e.GetPosition(ImgScreen).Y.ToString();
        }

        private void ImgScreen_MouseMove(object sender, MouseEventArgs e)
        {
           teets.Text = "X: " + e.GetPosition(ImgScreen).X.ToString() + "," + "Y: " + e.GetPosition(ImgScreen).Y.ToString();
        }

        private void ImgScreen_MouseUp(object sender, MouseButtonEventArgs e)
        {

        }


        private void ImgScreen_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }


        private async void SendImageCommand()
        {
            var byteImage = Converter.ImageSourceToBytes(ImgScreen.Source);

            var matImage = await socketHandler.SendImgCmd(byteImage);

            var bitmapImage = Converter.MatToBitmapImage(matImage);
            ImgScreen.Source = bitmapImage;
        }
//--------------------------------------------------------------------------------------------------------------------------------------
        private void SendCropImgBtn_Click(object sender, RoutedEventArgs e)
        {
            SendImageCommand();
            //ImgScreen.Source = Converter.MatToBitmapImage(matImage);

            //  canvas_on_imgscreen.children.add(converted);
        }

        ///******************************************** COUNT *****************************************************************
        //////
        private async void CountBtn_Click(object sender, RoutedEventArgs e)
        {
            var countData = "10"; // "10"

            Total_Count_Value.Text = countData;

            Console.WriteLine(countData);
        }

///***********************************************************************************************************
///

        private void ConfirmBtn_Click(object sender, RoutedEventArgs e)
        {
            ImgAfterAddMask = new Mat();
            if (circleCheck == true)
            {
                if (ZoomInRatio > 0)
                {
                    Mat blackMask = new Mat(SourceImg.Height, SourceImg.Width, MatType.CV_8UC3, Scalar.Black);
                    Cv2.Circle(blackMask, (int)(centerCircleX * Math.Pow(1.1, ZoomInRatio)), (int)(centerCircleY * Math.Pow(1.1, ZoomInRatio)), (int)radiusCircle / 2, Scalar.White, -1);
                    Cv2.BitwiseAnd(SourceImg, blackMask, ImgAfterAddMask);
                    var converted = Convert(BitmapConverter.ToBitmap(ImgAfterAddMask));
                    ImgScreen.Source = converted;
                }
                if (ZoomOutRatio > 0)
                {
                    Mat blackMask = new Mat(SourceImg.Height, SourceImg.Width, MatType.CV_8UC3, Scalar.Black);
                    Cv2.Circle(blackMask, (int)(centerCircleX * Math.Pow(1 / 1.1, ZoomOutRatio)), (int)(centerCircleY * Math.Pow(1 / 1.1, ZoomOutRatio)), (int)radiusCircle / 2, Scalar.White, -1);
                    Cv2.BitwiseAnd(SourceImg, blackMask, ImgAfterAddMask);
                    var converted = Convert(BitmapConverter.ToBitmap(ImgAfterAddMask));
                    ImgScreen.Source = converted;
                }
                if (ZoomInRatio == 0)
                {
                    Mat blackMask = new Mat(SourceImg.Height, SourceImg.Width, MatType.CV_8UC3, Scalar.Black);
                    Cv2.Circle(blackMask, (int)(centerCircleX*6.5), (int)(centerCircleY*6.5), (int)radiusCircle / 2, Scalar.White, -1);
                    //Cv2.FillPoly()
                    Cv2.BitwiseAnd(SourceImg, blackMask, ImgAfterAddMask);
                    var converted = Convert(BitmapConverter.ToBitmap(ImgAfterAddMask));
                    ImgScreen.Source = converted;
                    

                    var currentWorkingDirectory = "C://Users//admin//Desktop//do_an_scan//GIAO_DIEN_UPDATE_FIX//GIAO_DIEN//WorkingImg" + "\\" + this.generateFilename() + "_Imgcropped";
                    Console.WriteLine(currentWorkingDirectory);
                    ImgAfterAddMask.SaveImage(currentWorkingDirectory + ".jpg");
                }
            }
            if (RectangleCheck == true)
            {
                Mat blackMask = new Mat(SourceImg.Height, SourceImg.Width, MatType.CV_8UC3, Scalar.Black);
                Cv2.Rectangle(blackMask, rectangle, Scalar.White, -1);
                Cv2.BitwiseAnd(SourceImg, blackMask, ImgAfterAddMask);
                var converted = Convert(BitmapConverter.ToBitmap(ImgAfterAddMask));
                ImgScreen.Source = converted;
            }
        }

        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            Canvas_On_ImgScreen.Children.Clear();
        }
//--------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// HSV CONTROL MANUAL, GRAYSCALE, THRESHOLD _______________________________________________________________
        bool ColorFilterEnable = false;
        bool GrayScaleEnable = false;
        bool ThreshEnable = false;
        bool DistanceEnable = false;
        Mat GrayScaleImg;
        Mat HSVImg;
        Mat ThreshImg;
        double threshsold = 100;
        System.Drawing.Bitmap ThreshBitmap;

        private void ColorFilter_Enable_Checked(object sender, RoutedEventArgs e)
        {
            ColorFilterEnable = true;
            HSVImg = new Mat();
            if (ColorFilterEnable == true)
            {
                H_Slider.IsEnabled = true;
                S_Slider.IsEnabled = true;
                V_Slider.IsEnabled = true;
                Cv2.CvtColor(SourceImg, HSVImg, ColorConversionCodes.BGR2HSV);
                // Mat H = new Mat (SourceImg.Height, SourceImg.Width, )
                //HSVImg(SourceImg.Height, SourceImg.Width) = new Mat(SourceImg.Height, SourceImg.Width, MatType.CV_8UC1, H_Slider.Value);
                System.Drawing.Bitmap HSV = MatToBitmap(HSVImg);
                var converted = Convert(BitmapConverter.ToBitmap(HSVImg));
                ImgScreen.Source = converted;
            } 
        }

        private void ColorFilter_Enable_Unchecked(object sender, RoutedEventArgs e)
        {
            ColorFilterEnable = false;
            if (ColorFilterEnable == false)
            {
                H_Slider.IsEnabled = false;
                S_Slider.IsEnabled = false;
                V_Slider.IsEnabled = false;
            }
        }


        private void GrayScale_CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            GrayScaleEnable = false;
            if (GrayScaleEnable == false)
            {
                GrayScaleImg = SourceImg;
                //Cv2.CvtColor(SourceImg, GrayScaleImg, ColorConversionCodes.BGR2GRAY);
                var converted = Convert(BitmapConverter.ToBitmap(GrayScaleImg));
                ImgScreen.Source = converted;
            }
        }

        private void GrayScale_CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            GrayScaleEnable = true;
            if (GrayScaleEnable == true)
            {
                GrayScaleImg = new Mat();
                Cv2.CvtColor(SourceImg, GrayScaleImg, ColorConversionCodes.BGR2GRAY);
                var converted = Convert(BitmapConverter.ToBitmap(GrayScaleImg));
                ImgScreen.Source = converted;
            }
        }
       private void Ena_Threshold_Checked(object sender, RoutedEventArgs e)
        {
            ThreshEnable = true;
            if (ThreshEnable == true)
            {
                MatThreshold = new Mat();

                Thresh_Slider.IsEnabled = true;
                Cv2.CvtColor(ImgAfterAddMask, MatThreshold, ColorConversionCodes.BGR2GRAY);
                Cv2.Threshold(MatThreshold, MatThreshold, 10, 255, ThresholdTypes.BinaryInv);
                var threshImg = Convert(BitmapConverter.ToBitmap(MatThreshold));
                ImgScreen.Source = threshImg;
            }
        }

        private void Ena_Threshold_Unchecked(object sender, RoutedEventArgs e)
        {
            ThreshEnable = false;
            if (ThreshEnable == false)
            {
                Thresh_Slider.IsEnabled = false;
                var converted = Convert(BitmapConverter.ToBitmap(ImgAfterAddMask));
                ImgScreen.Source = converted;
            }
        }

        private void Ena_Distance_Checked(object sender, RoutedEventArgs e)
        {

            DistanceEnable = true;
            if (DistanceEnable == true)
            {
                SendImageCommand();
            }
        }

        private void Ena_Distance_Unchecked(object sender, RoutedEventArgs e)
        {
            DistanceEnable = true;
            if (DistanceEnable == true)
            {
                MatThreshold = new Mat();

                Sens_Slider.IsEnabled = true;
                Cv2.CvtColor(ImgAfterAddMask, MatThreshold, ColorConversionCodes.BGR2GRAY);
                Cv2.Threshold(MatThreshold, MatThreshold, 10, 255, ThresholdTypes.BinaryInv);
                var threshImg = Convert(BitmapConverter.ToBitmap(MatThreshold));
                ImgScreen.Source = threshImg;
            }
        }


        //-------------------------------------------------------------------------------------------------------------------------------------- 
        private void PrintBackStack()
        {
            foreach (var data in backStack)
            {
                Console.WriteLine(data);
            }
            Console.WriteLine("================");
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (backStack.Count() != 0)
            {

                nextStack.Push(backStack.Peek());
                Console.WriteLine(">>>>>>>>>>>>>>>>");
                foreach (var item in nextStack)
                    Console.WriteLine(item);

                backStack.Pop();

                if (backStack.Count() != 0)
                {

                    BACKDATA dataPo = backStack.Peek();

                    switch (dataPo.labels)
                    {
                        case "thresh":
                            Console.WriteLine("...............");
                            Thresh_Slider.Value = dataPo.values;
                            //foreach (var item in mystackValues)
                            //    Console.WriteLine(item);
                            //foreach (var item in mystackLabels)
                            //    Console.WriteLine(item);
                            break;
                        case "local":
                            Console.WriteLine("...............");
                            Sens_Slider.Value = dataPo.values;
                            break;
                        case "h":
                            H_Slider.Value = dataPo.values;
                            break;
                        case "s":
                            S_Slider.Value = dataPo.values;
                            break;
                        case "v":
                            V_Slider.Value = dataPo.values;
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    MessageBox.Show("stack empty");
                    backStack.Clear();
                    tamThresh = true;
                    tamH = true;
                    tamLocal = true;
                    tamS = true;
                    tamV = true;
                }


            }

            PrintBackStack();
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            if (nextStack.Count() != 0)
            {
                BACKDATA dataPre = nextStack.Peek();

                BACKDATA popData;

                if (nextStack.Count() != 0)
                {

                    switch (dataPre.labels)
                    {
                        case "thresh":
                            Console.WriteLine("value cur peek");
                            Thresh_Slider.Value = dataPre.values;
                            popData = nextStack.Pop();  
                            curValue = popData.values;
                            curLabel = popData.labels;
                            backStack.Push(popData);

                            break;
                        case "local":
                            Console.WriteLine("value cur peek");
                            Sens_Slider.Value = dataPre.values;
                            popData = nextStack.Pop();  
                            curValue = popData.values;
                            curLabel = popData.labels;
                            backStack.Push(popData);
                            break;
                        case "h":
                            Console.WriteLine("value cur peek");
                            H_Slider.Value = dataPre.values;
                            popData = nextStack.Pop();  
                            curValue = popData.values;
                            curLabel = popData.labels;
                            backStack.Push(popData);
                            break;
                        case "s":
                            Console.WriteLine("value cur peek");
                            S_Slider.Value = dataPre.values;
                            popData = nextStack.Pop();  
                            curValue = popData.values;
                            curLabel = popData.labels;
                            backStack.Push(popData);
                            break;
                        case "v":
                            Console.WriteLine("value cur peek");
                            V_Slider.Value = dataPre.values;
                            popData = nextStack.Pop();  
                            curValue = popData.values;
                            curLabel = popData.labels;
                            backStack.Push(popData);
                            break;
                        default:
                            break;

                    }
                }
            }

        }

        private void Thresh_Slider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {

            if (tamThresh == true)
            {
                backStack.Push(new BACKDATA("thresh", 0));
                tamThresh = false;
            }
            backStack.Push(new BACKDATA("thresh", Thresh_Slider.Value));


            PrintBackStack();

        }

        private void OK_Thresh_Click(object sender, RoutedEventArgs e)
        {

            BACKDATA topData;

            if (backStack.Count() == 0)
            {
                topData = new BACKDATA("thresh", -1);
            }
            else
            {
                topData = backStack.Peek();
            }


            double TextBoxThreshValue = double.Parse(Thresh_Value.Text);
            TextBoxThreshValue = Math.Round(TextBoxThreshValue);

            double SliderThreshValue = Math.Round(Thresh_Slider.Value);

            if (topData.labels == "thresh" && topData.values == Thresh_Slider.Value && SliderThreshValue == TextBoxThreshValue)
            {
                return;
            }

            if (tamThresh == true)
            {
                backStack.Push(new BACKDATA("thresh", 0));
                tamThresh = false;
            }

            backStack.Push(new BACKDATA("thresh", Thresh_Slider.Value));

            PrintBackStack();


        }


        private void Sens_Slider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (tamLocal == true)
            {
                backStack.Push(new BACKDATA("local", 0));
                tamLocal = false;
            }
            backStack.Push(new BACKDATA("local", Sens_Slider.Value));


            PrintBackStack();

        }

        private void OK_Sens_Click(object sender, RoutedEventArgs e)
        {

            BACKDATA topData;

            if (backStack.Count() == 0)
            {
                topData = new BACKDATA("local", -1);
            }
            else
            {
                topData = backStack.Peek();
            }


            double TextBoxLocalValue = double.Parse(Sens_Value.Text);
            TextBoxLocalValue = Math.Round(TextBoxLocalValue);

            double SliderLocalValue = Math.Round(Sens_Slider.Value);

            if (topData.labels == "local" && topData.values == Sens_Slider.Value && SliderLocalValue == TextBoxLocalValue)
            {
                return;
            }


            if (tamLocal == true)
            {
                backStack.Push(new BACKDATA("local", 0));
                tamLocal = false;
            }
            backStack.Push(new BACKDATA("local", Sens_Slider.Value));

            PrintBackStack();
        }


        private void OK_H_Click(object sender, RoutedEventArgs e)
        {

            BACKDATA topData;

            if (backStack.Count() == 0)
            {
                topData = new BACKDATA("h", -1);
            }
            else
            {
                topData = backStack.Peek();
            }


            double TextBoxHValue = double.Parse(H_Slider_Value.Text);
            TextBoxHValue = Math.Round(TextBoxHValue);

            double SliderHValue = Math.Round(H_Slider.Value);

            if (topData.labels == "h" && topData.values == H_Slider.Value && SliderHValue == TextBoxHValue)
            {
                return;
            }


            if (tamH == true)
            {
                backStack.Push(new BACKDATA("h", 0));
                tamH = false;
            }
            backStack.Push(new BACKDATA("h", H_Slider.Value));

            PrintBackStack();

        }

        private void H_Slider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {

            if (tamH == true)
            {
                backStack.Push(new BACKDATA("h", 0));
                tamH = false;
            }
            backStack.Push(new BACKDATA("h", H_Slider.Value));


            PrintBackStack();

        }

        private void OK_S_Click(object sender, RoutedEventArgs e)
        {

            BACKDATA topData;

            if (backStack.Count() == 0)
            {
                topData = new BACKDATA("s", -1);
            }
            else
            {
                topData = backStack.Peek();
            }


            double TextBoxSValue = double.Parse(S_Slider_Value.Text);
            TextBoxSValue = Math.Round(TextBoxSValue);

            double SliderSValue = Math.Round(S_Slider.Value);

            if (topData.labels == "s" && topData.values == S_Slider.Value && SliderSValue == TextBoxSValue)
            {
                return;
            }


            if (tamS == true)
            {
                backStack.Push(new BACKDATA("s", 0));
                tamS = false;
            }
            backStack.Push(new BACKDATA("s", S_Slider.Value));

            PrintBackStack();

        }

        private void S_Slider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {

            if (tamS == true)
            {
                backStack.Push(new BACKDATA("s", 0));
                tamS = false;
            }
            backStack.Push(new BACKDATA("s", S_Slider.Value));


            PrintBackStack();


        }

        private void OK_V_Click(object sender, RoutedEventArgs e)
        {

            BACKDATA topData;

            if (backStack.Count() == 0)
            {
                topData = new BACKDATA("v", -1);
            }
            else
            {
                topData = backStack.Peek();
            }


            double TextBoxVValue = double.Parse(V_Slider_Value.Text);
            TextBoxVValue = Math.Round(TextBoxVValue);

            double SliderVValue = Math.Round(V_Slider.Value);

            if (topData.labels == "v" && topData.values == V_Slider.Value && SliderVValue == TextBoxVValue)
            {
                return;
            }


            if (tamV == true)
            {
                backStack.Push(new BACKDATA("v", 0));
                tamV = false;
            }
            backStack.Push(new BACKDATA("v", V_Slider.Value));

            PrintBackStack();

        }

        private void V_Slider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {


            if (tamV == true)
            {
                backStack.Push(new BACKDATA("v", 0));
                tamV = false;
            }
            backStack.Push(new BACKDATA("v", V_Slider.Value));


            PrintBackStack();

        }

        //-------------------------------------------------------------------------------------------------------------------------------------- 


        
        private ImageSource backupSource = null;

        private void Thresh_Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            MatThreshold = new Mat();

            Thresh_Value.Text = Thresh_Slider.Value.ToString();
            Cv2.CvtColor(ImgAfterAddMask, MatThreshold, ColorConversionCodes.BGR2GRAY);
            Cv2.Threshold(MatThreshold, MatThreshold, Thresh_Slider.Value, 255, ThresholdTypes.BinaryInv);
            var threshImg = Convert(BitmapConverter.ToBitmap(MatThreshold));
            ImgScreen.Source = threshImg;


            //if (backupSource == null)
            //{
            //    backupSource = ImgScreen.Source.Clone();
            //}

            //var threshSlideValueString = Thresh_Slider.Value.ToString();

            //double TextBoxThreshValue = double.Parse(threshSlideValueString);
            //TextBoxThreshValue = Math.Round(TextBoxThreshValue);

            //Console.WriteLine(threshSlideValueString);

            //try
            //{
            //    var taskMatImage = socketHandler.SendValueThreshSliderCmd(backupSource, TextBoxThreshValue.ToString());

            //    await taskMatImage.ContinueWith((matImageTask) =>
            //    {
            //        var matImage = matImageTask.Result;

            //        Console.WriteLine("continue with result");

            //        if (matImage == null)
            //        {
            //            return;
            //        }

            //        var bitmapImage = Converter.MatToBitmapImage(matImage);


            //        this.Dispatcher.Invoke(() => ImgScreen.Source = bitmapImage);
            //    });

            //    taskMatImage.Start();

            //}
            //catch
            //{

            //}


        }   

        private void Sens_Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Sens_Value.Text = Sens_Slider.Value.ToString();
        }

        private void H_Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            H_Slider_Value.Text = H_Slider.Value.ToString();
        }

        private void S_Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            S_Slider_Value.Text = S_Slider.Value.ToString(); ;
        }

        private void V_Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            V_Slider_Value.Text = V_Slider.Value.ToString();
        }
//-------------------------------------------------------------------------------------------------------------------------------------- 




        private void Thresh_Value_TextChanged(object sender, TextChangedEventArgs e)
        {
            double TextBoxThreshValue = double.Parse(Thresh_Value.Text);
            TextBoxThreshValue = Math.Round(TextBoxThreshValue);

            Thresh_Slider.Value = TextBoxThreshValue;
        }

        private void Sens_Value_TextChanged(object sender, TextChangedEventArgs e)
        {
            double TextBoxLocalValue = double.Parse(Sens_Value.Text);
            TextBoxLocalValue = Math.Round(TextBoxLocalValue);

            Sens_Slider.Value = TextBoxLocalValue;
        }

        private void H_Slider_Value_TextChanged(object sender, TextChangedEventArgs e)
        {
            double TextBoxHValue = double.Parse(H_Slider_Value.Text);
            TextBoxHValue = Math.Round(TextBoxHValue);

            H_Slider.Value = TextBoxHValue;
        }

        private void S_Slider_Value_TextChanged(object sender, TextChangedEventArgs e)
        {
            double TextBoxSValue = double.Parse(S_Slider_Value.Text);
            TextBoxSValue = Math.Round(TextBoxSValue);

            S_Slider.Value = TextBoxSValue;
        }

        private void V_Slider_Value_TextChanged(object sender, TextChangedEventArgs e)
        {

            double TextBoxVValue = double.Parse(V_Slider_Value.Text);
            TextBoxVValue = Math.Round(TextBoxVValue);

            V_Slider.Value = TextBoxVValue;
        }



        /// <summary>
        /// CAMERA CONECTION /////////////////////////////////////////////////////////////////////////////////////////////
        /// /////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        Thread Runcam;
        bool CameraIsEnabled = false;
        private void Camera__Click(object sender, RoutedEventArgs e)
        {
            if (CameraIsEnabled == true)
            {
                Runcam = new Thread(RunCamera);
                Runcam.Start();
            }
        }
        void RunCamera()
        {
            try
            {
                using (Camera camera = new Camera())
                {
                    // Print the model name of the camera.
                    Console.WriteLine("Using camera {0}.", camera.CameraInfo[CameraInfoKey.ModelName]);
                    camera.CameraOpened += Configuration.AcquireContinuous;
                    camera.Open();
                    camera.Parameters[PLCameraInstance.MaxNumBuffer].SetValue(5);
                    camera.StreamGrabber.Start();

                    while (CameraIsEnabled == true)
                    {
                        IGrabResult grabResult = camera.StreamGrabber.RetrieveResult(5000, TimeoutHandling.ThrowException);
                        using (grabResult)
                        {
                            // Image grabbed successfully?
                            if (grabResult.GrabSucceeded)
                            {
                                //ImageWindow.DisplayImage(0, grabResult);
                                this.Dispatcher.Invoke((Action)(() =>
                                {//this refer to form in WPF application 
                                    Mat rtnMat = convertToMat(grabResult);
                                    Mat outp = new Mat();
                                    var faceSize = new OpenCvSharp.Size(rtnMat.Width / 6.5, rtnMat.Height / 6.5);
                                    Cv2.Resize(rtnMat, outp, faceSize);
                                    var converted = Convert(BitmapConverter.ToBitmap(rtnMat));
                                    ImgScreen.Source = converted;
                                }));

                                //Cv2.ImShow("test", rtnMat);
                            }
                            else
                            {
                                Console.WriteLine("Error: {0} {1}", grabResult.ErrorCode, grabResult.ErrorDescription);
                            }
                        }
                    }
                    camera.StreamGrabber.Stop();
                    camera.Close();
                }
            }
            catch
            { }

        }

        void CheckCamConnection()
        {
            bool isExceptionThrowed = false;
            while (!isExceptionThrowed)
            {
                    this.Dispatcher.Invoke((Action)(() =>
                {
                    try
                    {
                        using (Camera camera = new Camera())
                        {

                            Camera_Check_TBlock.Text = $"Using camera {camera.CameraInfo[CameraInfoKey.ModelName]}.";
                            CameraIsEnabled = true;
                        }
                    }
                    catch
                    {
                        Camera_Check_TBlock.Text = "No camera device is connected.";
                        CameraIsEnabled = false;

                        isExceptionThrowed = true;
                    }
                    finally
                    {
                        
                        if (Runcam != null && Runcam.ThreadState == ThreadState.Running)
                        {
                            Runcam.Abort();
                        }
                    }
                }));
                Thread.Sleep(1000);
            }

        }
        public BitmapImage Convert(System.Drawing.Bitmap src)
        {
            MemoryStream ms = new MemoryStream();
            ((System.Drawing.Bitmap)src).Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            image.StreamSource = ms;
            image.EndInit();
            return image;
        }


        PixelDataConverter converter = new PixelDataConverter();
        private Mat convertToMat(IGrabResult rtnGrabResult)
        {
            PixelDataConverter converter = new PixelDataConverter();

            IImage image = rtnGrabResult;
            converter.OutputPixelFormat = PixelType.BGR8packed;
            byte[] buffer = new byte[converter.GetBufferSizeForConversion(rtnGrabResult)];
            converter.Convert(buffer, rtnGrabResult);
            return new Mat(rtnGrabResult.Height, rtnGrabResult.Width, MatType.CV_8UC3, buffer);
        }
        /*[System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);
        public static System.Windows.Media.Imaging.BitmapSource Convert(System.Drawing.Bitmap source)
        {
            var hBitmap = source.GetHbitmap();
            var result = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, System.Windows.Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            DeleteObject(hBitmap);
            return result;

        }*/
        /*public static BitmapSource Convert(System.Drawing.Bitmap bitmap)
        {
            var bitmapData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);

            var bitmapSource = BitmapSource.Create(
                bitmapData.Height, bitmapData.Width,
                bitmap.HorizontalResolution, bitmap.VerticalResolution,
                PixelFormats.Bgr24, null,
                bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

            bitmap.UnlockBits(bitmapData);

            return bitmapSource;
        }*/
        /*[System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);
        public static System.Windows.Media.Imaging.BitmapSource Convert( System.Drawing.Bitmap source)
        {
            var hBitmap = source.GetHbitmap();
            var result = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, System.Windows.Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

            DeleteObject(hBitmap);

            return result;
        }*/
        /*public ImageSource Convert(System.Drawing.Bitmap yourBitmap)
        {
            ImageSourceConverter c = new ImageSourceConverter();
            return (ImageSource)c.ConvertFrom(yourBitmap);

        }*/ 
        public static System.Drawing.Bitmap MatToBitmap(Mat image)
        {
            return OpenCvSharp.Extensions.BitmapConverter.ToBitmap(image);
        } 
    }
}
