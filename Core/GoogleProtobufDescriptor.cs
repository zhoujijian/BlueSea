using Google.Protobuf;

namespace Core {
    public class GoogleProtobufDescriptor {
        public int Session { get; }
        public string Service { get; }
        public string Method  { get; }
        public IMessage Argument { get; }        

        public GoogleProtobufDescriptor(int session, string service, string method, IMessage argument) {
            Session = session;
            Service = service;
            Method = method;
            Argument = argument;
        }
    }
}