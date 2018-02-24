using FieryOpal.src.actors;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieryOpal.src.procgen
{
    public class BasicRoomDesigner : BuildingDesigner
    {
        public BasicRoomDesigner(Rectangle area) : base(area)
        {
        }

        protected override void GenerateOntoWorkspace()
        {
            int area_type = Util.GlobalRng.Next(3);
            switch (area_type)
            {
                default:
                    break;
                case 0: // Bookshelves
                    if(Workspace.Width <= 4 || Workspace.Height <= 4)
                    {
                        GenerateOntoWorkspace();
                        return;
                    }

                    bool step_dir = Util.GlobalRng.Next(2) == 0;
                    Point step = new Point(step_dir ? 2 : 1, step_dir ? 1 : 2);
                    for (int x = 1; x < Workspace.Width - 1; x += step.X)
                    {
                        for (int y = 1; y < Workspace.Height - 1; y += step.Y)
                        {
                            var bookshelf = new Bookshelf();
                            bookshelf.ChangeLocalMap(Workspace, new Point(x, y));
                        }
                    }
                    break;
                case 1: // Writing table and chair
                    Point p = new Point(Util.GlobalRng.Next(1, Workspace.Width - 1), Util.GlobalRng.Next(1, Workspace.Height - 1));
                    var chair = new Chair();
                    chair.ChangeLocalMap(Workspace, p);

                    var writing_table = new WritingTable();
                    writing_table.ChangeLocalMap(Workspace, p + Util.RandomUnitPoint(xy: false));
                    Util.Log(String.Format("Chair pos: {0}; Table pos: {1}", p, writing_table.LocalPosition), true);

                    break;
            }
        }
    }
}
