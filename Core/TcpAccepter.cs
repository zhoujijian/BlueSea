using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;

namespace Core {
    public class TcpAccepter {
        private int port;

        public TcpAccepter(int port) {
            this.port = port;
        }

        public async void Start() {
            var server = new TcpListener(IPAddress.Any, port);
            server.Start();
            
            while(true) {
                var client = await server.AcceptTcpClientAsync();
                var channel = new TcpChannel(client);
                await Task.Run(() => channel.Run()); // TODO: catch exception
            }
        }
    }
}