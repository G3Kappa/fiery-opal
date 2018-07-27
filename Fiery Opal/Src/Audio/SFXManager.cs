using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieryOpal.Src.Audio
{
    public static class SFXManager
    {
        public enum SoundEffectType
        {
            None = 0,
            SoundTrack = 1,
            Eerie01 = 2,
            UiBlip = 3,
            UiSuccess = 4,
            UiError = 5,
        }

        public enum SoundTrackType
        {
            None = 0,
            Caves = 1
        }

        private static string GetSFXPath(SoundEffectType fx)
        {
            if (fx == SoundEffectType.SoundTrack) return null;
            return @"sfx\effects\{0}.ogg".Fmt(Enum.GetName(typeof(SoundEffectType), fx));
        }

        private static string GetTrackPath(SoundTrackType track)
        {
            return @"sfx\soundtrack\{0}.ogg".Fmt(Enum.GetName(typeof(SoundTrackType), track));
        }

        private static void WriteWave(BinaryWriter writer, int channels, int rate, byte[] data)
        {
            writer.Write(new char[4] { 'R', 'I', 'F', 'F' });
            writer.Write((int)(36 + data.Length));
            writer.Write(new char[4] { 'W', 'A', 'V', 'E' });

            writer.Write(new char[4] { 'f', 'm', 't', ' ' });
            writer.Write((int)16);
            writer.Write((short)1);
            writer.Write((short)channels);
            writer.Write((int)rate);
            writer.Write((int)(rate * ((16 * channels) / 8)));
            writer.Write((short)((16 * channels) / 8));
            writer.Write((short)16);

            writer.Write(new char[4] { 'd', 'a', 't', 'a' });
            writer.Write((int)data.Length);
            writer.Write(data);
        }

        private static Dictionary<SoundEffectType, List<SoundEffectInstance>> Instances = new Dictionary<SoundEffectType, List<SoundEffectInstance>>();
        private static Dictionary<SoundEffectType, SoundEffect> Effects = new Dictionary<SoundEffectType, SoundEffect>();
        private static Dictionary<SoundTrackType, SoundEffect> Tracks = new Dictionary<SoundTrackType, SoundEffect>();

        public static SoundEffect FromFile(string path)
        {
            var decoder = new OggSharp.OggDecoder();
            decoder.Initialize(TitleContainer.OpenStream(path));
            byte[] data = decoder.SelectMany(chunk => chunk.Bytes.Take(chunk.Length)).ToArray();
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                WriteWave(writer, decoder.Stereo ? 2 : 1, decoder.SampleRate, data);
                stream.Position = 0;
                return SoundEffect.FromStream(stream);
            }
        }

        private static SoundEffectInstance Play(SoundEffectType fx, SoundEffect effect, float volume = 1f, float pitch = 0f, float pan = 0f, bool loop = false, bool stopSimilar = false)
        {
            if (Instances.ContainsKey(fx))
            {
                var instance = effect.CreateInstance();
                instance.Pitch = pitch;
                instance.Pan = pan;
                instance.Volume = volume;
                instance.IsLooped = loop;
                if (stopSimilar) StopAllFX(fx);
                instance.Play();
                Instances[fx].Add(instance);
                return instance;
            }

            Instances[fx] = new List<SoundEffectInstance>();
            return Play(fx, effect, volume, pitch, pan, loop, stopSimilar);
        }

        private static SoundTrackType LastSoundTrack = SoundTrackType.None;
        public static bool PlaySoundtrack(SoundTrackType track, float volume)
        {
            if(LastSoundTrack == track)
            {
                return false; // Already playing and won't stop by itself.
            }

            if (track == SoundTrackType.None)
            {
                StopSoundtrack();
                return true;
            }

            if (!Tracks.ContainsKey(track))
            {
                Tracks[track] = FromFile(GetTrackPath(track));
            }

            Play(SoundEffectType.SoundTrack, Tracks[track], volume, 0f, 0f, true, false);
            LastSoundTrack = track;
            return true;
        }

        public static SoundEffectInstance PlayFX(SoundEffectType fx, float volume = 1f, float pitch = 0f, float pan = 0f, bool loop = false, bool stopSimilar = false)
        {
            if (fx == SoundEffectType.None || fx == SoundEffectType.SoundTrack) return null;

            if (!Effects.ContainsKey(fx))
            {
                Effects[fx] = FromFile(GetSFXPath(fx));
            }

            return Play(fx, Effects[fx], volume, pitch, pan, loop, stopSimilar);
        }

        public static void StopSoundtrack()
        {
            StopAllFX(SoundEffectType.SoundTrack);
        }

        public static void StopAllFX(SoundEffectType ofType=SoundEffectType.None)
        {
            if(ofType == SoundEffectType.None)
            {
                Instances.Keys.ToList().ForEach(k =>
                {
                    Instances[k].ForEach(i => { i.Stop(true); i.Dispose(); });
                    Instances[k].Clear();
                });
                return;
            }

            else if (ofType == SoundEffectType.SoundTrack)
            {
                LastSoundTrack = SoundTrackType.None;
            }

            if (Instances.ContainsKey(ofType))
            {
                Instances[ofType].ForEach(i => { i.Stop(true); i.Dispose(); });
                Instances[ofType].Clear();
            }
        }
    }
}
