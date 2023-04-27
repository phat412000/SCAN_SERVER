using System;
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
using GLORY_TO_GOD.backActionChild;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GLORY_TO_GOD
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class WindowStart : Window

    {
        double scaleCanvasBe = 6.5;
        double scaleCanvasNow = 3.5;

        JArray bacteriaCentersJarray;

        int ButtonFile_Click_Mode = 0;
        string SelectImgPath;
        Mat SourceImg;
        Mat ImgAfterAddMask;
        int ZoomInRatio;
        int ZoomOutRatio;
        bool AutoMode = false;
        bool ManualMode = false;

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


        bool tempConfirm = true;
        bool tempSend = true;
        


        enum State
        {
            IDLE,
            EDIT,
            SEND_SEGMENT_MODE,
            SEND_AUTO_MODE,
            SEGMENT,
            ADD_SEGMENT
        }
        enum State_State
        {
            
        }

        
        

        State applicationState = State.IDLE;


        





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
//----------------------------------------------------------------- STATE --------------------------------------------------------------------------------
            EditBtn.IsEnabled         = false;
            CountBtn.IsEnabled        = false;
            DeleteBtn.IsEnabled       = false;
            Add_bacteriaBtn.IsEnabled = false;
            SendCropImgBtn.IsEnabled  = false;
            AddSegmentBtn.IsEnabled   = false;
            CountBtn.IsEnabled        = false;

//----------------------------------------------------------------- STATE --------------------------------------------------------------------------------






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

                ImgScreen.Width = SourceImg.Width / scaleCanvasNow;
                ImgScreen.Height = SourceImg.Height / scaleCanvasNow;
                Canvas_On_ImgScreen.Width = SourceImg.Width / scaleCanvasNow;
                Canvas_On_ImgScreen.Height = SourceImg.Height / scaleCanvasNow;

                ImgScreen.Source = bitmap;

            }

            ButtonFile_canvas.Visibility = Visibility.Hidden;
            ButtonFile_Click_Mode = 0;
            FileButton.Background = null;
            SolidColorBrush Foreground_color = new SolidColorBrush();
            Foreground_color.Color = Colors.White;
            FileButton.Foreground = Foreground_color;

            currentImagePath = SelectImgPath;



            var saveFileName = "image_canvas.jpg";
            SourceImg.SaveImage(saveFileName);
            //SourceImgSegment = Directory.GetCurrentDirectory() + "\\" + saveFileName;
            currentImagePath = Directory.GetCurrentDirectory() + "\\" + saveFileName;

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
                Heightsize90 = 3180 / scaleCanvasNow;
                Widthsize90 = 3180 / scaleCanvasNow;
                radiusCircle = 3180;
                size100mm.IsChecked = false;
                size150mm.IsChecked = false;
                size150x300mm.IsChecked = false;
                size200x300mm.IsChecked = false;
                centerCircleX = (SourceImg.Width / 2) / scaleCanvasNow;
                centerCircleY = SourceImg.Height / 2 / scaleCanvasNow;
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
            //var command = PythonInterface.BuildCommand("count");
            //string total = pythonInterface.SendCommandAndReceiveRawString(command);
            //Total_Count_Value.Text = total;


            if (applicationState == State.SEND_AUTO_MODE)
            {
                var statefulPoints = GenerateStatefulPoints();

                string addPointsJson = JsonConvert.SerializeObject(statefulPoints);


                var command = PythonInterface.BuildCommand("count", addPointsJson, currentImagePath);
                string total = pythonInterface.SendCommandAndReceiveRawString(command);
                Total_Count_Value.Text = total;
            }
            if (applicationState == State.SEND_SEGMENT_MODE)
            {

                var statefulPoints = GenerateStatefulPoints();

                string addPointsJson = JsonConvert.SerializeObject(statefulPoints);


                var command = PythonInterface.BuildCommand("count", addPointsJson, SourceImgSegment);
                string total = pythonInterface.SendCommandAndReceiveRawString(command);
                Total_Count_Value.Text = total;
            }
            


        }


        ///********************  BIến bacteriaCenters chứa list center position: polyPoints của Bacterias    ***************************************************************************************

        private void EditBtn_Click(object sender, RoutedEventArgs e)
        {
            applicationState = State.EDIT;

        }


        private List<StatefulPointBackAction> GenerateStatefulPoints()
        {
            List<StatefulPointBackAction> statefulPoints = new List<StatefulPointBackAction>();
            
            foreach (var item in backStack)
            {
                if (item is StatefulPointBackAction)
                {
                    Console.WriteLine(item);
                }
            }


            foreach (var item in backStack.Reverse())
            {
                if (item is StatefulPointBackAction)
                {
                    statefulPoints.Add((StatefulPointBackAction)item);
                }
            }

            return statefulPoints;
        }






        private void SendProcessPoints()
        {

            var statefulPoints = GenerateStatefulPoints();

            string addPointsJson = JsonConvert.SerializeObject(statefulPoints);


            var command = PythonInterface.BuildCommand("processpoint", addPointsJson, currentImagePath);

            dynamic jsonResponse_processpoint = pythonInterface.SendCommandAndReceiveJson(command);

            string total = jsonResponse_processpoint["total"];

            Total_Count_Value.Text = total;

            string imageUrl = jsonResponse_processpoint["image"];

            Mat image = Cv2.ImRead(imageUrl);

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
            Canvas_On_ImgScreen.Children.Clear();


        }







        private void Add_bacteriaBtn_Click(object sender, RoutedEventArgs e)
        {
            if (applicationState == State.EDIT)
            {
                List<BackAction> listBackActions = backStack.ToList();
                for (int i = 0; i < listBackActions.Count; i++)
                {
                    if (listBackActions[i] is StatefulPointBackAction)
                    {
                        StatefulPointBackAction statePoint = (StatefulPointBackAction)backStack.ElementAt(i);

                        if (statePoint.action == "noaction")
                        {
                            statePoint.action = "add";
                        }
                    }
                }
                SendProcessPoints();
            }
            applicationState = State.IDLE;



        }


  





  
        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {

            if (applicationState == State.EDIT)
            {
                List<BackAction> listBackActions = backStack.ToList();
                for (int i = 0; i < listBackActions.Count; i++)
                {
                    if (listBackActions[i] is StatefulPointBackAction)
                    {
                        StatefulPointBackAction statePoint = (StatefulPointBackAction)backStack.ElementAt(i);

                        if (statePoint.action == "noaction")
                        {
                            statePoint.action = "delete";
                        }
                    }                   
                }
                SendProcessPoints();
            }
            applicationState = State.IDLE;
        }









        ///***********************************************************************************************************
        ///

        //private void ConfirmBtn_Click(object sender, RoutedEventArgs e)
        //{


        //    var imageActionInTam = new ImageBackAction();
        //    imageActionInTam.image = ImgScreen.Source as BitmapImage;
        //    backStack.Push(imageActionInTam);





        //    PrintBackStack();
        //    ImgAfterAddMask = new Mat();
        //    if (circleCheck == true)
        //    {
        //        if (ZoomInRatio > 0)
        //        {
        //            Mat blackMask = new Mat(SourceImg.Height, SourceImg.Width, MatType.CV_8UC3, Scalar.Black);
        //            Cv2.Circle(blackMask, (int)(centerCircleX * Math.Pow(1.1, ZoomInRatio)), (int)(centerCircleY * Math.Pow(1.1, ZoomInRatio)), (int)radiusCircle / 2, Scalar.White, -1);
        //            Cv2.BitwiseAnd(SourceImg, blackMask, ImgAfterAddMask);
        //            var converted = Convert(BitmapConverter.ToBitmap(ImgAfterAddMask));
        //            ImgScreen.Source = converted;

        //        }
        //        if (ZoomOutRatio > 0)
        //        {
        //            Mat blackMask = new Mat(SourceImg.Height, SourceImg.Width, MatType.CV_8UC3, Scalar.Black);
        //            Cv2.Circle(blackMask, (int)(centerCircleX * Math.Pow(1 / 1.1, ZoomOutRatio)), (int)(centerCircleY * Math.Pow(1 / 1.1, ZoomOutRatio)), (int)radiusCircle / 2, Scalar.White, -1);
        //            Cv2.BitwiseAnd(SourceImg, blackMask, ImgAfterAddMask);
        //            var converted = Convert(BitmapConverter.ToBitmap(ImgAfterAddMask));
        //            ImgScreen.Source = converted;
        //        }
        //        if (ZoomInRatio == 0)
        //        {
        //            Mat blackMask = new Mat(SourceImg.Height, SourceImg.Width, MatType.CV_8UC3, Scalar.Black);
        //            Cv2.Circle(blackMask, (int)(centerCircleX * scaleCanvasNow), (int)(centerCircleY * scaleCanvasNow), (int)radiusCircle / 2, Scalar.White, -1);
        //            Cv2.BitwiseAnd(SourceImg, blackMask, ImgAfterAddMask);
        //            var converted = Convert(BitmapConverter.ToBitmap(ImgAfterAddMask));
        //            ImgScreen.Source = converted;

        //            size90mm.IsChecked = false;

        //            var saveFileName = "imgcropped.jpg";
        //            Console.WriteLine(saveFileName);
        //            ImgAfterAddMask.SaveImage(
        //
        //
        //
        //
        //            );

        //            currentImagePath = Directory.GetCurrentDirectory() + "\\" + saveFileName;

        //        }

        //    }
        //    if (RectangleCheck == true)
        //    {
        //        Mat blackMask = new Mat(SourceImg.Height, SourceImg.Width, MatType.CV_8UC3, Scalar.Black);
        //        Cv2.Rectangle(blackMask, rectangle, Scalar.White, -1);
        //        Cv2.BitwiseAnd(SourceImg, blackMask, ImgAfterAddMask);
        //        var converted = Convert(BitmapConverter.ToBitmap(ImgAfterAddMask));
        //        ImgScreen.Source = converted;
        //    }

        //    var imageActionInOutside = new ImageBackAction();
        //    imageActionInOutside.image = ImgScreen.Source as BitmapImage;
        //    backStack.Push(imageActionInOutside);


        //}
        //----------------------------------------------------------------------------------------------------------------------------------------
        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            int x = 0;
            Total_Count_Value.Text =  x.ToString();
            backStack.Clear();
            nextStack.Clear();

            Canvas_On_ImgScreen.Children.Clear();

            ImgScreen.Source        = null;

            applicationState        = State.IDLE;

            SegmentBtn.IsEnabled    = true;
            Send_AutoBtn.IsEnabled  = true;
            AddSegmentBtn.IsEnabled = false;




        }
        //--------------------------------------------------------------------------------------------------------------------------------------






        private void Send_AutoBtn_Click(object sender, RoutedEventArgs e)
        {

            //-----------------------------------------------------------STATE----------------------------------------------------------------------------
            EditBtn.IsEnabled = true;
            CountBtn.IsEnabled = true;
            Add_bacteriaBtn.IsEnabled = true;
            DeleteBtn.IsEnabled = true;
            SegmentBtn.IsEnabled = false;
            AddSegmentBtn.IsEnabled = false;


            //-----------------------------------------------------------STATE--------------------------------------------------------------------------


            applicationState = State.SEND_AUTO_MODE;

            if (applicationState == State.SEND_AUTO_MODE)
            {
                List<(int x, int y)> polyPointList = new List<(int x, int y)>();


                polyPointList.Add((0, 0));
                polyPointList.Add((0, (int)Canvas_On_ImgScreen.Height));
                polyPointList.Add(((int)Canvas_On_ImgScreen.Height, (int)Canvas_On_ImgScreen.Width));
                polyPointList.Add(((int)Canvas_On_ImgScreen.Width, 0));


                string polyListJson = JsonConvert.SerializeObject(polyPointList);


                var command = PythonInterface.BuildCommand("send_auto_mode", currentImagePath, polyListJson);
                dynamic jsonResponse = pythonInterface.SendCommandAndReceiveJson(command);

                bacteriaCentersJarray = jsonResponse["centers"];

                string total = jsonResponse["total"];

                Total_Count_Value.Text = total;

                string imageUrl = jsonResponse["image"];

                Mat image = Cv2.ImRead(imageUrl);

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

        //---------------------------------------------------------------------------------------------------------------


        /// <summary>
        /// HSV CONTROL MANUAL, GRAYSCALE, THRESHOLD _______________________________________________________________


        //-------------------------------------------------------------------------------------------------------------------------------------- 
        private void PrintBackStack()
        {
            foreach (var data in backStack)
            {
                Console.WriteLine(data);
            }
            Console.WriteLine("================");
        }
        
 //-----------------------------------------------------------------------------------------------------------------------       

        private void Back_Click(object sender, RoutedEventArgs e)
        {
  
            if (backStack.Count() > 0)
            {

                nextStack.Push(backStack.Peek());
                Console.WriteLine(">>>>>>>>>>>>>>>>");
                foreach (var item in nextStack)
                    Console.WriteLine(item);

                backStack.Pop(); 

                if (backStack.Count() != 0)
                {

                    BackAction dataPo = backStack.Peek();

                    if (dataPo is ImageBackAction)
                    {

                        var imageAction = (ImageBackAction)dataPo;
                        ImgScreen.Source = imageAction.image;


                    }
                    else if (dataPo is PolyBackAction)
                    {
                        var polyAction = (PolyBackAction)dataPo;

                        SelectTopPolyToDraw();
                        DrawPolies();
                    }
                    else if (dataPo is StatefulPointBackAction)
                    {
                        DeletePolylineAndRectangle();

                        foreach (var item in backStack)
                        {
                            if (item is StatefulPointBackAction)
                            {
                                var unstatePoint = (StatefulPointBackAction)item;
                                DrawStatefulPoints(unstatePoint);
                            }
                        }
                    }
        
                }
                else
                {
                    DeletePolylineAndRectangle();
                    MessageBox.Show("stack empty");
                    
                }

            }

            PrintBackStack();
        }



        private void Next_Click(object sender, RoutedEventArgs e)
        {

            if (nextStack.Count() > 0)
            {
                
                BackAction popData = nextStack.Pop();

                backStack.Push(popData);

                if (popData is ImageBackAction)
                {

                    ImageBackAction imageBackAction = (ImageBackAction)popData;
                    ImgScreen.Source = Convert(BitmapConverter.ToBitmap(ImgAfterAddMask));

                }

                else if (popData is PolyBackAction)
                {
                    DrawPolies();
                }
                else if (popData is StatefulPointBackAction)
                {
                    DeletePolylineAndRectangle();

                    foreach (var item in backStack)
                    {
                        if (item is StatefulPointBackAction)
                        {
                            var unstatePoint = (StatefulPointBackAction)item;
                            DrawStatefulPoints(unstatePoint);
                        }
                    }
                }


            }
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
            if (command.Contains("segment"))
            {
                image.SaveImage(AppConstraint.SEGMENT_OUTPUT_IMAGE);
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




        Mat ImgAfterSegment;
        private void SendCropImgBtn_Click(object sender, RoutedEventArgs e)
        {
//-------------------------------------------------------STATE------------------------------------------------------------------------
            EditBtn.IsEnabled         = true;
            CountBtn.IsEnabled        = true;
            Add_bacteriaBtn.IsEnabled = true;
            DeleteBtn.IsEnabled       = true;
            SegmentBtn.IsEnabled      = false;
            AddSegmentBtn.IsEnabled   = false;
            Send_AutoBtn.IsEnabled    = false;


//--------------------------------------------------------STATE--------------------------------------------------------------------------



            applicationState = State.SEND_SEGMENT_MODE;

            if (applicationState == State.SEND_SEGMENT_MODE)
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

                var saveFileName = "image_canvas.jpg";
                ImgAfterSegment.SaveImage(saveFileName);
                //SourceImgSegment = Directory.GetCurrentDirectory() + "\\" + saveFileName;
                currentImagePath = Directory.GetCurrentDirectory() + "\\" + saveFileName;

                DeletePolylineAndRectangle();

                List<BackAction> polyPointList = new List<BackAction>();

                foreach (BackAction backAction in backStack)
                {
                    if (backAction is PolyBackAction)
                    {
                        polyPointList.Add(backAction);
                    }
                }

                string jsonCommand = JsonConvert.SerializeObject(polyPointList);

                //---------
                var command = PythonInterface.BuildCommand("segment", currentImagePath, jsonCommand);

                dynamic jsonResponse_segment = pythonInterface.SendCommandAndReceiveJson(command);

                string total = jsonResponse_segment["total"];

                Total_Count_Value.Text = total;

                string imageUrl = jsonResponse_segment["image"];

                Mat image = Cv2.ImRead(imageUrl);

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
                                    var faceSize = new OpenCvSharp.Size(rtnMat.Width / scaleCanvasNow, rtnMat.Height / scaleCanvasNow);
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

            applicationState = State.SEGMENT;
            AddSegmentBtn.IsEnabled  = true;
            SendCropImgBtn.IsEnabled = true;
            Send_AutoBtn.IsEnabled   = false;

            
            
        }



        int positionMouseX;
        int positionMouseY;


        private void TopCanvas_MouseMove(object sender, MouseEventArgs e)
        {

            positionMouseX = (int)e.GetPosition(ImgScreen).X;
            positionMouseY = (int)e.GetPosition(ImgScreen).Y;

            teets.Text = "X: " + positionMouseX + "," + "Y: " + positionMouseY;


        }




        int currentPolyName = 1;

        private void TopCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            List<PositionMouse> polyPoints = new List<PositionMouse>();

            if (applicationState == State.SEGMENT || applicationState == State.ADD_SEGMENT)
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

            

            if (applicationState == State.EDIT)
            {
                StatefulPointBackAction unstatepoint = new StatefulPointBackAction();

                unstatepoint.mouseX = positionMouseX;
                unstatepoint.mouseY = positionMouseY;


                backStack.Push(unstatepoint);

                PositionMouse positionMouse = new PositionMouse(positionMouseX, positionMouseY);

                DrawStatefulPoints(unstatepoint);
            }

                      




        }
        private void DrawStatefulPoints(StatefulPointBackAction statefulPointBackAction)
        {
            SolidColorBrush color = Brushes.Red;

            if (statefulPointBackAction.action == "delete")
            {
                color = Brushes.Red;
            }else
            if (statefulPointBackAction.action == "add")
            {
                color = Brushes.Green;
            }
            else
            {
                color = Brushes.Yellow;
            }

            Ellipse smallDot = new Ellipse()
            {
                Fill = color,
                Width = 9,
                Height = 9
            };
            Canvas.SetLeft(smallDot, statefulPointBackAction.mouseX);
            Canvas.SetTop(smallDot, statefulPointBackAction.mouseY);
            Canvas_On_ImgScreen.Children.Add(smallDot);
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
                if (item is Polyline || item is Rectangle || item is Ellipse)
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

            double scaleRatio = scaleCanvasNow;
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


        private void AddSegmentBtn_Click(object sender, RoutedEventArgs e)
        {
            applicationState = State.ADD_SEGMENT;

            if (applicationState == State.ADD_SEGMENT)
            {
                currentPolyName++;
            }
    
        }

    }
}
