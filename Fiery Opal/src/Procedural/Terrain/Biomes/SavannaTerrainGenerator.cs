using FieryOpal.Src.Lib;
using FieryOpal.Src.Actors;
using FieryOpal.Src.Ui;
using Microsoft.Xna.Framework;
using SadConsole;
using System.Collections.Generic;
using System.Linq;

namespace FieryOpal.Src.Procedural.Terrain.Biomes
{
    public class DryGrassSkeleton : GrassSkeleton
    {
        public override string DefaultName => "Dry Grass";
        public override Cell DefaultGraphics => new Cell(Palette.Terrain["DryGrassForeground"], Palette.Terrain["DryGrassBackground"], ';');
    }

    public class UmbrellaThornAcacia : Plant
    {
        public UmbrellaThornAcacia()
        {
            PossibleGlyphs = new[] { (int)'T' };

            PossibleColors = new[] {
                Palette.Vegetation["GenericPlant1"],
                Palette.Vegetation["GenericPlant2"],
                Palette.Vegetation["GenericPlant3"],
            };

            FirstPersonVerticalOffset = -35f;
            FirstPersonScale = new Vector2(.50f, .1f);

            SetGraphics();
        }
    }

    public class ShortDryGrass : Plant
    {
        public override bool BlocksMovement => false;

        public ShortDryGrass()
        {
            PossibleGlyphs = new[] { 19 };

            PossibleColors = new[] {
                Palette.Vegetation["ShortDryGrass"],
            };

            FirstPersonVerticalOffset = 1.4f;
            FirstPersonScale = new Vector2(2f, 1.2f);

            SetGraphics();
        }
    }

    public class SavannaTerrainGenerator : BiomeTerrainGenerator
    {
        protected SavannaTerrainGenerator(WorldTile worldPos) : base(worldPos) { }

        public override void Generate(OpalLocalMap m)
        {
            base.Generate(m);

            var trees = PoissonDiskSampler.SampleRectangle(new Vector2(0), new Vector2(m.Width, m.Height), 10f).Select(v => v.ToPoint());
            Tiles.Iter((s, x, y, t) =>
            {
                s.SetTile(x, y, OpalTile.GetRefTile<DryGrassSkeleton>());
                if(trees.Contains(new Point(x, y)))
                {
                    var tree = new UmbrellaThornAcacia();
                    tree.ChangeLocalMap(s, new Point(x, y));
                }
                else
                {
                    var tree = new ShortDryGrass();
                    tree.ChangeLocalMap(s, new Point(x, y));
                }
                return false;
            });
        }
    }
}
