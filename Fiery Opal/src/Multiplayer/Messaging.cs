using FieryOpal.Src;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace FieryOpal.Src.Multiplayer
{
    public delegate ServerPacket ClientMessageHandler(ClientPacket msg);
    public delegate ClientPacket ServerMessageHandler(ServerPacket msg);

    public enum ClientMsgType
    {
        Ok = 0,
        ClientConnected = 1,
        ClientDisconnected = 2,
        Chat = 3,
        PlayerMoved = 4
    }

    public enum ServerMsgType
    {
        None = 0,
        ClientConnected = 1,
        ClientDisconnected = 2,
        Chat = 3,
        PlayerMoved = 4
    }

    public struct ClientPacket
    {
        private static ClientMsgType[] MsgLookup = (ClientMsgType[])Enum.GetValues(typeof(ClientMsgType));

        public ClientMsgType Type => RawData[0] < MsgLookup.Length ? MsgLookup[RawData[0]] : ClientMsgType.Ok;
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
            if (!String.IsNullOrEmpty(data)) Encoding.ASCII.GetBytes(data).CopyTo(RawData, 1);
            Sender = (IPEndPoint)sender;
        }
    }


    public struct ServerPacket
    {
        private static ServerMsgType[] MsgLookup = (ServerMsgType[])Enum.GetValues(typeof(ServerMsgType));

        public ServerMsgType Type => (uint)RawData[0] < MsgLookup.Length ? MsgLookup[(uint)RawData[0]] : ServerMsgType.None;
        public bool IsBroadcast => new[] {
            ServerMsgType.Chat, ServerMsgType.ClientConnected,
            ServerMsgType.ClientDisconnected
        }.Contains(Type);
        public byte[] RawData { get; }
        public string StringData => Encoding.ASCII.GetString(RawData).Substring(1).TrimEnd('\0');

        public ServerPacket(byte[] data)
        {
            RawData = data;
        }

        public ServerPacket(ServerMsgType type, string data)
        {
            RawData = new byte[String.IsNullOrEmpty(data) ? 1 : data.Length + 2];
            RawData[0] = (byte)Array.IndexOf(MsgLookup, type);
            if (!String.IsNullOrEmpty(data)) Encoding.ASCII.GetBytes(data).CopyTo(RawData, 1);
        }
    }

}
