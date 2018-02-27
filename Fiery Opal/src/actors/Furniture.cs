using FieryOpal.src.ui;
using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieryOpal.src.actors
{
    public abstract class Furniture : DecorationBase
    {
        public override bool BlocksMovement => true;
    }
    public abstract class Seating : Furniture, IInteractive
    {
        bool IsOccupied => Occupant != null;

        OpalActorBase occupant = null;
        OpalActorBase Occupant => occupant;

        public Seating()
        {

        }

        public bool InteractWith(OpalActorBase actor)
        {
            if (occupant == actor)
            {
                occupant.ReleaseMovement(this);
                occupant = null;
                return true;
            }
            if (IsOccupied) return false;
            occupant = actor;
            occupant.MoveTo(LocalPosition, absolute: true);
            occupant.BlockMovement(this);
            return true;
        }
    }

    public class Chair : Seating
    {
        public Chair()
        {
            Graphics = FirstPersonGraphics = new ColoredGlyph(new Cell(Palette.Terrain["DirtForeground"], Palette.Terrain["DirtBackground"], 208));
            FirstPersonVerticalOffset = 2f;
            FirstPersonScale = new Vector2(2, 2);
        }
    }

    public class MapGlobe : Furniture, IInteractive
    {
        public MapGlobe()
        {
            Graphics = FirstPersonGraphics = new ColoredGlyph(new Cell(Palette.Terrain["DirtForeground"], Palette.Terrain["DirtBackground"], 'o'));
            FirstPersonVerticalOffset = 0f;
            FirstPersonScale = new Vector2(1f, 1f);
        }

        public bool InteractWith(OpalActorBase actor)
        {
            throw new NotImplementedException();
        }
    }

    public abstract class Workbench : Furniture, IInteractive
    {
        public override bool DisplayAsBlock => true;

        public abstract bool InteractWith(OpalActorBase actor);
        public Workbench()
        {
            Graphics = FirstPersonGraphics = new ColoredGlyph(new Cell(Color.White, Color.White, 209));
            FirstPersonVerticalOffset = 0f;
            FirstPersonScale = new Vector2(1f, 1f);
        }
    }

    public class WritingTable : Workbench
    {
        public override bool InteractWith(OpalActorBase actor)
        {
            throw new NotImplementedException();
        }
    }

    public abstract class PhysicalStorage<T> : Furniture
        where T : Item
    {
        public ItemContainer<T> Contents { get; }

        public PhysicalStorage(int cap)
        {
            Contents = new ItemContainer<T>(cap);
        }
    }

    public class Bookshelf : PhysicalStorage<Book>, IInteractive
    {
        public override bool DisplayAsBlock => true;

        public Bookshelf() : base(16)
        {
            Graphics = FirstPersonGraphics = new ColoredGlyph(new Cell(Color.White, Color.White, 245));
            FirstPersonVerticalOffset = 0f;
            FirstPersonScale = new Vector2(1f, 1f);
        }

        public bool InteractWith(OpalActorBase actor)
        {
            return true;
        }
    }

    public class ItemChest : PhysicalStorage<Item>, IInteractive
    {
        public ItemChest() : base(10)
        {
            Graphics = FirstPersonGraphics = new ColoredGlyph(new Cell(Color.White, Color.White, 243));
            FirstPersonVerticalOffset = 0f;
            FirstPersonScale = new Vector2(1f, 1f);
        }

        public bool InteractWith(OpalActorBase actor)
        {
            return true;
        }
    }
}
