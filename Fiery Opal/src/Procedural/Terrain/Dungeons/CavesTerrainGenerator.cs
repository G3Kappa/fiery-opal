using FieryOpal.Src.Audio;
using FieryOpal.Src.Procedural.Terrain.Tiles;
using FieryOpal.Src.Procedural.Terrain.Tiles.Skeletons;
using Microsoft.Xna.Framework;
using System;
using System.Linq;

namespace FieryOpal.Src.Procedural.Terrain.Dungeons
{
    public class CavesTerrainGenerator : TerrainGeneratorBase
    {
        public int Depth { get; }

        public CavesTerrainGenerator(WorldTile region, int depth) : base(region)
        {
            Depth = depth;
        }

        public override void Generate(OpalLocalMap m)
        {
            base.Generate(m);

            OpalTile wallTile = OpalTile.GetRefTile<RockWallSkeleton>();
            OpalTile floorTile = OpalTile.GetRefTile<RockFloorSkeleton>();

            Workspace.Iter((s, x, y, t) =>
            {
                s.SetTile(x, y, wallTile);
                return false;
            });

            var allPartitions = GenUtil.Partition(new Rectangle(0, 0, m.Width, m.Height), .03f).ToList();
            var partitions = Util.ChooseN(allPartitions, (int)(allPartitions.Count * .16f))
                             .Select(r => new Rectangle(r.X + Util.Rng.Next(-r.Width / 2, r.Width / 2), r.Y + Util.Rng.Next(-r.Height / 2, r.Height / 2), r.Width, r.Height));

            foreach (var r in partitions)
            {
                Workspace.Iter((s, x, y, t) =>
                {
                    s.SetTile(x, y, floorTile);
                    return false;
                }, r);
            }

            GenUtil.ConnectEnclosedAreas(Workspace, GenUtil.GetEnclosedAreasAndCentroids(Workspace, t => t == floorTile), floorTile, 3, 4, 1000, 2);

            // If some areas are left unconnected, fill them
            var enclosed = GenUtil.GetEnclosedAreasAndCentroids(Workspace, t => t == floorTile).ToList();
            var max = enclosed.MaxBy(tup => tup.Item1.Count()).Item1.Count();
            int areasFilled = 0;
            int cellsFilled = 0;
            foreach (var tup in enclosed)
            {
                int cur = tup.Item1.Count();
                if (cur == max) continue;

                var p = Util.Choose(tup.Item1.ToList());
                Workspace.FloodFill(p.X, p.Y, wallTile).ToList();
                areasFilled++;
                cellsFilled += cur;
            }
            if (areasFilled > 0)
            {
                if ((float)cellsFilled / (Workspace.Width * Workspace.Height) >= .2f)
                {
                    Util.LogText("CavesTerrainGenerator.Generate: Too many unconnected areas, regenerating terrain.", true);
                    Generate(m);
                    return;
                }
                else
                {
                    Util.LogText("CavesTerrainGenerator.Generate: Filled {0} unconnected areas, for a total of {1} out of {2} map cells filled."
                        .Fmt(areasFilled, cellsFilled, Workspace.Width * Workspace.Height), true);
                }
            }

            GenUtil.MatrixReplacement.CaveSystemRules.SlideAcross(
                Workspace,
                new Point(1),
                new GenUtil.MRRule(t => t == floorTile, (_) => floorTile),
                new GenUtil.MRRule(t => t == wallTile, (_) => wallTile),
                10,
                break_early: false,
                shuffle: true,
                randomize_order: true
            );

            m.CeilingTile = TileSkeleton.Get<RockWallSkeleton>();
            m.SoundTrack = SFXManager.SoundTrackType.Caves;
            m.SoundEffects.Add(new Tuple<SFXManager.SoundEffectType, float>(SFXManager.SoundEffectType.Eerie01, .003f));
            m.AmbientLightIntensity = .15f;
            m.Indoors = true;
        }

    }
}
