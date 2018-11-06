using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace Core {
    internal class TcpChannel {
        private TcpClient client;
        private byte[] buffer = new byte[1024];

        public TcpChannel(TcpClient client) {
            this.client = client;
        }

        public async void Run() {
            var stream = client.GetStream();
            while(true) {
                var read = await stream.ReadAsync(buffer, 0, buffer.Length);
            }
        }
    }
}