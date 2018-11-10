namespace Core {
    internal class PipelineSegment {
        public IDecoder Decoder { get; set; }
        public PipelineSegment Next { get; set; }
    }

    public class ChannelContext {
        private PipelineSegment current;
        private PipelineSegment head;
        private TcpChannel channel;

        internal ChannelContext(TcpChannel channel) {
            this.channel = channel;
        }

        public void FireStart(object parameter) {
            current = head;
            if (current != null) {
                current.Decoder.Decode(this, parameter);
            }
        }

        public void FireNext(object parameter) {            
            if (current != null) {
                current = current.Next;
            }
            if (current != null) {
                current.Decoder.Decode(this, parameter);
            }
        }

        public void FireTerminated() { }

        public void AddNext(IDecoder next) {
            if (head == null) {
                head = new PipelineSegment { Decoder = next };
                return;
            }

            var segment = head;
            while (segment.Next != null) {
                segment = segment.Next;
            }
            segment.Next = new PipelineSegment { Decoder = next };
        }
    }
}