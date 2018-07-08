using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FieryOpal.Src.Actors;
using FieryOpal.Src.Actors.Items;
using FieryOpal.Src.Ui;
using Microsoft.Xna.Framework;
using SadConsole;

namespace FieryOpal.Src.Quests
{
    public class GatheringQuestObjective<T> : QuestObjective
        where T : OpalItem
    {
        private static ColoredString GenerateDescription(int progress, int qty)
        {
            return "Gather {2:#F00} [{0:#077}/{1:#0FF}].".FmtC(Palette.Ui["DefaultBackground"], null, progress, qty, typeof(T).Name);
        }

        public int ObjectsGathered { get; private set; }
        public int ObjectsRequired { get; private set; }

        public GatheringQuestObjective(bool optional, int quantity) : base(GenerateDescription(0, quantity), optional)
        {
            ObjectsRequired = quantity;
            ObjectsGathered = 0;
        }

        public override void UpdateProgress(TurnTakingActor actor)
        {
            base.UpdateProgress(actor);

            ObjectsGathered = actor.Inventory.GetContents().Count(item => item.GetType() == typeof(T));
            Progress = ObjectsGathered / (float)ObjectsRequired;
            Descrption = GenerateDescription(ObjectsGathered, ObjectsRequired);
        }
    }

    public class GatheringQuest : Quest
    {
        public GatheringQuest(ColoredString name, ColoredString desc) : base(name, desc)
        {

        }

        public void AddObjective<T>(GatheringQuestObjective<T> objective)
            where T : OpalItem
        {
            Objectives.Add(objective);
        }
    }
}
