using System;
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

		public SocketChannel(IActorProxy proxy, Socket socket, bool listening) {
			this.proxy  = proxy;
			this.socket = socket;
			this.listening = listening;
			if (listening) {
				send = new SocketBuffer();
				recv = new SocketBuffer();
			}
		}
	}

	public class SocketContainer : IActorContainer {
		private Queue<ActorMessage> msgQ  = new Queue<ActorMessage>();
		private Queue<ActorMessage> helpQ = new Queue<ActorMessage>();
		private TcpManager tcp;

		public ActorContext Context { get; private set; }

		public SocketContainer(ActorSystem system, Func<ChannelAgent> agentCreate) {
			tcp = new TcpManager(handle, agentCreate);
			Context = system.CreateContext(Actorid.TCPMANAGER, tcp);
		}

		public void Start() {
			Thread thread = new Thread(() => {
				handle();
				tcp.Update();
				Thread.Sleep(10);
			});
			thread.Start();
		}

		public void Post(ActorMessage msg) {
			lock(msgQ) {
				msgQ.Enqueue(msg);
			}
		}

		private void handle() {
			CAssert.Assert(helpQ.Count <= 0);

			lock(msgQ) {
				while(msgQ.Count > 0) {
					helpQ.Enqueue(msgQ.Dequeue());
				}
			}
			while(helpQ.Count > 0) {
				ActorMessage msg = helpQ.Dequeue();
				Context.RecvCall(msg);
			}			
		}
	}

	public class TcpManager : Actor {
		private Func<ChannelAgent> agentCreate;
		private Action handler;

		private List<Socket> reads   = new List<Socket>();
		private List<Socket> writes  = new List<Socket>();
		private List<Socket> errors  = new List<Socket>();
		private List<Socket> sockets = new List<Socket>();

		private List<SocketChannel> channels = new List<SocketChannel>();
		private List<SocketChannel> errChans = new List<SocketChannel>();

		public TcpManager(Action handler, Func<ChannelAgent> agentCreate) {
			this.handler = handler;
			this.agentCreate = agentCreate;
		}

		public void Start() {
			Thread thread = new Thread(() => {
				if (handler != null) {
					handler();
				}
				Update();
				Thread.Sleep(10);
			});
		}

		public void Listen(ServerListen svr) {
			Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
			IActorProxy proxy = ActorProxyFactory.Create(Context, svr.ActorId);
			channels.Add(new SocketChannel(proxy, socket, true));

			socket.Bind(new IPEndPoint(IPAddress.Parse(svr.Ip), svr.Port));
			socket.Listen(10); // backlog?
		}

		public void Send(ServerData svr) {
			byte[] buf = new byte[svr.Data.Length+2];
			buf[0] = (byte)((svr.Data.Length >> 8) & 0xFF);
			buf[1] = (byte) (svr.Data.Length & 0xFF);
			Array.Copy(svr.Data, 0, buf, 2, svr.Data.Length);

			SocketChannel chan = channels.Find(x => x.proxy.Target == svr.Chanid);
			CAssert.Assert(chan != null);
			chan.send.buffers.Add(buf);
		}

		public void Update() {
			onRead();
			onWrite();
			onError();
		}

		public void Close(int chanid) {
			for(int i=0; i<channels.Count; ++i) {
				SocketChannel chan = channels[i];
				if (chan.proxy.Target == chanid) {
					chan.socket.Close();
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
				ActorContext agent = Context.System.RegActor(agentCreate());
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

			Socket.Select(reads, null, null, 1); // check parameter [microSeconds] => how many is proper?
			if (reads.Count > 0) {
				foreach (Socket read in reads) {
					SocketChannel channel = channels.Find(chan => chan.socket == read);
					if (channel.listening) {
						onAccept(channel.socket);
					} else {
						onSocket(read, onRecv);
					}
				}
			}
		}

		private void onWrite() {
			writes.Clear();
			foreach (SocketChannel chan in channels) {
				if (chan.send.buffers.Count > 0) {
					writes.Add(chan.socket);
				}
			}

			if (writes.Count > 0) {
				Socket.Select(null, writes, null, 1);
				if (writes.Count > 0) {
					foreach (Socket write in writes) {
						onSocket(write, onSend);
					}
				}
			}
		}

		private void onError() {
			errors.Clear();
			foreach (SocketChannel chan in channels) {
				errors.Add(chan.socket);
			}

			Socket.Select(null, null, errors, 1);
			if (errors.Count > 0) {
				foreach (Socket error in errors) {
					SocketChannel chan = channels.Find(x => x.socket == error);
					closeChannel(chan);
				}
			}
		}

		private void checkSockets() {
			CAssert.Assert(reads.Count <= 0);
			CAssert.Assert(writes.Count <= 0);
			CAssert.Assert(errors.Count <= 0);
		}

		private void onRecv(SocketChannel chan) {
			Socket socket = chan.socket;
			CAssert.Assert(socket.Available >= 0);
			if (socket.Available <= 0) {
				CLogger.Log("[TcpManager]Client {0} disconnect", chan.proxy.Target);
				errChans.Add(chan);
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
				errChans.Add(chan);
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

		private void onSend(SocketChannel chan) {
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
					errChans.Add(chan);
					CLogger.Log("[TcpManager]Send to socket(id:{0}) exception:{1}", chan.proxy.Target, e.Message);
					break;
				}
				skt.offset = 0;
				skt.buffers.RemoveAt(0);
			}
		}

		private void onSocket(Socket socket, Action<SocketChannel> action) {
			SocketChannel chan = channels.Find(x => x.socket == socket);
			CAssert.Assert(chan != null);

			errChans.Clear();
			action(chan);

			foreach (SocketChannel err in errChans) {
				closeChannel(err);
			}
		}

		private void closeChannel(SocketChannel chan) {
			chan.socket.Close();
			channels.Remove(chan);
			CLogger.Log("[TcpManager]Remove socket(id:{0})", chan.proxy.Target);
			chan.proxy.SendCmd(AgentMessage.CLOSE, null);
		}
	}
}