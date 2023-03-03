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
using System.IO.Ports;
using System.Windows.Threading;

namespace GIAO_DIEN
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class UserControl1 : UserControl
    {
        MainWindow m;
        public event FireEventForScanSuccess FireEventForScanSuccess = null;
        public UserControl1()
        {
            InitializeComponent();
            label1.IsEnabled = false;
            label2.IsEnabled = false;
            BarCodeText.IsEnabled = false;
            ComboBoxCOM.IsEnabled = false;
            NextBtn.IsEnabled = false;
        }
        SerialPort _serialPort = new SerialPort(); // Create a new SerialPort object with default settings
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }



        private void Find_Device_Click(object sender, RoutedEventArgs e)
        {
            ComboBoxCOM.Items.Clear();
            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {
                ComboBoxCOM.Items.Add(port);
            }
            if (ports.Any())
            {
                Scannerstatus.Content = "Scanner found.";
                Scannerstatus.Foreground = Brushes.Green;
                label1.IsEnabled = true;
                ComboBoxCOM.IsEnabled = true;

            }
            else
            {
                Scannerstatus.Content = "No device available";
                Scannerstatus.Foreground = Brushes.Red;
                label1.IsEnabled = false;
                ComboBoxCOM.IsEnabled = false;
            }
        }
        string get_code;
        private delegate void UpdateUiTextDelegate(string text);
        private void sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            get_code = sp.ReadExisting();
            //txtBlock.Text = get_code;
            Dispatcher.Invoke(DispatcherPriority.Send, new UpdateUiTextDelegate(DataWrited), get_code);
        }
        private void DataWrited(string text)
        {
            BarCodeText.Text = String.Empty;
            BarCodeText.Text += text.Trim();
        }

        private void ComboBoxCOM_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboBoxCOM.SelectedItem.ToString().Length > 2)
            {
                _serialPort.PortName = ComboBoxCOM.SelectedItem.ToString();
                _serialPort.BaudRate = 9600;
                _serialPort.Parity = Parity.None;
                _serialPort.StopBits = StopBits.One;
                _serialPort.DataBits = 8;
                _serialPort.Handshake = Handshake.None;
                _serialPort.RtsEnable = true;
                _serialPort.Open();
                Scannerstatus.Content = "Scanner has been connected.\nYou can scan your barcode right now";
                label2.IsEnabled = true;
                BarCodeText.IsEnabled = true;
                if (!_serialPort.IsOpen)
                {
                    Scannerstatus.Content = "Not opened port yet";
                    _serialPort.Open();
                    label2.IsEnabled = false;
                    BarCodeText.IsEnabled = false;
                }
                _serialPort.DataReceived += new SerialDataReceivedEventHandler(sp_DataReceived);
            }

        }

        private void BarCodeText_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {

        }

        private void NextBtn_Click(object sender, RoutedEventArgs e)
        {
            WindowStart w = new WindowStart(DateTime.Now.ToString(), BarCodeText.Text);
            w.Visibility = Visibility.Visible;

            // m.Visibility = Visibility.Hidden;

            this.Visibility = Visibility.Hidden;
            ////GloblalV.datetime = DateTime.Now;
            //Console.WriteLine(GloblalV.datetime);
            //if (FireEventForScanSuccess != null)
            //{
            // FireEventForScanSuccess.Invoke(this, new ScanSuccess { Barcode = BarCodeText.Text, DateTime = DateTime.Now.ToString() });
            //}
        }

        private void BarCodeText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (BarCodeText.Text.Length >= 5)
            {
                NextBtn.IsEnabled = true;
            }
        }
    }
}
