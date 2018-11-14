using System;
using Google.Protobuf;

namespace Core {
    public class RpcDecoder : IMessagePipeSection {
        private Func<string, MessageParser> factory;

        public RpcDecoder(Func<string, MessageParser> factory) {
            this.factory = factory;
        }

        public void Through(ChannelContext ctx, object arg) {
            var buffer = arg as byte[];
            var session    = BitConverter.ToInt32(buffer, 0);
            var lServiceId = BitConverter.ToInt16(buffer, 4);
            var lMethodId  = BitConverter.ToInt16(buffer, 6);
            var lArgument  = BitConverter.ToInt16(buffer, 8);

            var offset = 8;
            var strService = BitConverter.ToString(buffer, offset, lServiceId);
            offset += lServiceId;
            var strMethod = BitConverter.ToString(buffer, offset, lMethodId);
            offset += lMethodId;
            var strArgument = BitConverter.ToString(buffer, offset, lArgument);
            offset += lArgument;

            var parser = factory(strArgument);
            var message = parser.ParseFrom(buffer, offset, buffer.Length - offset);
            var descriptor = new GoogleProtobufDescriptor(session, strService, strMethod, message);

            ctx.FireNext(descriptor);
        }
    }
}