using System;
using System.Net.Sockets;
using CUtil;

namespace Core {
    internal class TcpChannel {
        private TcpClient client;
        private byte[] buffer = new byte[1024];
        private ChannelContext context;

        public TcpChannel(TcpClient client) {
            this.client = client;
            this.context = new ChannelContext(this);
        }

        public async void Run() {
            var stream = client.GetStream();
            while(true) {
                var read = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (read <= 0) {                    
                    CLogger.Log("[TcpChannel](Run)remote endpoint finished sending");
                    context.FireTerminated();
                    break;
                }

                byte[] arr = new byte[read];
                Array.Copy(buffer, 0, arr, 0, read);
                context.FireStart(arr);
            }
        }
    }
}