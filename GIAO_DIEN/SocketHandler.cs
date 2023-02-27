using OpenCvSharp;
using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace GIAO_DIEN
{
    public class SocketHandler
    {
        private SemaphoreSlim mutex = new SemaphoreSlim(1);

        private ClientWebSocket socket;
        public string threshslidervalue;

        public SocketHandler()
        {
        }

        public async void Connect()
        {
            CancellationTokenSource source = new CancellationTokenSource();
            socket = new ClientWebSocket();
            await socket.ConnectAsync(new Uri("ws://localhost:8001"), CancellationToken.None);

        }


        private async Task<bool> IsCloseMessage(WebSocketReceiveResult webSocketReceiveResult)
        {
            if (webSocketReceiveResult.MessageType == WebSocketMessageType.Close)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                return true;
            }

            return false;
        }


        public async Task<byte[]> WaitingForData()
        {

            byte[] buffer = new byte[8192];
            var bufferSegment = new ArraySegment<byte>(buffer);

            WebSocketReceiveResult result = null;

            using (MemoryStream memoryStream = new MemoryStream())
            {
                do
                {

                    result = await socket.ReceiveAsync(bufferSegment, CancellationToken.None);
                    memoryStream.Write(bufferSegment.Array, bufferSegment.Offset, result.Count);

                }
                while (!result.EndOfMessage);

                var concatedBytesData = memoryStream.ToArray();
                return concatedBytesData;
            }

            return null;
        }

        public async void SendCountCmd(byte[] imgbyte)
        {
            await SendMessage("cmdcount");
            await SendImage(imgbyte);
            var data = await WaitingForData();
        }

        public async Task<Mat> SendImgCmd(byte[] imgbyte)
        {
            await SendMessage("cmdimg");
            await SendImage(imgbyte);

            var bytesData = await WaitingForData();

            Mat imagefinal = Converter.bytesToMat(bytesData);
            return imagefinal;


        }
        public async Task<Mat> SendValueSensSliderCmd(ImageSource imageSource, string threshslidervalue, string sensslidervalue)
        {
            await mutex.WaitAsync();
            Console.WriteLine("start sending value sens");
            Mat imageSens = null;

            try
            {
                var imgbyte = Converter.ImageSourceToBytes(imageSource);
                await SendMessage("cmdsens_" +threshslidervalue + "_" + sensslidervalue);
                await SendImage(imgbyte);
                var SensData = await WaitingForData();
                imageSens = Converter.bytesToMat(SensData);
            }
            catch
            {

            }
            finally
            {
                mutex.Release();
            }
            return imageSens;
        }

        
        //public async Task<Mat> SendValueThreshSliderCmd(ImageSource imageSource, string threshslidervalue)
        //{
        //    await mutex.WaitAsync();

        //    Console.WriteLine("start sending value thresh");

        //    Mat imageThresh = null;
        //    try
        //    {
        //        var imgbyte = Converter.ImageSourceToBytes(imageSource);

        //        await SendMessage("cmdthresh_" + threshslidervalue);
        //        await SendImage(imgbyte);

        //        var threshData = await WaitingForData();

        //        imageThresh = Converter.bytesToMat(threshData);

        //    }
        //    catch
        //    {
                
        //    }
        //    finally
        //    {
        //        mutex.Release();
        //    }

        //    return imageThresh;

        //}


        public async Task<bool> SendMessage(string text )
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            var texts = new ArraySegment<byte>(bytes);
            await socket.SendAsync(texts, WebSocketMessageType.Text, true, CancellationToken.None);

            return true;
        }

        public async Task<bool> SendImage(byte[] imageBytes)
        {
            var imageSegment = new ArraySegment<byte>(imageBytes);

            await socket.SendAsync(imageSegment, WebSocketMessageType.Binary, true, CancellationToken.None);

            return true;
        }

    }
}