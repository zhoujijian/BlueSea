using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using CUtil;

namespace Core {
    public interface IActorMailbox {
        int Id { get; }
        int MessagesCount { get; }

        void Post(ActorMessage message);
        void Clear();
    }

    public class ActorMailbox : IActorMailbox {
        private ActorContext context;
        private Queue<ActorMessage> messages = new Queue<ActorMessage>();

        public int Id { get { return context.ID; } }
        public int MessagesCount { get { lock (messages) { return messages.Count; } } }

        public ActorMailbox(ActorContext context) {
            this.context = context;
        }

        public void Post(ActorMessage msg) {
            lock(messages) {
                messages.Enqueue(msg);
                if (messages.Count > 1) { return; }
            }
            run(msg);
        }

        public void Clear() {
            lock(messages) {
                messages.Clear();
            }
        }

        private void run(ActorMessage msg) {
            Task.Run(() => {
                try {
                    context.RecvCall(msg);
                } catch (Exception ex) {
                    CLogger.Log("Task ({0}) Exception: {1} StackTrace: {2}", Task.CurrentId, ex.Message, ex.StackTrace);
                }

                ActorMessage next = null;
                lock (messages) {
                    messages.Dequeue();
                    if (messages.Count > 0) {
                        if (messages.Count >= 128) {
                            CLogger.Log("------> CAUTION <------ messages count ({0}) out range", messages.Count);
                        }
                        next = messages.Peek();
                    }
                }
                if (next != null) {
                    run(next);
                }
            }).ContinueWith(prevTask => {
                if (prevTask.Exception != null) {
                    CLogger.Log("Task ({0}) Exception: {1} StackTrace: {2}",
                                prevTask.Id, prevTask.Exception.Message, prevTask.Exception.StackTrace);
                    foreach (Exception e in prevTask.Exception.InnerExceptions) {
                        CLogger.Log("    " + e.Message);
                    }
                }
            });
        }
    }
}
