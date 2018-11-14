using System;
using System.Reflection;
using System.Collections.Generic;

using Google.Protobuf;
using CUtil;

namespace Core {
    public class RpcService : ServiceAdapter {
        public class RpcCallback {
            private ChannelContext context;
            private int session;

            public RpcCallback(ChannelContext context, int session) {
                this.context = context;
                this.session = session;
            }

            public void Return(IMessage response) {
                context.FireStart(new Tuple<int, IMessage>(session, response));
            }
        }

        private Dictionary<string, object> invokers = new Dictionary<string, object>();
        private Dictionary<ChannelContext, ChannelEntity> entities = new Dictionary<ChannelContext, ChannelEntity>();

        public override void OnGameEvent(GameEvent evt) {
            switch (evt.EventKind) {
                case GameEvent.Kind.ClientEstablished: { OnClientEstablished(evt.Context); break; }
                case GameEvent.Kind.ClientRecvData:    { OnClientRecvData(evt.Context, evt.Argument); break; }
                case GameEvent.Kind.ClientTerminated:  { OnClientTerminated(evt.Context); break; }
                default: { break; }
            }
        }

        protected virtual void OnClientEstablished(ChannelContext context) {
            CAssert.Assert(!entities.ContainsKey(context));
            var entity = new ChannelEntity();
            entities.Add(context, entity);
        }

        protected virtual void OnClientRecvData(ChannelContext context, object argument) {
            var descriptor = argument as GoogleProtobufDescriptor;
            CAssert.Assert(descriptor != null);
            object invoker = null;

            if (!invokers.TryGetValue(descriptor.Service, out invoker)) {
                CLogger.Log("[RpcService](OnClientRecvData)RPC target invoker not found:" + descriptor.Service);
                return;
            }

            MethodInfo method = null;
            try {
                method = invoker.GetType().GetMethod(descriptor.Method);
            } catch (AmbiguousMatchException e) {
                CLogger.Log("[RpcService](OnClientRecvData)RPC does not support methods of same name:" + e);
                return;
            }

            var callback = new RpcCallback(context.Reverse, descriptor.Session);
            try {
                method.Invoke(invoker, new object[] { descriptor.Argument, callback });
            } catch (Exception e) {
                CLogger.Log("[RpcService](OnClientRecvData)RPC call exception caught:" + e);
            }
        }

        protected virtual void OnClientTerminated(ChannelContext context) {
            CAssert.Assert(entities.ContainsKey(context));
            entities.Remove(context);
        }
    }
}