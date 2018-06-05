using FieryOpal.src.Actors.Animals;
using FieryOpal.src.Actors.Decorations;
using FieryOpal.Src;
using FieryOpal.Src.Lib;
using FieryOpal.Src.Procedural;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FieryOpal.src.Procedural.Terrain.Dungeons
{
    public class CavesTerrainDecorator : TerrainDecoratorBase
    {
        public int Depth { get; }
        protected List<Point> BoulderPoisson;

        public CavesTerrainDecorator(int depth) : base()
        {
            Depth = depth;
        }

        public override void Generate(OpalLocalMap m)
        {
            base.Generate(m);
            BoulderPoisson = PoissonDiskSampler.SampleRectangle(new Vector2(0, 0), new Vector2(m.Width, m.Height), 9).Select(v => v.ToPoint()).ToList();

            int n_anims = (int)Math.Sqrt(m.Width * m.Height) / 10;
            for (int i = 0; i < n_anims; ++i)
            {
                OpalActorBase anim = null;
                switch (Util.Rng.Next(6))
                {
                    case 0:
                        anim = new Mole();
                        break;
                    case 1:
                        anim = new CaveBat();
                        break;
                    case 2:
                        anim = new CaveBear();
                        break;
                    case 3:
                        anim = new CaveLeech();
                        break;
                    case 4:
                        anim = new Rat();
                        break;
                    case 5:
                    default:
                        anim = new GiantCaveSpider();
                        break;

                }
                anim.ChangeLocalMap(m, m.FirstAccessibleTileAround(new Point(Util.Rng.Next(m.Width), Util.Rng.Next(m.Height))));
            }
        }

        public override IDecoration GetDecoration(int x, int y)
        {
            if (CurrentMap.TileAt(x, y).Properties.BlocksMovement) return null;
            if (BoulderPoisson.Contains(new Point(x, y))) return (Util.CoinToss() ? new Boulders() : (IDecoration)new Fungus());
            return base.GetDecoration(x, y);
        }
    }
}
