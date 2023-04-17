using Newtonsoft.Json;
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
            byte[] messageBuffer = new byte[4098];
            do
            {
                pipeServerStream.Read(messageBuffer, 0, messageBuffer.Length);
                messageChunk = Encoding.UTF8.GetString(messageBuffer);
                messageBuilder.Append(messageChunk);
                messageBuffer = new byte[messageBuffer.Length];
            }
            while (!pipeServerStream.IsMessageComplete);

            //bytes => chuoi

            var pythonMessage = messageBuilder.ToString();

            var removeStart = pythonMessage.Split(new String[] { "$START$" }, StringSplitOptions.RemoveEmptyEntries);//start_URL_end
            var outputData = removeStart[0].Split(new String[] { "$END$" }, StringSplitOptions.RemoveEmptyEntries)[0];//start_URL_end

            return outputData;   
        }
        //command, duong dan anh
        //command, parameter1, parameter2

        //SendCommand("a") => a
        //SendCommand("a","b","C") => [a,b,c]
        //SendCommand("a","b","C",.....) => [a,b,c,....]
        //duong__dan, thong_so_thresh, thong_so_distance
        //$START$duong_dan$$$thong_so_thresh$$$thong_so_distance


        public Mat SendCommand(string command)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(command);
                pipeServerStream.Write(stream.ToArray(), 0, stream.ToArray().Length);
            }

            var pythonMessage = readingMessageFromPython();

            if (pythonMessage == "NO_IMAGE")
            {
                return null;
            }

            Mat imageReturn = Cv2.ImRead(pythonMessage);

            return imageReturn;
        }

        public dynamic SendCommandAndReceiveJson(string command)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(command);
                pipeServerStream.Write(stream.ToArray(), 0, stream.ToArray().Length);
            }

            var pythonMessage = readingMessageFromPython();


            return (dynamic)JsonConvert.DeserializeObject(pythonMessage);
        }

        public string SendCommandAndReceiveRawString(string command)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(command);
                pipeServerStream.Write(stream.ToArray(), 0, stream.ToArray().Length);
            }

            var pythonMessageCenters = readingMessageFromPython();

            Console.WriteLine(pythonMessageCenters);
            return pythonMessageCenters;
        }

        public static string BuildCommand(params string[] commands)
        {
            string commandToSend = "$START$";

            foreach (string command in commands)
            {
                commandToSend += command + "$$$";
            }
            //$START$abc$$$def$END$

            commandToSend = commandToSend.Substring(0, commandToSend.Length - 3) + "$END$";

            return commandToSend;
        }

        public void Connect()
        {
            pipeServerStream = new NamedPipeServerStream("process_pipe", PipeDirection.InOut, 1, PipeTransmissionMode.Message);

            //var processstartinfo = new processstartinfo();

            //processstartinfo.workingdirectory = directory.getcurrentdirectory();
            //processstartinfo.filename = "cmd.exe";


            //processstartinfo.useshellexecute = false;
            //processstartinfo.redirectstandardinput = true;
            //processstartinfo.createnowindow = true;

            //var process = process.start(processstartinfo);

            //process.standardinput.writeline("python_source\\start_server.cmd");
            //process.standardinput.flush();


            pipeServerStream.WaitForConnection();

            pipeServerStream.ReadMode = PipeTransmissionMode.Message;


        }

     
    }
}