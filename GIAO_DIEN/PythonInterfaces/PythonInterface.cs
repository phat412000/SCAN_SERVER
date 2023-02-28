using OpenCvSharp;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Pythonzxrr
{
    public class PythonInterface
    {

        public string pythonPath { get; private set; }
        public string executablePath { get; private set; }


        private Process pythonProcess;

        NamedPipeServerStream pipeServerStream;



        public PythonInterface(string pythonPath = "python.exe", string executablePath = "-u python_source\\server.py")
        {
            this.pythonPath = pythonPath;
            this.executablePath = executablePath;

            

       
        }


        private string readingMessageFromPython()
        {
            StringBuilder messageBuilder = new StringBuilder();
            string messageChunk = string.Empty;
            byte[] messageBuffer = new byte[20];
            do
            {
                pipeServerStream.Read(messageBuffer, 0, messageBuffer.Length);
                messageChunk = Encoding.UTF8.GetString(messageBuffer);
                messageBuilder.Append(messageChunk);
                messageBuffer = new byte[messageBuffer.Length];
            }
            while (!pipeServerStream.IsMessageComplete);

            var pythonMessage = messageBuilder.ToString();

            var removeStart = pythonMessage.Split(new String[] { "START" }, StringSplitOptions.RemoveEmptyEntries);//start_URL_end
            var outputData = removeStart[0].Split(new String[] { "END" }, StringSplitOptions.RemoveEmptyEntries)[0];//start_URL_end

            return outputData;   
        }

        public Mat SendCommand(string command, Mat image = null)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(command);
                pipeServerStream.Write(stream.ToArray(), 0, stream.ToArray().Length);
            }

            var pythonMessage = readingMessageFromPython();

            Mat imageReturn = Cv2.ImRead(pythonMessage);

            return imageReturn;
        }



        public void Connect()
        {
            pipeServerStream = new NamedPipeServerStream("process_pipe", PipeDirection.InOut, 1, PipeTransmissionMode.Message);
            pipeServerStream.WaitForConnection();

            pipeServerStream.ReadMode = PipeTransmissionMode.Message;


        }

     
    }
}