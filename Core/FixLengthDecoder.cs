using System;

namespace Core {
    public interface IDecoder {
        void Decode(ChannelContext context, object argument);
    }

    public class FixLengthDecoder : IDecoder {
        private class Block {
            public int length = 0;
            public byte[] buffer = new byte[1024];
        }

        private Block block = new Block();
        private static readonly int HeaderSize = 2;

        public void Decode(ChannelContext context, object argument) {
            var buffer = argument as byte[]; // TODO: use ArraySegment
            var offset = 0;

            while (offset < buffer.Length) {
                if (block.length < HeaderSize) {
                    var copyLength = HeaderSize - block.length;
                    Buffer.BlockCopy(buffer, offset, block.buffer, block.length, copyLength);
                    block.length += copyLength;
                }

                if (block.length >= HeaderSize) {
                    var bodyLength = BitConverter.ToUInt16(block.buffer, 0);
                    var copyLength = bodyLength < buffer.Length - offset ? bodyLength : buffer.Length - offset;
                    Buffer.BlockCopy(buffer, offset, block.buffer, block.length, copyLength);
                    offset += copyLength;
                    block.length += copyLength;

                    if (copyLength >= bodyLength) {
                        var delivery = new byte[bodyLength];
                        Buffer.BlockCopy(block.buffer, HeaderSize, delivery, 0, bodyLength);
                        context.FireNext(delivery);
                        block.length = 0;
                    }
                }
            }
        }
    }
}