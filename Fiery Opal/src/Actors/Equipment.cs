using System;
using System.Collections.Generic;
using System.Linq;

namespace FieryOpal.Src.Actors
{
    public enum EquipmentSlotType
    {
        Hand,
        Head,
        Torso,
        Arm,
        Leg,
        Foot
    }

    public interface IEquipable
    {
        EquipmentSlotType SlotType { get; }

        int RequiredSlots { get; }

        void OnEquip(IEquipmentUser actor);
        void OnUnequip(IEquipmentUser actor);
    }

    public struct EquipmentSlot
    {
        public EquipmentSlotType Type { get; }
        public int TotalSlots { get; }

        public int AvailableSlots => TotalSlots - Equiped.Sum(e => e.RequiredSlots);

        private List<IEquipable> Equiped;

        public EquipmentSlot(EquipmentSlotType type, int slotCount)
        {
            Type = type;
            TotalSlots = slotCount;
            Equiped = new List<IEquipable>(TotalSlots);
        }

        public bool TryEquip(IEquipable eq, TurnTakingActor actor)
        {
            if (AvailableSlots < eq.RequiredSlots || eq.SlotType != Type) return false;
            if (IsEquiped(eq)) return false;
            Equiped.Add(eq);
            eq.OnEquip(actor);
            return true;
        }

        public bool TryUnequip(IEquipable eq, TurnTakingActor actor)
        {
            if (AvailableSlots == TotalSlots || eq.SlotType != Type) return false;
            if (!IsEquiped(eq)) return false;
            Equiped.Remove(eq);
            eq.OnUnequip(actor);
            return true;
        }

        public bool IsEquiped(IEquipable eq)
        {
            return Equiped.Contains(eq);
        }

        public IEnumerable<IEquipable> GetEquipedItems()
        {
            return Equiped.AsEnumerable();
        }
    }

    public class PersonalEquipment
    {
        private Dictionary<EquipmentSlotType, EquipmentSlot> Slots;

        public PersonalEquipment(Func<EquipmentSlotType, int> slotCounts)
        {
            Slots = new Dictionary<EquipmentSlotType, EquipmentSlot>();
            foreach (EquipmentSlotType t in Enum.GetValues(typeof(EquipmentSlotType)))
            {
                int count = slotCounts(t);
                if (count <= 0) continue;
                Slots[t] = new EquipmentSlot(t, count);
            }
        }

        public bool TryEquip(IEquipable eq, TurnTakingActor actor)
        {
            if (!Slots.ContainsKey(eq.SlotType)) return false;
            return Slots[eq.SlotType].TryEquip(eq, actor);
        }

        public bool TryUnequip(IEquipable eq, TurnTakingActor actor)
        {
            if (!Slots.ContainsKey(eq.SlotType)) return false;
            return Slots[eq.SlotType].TryUnequip(eq, actor);
        }

        public bool IsEquiped(IEquipable eq)
        {
            if (!Slots.ContainsKey(eq.SlotType)) return false;
            return Slots[eq.SlotType].IsEquiped(eq);
        }

        public IEnumerable<IEquipable> GetContents()
        {
            return Slots.Values.SelectMany(s => s.GetEquipedItems());
        }
    }
}
