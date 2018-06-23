using FieryOpal.Src;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FieryOpal.src.Multiplayer
{
    public delegate ClientPacket ServerMessageHandler(ServerPacket msg);
    public enum ServerMessage
    {
        Invalid = 0,
        Chat = 1,
        ClientDropped = 2,
    }

    public struct ServerPacket
    {
        private static ServerMessage[] MsgLookup = (ServerMessage[])Enum.GetValues(typeof(ServerMessage));

        public ServerMessage Type => (uint)RawData[0] < MsgLookup.Length ? MsgLookup[(uint)RawData[0]] : ServerMessage.Invalid;
        public bool IsBroadcast => new[] { ServerMessage.Chat, ServerMessage.ClientDropped }.Contains(Type);
        public byte[] RawData { get; }
        public string StringData => Encoding.ASCII.GetString(RawData).Substring(1).TrimEnd('\0');

        public ServerPacket(byte[] data)
        {
            RawData = data;
        }

        public ServerPacket(ServerMessage type, string data)
        {
            RawData = new byte[String.IsNullOrEmpty(data) ? 1 : data.Length + 2];
            RawData[0] = (byte)Array.IndexOf(MsgLookup, type);
            if(!String.IsNullOrEmpty(data)) Encoding.ASCII.GetBytes(data).CopyTo(RawData, 1);
        }
    }

    public abstract class OpalClientBase
    {
        protected TcpClient Client { get; }
        protected DefaultDictionary<ServerMessage, List<ServerMessageHandler>> ServerMsgHandlers;

        public Queue<ClientPacket> EnqueuedPackets;
        public bool IsRunning { get; private set; }

        public OpalClientBase()
        {
            Client = new TcpClient();
            ServerMsgHandlers = new DefaultDictionary<ServerMessage, List<ServerMessageHandler>>((_) => new List<ServerMessageHandler>());
            EnqueuedPackets = new Queue<ClientPacket>();
        }

        public void Connect(string serverHostname, int port, string playerName="Player")
        {
            try
            {
                Client.Connect(serverHostname, port);
                Util.LogClient("Connection to server established.".Fmt(serverHostname, port));
            }
            catch (SocketException se)
            {
                Util.LogClient("Could not connect to the remote host. ({0})".Fmt(se.ErrorCode));
            }

            IsRunning = true;
            while (Client.Connected)
            {
                NetworkStream stream = Client.GetStream();
                var hello = new ClientPacket(ClientMsgType.ClientConnected, playerName, null);
                stream.Write(hello.RawData, 0, hello.RawData.Length);

                Byte[] buffer = new Byte[256]; int b = 0;
                try
                {
                    while ((b = stream.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        var packet = new ServerPacket(buffer.Take(b).ToArray());
                        ServerMsgHandlers[packet.Type].ForEach(h =>
                        {
                            var reply = h(packet);
                            if (reply.Type == ClientMsgType.Invalid) return;
                            stream.Write(reply.RawData, 0, reply.RawData.Length);
                        });

                        while (EnqueuedPackets.Count > 0)
                        {
                            var reply = EnqueuedPackets.Dequeue();
                            stream.Write(reply.RawData, 0, reply.RawData.Length);
                        }
                    }
                }
                catch (System.IO.IOException e)
                {
                    Util.LogClient("Server forcibly closed the connection.", true);
                    break;
                }
            }
            IsRunning = false;
        }

        public void SendChatMsg(string msg)
        {
            EnqueuedPackets.Enqueue(new ClientPacket(ClientMsgType.Chat, msg, null));
        }

        public void RegisterHandler(ServerMessage type, ServerMessageHandler handler)
        {
            ServerMsgHandlers[type].Add(handler);
        }

        public void UnregisterHandler(ServerMessage type, ServerMessageHandler handler)
        {
            ServerMsgHandlers[type].Remove(handler);
        }
    }

    public class OpalClient : OpalClientBase
    {
        public OpalClient() : base()
        {
            RegisterHandler(ServerMessage.Chat, (msg) =>
            {
                Util.LogChat(msg.StringData);
                return new ClientPacket(ClientMsgType.Invalid, null, null);
            });
        }
    }
}
