using FieryOpal.Src.Ui;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieryOpal.Src
{
    public class DayNightCycleManager
    {
        public int DayLength { get; }
        public int GradientSteps { get; }
        public float CurrentTime { get; private set; }
        private float DayProgress;

        protected List<Color> BottomGradient { get; }
        protected List<Color> TopGradient { get; }
        protected Color[] LerpLookup { get; }

        public DayNightCycleManager(int dayLengthInTurns=1000, int gradientSteps=40)
        {
            DayLength = dayLengthInTurns;
            GradientSteps = gradientSteps;
            LerpLookup = new Color[GradientSteps];
            BottomGradient = new List<Color>();
            TopGradient = new List<Color>();

            /*0 AM*/
            BottomGradient.Add(new Color(0, 33, 77));
            TopGradient.Add(new Color(33, 22, 110));
            /*1 AM*/
            BottomGradient.Add(new Color(0, 44, 99));
            /*2 AM*/
            BottomGradient.Add(new Color(0, 44, 99));
            TopGradient.Add(new Color(0, 11, 66));
            /*3 AM*/
            BottomGradient.Add(new Color(0, 55, 110));
            /*4 AM*/
            BottomGradient.Add(new Color(0, 55, 110));
            TopGradient.Add(new Color(0, 33, 77));
            /*5 AM*/
            BottomGradient.Add(new Color(0, 99, 144));

            /*6 AM*/
            BottomGradient.Add(new Color(0, 110, 155));
            TopGradient.Add(new Color(0, 44, 99));
            /*7 AM*/
            BottomGradient.Add(new Color(11, 155, 188));
            /*8 AM*/
            BottomGradient.Add(new Color(110, 210, 199));
            TopGradient.Add(new Color(0, 99, 144));
            /*9 AM*/
            BottomGradient.Add(new Color(232, 243, 188));
            /*10 AM*/
            BottomGradient.Add(new Color(255, 232, 88));
            TopGradient.Add(new Color(0, 110, 155));
            /*11 AM*/
            BottomGradient.Add(new Color(255, 199, 77));

            /*12 AM*/
            BottomGradient.Add(new Color(255, 199, 110));
            /*1 PM*/
            BottomGradient.Add(new Color(255, 177, 88));
            /*2 PM*/
            BottomGradient.Add(new Color(255, 177, 88));
            /*3 PM*/
            BottomGradient.Add(new Color(243, 133, 66));
            /*4 PM*/
            BottomGradient.Add(new Color(243, 110, 121));
            /*5 PM*/
            BottomGradient.Add(new Color(210, 88, 144));


            /*6 PM*/
            BottomGradient.Add(new Color(210, 88, 144));
            /*7 PM*/
            BottomGradient.Add(new Color(99, 44, 133));
            /*8 PM*/
            BottomGradient.Add(new Color(99, 44, 133));
            /*9 PM*/
            BottomGradient.Add(new Color(33, 22, 110));
            /*10 PM*/
            BottomGradient.Add(new Color(33, 22, 110));
            /*11 PM*/
            BottomGradient.Add(new Color(0, 11, 66));
        }

        public void UpdateLocal(OpalLocalMap m)
        {
            if(!m.Indoors)
            {
                var ambient = GetAmbientLightIntensity();
                m.AmbientLightIntensity = ambient;
            }
        }

        public Color GetSkyColor(float y, bool cached=true)
        {
            if (cached) return LerpLookup[Math.Min(GradientSteps - 1, (int)(y * GradientSteps))];

            float bf = BottomGradient.Count * DayProgress;
            int b_idx = (int)bf;
            float b_lerp = bf - b_idx;

            float tf = TopGradient.Count * DayProgress;
            int t_idx = (int)tf;
            float t_lerp = tf - t_idx;

            return Color.Lerp(
                Color.Lerp(BottomGradient[b_idx], BottomGradient[(b_idx + 1) % BottomGradient.Count], b_lerp),
                Color.Lerp(TopGradient[t_idx], TopGradient[(t_idx + 1) % TopGradient.Count], t_lerp),
                y
            );
        }

        private float GetAmbientLightIntensity()
        {
            return .85f * (1 - 2 * Math.Abs(DayProgress - .5f)) + .15f;
        }

        public int GetBaseViewDistance(bool indoors)
        {
            return indoors ? 32 : (int)(GetAmbientLightIntensity() * 56) + 8;
        }

        public void Update(float turnsElapsed=1)
        {
            CurrentTime = (CurrentTime + turnsElapsed) % DayLength;
            DayProgress = (CurrentTime / DayLength);
            for(int y = 0; y < GradientSteps; ++y)
            {
                LerpLookup[y] = GetSkyColor(Math.Max(1.75f * y / GradientSteps - .75f, 0), false);
            }
        }
    }
}
