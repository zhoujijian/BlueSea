using System;
using System.Threading;
using System.Collections.Concurrent;

using CUtil;

namespace Core {
    public class GameEventDispatcher {
        private int sleepMilliseconds = 10;
        private ConcurrentQueue<GameEvent> events = new ConcurrentQueue<GameEvent>();
        private ConcurrentDictionary<string, IService> services = new ConcurrentDictionary<string, IService>();

        public void Start() {
            var thread = new Thread(() => {
                while (true) {
                    GameEvent evt;
                    while (events.TryDequeue(out evt)) {
                        IService service = null;
                        if (!services.TryGetValue(evt.Target, out service)) {
                            CLogger.Log("[GameEventDispatcher](Start)target service not found:" + evt.Target);
                            return;
                        }
                        service.OnGameEvent(evt);
                    }
                    Thread.Sleep(sleepMilliseconds);
                }
            });
            thread.Start();
        }

        public void RegisterSerivce(string name, IService service) {
            if (!services.TryAdd(name, service)) {
                throw new InvalidOperationException("service existed already:" + name);
            }
            service.OnStart();
        }

        public void UnregisterService(string name) {
            IService service = null;
            if (!services.TryRemove(name, out service)) {
                throw new InvalidOperationException("service not existed:" + name);
            }
            service.OnDestroy();
        }
    }
}