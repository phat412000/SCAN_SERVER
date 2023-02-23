using OpenCvSharp;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GIAO_DIEN
{
    public class SocketHandler
    {
        
        private ClientWebSocket socket;

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


        public async Task<SocketHandlerResult> WaitingForData()
        {
            int buffterMaxSize = (int)Math.Pow(2, 26);
            byte[] buffer = new byte[buffterMaxSize];
            
            
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                var isCloseMessage = await IsCloseMessage(result);

                if (isCloseMessage)
                {
                    Console.WriteLine("websocket closed");
                    return null;
                }
                
            
            return new SocketHandlerResult(buffer, result.Count);
        }

        public async void SendCountCmd()
        {
            SendMessage("cmdcount");
           SocketHandlerResult data = await WaitingForData();
        }

        public async Task<Mat> SendImgCmd(byte[] imgbyte)
        {
            SendMessage("cmdimg");
            SendImage(imgbyte);

            SocketHandlerResult result = await WaitingForData();

            Mat imagefinal = Converter.bytesToMat(result.buffer, result.lenght);
            return imagefinal;
        }


        public async void SendMessage(string text )
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            var texts = new ArraySegment<byte>(bytes);
            await socket.SendAsync(texts, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public async void SendImage(byte[] imageBytes)
        {
            var imageSegment = new ArraySegment<byte>(imageBytes);

            await socket.SendAsync(imageSegment, WebSocketMessageType.Binary, true, CancellationToken.None);
        }


        private void HandleMessage(byte[] buffer, int count)
        {
            Console.WriteLine($"Received {BitConverter.ToString(buffer, 0, count)}");
        }
    }
}