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
using OpenCvSharp;

using OpenCvSharp.Extensions;
using System.Windows.Threading;
using Window = System.Windows.Window;
using System.Threading;
using Basler.Pylon;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Hosting;
using Python.Runtime;

using Pythonzxrr;
using OpenCvSharp.WpfExtensions;
using System.ComponentModel;
using Microsoft.Win32;
using System.Threading.Channels;
using Rectangle = System.Windows.Shapes.Rectangle;
//using System.Drawing;
using Size = OpenCvSharp.Size;
using Image = System.Windows.Controls.Image;
using Point = OpenCvSharp.Point;

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
        //string a;
        //object b;
        bool tamThresh = true;
        bool tamLocal = true;
        bool tamH = true;
        bool tamS = true;
        bool tamV = true;
        bool tamConfirm = true;
        bool DistanceEnable = false;
        bool ThreshEnable = false;
        bool addSecmentEnable = false;
        bool getPosByClickEnable = false;


        //int zoomX = 0;
        //  PixelDataConverter converter = new PixelDataConverter();
        private DispatcherTimer Timer1;
        private int time = 0;
        PythonInterface pythonInterface;
        string currentImagePath;

        public WindowStart()
        {

            InitializeComponent();
            ButtonFile_canvas.Visibility = Visibility.Hidden;
            AutoScreen.Visibility = Visibility.Hidden;
            ManualScreen.Visibility = Visibility.Hidden;
            WelcomeScreen.Visibility = Visibility.Visible;

            //test.Background = imgBrush;

            pythonInterface = new PythonInterface();
            pythonInterface.Connect();

            Task.Run(SliderValueConsumerAsync);
            //Task.Run(TotalValueConsumerAsync);

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


        private void FileButton_Click(object sender, RoutedEventArgs e)
        {
            ButtonFile_Click_Mode += 1; //really
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
            //S_Slider_Value.Text = "in: " + ZoomInRatio.ToString();
            //V_Slider_Value.Text = "out: " + ZoomOutRatio.ToString();
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
            //openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
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

            currentImagePath = SelectImgPath;

            //SolidColorBrush brush = new SolidColorBrush();
            //brush.Color = Colors.Black;

            //Polyline polyline = new Polyline();
            //polyline.Stroke = brush;
            //polyline.StrokeThickness = 1;
            ////ok thu lai

            //PointCollection polygonPoints = new PointCollection();
            //polygonPoints.Add(new System.Windows.Point(289.8, 184.4));
            //polygonPoints.Add(new System.Windows.Point(575.4, 220.4));

            //polyline.Points = polygonPoints;

            //Console.WriteLine("draw?");


            //ImgScreen_Canvas.Children.Add(polyline);


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
                Canvas.SetTop(textBlockSize, ObjectStartLocation.Y + deltaY - 25);

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


        //--------------------------------------------------------------------------------------------------------------------------------------




        private void ImgScreen_MouseUp(object sender, MouseButtonEventArgs e)
        {
        }



        //--------------------------------------------------------------------------------------------------------------------------------------



        ///******************************************** COUNT *****************************************************************
        //////
        private void CountBtn_Click(object sender, RoutedEventArgs e)
        {
            var command = PythonInterface.BuildCommand("count");
            string total = pythonInterface.SendCommandAndReceiveRawString(command);
            Total_Count_Value.Text = total;

        }
        ///********************  BIến bacteriaCenters chứa list center position của Bacterias    ***************************************************************************************
        ///
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


            if (tamConfirm == true)
            {
                backStack.Push(new BACKDATA("confirm", 0));
                tamConfirm = false;
            }
            backStack.Push(new BACKDATA("confirm", 999));

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
                    //Cv2.FillPoly()
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
        }
        //----------------------------------------------------------------------------------------------------------------------------------------
        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            ImgScreen_Canvas.Children.Remove(polyline);
            Canvas_On_ImgScreen.Children.Remove(smallDot);
            

            Canvas_On_ImgScreen.Children.Clear();

            ImgScreen.Source = null;

            addSecmentEnable = false;
            getPosByClickEnable = false;    

            DistanceEnable = false;
            ThreshEnable = false;
            circleCheck = false;

            if (circleCheck == false)
            {
                size90mm.IsChecked = false;
                size100mm.IsChecked = false;
                size150mm.IsChecked = false;
            }

            if (DistanceEnable == false)
            {
                Sens_Slider.IsEnabled = false;
            }
            if (ThreshEnable == false)
            {
                Thresh_Slider.IsEnabled = false;
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
                Thresh_Slider.IsEnabled = true;
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

            //------------------------------- NOTE:  --------------------------------------------------------------------------------
        }
        private void Ena_Threshold_Unchecked(object sender, RoutedEventArgs e)
        {
            ThreshEnable = false;
            if (ThreshEnable == false)
            {
                Thresh_Slider.IsEnabled = false;
            }
            ImgScreen.Source = Convert(BitmapConverter.ToBitmap(ImgAfterAddMask));



        }
        private void Ena_Distance_Checked(object sender, RoutedEventArgs e)
        {
            DistanceEnable = true;
            if (DistanceEnable == true)
            {
                Sens_Slider.IsEnabled = true;
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


        }

        private void Ena_Distance_Unchecked(object sender, RoutedEventArgs e)
        {
            DistanceEnable = false;
            if (DistanceEnable == false)
            {
                Sens_Slider.IsEnabled = false;
            }
            ImgScreen.Source = Convert(BitmapConverter.ToBitmap(ImgAfterAddMask));

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
                        case "confirm":
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

                            Canvas_On_ImgScreen.Children.Clear();
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
                    tamConfirm = true;
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
                        case "confirm":
                            ImgScreen.Source = Convert(BitmapConverter.ToBitmap(ImgAfterAddMask));
                            double i = 999;
                            i = dataPre.values;
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
            var thresholdRoundValue = Math.Round(e.NewValue);
            var command = PythonInterface.BuildCommand("thresh", thresholdRoundValue.ToString(), currentImagePath);
            await channelValues.Writer.WriteAsync(command, CancellationToken.None);

            Thresh_Value.Text = Thresh_Slider.Value.ToString();

        }

        private async void Sens_Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var thresholdRoundValue = Math.Round(Thresh_Slider.Value).ToString();
            var distanceRoundValue = Math.Round(e.NewValue).ToString();

            var command = PythonInterface.BuildCommand("distance", distanceRoundValue, thresholdRoundValue, currentImagePath);
            await channelValues.Writer.WriteAsync(command, CancellationToken.None);

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

        private void AddSecmentBtn_Click(object sender, RoutedEventArgs e)
        {
            addSecmentEnable = true;
        }


        double positionMouseX;
        double positionMouseY;

        private void TopCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (addSecmentEnable == true)
            {
                positionMouseX = e.GetPosition(ImgScreen_Canvas).X;
                positionMouseY = e.GetPosition(ImgScreen_Canvas).Y;

                teets.Text = "X: " + positionMouseX + "," + "Y: " + positionMouseY;

                getPosByClickEnable = true;
            }
        }


        List<PositionMouse> positionMouses = new List<PositionMouse>();

        Polyline polyline = new Polyline();
        Rectangle smallDot = new Rectangle();


        private void TopCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {

            if (addSecmentEnable == true && getPosByClickEnable == true)
            {
                var positionMousesCur = new PositionMouse(positionMouseX, positionMouseY);
                positionMouses.Add(positionMousesCur);

                foreach (var item in positionMouses)
                {
                    Console.WriteLine(item);
                }
                Console.WriteLine("++++++++++++++++++");

                SolidColorBrush brush = new SolidColorBrush();
                brush.Color = Colors.Black;
                PointCollection polygonPoints = new PointCollection();

                polyline.Stroke = brush;
                polyline.StrokeThickness = 2;

                smallDot = new Rectangle()
                {
                    Fill = System.Windows.Media.Brushes.Red,
                    Width = 5,
                    Height = 5
                };


                if (positionMouses.Count() > 0)
                {
                    //foreach (PositionMouse items in positionMouses)
                    //{
                    //    Canvas.SetLeft(smallDot, items.posx);
                    //    Canvas.SetTop(smallDot, items.posy);

                    //    var p = new System.Windows.Point(items.posx, items.posy);
                    //    polygonPoints.Add(p);

                    //}  
                    for (int i = 0; i < positionMouses.Count(); i++)
                    {
                        Canvas.SetLeft(smallDot, positionMouses[i].posx);
                        Canvas.SetTop(smallDot, positionMouses[i].posy);

                        polygonPoints.Add(new System.Windows.Point(positionMouses[i].posx, positionMouses[i].posy));
                    }
                    polygonPoints.Add(new System.Windows.Point(positionMouses[0].posx, positionMouses[0].posy));


                    if (ImgScreen_Canvas.Children.Count > 0)
                    {
                        ImgScreen_Canvas.Children.RemoveAt(ImgScreen_Canvas.Children.Count - 1);
                        polyline.Points = polygonPoints;
 
                    }
                    ImgScreen_Canvas.Children.Add(smallDot);
                    ImgScreen_Canvas.Children.Add(polyline);

                }
            }
        }

        //--------------------------------- BUG: khong the xoa smallDot ---------------------------------------------
        Mat KQ;
        private void SendCropImgBtn_Click(object sender, RoutedEventArgs e)
        {
            KQ = new Mat();
            List<PositionMouseInImgSource> positionMousesInImgSoucre = new List<PositionMouseInImgSource>();

            double[][] positionArray = new double[positionMouses.Count + 1][];
            double scaleRatio = 6.5;
            OpenCvSharp.Point[] position = new OpenCvSharp.Point[positionMouses.Count];
            for (int i = 0; i < positionMouses.Count; i++)
            {
                //positionMousesInImgSoucre[i].posx = positionMouses[i].posx * scaleRatio;
                //positionMousesInImgSoucre[i].posy = positionMouses[i].posy * scaleRatio;
                //double[] position = new double[2];
                //position[0] = positionMouses[i].posx * scaleRatio;
                //position[1] = positionMouses[i].posy * scaleRatio;
                //positionArray.Append(position);
                position[i] = new OpenCvSharp.Point((int)(positionMouses[i].posx * scaleRatio), (int)(positionMouses[i].posy * scaleRatio));

            }
            //
            LineTypes line = new LineTypes();
            Mat blackMask = new Mat(SourceImg.Height, SourceImg.Width, MatType.CV_8UC3, Scalar.Black);
            Cv2.FillPoly(blackMask, new OpenCvSharp.Point[][] { position}, Scalar.All(255));
            Cv2.BitwiseAnd(SourceImg, blackMask, KQ);
            var converted = Convert(BitmapConverter.ToBitmap(KQ));
            var saveFileName = "imgcanvas.jpg";
            KQ.SaveImage(saveFileName);

            
            string SourceImgSec = Directory.GetCurrentDirectory() + "\\" + saveFileName;

            //SourceImgSec = Cv2.ImRead(Directory.GetCurrentDirectory() + "\\" + saveFileName);
            //ImgScreen.Source = converted;
            // Cv2.FillPoly()

            //double[] positionArray = positionMousesInImgSoucre.ToArray();
            
            //Mat MatThreshold = new Mat();
            //Mat gray = new Mat();
            //Mat maskSecmentMat = new Mat(SourceImg.Height, SourceImg.Width, MatType.CV_8UC3, Scalar.Black);
            //RenderTargetBitmap renderBitmap = new RenderTargetBitmap((int)Canvas_On_ImgScreen.ActualWidth, (int)Canvas_On_ImgScreen.ActualHeight, 96d, 96d, System.Windows.Media.PixelFormats.Pbgra32);
            //renderBitmap.Render(ImgScreen_Canvas);

            //BitmapEncoder encoder = new PngBitmapEncoder(); 
            //encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
            //var saveFileName = "imgcanvas.jpg";
            //encoder.Save(new FileStream(Directory.GetCurrentDirectory() + "\\" + saveFileName, FileMode.Create));

            //SourceImgSec = Cv2.ImRead(Directory.GetCurrentDirectory() + "\\" + saveFileName);

      

            //Point[] scaledpoints = new Point[positionMouses.Count()];
            




            //Polygon scaledPolygon = new Polygon(scaledpoints);

            //Mat mat = new Mat(SourceImg.Height, SourceImg.Width, DepthType.Cv8U, 1);
            //mat.SetTo(new MCvScalar(255)); // Đặt tất cả các điểm ảnh màu trắng
            //Point[][] contours = new Point[1][] { scaledPolygon.Points };
            //CvInvoke.FillPoly(mat, contours, new MCvScalar(0)); //



            //Cv2.ImShow("dssd", SourceImgSec);
            //Cv2.CvtColor(SourceImgSec, gray, ColorConversionCodes.BGR2GRAY);

            //Cv2.Threshold(gray, MatThreshold, Thresh_Slider.Value, 255, ThresholdTypes.Binary);
            //Cv2.ImShow("my img", MatThreshold);

            //OpenCvSharp.Point[][] contours;
            //HierarchyIndex[] hierarchy;

            //Cv2.FindContours(MatThreshold, out contours, out hierarchy, RetrievalModes.List, ContourApproximationModes.ApproxNone);
            //Cv2.ImShow("fdbfjs",MatThreshold);
            //Mat mask = new Mat(SourceImgSec.Size(), MatType.CV_8UC1, 0);
            //Cv2.FillPoly(mask, contours, Scalar.All(255));

            //Mat result = new Mat();
            //SourceImg.CopyTo(result, mask);
            //Cv2.ImShow("Result", result);




        }

    }
}
