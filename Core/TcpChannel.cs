using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using CUtil;

namespace Core {
    public class TcpChannel {
        private readonly int MaxRecvSize;

        private TcpClient client;
        private byte[] buffer;
        private ChannelContext context;

        public TcpChannel(TcpClient client, int maxRecvSize) {
            this.client = client;
            this.MaxRecvSize = maxRecvSize;
            this.context = new ChannelContext(this);
            this.buffer = new byte[MaxRecvSize];
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

        public void Send(byte[] buffer) {
            // TODO: not thread safe
            Task.Factory.StartNew(() => {
                client.GetStream().Write(buffer, 0, buffer.Length);
            });
        }
    }
}