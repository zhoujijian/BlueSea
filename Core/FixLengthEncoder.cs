using System.IO;

namespace Core {
    public class FixLengthEncoder : IMessagePipeSection {
        public void Through(ChannelContext context, object argument) {
            var buffer1 = argument as byte[];
            byte[] buffer2 = null;

            using (var stream = new MemoryStream()) {
                using (var writer = new BinaryWriter(stream)) {
                    writer.Write((short)buffer1.Length);
                    writer.Write(buffer1);
                    buffer2 = stream.GetBuffer();
                }
            }

            context.Channel.Send(buffer2);
        }
    }
}