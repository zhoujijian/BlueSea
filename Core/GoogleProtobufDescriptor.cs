using Google.Protobuf;

namespace Core {
    public class GoogleProtobufDescriptor {
        public string Service { get; }
        public string Method  { get; }
        public IMessage Argument { get; }

        public GoogleProtobufDescriptor(string service, string method, IMessage argument) {
            Service = service;
            Method = method;
            Argument = argument;
        }
    }
}