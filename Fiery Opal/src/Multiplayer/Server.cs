using FieryOpal.Src;
using FieryOpal.Src.Actors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace FieryOpal.Src.Multiplayer
{
    public class ClientHandler : IPipelineSubscriber<ClientHandler>
    {
        public TcpClient Client { get; }
        protected OpalServerBase Server { get; }

        private object StreamLock = new object();

        public Guid Handle { get; }

        public ClientHandler(OpalServerBase parent, TcpClient client)
        {
            Client = client;
            Server = parent;
            Handle = Guid.NewGuid();
        }

        public void Start()
        {
            Thread handlerThread = new Thread(() =>
            {
                while (Server.IsRunning)
                {
                    NetworkStream stream = Client.GetStream();

                    Byte[] buffer = new Byte[1024]; int b = 0;
                    try
                    { 
                        while ((b = stream.Read(buffer, 0, buffer.Length)) != 0)
                        {
                            var packet = new ClientPacket(buffer.Take(b).ToArray(), Client.Client.RemoteEndPoint);
                            Server.ClientMsgHandlers[packet.Type].ToList().ForEach(h =>
                            {
                                var reply = h(packet);
                                if (reply.Type == ServerMsgType.None) return;

                                if (reply.IsBroadcast)
                                {
                                    Server.Broadcast(reply); // Make sure everyone gets the same reply
                                }
                                else
                                {
                                    lock (StreamLock) stream.Write(reply.RawData, 0, reply.RawData.Length);
                                }
                            });
                        }
                    }
                    catch
                    {
                        Util.LogServer("Client forcibly closed the connection. (IP: {0}) ".Fmt(Client.Client.RemoteEndPoint.ToString()), true);
                        Server.ClientPipeline.Broadcast(null, (ch) =>
                        {
                            Server.ClientPipeline.Unsubscribe(this);
                            return "UnsubscribeCrashedClient";
                        });
                        Server.Broadcast(new ServerPacket(ServerMsgType.ClientDisconnected, Client.Client.RemoteEndPoint.ToString()));
                        break;
                    }
                }

                Client.Close();
            });
            handlerThread.IsBackground = true;
            handlerThread.Start();
        }

        public void Write(ServerPacket p)
        {
            lock (StreamLock) Client.GetStream().Write(p.RawData, 0, p.RawData.Length);
        }

        public void ReceiveMessage(Guid pipeline_handle, Guid sender_handle, Func<ClientHandler, string> msg, bool is_broadcast)
        {
            string msgType = msg(this);
        }
    }

    public abstract class OpalServerBase
    {
        protected TcpListener /*FieryOpal.DarkBrotherhood.*/Listener { get; }
        public MessagePipeline<ClientHandler> ClientPipeline { get; }

        public DefaultDictionary<ClientMsgType, List<ClientMessageHandler>> ClientMsgHandlers { get; }

        public int Port { get; }
        public bool IsRunning { get; private set; }

        public OpalServerBase(int port)
        {
            Listener = new TcpListener(IPAddress.Loopback, (Port = port));
            ClientMsgHandlers = new DefaultDictionary<ClientMsgType, List<ClientMessageHandler>>((_) => new List<ClientMessageHandler>());
            ClientPipeline = new MessagePipeline<ClientHandler>();
        }

        public virtual void Start(int maxConnections = 0)
        {
            IsRunning = true;
            if (maxConnections <= 0) Listener.Start();
            else Listener.Start(maxConnections);

            while (IsRunning)
            {
                TcpClient client = Listener.AcceptTcpClient();
                var ch = new ClientHandler(this, client);
                ClientPipeline.Subscribe(ch);
                ch.Start();
            }
        }

        public void Broadcast(ServerPacket msg)
        {
            ClientPipeline.Broadcast(null, (ch) =>
            {
                ch.Write(msg);
                return "ServerBroadcast";
            });
        }

        public void Stop()
        {
            IsRunning = false;
        }

        public void RegisterHandler(ClientMsgType type, ClientMessageHandler handler)
        {
            ClientMsgHandlers[type].Add(handler);
        }

        public void RegisterHandlers(ClientMsgType type, IEnumerable<ClientMessageHandler> handlers)
        {
            ClientMsgHandlers[type].AddRange(handlers);
        }

        public void UnregisterHandler(ClientMsgType type, ClientMessageHandler handler)
        {
            ClientMsgHandlers[type].Remove(handler);
        }
    }

    public class OpalServer : OpalServerBase
    {
        public int MaxPlayers { get; }

        public OpalServer(int port, int maxPlayers) : base(port)
        {
            RegisterHandler(ClientMsgType.Chat, (p) =>
            {
                return new ServerPacket(ServerMsgType.Chat, p.StringData);
            });

            RegisterHandler(ClientMsgType.ClientConnected, (p) =>
            {
                return new ServerPacket(ServerMsgType.Chat, "{0} has entered the game.".Fmt(p.StringData));
            });

            RegisterHandler(ClientMsgType.ClientConnected, (p) =>
            {
                return new ServerPacket(ServerMsgType.ClientConnected, "{0}\x1{1}".Fmt(p.StringData, p.Sender.ToString()));
            });

            RegisterHandler(ClientMsgType.ClientDisconnected, (p) =>
            {
                ClientPipeline.Broadcast(null, (ch) =>
                {
                    if(ch.Client.Client.RemoteEndPoint == p.Sender)
                    {
                        ClientPipeline.Unsubscribe(ch);
                    }
                    return "UnsubscribeDroppedClient";
                });

                return new ServerPacket(ServerMsgType.ClientDisconnected, p.Sender.ToString());
            });

            RegisterHandler(ClientMsgType.ClientDisconnected, (p) =>
            {
                return new ServerPacket(ServerMsgType.Chat, "{0} has left the game.".Fmt(p.StringData));
            });

            RegisterHandler(ClientMsgType.PlayerMoved, (p) =>
            {
                return new ServerPacket(ServerMsgType.Chat, "{0} has entered the game.".Fmt(p.StringData));
            });

        }

        public void Start()
        {
            Util.LogServer("Server initialized. PORT: {0}. MAX PLAYERS: {1}.".Fmt(Port, MaxPlayers));
            base.Start(MaxPlayers);
        }

        public override void Start(int unused = 0)
        {
            Start();
        }
    }
}
