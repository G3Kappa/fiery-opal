using FieryOpal.Src.Actors;
using FieryOpal.Src.Procedural;
using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieryOpal.Src.Quests
{
    public abstract class QuestObjective
    {
        public ColoredString Descrption { get; protected set; }
        public float Progress { get; protected set; }
        public bool Completed => !Failed && Progress >= 1;
        public bool Optional { get; } = false;
        public bool Failed { get; private set; } = false;

        public QuestObjective(ColoredString desc, bool optional)
        {
            Descrption = desc;
            Optional = optional;
        }

        public virtual void UpdateProgress(TurnTakingActor actor)
        {
        }
    }

    public abstract class Quest
    {
        public ColoredString Name { get; }
        public ColoredString Descrption { get; }

        protected List<QuestObjective> Objectives { get; }
        public bool Completed => Objectives.All(o => o.Optional || o.Completed);
        public bool Failed    => Objectives.Any(o => !o.Optional && o.Failed);

        public Quest(ColoredString name, ColoredString desc, params QuestObjective[] objectives)
        {
            Name = name;
            Descrption = desc;
            Objectives = new List<QuestObjective>();
            Objectives.AddRange(objectives);
        }

        public virtual void Start(Quest previousStage)
        {

        }

        public virtual bool UpdateProgress(TurnTakingActor actor)
        {
            Objectives.ForEach(o => { if (!o.Failed && !o.Completed) o.UpdateProgress(actor); });
            if (Completed)
            {
                QuestCompleted?.Invoke(this);
                return true;
            }
            else if(Failed)
            {
                QuestFailed?.Invoke(this);
                return true;
            }
            return false;
        }

        public IEnumerable<QuestObjective> GetObjectives()
        {
            return Objectives.AsEnumerable();
        }

        public event Action<Quest> QuestFailed;
        public event Action<Quest> QuestCompleted;
    }

    public class QuestManager
    {
        protected List<Quest> Completed { get; }
        protected List<Quest> InProgress { get; }

        public TurnTakingActor ParentActor { get; }

        public QuestManager(TurnTakingActor act)
        {
            ParentActor = act;
            Completed = new List<Quest>();
            InProgress = new List<Quest>();
        }

        public void UpdateProgress()
        {
            InProgress.ToList().ForEach(q => q.UpdateProgress(ParentActor));
        }

        private void OnQuestComplete(Quest q)
        {
            Completed.Add(q);
            InProgress.Remove(q);
        }

        public void StartQuest(Quest q)
        {
            InProgress.Add(q);
            q.QuestCompleted += OnQuestComplete;
            q.QuestFailed += OnQuestComplete;
            q.Start(null);
        }

        public IEnumerable<Quest> GetActiveQuests()
        {
            return InProgress.AsEnumerable();
        }

        public IEnumerable<Quest> GetCompletedQuests()
        {
            return Completed.AsEnumerable();
        }
    }
}
