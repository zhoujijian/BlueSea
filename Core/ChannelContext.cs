namespace Core {
    public class ChannelContext {
        private class PipelineSegment {
            public IMessagePipeSection section;
            public PipelineSegment next;
        }

        private PipelineSegment current;
        private PipelineSegment head;
        private TcpChannel channel;

        public ChannelContext Reverse { get; private set; }
        public TcpChannel Channel { get { return channel; } }

        internal ChannelContext(TcpChannel channel) {
            this.channel = channel;
        }

        public void FireStart(object parameter) {
            current = head;
            if (current != null) {
                current.section.Through(this, parameter);
            }
        }

        public void FireNext(object parameter) {            
            if (current != null) {
                current = current.next;
            }
            if (current != null) {
                current.section.Through(this, parameter);
            }
        }

        public void FireTerminated() { }

        public void AddNext(IMessagePipeSection next) {
            if (head == null) {
                head = new PipelineSegment { section = next };
                return;
            }

            var segment = head;
            while (segment.next != null) {
                segment = segment.next;
            }
            segment.next = new PipelineSegment { section = next };
        }
    }
}