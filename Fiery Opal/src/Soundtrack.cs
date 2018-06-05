using Microsoft.Xna.Framework.Media;
using System;

namespace FieryOpal.Src
{
    public static class Soundtrack
    {
        static Soundtrack()
        {
            MediaPlayer.IsRepeating = true;
        }

        public enum TrackName
        {
            No_Track = 0,
            Caves = 1
        }

        private static string GetTrackNameStr(TrackName track)
        {
            switch (track)
            {
                default:
                    return Enum.GetName(typeof(TrackName), track);
            }
        }

        public static void Play(TrackName track)
        {
            if (track == TrackName.No_Track)
            {
                Stop();
                return;
            }

            var song = Util.LoadContent<Song>(GetTrackNameStr(track));
            MediaPlayer.Play(song);
        }

        public static void Stop()
        {
            MediaPlayer.Stop();
        }
    }
}
