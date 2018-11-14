using System;
using Google.Protobuf;

namespace Core {
    public class RpcEncoder : IMessagePipeSection {
        public void Through(ChannelContext context, object argument) {
            var tuple = argument as Tuple<int, IMessage>;
            var session = tuple.Item1;
            var message = tuple.Item2;

            var length = message.CalculateSize() + 2;
            var buffer = new byte[length];

            using (var stream = new CodedOutputStream(buffer)) {
                stream.WriteInt32(session);
                message.WriteTo(stream);
                context.FireNext(buffer);
            }
        }
    }
}