using FieryOpal.Src;
using FieryOpal.Src.Actors;
using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FieryOpal.src.Multiplayer
{
    public enum ClientMsgType
    {
        Invalid = 0,
        ClientConnected = 1,
        Chat = 2
    }

    public struct ClientPacket
    {
        private static ClientMsgType[] MsgLookup = (ClientMsgType[])Enum.GetValues(typeof(ClientMsgType));

        public ClientMsgType Type => RawData[0] < MsgLookup.Length ? MsgLookup[RawData[0]] : ClientMsgType.Invalid;
        public byte[] RawData { get; }
        public string StringData => Encoding.ASCII.GetString(RawData).Substring(1).TrimEnd('\0');
        public IPEndPoint Sender;

        public ClientPacket(byte[] data, EndPoint sender)
        {
            RawData = data;
            Sender = (IPEndPoint)sender;
        }

        public ClientPacket(ClientMsgType type, string data, EndPoint sender)
        {
            RawData = new byte[String.IsNullOrEmpty(data) ? 1 : data.Length + 2];
            RawData[0] = (byte)Array.IndexOf(MsgLookup, type);
            if(!String.IsNullOrEmpty(data)) Encoding.ASCII.GetBytes(data).CopyTo(RawData, 1);
            Sender = (IPEndPoint)sender;
        }
    }

    public delegate ServerPacket ClientMessageHandler(ClientPacket msg);

    public class ClientHandler : IPipelineSubscriber<ClientHandler>
    {
        protected TcpClient Client { get; }
        protected OpalServerBase Server { get; }

        public Guid Handle { get; }

        public ClientHandler(OpalServerBase parent, TcpClient client)
        {
            Client = client;
            Server = parent;
            Handle = Guid.NewGuid();
        }

        public void Start()
        {
            Thread handlerThread = new Thread(() => {
                while (Server.IsRunning)
                {
                    NetworkStream stream = Client.GetStream();

                    Byte[] buffer = new Byte[256]; int b = 0;
                    try
                    {
                        while ((b = stream.Read(buffer, 0, buffer.Length)) != 0)
                        {
                            var packet = new ClientPacket(buffer.Take(b).ToArray(), Client.Client.RemoteEndPoint);
                            Server.ClientMsgHandlers[packet.Type].ForEach(h =>
                            {
                                var reply = h(packet);
                                if (reply.Type == ServerMessage.Invalid) return;

                                if(reply.IsBroadcast)
                                {
                                    Server.Broadcast(reply); // Make sure everyone gets the same reply
                                }
                                else stream.Write(reply.RawData, 0, reply.RawData.Length);
                            });
                        }
                    }
                    catch (System.IO.IOException e)
                    {
                        Util.LogServer("Client forcibly closed the connection. (IP: {0}) ".Fmt(Client.Client.RemoteEndPoint.ToString()), true);
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
            Client.GetStream().Write(p.RawData, 0, p.RawData.Length);
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

        public virtual void Start(int maxConnections=0)
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

        public void UnregisterHandler(ClientMsgType type, ClientMessageHandler handler)
        {
            ClientMsgHandlers[type].Remove(handler);
        }
    }

    public class OpalServer : OpalServerBase
    {
        public List<TurnTakingActor> Players { get; }
        public int MaxPlayers { get; }

        public OpalServer(int port, int maxPlayers) : base(port)
        {
            Players = new List<TurnTakingActor>((MaxPlayers = maxPlayers));

            RegisterHandler(ClientMsgType.Chat, (p) =>
            {
                return new ServerPacket(ServerMessage.Chat, "{0}: {1}".Fmt(p.Sender.ToString(), p.StringData));
            });

            RegisterHandler(ClientMsgType.ClientConnected, (p) =>
            {
                return new ServerPacket(ServerMessage.Chat, "{0} has entered the game.".Fmt(p.StringData));
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
