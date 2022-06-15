using System;
using System.Threading;
using System.Collections.Generic;
using CUtil;

namespace Core {
    public class ActorSystem {
        public const int PRESERVE = 1000;

        private int actorid = PRESERVE;
        private Dictionary<int, IActorMailbox> mailboxes = new Dictionary<int, IActorMailbox>();

        public void Start() { }

        public int NextActorid() {
            int nextid = Interlocked.Increment(ref actorid);
            return nextid;
        }

        public ActorContext Launch<T>() where T : IActor {
            IActor actor = null;
            try {
                actor = Activator.CreateInstance<T>();
            }
            catch (Exception ex) {
                CLogger.Log("Launch actor<{0}> failure, exception:{1}", typeof(T), ex.Message);
                return null;
            }
            return Launch(NextActorid(), actor);
        }

        public ActorContext Launch(int id, IActor actor) {
            ActorContext context = CreateContext(id, actor);
            ActorMailbox mailbox = new ActorMailbox(context);
            addMailbox(mailbox);
            actor.Start();

            return context;
        }

        public ActorContext CreateContext(int id, IActor actor) {
            ActorContext context = new ActorContext(id, actor, this);
            actor.Context = context;
            return context;
        }

        // You should deal with accidents by yourself
        // TODO: try to find a better strategy to solve EXIT
        public void Exit(int id) {
            lock(mailboxes) {
                CAssert.Assert(mailboxes.ContainsKey(id));
                mailboxes.Remove(id);
            }
        }

        public void Send(ActorMessage msg) {
            IActorMailbox mailbox = null;
            lock (mailboxes) {
                mailboxes.TryGetValue(msg.Target, out mailbox);
            }
            CAssert.Assert(mailbox != null, "Send Target:" + msg.Target);
            mailbox.Post(msg);
        }

        public Timer Delay(int target, int period, Action callback) {
            ActorMessage msg = new ActorMessage(ActorMessage.TIMER, 0, target, target, "None", callback);
            Timer timer = new Timer(_ => {
                Send(msg);
            }, null, period, period);
            return timer;
        }

        private void addMailbox(IActorMailbox mailbox) {
            int id = mailbox.Id;
            lock (mailboxes) {
                CAssert.Assert(!mailboxes.ContainsKey(id));
                mailboxes.Add(id, mailbox);
            }
        }
    }
}
