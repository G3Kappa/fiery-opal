using FieryOpal.Src.Multiplayer;
using FieryOpal.Src.Actors;
using FieryOpal.Src.Procedural;
using FieryOpal.Src.Procedural.Terrain.Tiles;
using Microsoft.Xna.Framework;
using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FieryOpal.Src
{
    public static class LuaVM
    {
        private static List<CommandDelegate> Delegates = new List<CommandDelegate>();

        private static IEnumerable<Tuple<string, object>> GetInternalObjects()
        {
            yield return new Tuple<string, object>(
                "Player",
                UserData.Create(Nexus.Player)
            );
            yield return new Tuple<string, object>(
                "World",
                UserData.Create(Nexus.Player.Map?.ParentRegion?.ParentWorld)
            );
            yield return new Tuple<string, object>(
                "Sleep",
                (Action<int>)System.Threading.Thread.Sleep
            );
        }

        private static IEnumerable<Tuple<string, object>> GetGlobalObjects()
        {
            foreach (var d in Delegates)
            {
                yield return new Tuple<string, object>(
                    d.Cmd,
                    (Func<string[], int>)d.Execute
                );
            }

            yield return new Tuple<string, object>(
                "DEBUG",
#if DEBUG
                true
#else
                false
#endif
            );

            foreach (var t in GetInternalObjects())
            {
                yield return t;
            }
        }

        public static void SetGlobals(Script env)
        {
            foreach (var t in GetGlobalObjects())
            {
                env.Globals[t.Item1] = t.Item2;
            }
        }

        public static void Init()
        {
            Delegates.Add(new CommandLog("log"));
            Delegates.Add(new CommandRect("rect"));
            Delegates.Add(new CommandSpawn("spawn"));
            Delegates.Add(new CommandStoreItem("store"));
            Delegates.Add(new CommandEquipItem("equip"));
            Delegates.Add(new CommandUnequipItem("unequip"));
            Delegates.Add(new CommandStartServer("startsv"));
            Delegates.Add(new CommandStartClient("connect"));

            UserData.RegisterType<TurnTakingActor>();
            UserData.RegisterType<OpalActorBase>();
            UserData.RegisterType<OpalLocalMap>();
            UserData.RegisterType<OpalTile>();
            UserData.RegisterType<TileSkeleton>();
            UserData.RegisterType<Vector2>();
            UserData.RegisterType<World>();
            UserData.RegisterType<WorldTile>();
            UserData.RegisterType<PlayerControlledAI>();
            UserData.RegisterType<Point>();
            UserData.RegisterType<ILocalFeatureGenerator>();
        }

        public static DynValue DoString(string script)
        {
            Script env = new Script();
            SetGlobals(env);
            return env.DoString(script);
        }

        public static DynValue DoFile(string filename)
        {
            Script env = new Script();
            SetGlobals(env);
            try
            {
                return env.DoFile(@"./cfg/scripts/{0}.lua".Fmt(filename));
            }
            catch (Exception e)
            {
                Util.Err(e.Message, false);
                return null;
            }
        }

        public static async Task<DynValue> DoFileAsync(string filename)
        {
            Script env = new Script();
            SetGlobals(env);
            try
            {
                return await env.DoFileAsync(@"./cfg/scripts/{0}.lua".Fmt(filename));
            }
            catch (Exception e)
            {
                Util.Err(e.Message, false);
#if DEBUG
                throw e;
#else
                return null;
#endif
            }
        }
    }
}
