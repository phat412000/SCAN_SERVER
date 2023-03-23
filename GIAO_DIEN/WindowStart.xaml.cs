﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Windows.Threading;
using Window = System.Windows.Window;
using System.Threading;
using Basler.Pylon;
using Pythonzxrr;
using OpenCvSharp.WpfExtensions;
using Microsoft.Win32;
using System.Threading.Channels;
using Rectangle = System.Windows.Shapes.Rectangle;
using OpenCvPoint = OpenCvSharp.Point;
using GIAO_DIEN.backActionChild;

namespace GIAO_DIEN
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class WindowStart : Window
    {
        int ButtonFile_Click_Mode = 0;
        string SelectImgPath;
        Mat SourceImg;
        Mat ImgAfterAddMask;
        int ZoomInRatio;
        int ZoomOutRatio;
        bool AutoMode = false;
        bool ManualMode = false;

        //Stack<double> mystackValues = new Stack<double>();
        //Stack<string> mystackLabels = new Stack<string>();
        //Stack<double> mystackCurValues = new Stack<double>();
        //Stack<string> mystackCurLabels = new Stack<string>();

        Stack<BackAction> backStack = new Stack<BackAction>();
        Stack<BackAction> nextStack = new Stack<BackAction>();

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
        bool tempThresh = true;
        bool tempDistance = true;
        bool tamH = true;
        bool tamS = true;
        bool tamV = true;
        bool tempConfirm = true;
        bool tempSend = true;
        bool DistanceEnable = false;
        bool ThreshEnable = false;

        bool getPosByClickEnable = false;
        bool SegmentActivated = false;


        private DispatcherTimer Timer1;
        private int time = 0;
        PythonInterface pythonInterface;
        string currentImagePath;
        string SourceImgSegment;

        public WindowStart()
        {
            InitializeComponent();
            ButtonFile_canvas.Visibility = Visibility.Hidden;
            AutoScreen.Visibility = Visibility.Hidden;
            ManualScreen.Visibility = Visibility.Hidden;
            WelcomeScreen.Visibility = Visibility.Visible;

            pythonInterface = new PythonInterface();
            pythonInterface.Connect();

            Task.Run(SliderValueConsumerAsync);
        }


        private string _datetime;
        private string _barcode;
        public WindowStart(string datetime, string barcode) : this()
        {

            _datetime = datetime;
            DateTime_Textbox.Text = _datetime;
            _barcode = barcode;
            BarcodeID.Text = _barcode;

            if (DistanceEnable == false)
            {
                Sens_Slider.IsEnabled = false;
            }
            if (ThreshEnable == false)
            {
                Thresh_Slider.IsEnabled = false;
            }

        }

        private String generateFilename()
        {

            Random rand = new Random();

            int stringlen = rand.Next(4, 10);
            int randValue;
            string str = "";
            char letter;
            for (int i = 0; i < stringlen; i++)
            {

                randValue = rand.Next(0, 26);

                letter = System.Convert.ToChar(randValue + 65);

                str = str + letter;

            }
            return str;
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
                Foreground_color.Color = System.Windows.Media.Color.FromRgb(3, 102, 169);
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
            System.Windows.Media.Matrix mat = ImgScreen.RenderTransform.Value;
            System.Windows.Media.Matrix matCanvas = ImgScreen_Canvas.RenderTransform.Value;
            mat.ScaleAtPrepend(1.1, 1.1, 0, 0);
            MatrixTransform mtf = new MatrixTransform(mat);
            ImgScreen.RenderTransform = mtf;
            Canvas_On_ImgScreen.RenderTransform = mtf;

        }

        private void Zoom_out_Click(object sender, RoutedEventArgs e)
        {

            ZoomOutRatio += 1;
            ZoomInRatio -= 1;
            System.Windows.Media.Matrix mat = ImgScreen.RenderTransform.Value;
            System.Windows.Media.Matrix matCanvas = ImgScreen_Canvas.RenderTransform.Value;
            mat.ScaleAtPrepend(1 / 1.1, 1 / 1.1, 0, 0);
            MatrixTransform mtf = new MatrixTransform(mat);
            ImgScreen.RenderTransform = mtf;
            Canvas_On_ImgScreen.RenderTransform = mtf;

        }

        private void Fit_to_screen_btn_click(object sender, RoutedEventArgs e)
        {

            if (ZoomInRatio > 0)
            {

                System.Windows.Media.Matrix mat = ImgScreen.RenderTransform.Value;
                System.Windows.Media.Matrix matCanvas = ImgScreen_Canvas.RenderTransform.Value;
                mat.ScaleAtPrepend(Math.Pow(1 / 1.1, ZoomInRatio), Math.Pow(1 / 1.1, ZoomInRatio), 0, 0);
                MatrixTransform mtf = new MatrixTransform(mat);
                ImgScreen.RenderTransform = mtf;
                Canvas_On_ImgScreen.RenderTransform = mtf;
                ZoomInRatio = 0;
                ZoomOutRatio = 0;
            }

            if (ZoomOutRatio > 0)
            {

                System.Windows.Media.Matrix mat = ImgScreen.RenderTransform.Value;
                System.Windows.Media.Matrix matCanvas = ImgScreen_Canvas.RenderTransform.Value;
                mat.ScaleAtPrepend(Math.Pow(1.1, ZoomOutRatio), Math.Pow(1.1, ZoomOutRatio), 0, 0);
                MatrixTransform mtf = new MatrixTransform(mat);
                ImgScreen.RenderTransform = mtf;
                Canvas_On_ImgScreen.RenderTransform = mtf;
                ZoomOutRatio = 0;
                ZoomInRatio = 0;

            }

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }


        private void OpenFile()
        {

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Select Image";
            openFileDialog.Filter = "Image files (*.png;*.jpeg;*jpg;*bmp)|*.png;*.jpeg;*jpg;*bmp|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;

            if (openFileDialog.ShowDialog() == false)
            { MessageBox.Show("fail to show"); }

            if (openFileDialog.FileName != "")
            {

                SelectImgPath = openFileDialog.FileName;

                SourceImg = Cv2.ImRead(SelectImgPath);
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(SelectImgPath);
                bitmap.EndInit();

                ImgScreen.Width = SourceImg.Width / 6.5;
                ImgScreen.Height = SourceImg.Height / 6.5;
                Canvas_On_ImgScreen.Width = SourceImg.Width / 6.5;
                Canvas_On_ImgScreen.Height = SourceImg.Height / 6.5;

                ImgScreen.Source = bitmap;

            }

            ButtonFile_canvas.Visibility = Visibility.Hidden;
            ButtonFile_Click_Mode = 0;
            FileButton.Background = null;
            SolidColorBrush Foreground_color = new SolidColorBrush();
            Foreground_color.Color = Colors.White;
            FileButton.Foreground = Foreground_color;

            currentImagePath = SelectImgPath;

        }

        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFile();

        }

        private void MenuFileOpenBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFile();
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
                PetriSize_Textbox.Text = String.Empty;
                TypeOfPetri_Textbox.Text = String.Empty;

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
            if (circleCheck == true)

            {
                size90mm.IsChecked = true;
                RectangleCheck = false;
                Heightsize90 = 3180 / 6.5;
                Widthsize90 = 3180 / 6.5;
                radiusCircle = 3180;
                size100mm.IsChecked = false;
                size150mm.IsChecked = false;
                size150x300mm.IsChecked = false;
                size200x300mm.IsChecked = false;
                centerCircleX = (SourceImg.Width / 2) / 6.5;
                centerCircleY = SourceImg.Height / 2 / 6.5;
                Ellipse ACircle = new Ellipse()
                {

                    Height = Heightsize90,
                    Width = Widthsize90,
                    Stroke = System.Windows.Media.Brushes.LightGreen,
                    StrokeThickness = 3,
                    Fill = System.Windows.Media.Brushes.Transparent

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

                Console.WriteLine(Canvas_On_ImgScreen.Width);

            }
        }

        private void size90mm_Unchecked(object sender, RoutedEventArgs e)
        {

            circleCheck = false;
            if (circleCheck == false)
            {
                size90mm.IsChecked = false;
                Canvas_On_ImgScreen.Children.Clear();

            }

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
                Stroke = System.Windows.Media.Brushes.Green,
                StrokeThickness = 3,
                Fill = System.Windows.Media.Brushes.Transparent
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
                Stroke = System.Windows.Media.Brushes.Green,
                StrokeThickness = 3,
                Fill = System.Windows.Media.Brushes.Transparent
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

            System.Windows.Shapes.Rectangle ARectangle = new Rectangle() { Height = 150, Width = 300, Stroke = System.Windows.Media.Brushes.Green, StrokeThickness = 2, Fill = System.Windows.Media.Brushes.Transparent };

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

            Rectangle ARectangle = new Rectangle() { Height = 200, Width = 350, Stroke = System.Windows.Media.Brushes.Green, Fill = System.Windows.Media.Brushes.Transparent, StrokeThickness = 2 };

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

            ObjectStartLocation.X = Canvas.GetLeft(r);
            ObjectStartLocation.Y = Canvas.GetTop(r);

            ClickedObject = r;

        }


        private void Canvas_On_ImgScreen_PreviewMouseMove(object sender, MouseEventArgs e)
        {
 
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
                Canvas.SetTop(textBlockSize, ObjectStartLocation.Y + deltaY - 25);

            }
            else if (ClickedObject is Ellipse)
            {

                Ellipse c = ClickedObject as Ellipse;

                Canvas.SetLeft(c, ObjectStartLocation.X + deltaX);
                Canvas.SetTop(c, ObjectStartLocation.Y + deltaY);
                Canvas.SetLeft(textBlockSize, ObjectStartLocation.X + deltaX + 80);
                Canvas.SetTop(textBlockSize, ObjectStartLocation.Y + deltaY - 25);
                cropX = ObjectStartLocation.X + deltaX;
                cropY = ObjectStartLocation.Y + deltaY;

                cropWidth = c.Width;
                cropHeight = c.Height;

            }
            else
                return;
        }


        private void Canvas_On_ImgScreen_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

            dragClickdown.X = e.GetPosition(ImgScreen).X;
            dragClickdown.Y = e.GetPosition(ImgScreen).Y;

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

            }
        }


        //--------------------------------------------------------------------------------------------------------------------------------------


        private void ImgScreen_MouseUp(object sender, MouseButtonEventArgs e)
        {
        }



        //--------------------------------------------------------------------------------------------------------------------------------------



        ///******************************************** COUNT *****************************************************************


        private void CountBtn_Click(object sender, RoutedEventArgs e)
        {
            var command = PythonInterface.BuildCommand("count");
            string total = pythonInterface.SendCommandAndReceiveRawString(command);
            Total_Count_Value.Text = total;

        }


        ///********************  BIến bacteriaCenters chứa list center position của Bacterias    ***************************************************************************************
 
        private void EditBtn_Click(object sender, RoutedEventArgs e)
        {
            var command = PythonInterface.BuildCommand("centers");
            string bacteriaCentersString = pythonInterface.SendCommandAndReceiveRawString(command);
            List<BacteriaCenter> bacteriaCenters = Converter.StringToBacteriaCenters(bacteriaCentersString);

            //lap qua cai mang bacter
            //imagesource 
            //lay toa do chuot tren image source x, y
            //for b in bacteriaCenters   ===> Distance(b.x, b.y, x, y) 


        }

        ///***********************************************************************************************************
        ///

        private void ConfirmBtn_Click(object sender, RoutedEventArgs e)
        {
                      
            if (tempConfirm == true)
            {
                var imageActionInTam = new ImageBackAction();
                imageActionInTam.image = ImgScreen.Source as BitmapImage;
                backStack.Push(imageActionInTam);
                
                tempConfirm = false;
            }

            PrintBackStack();
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
                    Cv2.Circle(blackMask, (int)(centerCircleX * 6.5), (int)(centerCircleY * 6.5), (int)radiusCircle / 2, Scalar.White, -1);
                    Cv2.BitwiseAnd(SourceImg, blackMask, ImgAfterAddMask);
                    var converted = Convert(BitmapConverter.ToBitmap(ImgAfterAddMask));
                    ImgScreen.Source = converted;

                    size90mm.IsChecked = false;

                    var saveFileName = "imgcropped.jpg";
                    Console.WriteLine(saveFileName);
                    ImgAfterAddMask.SaveImage(saveFileName);

                    currentImagePath = Directory.GetCurrentDirectory() + "\\" + saveFileName;

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

            var imageActionInOutside = new ImageBackAction();
            imageActionInOutside.image = ImgScreen.Source as BitmapImage;
            backStack.Push(imageActionInOutside);
        }
        //----------------------------------------------------------------------------------------------------------------------------------------
        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
           
            Canvas_On_ImgScreen.Children.Clear();

            ImgScreen.Source = null;

            SegmentActivated = false;
            getPosByClickEnable = false;

            DistanceEnable = false;
            ThreshEnable = false;

            Sens_Slider.IsEnabled = false;
            Thresh_Slider.IsEnabled = false;

            Ena_Distance.IsChecked = false;
            Ena_Threshold.IsChecked = false;

            Thresh_Slider.Value = 0;
            Sens_Slider.Value = 0;

            circleCheck = false;

            if (circleCheck == false)
            {
                size90mm.IsChecked = false;
                size100mm.IsChecked = false;
                size150mm.IsChecked = false;
            }

        }
        //--------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// HSV CONTROL MANUAL, GRAYSCALE, THRESHOLD _______________________________________________________________
        bool ColorFilterEnable = false;
        bool GrayScaleEnable = false;
        Mat GrayScaleImg;
        Mat HSVImg;

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
                Thresh_Slider.IsEnabled = true;
                Ena_Threshold.IsChecked = true;

                var thresholdRoundValue = Math.Round(Thresh_Slider.Value);
                var command = PythonInterface.BuildCommand("thresh", thresholdRoundValue.ToString(), currentImagePath);
                Mat image = pythonInterface.SendCommand(command);

                CommandCallbackEventDispatch(command, image);


                if (image != null)
                {
                    var bitmapSource = image.ToBitmapSource();

                    bitmapSource.Freeze();

                    ImgScreen.Dispatcher.Invoke(new Action(() =>
                    {
                        ImgScreen.Source = null;
                        ImgScreen.Source = bitmapSource;
                    }));
                }

            }


            if (ThreshEnable == true && SegmentActivated == true)
            {
                Thresh_Slider.IsEnabled = true;
                Ena_Threshold.IsChecked = true;
                var thresholdRoundValue = Math.Round(Thresh_Slider.Value);
                var command = PythonInterface.BuildCommand("thresh", thresholdRoundValue.ToString(), SourceImgSegment);
                Mat image = pythonInterface.SendCommand(command);

                CommandCallbackEventDispatch(command, image);


                if (image != null)
                {
                    var bitmapSource = image.ToBitmapSource();

                    bitmapSource.Freeze();

                    ImgScreen.Dispatcher.Invoke(new Action(() =>
                    {
                        ImgScreen.Source = null;
                        ImgScreen.Source = bitmapSource;
                    }));
                }

            }

            //------------------------------- NOTE:  --------------------------------------------------------------------------------
        }
        private void Ena_Threshold_Unchecked(object sender, RoutedEventArgs e)
        {
            ThreshEnable = false;
            if (ThreshEnable == false)
            {
                Thresh_Slider.IsEnabled = false;
                Ena_Threshold.IsChecked = false;

                ImgScreen.Source = Convert(BitmapConverter.ToBitmap(ImgAfterAddMask));
            }

            if (ThreshEnable == false && SegmentActivated == true)
            {
                Thresh_Slider.IsEnabled = false;
                Ena_Threshold.IsChecked = false;

                ImgScreen.Source = Convert(BitmapConverter.ToBitmap(ImgAfterSegment));
            }

        }


        private void Ena_Distance_Checked(object sender, RoutedEventArgs e)
        {
            DistanceEnable = true;
            if (DistanceEnable == true)
            {
                Sens_Slider.IsEnabled = true;
                Ena_Distance.IsChecked = true;

                var distanceRoundValue = Math.Round(Sens_Slider.Value).ToString();
                var thresholdRoundValue = Math.Round(Thresh_Slider.Value).ToString();
                var command = PythonInterface.BuildCommand("distance", distanceRoundValue, thresholdRoundValue, currentImagePath);
                Mat image = pythonInterface.SendCommand(command);

                CommandCallbackEventDispatch(command, image);


                if (image != null)
                {
                    var bitmapSource = image.ToBitmapSource();

                    bitmapSource.Freeze();

                    ImgScreen.Dispatcher.Invoke(new Action(() =>
                    {
                        ImgScreen.Source = null;
                        ImgScreen.Source = bitmapSource;
                    }));
                }

            }


            if (DistanceEnable == true && SegmentActivated == true)
            {
                Sens_Slider.IsEnabled = true;
                Ena_Distance.IsChecked = true;

                var distanceRoundValue = Math.Round(Sens_Slider.Value).ToString();
                var thresholdRoundValue = Math.Round(Thresh_Slider.Value).ToString();
                var command = PythonInterface.BuildCommand("distance", distanceRoundValue, thresholdRoundValue, SourceImgSegment);
                Mat image = pythonInterface.SendCommand(command);

                CommandCallbackEventDispatch(command, image);


                if (image != null)
                {
                    var bitmapSource = image.ToBitmapSource();

                    bitmapSource.Freeze();

                    ImgScreen.Dispatcher.Invoke(new Action(() =>
                    {
                        ImgScreen.Source = null;
                        ImgScreen.Source = bitmapSource;
                    }));
                }

            }

        }



        private void Ena_Distance_Unchecked(object sender, RoutedEventArgs e)
        {
            DistanceEnable = false;
            if (DistanceEnable == false)
            {
                Sens_Slider.IsEnabled = false;
                Ena_Distance.IsChecked = false;

                ImgScreen.Source = Convert(BitmapConverter.ToBitmap(ImgAfterAddMask));
            }

            if (DistanceEnable == false && SegmentActivated == true)
            {
                Sens_Slider.IsEnabled = false;
                Ena_Distance.IsChecked = false;

                ImgScreen.Source = Convert(BitmapConverter.ToBitmap(ImgAfterSegment));
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

                    BackAction dataPo = backStack.Peek();

                    if (dataPo is ThreshBackAction)
                    {
                        var threshAction = (ThreshBackAction)dataPo;
                        Thresh_Slider.Value = threshAction.value;
                    }
                    else if (dataPo is DistanceBackAction)
                    {
                        var distanceAction = (DistanceBackAction)dataPo;
                        Sens_Slider.Value = distanceAction.value;
                    }
                    else if (dataPo is ImageBackAction)
                    {
                        if (SegmentActivated == true)
                        {
                            var imageAction = (ImageBackAction)dataPo;
                            ImgScreen.Source = imageAction.image;

                        }

                        if (SegmentActivated == true)
                        {
                            var imageAction = (ImageBackAction)dataPo;
                            ImgScreen.Source = imageAction.image;

                            backStack.Clear();

                            var imageActionInTam = new ImageBackAction();
                            imageActionInTam.image = ImgScreen.Source as BitmapImage;
                            backStack.Push(imageActionInTam);

                        }

                    }
                    else if (dataPo is PolyBackAction)
                    {
                        var polyAction = (PolyBackAction)dataPo;

                        SelectTopPolyToDraw();
                        DrawPolies();
                    }
                }
                else
                {
                    MessageBox.Show("stack empty");
                    backStack.Clear();
                    tempThresh = true;
                    tamH = true;
                    tempDistance = true;
                    tamS = true;
                    tamV = true;
                    tempConfirm = true;

                }


                //switch (dataPo.backActionLabel)
                //{
                //    case "thresh":
                //        Console.WriteLine("...............");
                //        Thresh_Slider.Value = dataPo.backActionValue;
                //        //foreach (var item in mystackValues)
                //        //    Console.WriteLine(item);
                //        //foreach (var item in mystackLabels)
                //        //    Console.WriteLine(item);
                //        break;
                //    case "local":
                //        Console.WriteLine("...............");
                //        Sens_Slider.Value = dataPo.backActionValue;
                //        break;
                //    case "confirm":
                //        SourceImg = Cv2.ImRead(SelectImgPath);

                //        BitmapImage bitmap = new BitmapImage();
                //        bitmap.BeginInit();
                //        bitmap.UriSource = new Uri(SelectImgPath);
                //        bitmap.EndInit();

                //        ImgScreen.Width = SourceImg.Width / 6.5;
                //        ImgScreen.Height = SourceImg.Height / 6.5;
                //        Canvas_On_ImgScreen.Width = SourceImg.Width / 6.5;
                //        Canvas_On_ImgScreen.Height = SourceImg.Height / 6.5;

                //        ImgScreen.Source = bitmap;

                //        Canvas_On_ImgScreen.Children.Clear();
                //        break;
                //    default:
                //        break;
                //}

            }

            PrintBackStack();
        }



        private void Next_Click(object sender, RoutedEventArgs e)
        {

            if (nextStack.Count() != 0)
            {
                
                BackAction popData = nextStack.Pop();

                backStack.Push(popData);
                
                if (popData is ThreshBackAction)
                {
                  
                    ThreshBackAction threshBackAction = (ThreshBackAction)popData;
                   
                    Thresh_Slider.Value = threshBackAction.value;

                }
                else if (popData is DistanceBackAction)
                {
                    DistanceBackAction distanceBackAction = (DistanceBackAction)popData;
                    Sens_Slider.Value = distanceBackAction.value;
                }
                else if (popData is ImageBackAction)
                {
                    if (SegmentActivated == false)
                    {
                        ImageBackAction imageBackAction = (ImageBackAction)popData;
                        ImgScreen.Source = Convert(BitmapConverter.ToBitmap(ImgAfterAddMask));
                    }
                    if (SegmentActivated == true)
                    {
                        ImgScreen.Source = Convert(BitmapConverter.ToBitmap(ImgAfterSegment));
                    }

                }

                else if (popData is PolyBackAction)
                {
                    DrawPolies();
                }

                    //dataPre = nextStack.peek(); {label: "thresh", backActionValue: 100}

                    //switch (dataPre.backActionLabel)
                    //{
                    //    case "thresh":
                    //        Console.WriteLine("value cur peek");
                    //        Thresh_Slider.Value = dataPre.backActionValue;
                    //        popData = nextStack.Pop();
                    //        curValue = popData.backActionValue;
                    //        curLabel = popData.backActionLabel;
                    //        backStack.Push(popData);

                    //        break;
                    //    case "local":
                    //        Console.WriteLine("value cur peek");
                    //        Sens_Slider.Value = dataPre.backActionValue;
                    //        popData = nextStack.Pop();
                    //        curValue = popData.backActionValue;
                    //        curLabel = popData.backActionLabel;
                    //        backStack.Push(popData);
                    //        break;
                    //    case "confirm":
                    //        ImgScreen.Source = Convert(BitmapConverter.ToBitmap(ImgAfterAddMask));
                    //        double i = 999;
                    //        i = dataPre.backActionValue;
                    //        popData = nextStack.Pop();
                    //        curValue = popData.backActionValue;
                    //        curLabel = popData.backActionLabel;
                    //        backStack.Push(popData);
                    //        break;
                    //    default:
                    //        break;

                    //}               

            }

        }



        private void Thresh_Slider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {          

            if (tempThresh == true)
            {
                var threshActionInLocal = new ThreshBackAction();
                threshActionInLocal.value = 0;
                backStack.Push(threshActionInLocal);

                tempThresh = false;
            }

            var threshActionInSlider = new ThreshBackAction();
            threshActionInSlider.value = Thresh_Slider.Value;
            backStack.Push(threshActionInSlider);

            PrintBackStack();

        }

        private void OK_Thresh_Click(object sender, RoutedEventArgs e)
        {

            BackAction topData = new BackAction();

            if (backStack.Count() == 0)
            {
                var threshAction = new ThreshBackAction();
                threshAction.value = -1;

                topData = threshAction;
            }
            else
            {
                topData = backStack.Peek(); 
            }


            double TextBoxThreshValue = double.Parse(Thresh_Value.Text);
            TextBoxThreshValue = Math.Round(TextBoxThreshValue);

            double SliderThreshValue = Math.Round(Thresh_Slider.Value);

            if (topData is ThreshBackAction &&
                //tempObject.value == Thresh_Slider.Value && 
                SliderThreshValue == TextBoxThreshValue)
            {
                ThreshBackAction tempObject = (ThreshBackAction)topData; 

                if (tempObject.value == Thresh_Slider.Value)
                {
                    return;
                }
            }

            if (tempThresh == true)
            {
                var threshActionInLocal = new ThreshBackAction();
                threshActionInLocal.value = 0;

                backStack.Push(threshActionInLocal);

                tempThresh = false;

            }

            var threshSliderActionInSlider = new ThreshBackAction();
            threshSliderActionInSlider.value = Thresh_Slider.Value;

            backStack.Push(threshSliderActionInSlider);

            PrintBackStack();

        }



        private void Sens_Slider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            
            if (tempDistance == true)
            {
                var distanceActionInLocal = new DistanceBackAction();

                distanceActionInLocal.value = 0;

                backStack.Push(distanceActionInLocal);

                tempDistance = false;
            }

            var distanceActionInSlider = new DistanceBackAction();
            distanceActionInSlider.value = Sens_Slider.Value;
            backStack.Push(distanceActionInSlider);

            PrintBackStack();

        }




        private void OK_Sens_Click(object sender, RoutedEventArgs e)
        {

            BackAction topData;

            if (backStack.Count() == 0)
            {
                var distanceAction = new DistanceBackAction();
                distanceAction.value = -1;

                topData = distanceAction;
            }
            else
            {
                topData = backStack.Peek();
            }

            double TextBoxLocalValue = double.Parse(Sens_Value.Text);
            TextBoxLocalValue = Math.Round(TextBoxLocalValue);

            double SliderLocalValue = Math.Round(Sens_Slider.Value);

            if (topData is DistanceBackAction &&
                //topData.backActionValue == Sens_Slider.Value && 
                SliderLocalValue == TextBoxLocalValue)
            {
                DistanceBackAction tempobject = (DistanceBackAction)topData;
                if (tempobject.value == Sens_Slider.Value)
                {
                    return;
                }
            }

            if (tempDistance == true)
            {
                var distanceActionInTamLocal = new DistanceBackAction();
                distanceActionInTamLocal.value = 0;
                backStack.Push(distanceActionInTamLocal);

                tempDistance = false;
            }

            var distanceActionSliderValue = new DistanceBackAction();
            distanceActionSliderValue.value = Sens_Slider.Value;
            backStack.Push(distanceActionSliderValue);

            PrintBackStack();
        }




        private void OK_H_Click(object sender, RoutedEventArgs e)
        {
        }

        private void H_Slider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
        }

        private void OK_S_Click(object sender, RoutedEventArgs e)
        {


        }

        private void S_Slider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {

         
        }

        private void OK_V_Click(object sender, RoutedEventArgs e)
        {


        }

        private void V_Slider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
        }





        //-------------------------------------------------------------------------------------------------------------------------------------- 
        private readonly Channel<string> channelValues = Channel.CreateBounded<string>(new BoundedChannelOptions(2)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });

        // You'll need to start this consumer somewhere and observe it (via await) to ensure you see exceptions


        private void CommandCallbackEventDispatch(string command, Mat image)
        {
            if (image == null)
            {
                return;
            }
            if (command.Contains("distance"))
            {
                image.SaveImage(AppConstraint.DISTANCE_OUTPUT_IMAGE);
            }
            if (command.Contains("thresh"))
            {
                image.SaveImage(AppConstraint.THRESH_OUTPUT_IMAGE);
            }

        }


        private async Task SliderValueConsumerAsync()
        {
            var reader = channelValues.Reader;


            while (await reader.WaitToReadAsync(CancellationToken.None))
                try
                {
                    while (reader.TryRead(out var command))
                    {
                        Console.WriteLine("call from consumer: " + command);

                        Mat image = pythonInterface.SendCommand(command);

                        CommandCallbackEventDispatch(command, image);


                        if (image != null)
                        {
                            var bitmapSource = image.ToBitmapSource();

                            bitmapSource.Freeze();

                            ImgScreen.Dispatcher.Invoke(new Action(() =>
                            {
                                ImgScreen.Source = null;
                                ImgScreen.Source = bitmapSource;
                            }));
                        }


                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("what is wrong?! " + e.Message);
                }

        }





        private async void Thresh_Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SegmentActivated == false)
            {
                var thresholdRoundValue = Math.Round(e.NewValue);
                var command = PythonInterface.BuildCommand("thresh", thresholdRoundValue.ToString(), currentImagePath);
                await channelValues.Writer.WriteAsync(command, CancellationToken.None);

                Thresh_Value.Text = Thresh_Slider.Value.ToString();
            }

            if (SegmentActivated == true)
            {
                var thresholdRoundValue = Math.Round(e.NewValue);
                var command = PythonInterface.BuildCommand("thresh", thresholdRoundValue.ToString(), SourceImgSegment);
                await channelValues.Writer.WriteAsync(command, CancellationToken.None);

                Thresh_Value.Text = Thresh_Slider.Value.ToString();
            }           
        }




        private async void Sens_Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SegmentActivated == false)
            {
                var thresholdRoundValue = Math.Round(Thresh_Slider.Value).ToString();
                var distanceRoundValue = Math.Round(e.NewValue).ToString();

                var command = PythonInterface.BuildCommand("distance", distanceRoundValue, thresholdRoundValue, currentImagePath);
                await channelValues.Writer.WriteAsync(command, CancellationToken.None);

                Sens_Value.Text = Sens_Slider.Value.ToString();

                Console.WriteLine("----segment fasle-----");
            }

            if (SegmentActivated == true)
            {
                var thresholdRoundValue = Math.Round(Thresh_Slider.Value).ToString();
                var distanceRoundValue = Math.Round(e.NewValue).ToString();

                var command = PythonInterface.BuildCommand("distance", distanceRoundValue, thresholdRoundValue, SourceImgSegment);
                await channelValues.Writer.WriteAsync(command, CancellationToken.None);

                Sens_Value.Text = Sens_Slider.Value.ToString();
                Console.WriteLine("----segment true-----");
            }
         
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



        private Mat convertToMat(IGrabResult rtnGrabResult)
        {
            PixelDataConverter converter = new PixelDataConverter();

            IImage image = rtnGrabResult;
            converter.OutputPixelFormat = PixelType.BGR8packed;
            byte[] buffer = new byte[converter.GetBufferSizeForConversion(rtnGrabResult)];
            converter.Convert(buffer, rtnGrabResult);
            return new Mat(rtnGrabResult.Height, rtnGrabResult.Width, MatType.CV_8UC3, buffer);
        }
        public static System.Drawing.Bitmap MatToBitmap(Mat image)
        {
            return OpenCvSharp.Extensions.BitmapConverter.ToBitmap(image);
        }








        //-------------------------------------------- POSITION MOUSE ------------------------------------------------------------------

        private void SegmentBtn_Click(object sender, RoutedEventArgs e)
        {
            SegmentActivated = true;
        }




        int positionMouseX;
        int positionMouseY;
        private void TopCanvas_MouseMove(object sender, MouseEventArgs e)
        {

            if (SegmentActivated == true)
            {
                positionMouseX = (int)e.GetPosition(ImgScreen_Canvas).X;
                positionMouseY = (int)e.GetPosition(ImgScreen_Canvas).Y;

                teets.Text = "X: " + positionMouseX + "," + "Y: " + positionMouseY;

                getPosByClickEnable = true;
            }

        }




        int currentPolyName = 1;
        List<PositionMouse> polyPoints = new List<PositionMouse>();   
        private void TopCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {

            if (SegmentActivated == true && getPosByClickEnable == true)
            {            

                PolyBackAction polyBackAction = new PolyBackAction();
                polyBackAction.polyName = currentPolyName;
                polyBackAction.mouseX = positionMouseX;
                polyBackAction.mouseY = positionMouseY;

                backStack.Push(polyBackAction);

                PositionMouse positionMouse = new PositionMouse(positionMouseX, positionMouseY);
                polyPoints.Add(positionMouse);

                PrintBackStack();

                DrawPolies();

            }
        }

        private void SelectTopPolyToDraw()
        {

            foreach(var backAction in backStack)
            {
                if (backAction is PolyBackAction)
                {
                    PolyBackAction polyBackAction = (PolyBackAction)backAction;
                    currentPolyName = polyBackAction.polyName;
                    break;
                }
            }
        }



        private void DeletePolylineAndRectangle()
        {
            var willBeRemovedItems = new List<UIElement>();

            foreach (var item in Canvas_On_ImgScreen.Children)
            {
                if (item is Polyline || item is Rectangle)
                {
                    willBeRemovedItems.Add((UIElement)item);
                }
            }

            foreach (var item in willBeRemovedItems)
            {
                Canvas_On_ImgScreen.Children.Remove(item);
            }
        }





        private void DrawPolies()
        {
            SolidColorBrush brush = new SolidColorBrush();
            brush.Color = Colors.Black;

            DeletePolylineAndRectangle();

            for(int i = 0; i < backStack.Count; i++)
            {
                if (backStack.ElementAt(i) is PolyBackAction)
                {
                    PolyBackAction StarterPoly = (PolyBackAction)backStack.ElementAt(i);
                    int starterPolyName = StarterPoly.polyName;

                    int sameNameStarterPolyIndex = i;

                    PointCollection polygonPoints = new PointCollection();
                    Polyline polyline = new Polyline();

                    polyline.Stroke = brush;
                    polyline.StrokeThickness = 2;

                    while (
                        sameNameStarterPolyIndex < backStack.Count &&
                        backStack.ElementAt(sameNameStarterPolyIndex) is PolyBackAction
                        )
                    {
                        PolyBackAction currentPolyBackAction = (PolyBackAction)backStack.ElementAt(sameNameStarterPolyIndex);
                        if (currentPolyBackAction.polyName != starterPolyName)
                        {
                            break;
                        }

                        Rectangle smallDot = new Rectangle()
                        {
                            Fill = System.Windows.Media.Brushes.Red,
                            Width = 5,
                            Height = 5
                        };
                        Canvas.SetLeft(smallDot, currentPolyBackAction.mouseX);
                        Canvas.SetTop(smallDot, currentPolyBackAction.mouseY);
                        Canvas_On_ImgScreen.Children.Add(smallDot);
                        
                        polygonPoints.Add(new System.Windows.Point(currentPolyBackAction.mouseX, currentPolyBackAction.mouseY));
                        
                        sameNameStarterPolyIndex++;
                    }

                    polygonPoints.Add(polygonPoints.First());

                    polyline.Points = polygonPoints;
                    
                    Canvas_On_ImgScreen.Children.Add(polyline);

                    i = sameNameStarterPolyIndex - 1; //IMPORTANT!!
                }

            }

        }






//-----------------------------------------------------------------------------------------------------------------
        private void CropPolies()
        {

            double scaleRatio = 6.5;
            ImgAfterSegment = new Mat();
            Mat blackMask = new Mat(SourceImg.Height, SourceImg.Width, MatType.CV_8UC3, Scalar.Black);

            for (int i = 0; i < backStack.Count; i++)
            {
                if (backStack.ElementAt(i) is PolyBackAction)
                {
                    PolyBackAction Starterpoly = (PolyBackAction)backStack.ElementAt(i);
                    int starterPolyName = Starterpoly.polyName;
                    int sameNameStarterPolyIndex = i;

                    List<OpenCvPoint> opencvPoints = new List<OpenCvPoint>();

                    while (sameNameStarterPolyIndex < backStack.Count &&
                        backStack.ElementAt(sameNameStarterPolyIndex) is PolyBackAction)
                    {
                        PolyBackAction currentPolyBackAction = (PolyBackAction)backStack.ElementAt(sameNameStarterPolyIndex);
                        if (currentPolyBackAction.polyName != starterPolyName)
                        {
                            break;
                        }

                        opencvPoints.Add(new OpenCvPoint
                            (currentPolyBackAction.mouseX * scaleRatio,
                            currentPolyBackAction.mouseY * scaleRatio));
                        
                        sameNameStarterPolyIndex++;
                    }

                    Cv2.FillPoly(blackMask, new OpenCvSharp.Point[][] { opencvPoints.ToArray() }, Scalar.All(255));

                    DeletePolylineAndRectangle();

                    i = sameNameStarterPolyIndex - 1;
                }
            }

            Cv2.BitwiseAnd(SourceImg, blackMask, ImgAfterSegment);

        }








//-----------------------------------------------------------------------------------------------------------------------      
        Mat ImgAfterSegment;
        private void SendCropImgBtn_Click(object sender, RoutedEventArgs e)
        {
            if (tempSend == true)
            {
                var imageActionInLocal = new ImageBackAction();
                imageActionInLocal.image = ImgScreen.Source as BitmapImage;
                backStack.Push(imageActionInLocal);

                tempSend = false;
            }
            var imageActionInTam = new ImageBackAction();
            imageActionInTam.image = ImgScreen.Source as BitmapImage;
            backStack.Push(imageActionInTam);

            CropPolies();
            PrintBackStack();

            var imgAferSecmented = Convert(BitmapConverter.ToBitmap(ImgAfterSegment));
            ImgScreen.Source = imgAferSecmented;

            var saveFileName = "imgcanvas.jpg";
            ImgAfterSegment.SaveImage(saveFileName);
            SourceImgSegment = Directory.GetCurrentDirectory() + "\\" + saveFileName;

            DeletePolylineAndRectangle(); 

        }






        private void AddSegmentBtn_Click(object sender, RoutedEventArgs e)
        {
            currentPolyName++; 
        }

    }
}
