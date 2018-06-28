using FieryOpal.Src;
using System;
using System.Threading;

namespace FieryOpal.Src.Multiplayer
{
    public class CommandStartServer : CommandDelegate
    {
        public static Type[] _Signature = new Type[2] { typeof(int), typeof(int) };

        public CommandStartServer(string name = "start_server") : base(name, _Signature)
        {
        }

        protected override int ExecInternal(object[] args)
        {
            if (Nexus.GameServer?.IsRunning ?? false)
            {
                Util.Err("Game server is already running.");
                return 1;
            }

            int port = (int)args[0];
            int maxPlayers = (int)args[1];

            Thread serverThread = new Thread(
                () =>
                {
                    Nexus.GameServer = new OpalServer(port, maxPlayers);
                    Nexus.GameServer.Start();
                }
            );
            serverThread.IsBackground = true;
            serverThread.Start();

            return 0;
        }

        protected override dynamic ParseArgument(Type T, string str)
        {
            TypeConversionHelper<int>.Convert(str, out dynamic val);
            return val;
        }
    }

    public class CommandStartClient : CommandDelegate
    {
        public static Type[] _Signature = new Type[3] { typeof(string), typeof(int), typeof(string) };

        public CommandStartClient(string name = "start_client") : base(name, _Signature)
        {
        }

        protected override int ExecInternal(object[] args)
        {
            if (Nexus.GameClient?.IsRunning ?? false)
            {
                Util.Err("Game client is already running.");
                return 1;
            }

            string hostname = (string)args[0];
            int port = (int)args[1];
            string nickname = (string)args[2];

            Thread clientThread = new Thread(
                () =>
                {
                    Nexus.GameClient = new OpalClient();
                    Nexus.GameClient.Connect(hostname, port, nickname);
                }
            );
            clientThread.IsBackground = true;
            clientThread.Start();

            return 0;
        }

        protected override dynamic ParseArgument(Type T, string str)
        {
            dynamic val;
            if (T == typeof(int))
            {
                TypeConversionHelper<int>.Convert(str, out val);
            }
            else val = str;
            return val;
        }
    }

    public class CommandSendChatMsg : CommandDelegate
    {
        public static Type[] _Signature = new Type[1] { typeof(string) };

        public CommandSendChatMsg(string name = "say") : base(name, _Signature)
        {
            PrintZeroExitCode = false;
        }

        protected override int ExecInternal(object[] args)
        {
            if (!Nexus.GameClient?.IsRunning ?? true)
            {
                Util.LogChat("{0}: {1}".Fmt(Nexus.Player.Name ?? "You", (string)args[0]), false);
                return 0;
            }
            Nexus.GameClient.SendChatMsg((string)args[0]);
            return 0;
        }

        protected override dynamic ParseArgument(Type T, string str)
        {
            return str;
        }
    }
}
