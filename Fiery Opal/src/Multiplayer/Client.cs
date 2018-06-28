using FieryOpal.Src;
using FieryOpal.Src.Actors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace FieryOpal.Src.Multiplayer
{
    public abstract class OpalClientBase
    {
        protected TcpClient Client { get; }
        protected DefaultDictionary<ServerMsgType, List<ServerMessageHandler>> ServerMsgHandlers;

        public bool IsRunning { get; private set; }
        private object StreamLock = new object();

        public OpalClientBase()
        {
            Client = new TcpClient();
            ServerMsgHandlers = new DefaultDictionary<ServerMsgType, List<ServerMessageHandler>>((_) => new List<ServerMessageHandler>());
        }

        public void Connect(string serverHostname, int port, string playerName = "Player")
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
                lock(StreamLock) stream.Write(hello.RawData, 0, hello.RawData.Length);

                Byte[] buffer = new Byte[1024]; int b = 0;
                try
                {
                    while ((b = stream.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        var packet = new ServerPacket(buffer.Take(b).ToArray());
                        ServerMsgHandlers[packet.Type].ForEach(h =>
                        {
                            var reply = h(packet);
                            if (reply.Type == ClientMsgType.Ok) return;
                            lock (StreamLock) stream.Write(reply.RawData, 0, reply.RawData.Length);
                        });
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

        public void Write(ClientPacket p)
        {
            lock (StreamLock) Client.GetStream().Write(p.RawData, 0, p.RawData.Length);
        }

        public void SendChatMsg(string msg)
        {
            Write(new ClientPacket(ClientMsgType.Chat, "{0}: {1}".Fmt(Nexus.Player.Name, msg), null));
        }

        public void RegisterHandler(ServerMsgType type, ServerMessageHandler handler)
        {
            ServerMsgHandlers[type].Add(handler);
        }

        public void UnregisterHandler(ServerMsgType type, ServerMessageHandler handler)
        {
            ServerMsgHandlers[type].Remove(handler);
        }
    }

    public class OpalClient : OpalClientBase
    {
        protected Dictionary<string, TurnTakingActor> OtherPlayers = new Dictionary<string, TurnTakingActor>();

        public OpalClient() : base()
        {
            RegisterHandler(ServerMsgType.Chat, (msg) =>
            {
                Util.LogChat(msg.StringData);

                return new ClientPacket(ClientMsgType.Ok, null, null);
            });

            RegisterHandler(ServerMsgType.ClientConnected, (msg) =>
            {
                var args = msg.StringData.Split('\x1');
                // If it was us, don't spawn a new player.
                if (args[1].Equals(Client.Client.LocalEndPoint.ToString()))
                {
                    Nexus.Player.Name = args[0];
                    return new ClientPacket(ClientMsgType.Ok, null, null);
                }

                Humanoid player = new Humanoid();
                player.Name = args[0];
                player.Brain = new ServerControlledAI(player, this);
                OtherPlayers[args[1]] = player;
                player.ChangeLocalMap(Nexus.Player.Map, Nexus.Player.LocalPosition);

                return new ClientPacket(ClientMsgType.Ok, null, null);
            });

            RegisterHandler(ServerMsgType.ClientDisconnected, (msg) =>
            {
                var key = msg.StringData;
                // If it was us, warn the player.
                if (key.Equals(Client.Client.LocalEndPoint.ToString()))
                {
                    // TODO
                    return new ClientPacket(ClientMsgType.Ok, null, null);
                }
                // If we never spawned this player, no biggie at this point.
                if(!OtherPlayers.ContainsKey(key))
                {
                    return new ClientPacket(ClientMsgType.Ok, null, null);
                }
                OtherPlayers[key].Kill();
                Util.LogClient("{0} had an aneurysm.".Fmt(OtherPlayers[key].Name), true);
                OtherPlayers.Remove(key);
                return new ClientPacket(ClientMsgType.Ok, null, null);
            });
        }
    }
}
