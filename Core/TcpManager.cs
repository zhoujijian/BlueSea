﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using CUtil;

namespace Core.Net {
    public class ServerListen {
        public string Ip;
        public int Port;
        public int ActorId;
    }

    public class ServerData {
        public int Chanid  { get; set; }
        public byte[] Data { get; set; }
    }

    public class ServerMessage {
        public const string LISTEN   = "Listen";
        public const string SENDDATA = "SendData";
        public const string CLOSE    = "Close";
    }

    internal class SocketBuffer {
        public int offset = 0;
        public List<byte[]> buffers = new List<byte[]>();
    }

    internal class SocketChannel {
        public IActorProxy proxy;
        public Socket socket;
        public bool listening;
        public SocketBuffer send;
        public SocketBuffer recv;
        public bool error;

        public SocketChannel(IActorProxy proxy, Socket socket, bool listening) {
            this.proxy  = proxy;
            this.socket = socket;
            this.listening = listening;
            if (!listening) {
                send = new SocketBuffer();
                recv = new SocketBuffer();
            }
        }
    }

    public class SocketMailbox : IActorMailbox {
        private Queue<ActorMessage> msgQ  = new Queue<ActorMessage>();
        private Queue<ActorMessage> helpQ = new Queue<ActorMessage>();
        private readonly TcpManager tcp;

        public int Id { get { return Context.ID; } }
        public int MessagesCount { get { lock (msgQ) { return msgQ.Count; } } }
        public ActorContext Context { get; private set; }

        public SocketMailbox(ActorSystem system, Func<ChannelAgent> agentCreate) {
            tcp = new TcpManager(agentCreate);
            Context = system.CreateContext(Actorid.TCPMANAGER, tcp);
        }

        public void Start() {
            Thread thread = new Thread(() => {
                while (true) {
                    handle();
                    tcp.UpdateMany();
                    Thread.Sleep(10);
                }
            });
            thread.Start();
        }

        public void Post(ActorMessage msg) {
            lock (msgQ) {
                msgQ.Enqueue(msg);
            }
        }

        public void Clear() {
            lock (msgQ) {
                msgQ.Clear();
            }
            // do not handle helpQ: TODO => thread safety ?
        }

        private void handle() {
            CAssert.Assert(helpQ.Count <= 0);

            lock (msgQ) {
                while (msgQ.Count > 0) {
                    helpQ.Enqueue(msgQ.Dequeue());
                }
            }
            while (helpQ.Count > 0) {
                ActorMessage msg = helpQ.Dequeue();
                Context.RecvCall(msg);
            }
        }
    }

    public class TcpManager : Actor {
        private Func<ChannelAgent> agentCreate;
        private List<Socket> reads  = new List<Socket>();
        private List<Socket> writes = new List<Socket>();
        private List<Socket> errors = new List<Socket>();

        private List<SocketChannel> channels = new List<SocketChannel>();
        private List<SocketChannel> errorChannels = new List<SocketChannel>();

        public TcpManager(Func<ChannelAgent> agentCreate) {
            this.agentCreate = agentCreate;
        }

        public void Listen(ServerListen svr) {
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            IActorProxy proxy = ActorProxyFactory.Create(Context, svr.ActorId);
            channels.Add(new SocketChannel(proxy, socket, true));

            socket.Bind(new IPEndPoint(IPAddress.Any, svr.Port));
            // socket.Bind(new IPEndPoint(IPAddress.Parse(svr.Ip), svr.Port));
            socket.Listen(10); // backlog?
        }

        public void Send(ServerData svr) {
            byte[] buf = new byte[svr.Data.Length+2];
            buf[0] = (byte)((svr.Data.Length >> 8) & 0xFF);
            buf[1] = (byte) (svr.Data.Length & 0xFF);
            Array.Copy(svr.Data, 0, buf, 2, svr.Data.Length);

            SocketChannel chan = channels.Find(x => x.proxy.Target == svr.Chanid);
            if (chan == null) {
                CLogger.Log("[TcpManager]SocketChannel({0}) not found, disconnected?", svr.Chanid);
            } else {
                chan.send.buffers.Add(buf);
            }
        }

        public void Update() {
            onRead();
            onWrite();
            onError();
        }

        public void UpdateMany() {
            reads.Clear();
            writes.Clear();
            errors.Clear();
            errorChannels.Clear();

            foreach (SocketChannel channel in channels) {
                CAssert.Assert(!channel.error);
                reads.Add(channel.socket);
                if (!channel.listening && channel.send.buffers.Count > 0) {
                    writes.Add(channel.socket);
                }
                errors.Add(channel.socket);
            }

            if (reads.Count > 0 || writes.Count > 0 || errors.Count > 0) {
                Socket.Select(reads, writes, errors, 1);

                if (reads.Count > 0) {
                    foreach (Socket read in reads) {
                        SocketChannel channel = channels.Find(chan => chan.socket == read);
                        if (channel.listening) {
                            onAccept(channel.socket);
                        } else {
                            onSocket(read, errorChannels, onRecv);
                        }
                    }
                }

                if (writes.Count > 0) {
                    foreach (Socket write in writes) {
                        onSocket(write, errorChannels, onSend);
                    }
                }

                if (errors.Count > 0) {
                    foreach (Socket error in errors) {
                        SocketChannel chan = channels.Find(x => x.socket == error);
                        removeChannel(chan);
                    }
                }
            }
        }

        public void Close(int chanid) {
            for(int i=0; i<channels.Count; ++i) {
                SocketChannel chan = channels[i];
                if (chan.proxy.Target == chanid) {
                    chan.socket.Shutdown(SocketShutdown.Both);
                    channels.RemoveAt(i);
                    CLogger.Log("[TcpManager]Remove socket(id:{0})", chan.proxy.Target);
                    break;
                }
            }
        }

        public override void Handle(ActorMessage msg, Action<object> retback) {
            switch(msg.Method) {
                case ServerMessage.LISTEN:   { msg.With<ServerListen>(svr => Listen(svr)); break; }
                case ServerMessage.SENDDATA: { msg.With<ServerData>  (svr => Send(svr));   break; }
                case ServerMessage.CLOSE:    { Close((int)msg.Content);                    break; }
                default: { CAssert.Assert(false, "Unknown Method:" + msg.Method); break; }
            }
        }

        private void onAccept(Socket listen) {
            Socket newsock = null;
            try {
                newsock = listen.Accept();
            }
            catch (SocketException ske) {
                CLogger.Log("[TcpManager]Accept Socket Exception:{0}", ske.Message);
            }
            catch (Exception ex) {
                CLogger.Log("[TcpManager]Accept Exception:{0}", ex.Message);
            }

            if (newsock != null) {
                ActorContext agent = Context.System.Launch(Context.System.NextActorid(), agentCreate());
                IActorProxy proxy = ActorProxyFactory.Create(Context, agent.ID);
                channels.Add(new SocketChannel(proxy, newsock, false));
                CLogger.Log("[TcpManager]Accept new socket:{0}", proxy.Target);

                proxy.SendCmd(AgentMessage.OPEN, null);
            }
        }

        private void onRead() {
            reads.Clear();
            foreach (SocketChannel chan in channels) {
                reads.Add(chan.socket);
            }

            if (reads.Count > 0) {
                Socket.Select(reads, null, null, 1); // check parameter [microSeconds] => how many is proper?
                if (reads.Count > 0) {
                    foreach (Socket read in reads) {
                        SocketChannel channel = channels.Find(chan => chan.socket == read);
                        if (channel.listening) {
                            onAccept(channel.socket);
                        } else {
                            onSocket(read, errorChannels, onRecv);
                        }
                    }
                }				
            }
        }

        private void onWrite() {
            writes.Clear();
            foreach (SocketChannel chan in channels) {
                if (!chan.listening && chan.send.buffers.Count > 0) {
                    writes.Add(chan.socket);
                }
            }

            if (writes.Count > 0) {
                Socket.Select(null, writes, null, 1);
                if (writes.Count > 0) {
                    foreach (Socket write in writes) {
                        onSocket(write, errorChannels, onSend);
                    }
                }
            }
        }

        private void onError() {
            errors.Clear();
            foreach (SocketChannel chan in channels) {
                errors.Add(chan.socket);
            }

            if (errors.Count > 0) {
                Socket.Select(null, null, errors, 1);
                if (errors.Count > 0) {
                    foreach (Socket error in errors) {
                        SocketChannel chan = channels.Find(x => x.socket == error);
                        removeChannel(chan);
                    }
                }
            }
        }

        private void checkSockets() {
            CAssert.Assert(reads.Count <= 0);
            CAssert.Assert(writes.Count <= 0);
            CAssert.Assert(errors.Count <= 0);
        }

        private void addErrorChannel(List<SocketChannel> errChans, SocketChannel chan) {
            CAssert.Assert(!chan.error);
            chan.error = true;
            errChans.Add(chan);
        }

        private void onRecv(List<SocketChannel> errChans, SocketChannel chan) {
            Socket socket = chan.socket;
            CAssert.Assert(socket.Available >= 0);
            if (socket.Available <= 0) {
                CLogger.Log("[TcpManager]Client {0} disconnect", chan.proxy.Target);
                addErrorChannel(errChans, chan);
                return;
            }

            int recv = 0;
            byte[] buf = new byte[socket.Available];
            try {
                recv = socket.Receive(buf, buf.Length, SocketFlags.None);
                if (recv <= 0) {
                    throw new Exception("receive 0 bytes!");
                }
            }
            catch (Exception e) {
                addErrorChannel(errChans, chan);
                CLogger.Log("[TcpManager]Receive from socket(id:{0}) exception:{1}", chan.proxy.Target, e.Message);
                return;
            }
            CAssert.Assert(recv == buf.Length);

            int offset = 0;
            while(offset < buf.Length) {
                SocketBuffer skt = chan.recv;
                if (skt.buffers.Count <= 0) {
                    CAssert.Assert(skt.offset == 0);
                    skt.buffers.Add(new byte[2]);
                }

                byte[] tart = skt.buffers[skt.buffers.Count-1];
                int lcopy = tart.Length - skt.offset < buf.Length - offset ? tart.Length - skt.offset : buf.Length - offset;
                Array.Copy(buf, offset, tart, skt.offset, lcopy);
                offset += lcopy;
                skt.offset += lcopy;

                if (skt.offset == tart.Length) {
                    skt.offset = 0;
                    if (skt.buffers.Count % 2 != 0) {
                        int newl = (((int)tart[0]) << 8) + tart[1];
                        skt.buffers.Add(new byte[newl]);						
                    } else {
                        CAssert.Assert(skt.buffers.Count == 2);
                        skt.buffers.Clear();
                        chan.proxy.SendCmd(AgentMessage.CLIENT, tart); // TODO: a better rpc
                    }
                }
            }
        }

        private void onSend(List<SocketChannel> errChans, SocketChannel chan) {
            SocketBuffer skt = chan.send;

            while (skt.buffers.Count > 0) {
                byte[] buf = skt.buffers[0];
                try {
                    int remain = buf.Length - skt.offset;
                    skt.offset += chan.socket.Send(buf, skt.offset, remain, SocketFlags.None);
                    if (skt.offset < buf.Length) {
                        break;
                    }
                }
                catch (Exception e) {
                    addErrorChannel(errChans, chan);
                    CLogger.Log("[TcpManager]Send to socket(id:{0}) exception:{1}", chan.proxy.Target, e.Message);
                    break;
                }
                skt.offset = 0;
                skt.buffers.RemoveAt(0);
            }
        }

        private void onSocket(Socket socket, List<SocketChannel> errChans, Action<List<SocketChannel>, SocketChannel> action) {
            SocketChannel chan = channels.Find(x => x.socket == socket);
            CAssert.Assert(chan != null);

            errChans.Clear();
            if (!chan.error) {
                action(errChans, chan);
            }

            foreach (SocketChannel err in errChans) {
                CAssert.Assert(err.error);
                removeChannel(err);
            }
        }

        // these channels are exception or error, donot need to shutdown again
        private void removeChannel(SocketChannel chan) {
            channels.Remove(chan);
            CLogger.Log("[TcpManager]Remove socket(id:{0})", chan.proxy.Target);
            chan.proxy.SendCmd(AgentMessage.CLOSE, null);
        }
    }
}
