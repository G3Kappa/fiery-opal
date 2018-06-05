using System;
using System.Collections.Generic;

namespace FieryOpal.Src.Procedural
{
    public interface ISocialCreature { }

    public enum Industry
    {
        Agriculture,
        Livestock,
        Handicraft,
        Merchantilism
    }

    public class SocialRank
    {
        public string Title { get; }
        protected float InternalValue { get; }

        public SocialRank(string title, float internal_value)
        {
            Title = title;
            InternalValue = internal_value;
        }

        public override bool Equals(object obj)
        {
            if (obj is SocialRank)
                return (obj as SocialRank).InternalValue == InternalValue;
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator <(SocialRank a, SocialRank b)
        {
            return a.InternalValue < b.InternalValue;
        }

        public static bool operator >(SocialRank a, SocialRank b)
        {
            return a.InternalValue > b.InternalValue;
        }

        public static bool operator ==(SocialRank a, SocialRank b)
        {
            return a.InternalValue == b.InternalValue;
        }

        public static bool operator !=(SocialRank a, SocialRank b)
        {
            return a.InternalValue != b.InternalValue;
        }

        public static bool operator <=(SocialRank a, SocialRank b)
        {
            return a.InternalValue <= b.InternalValue;
        }

        public static bool operator >=(SocialRank a, SocialRank b)
        {
            return a.InternalValue >= b.InternalValue;
        }
    }

    public abstract class SocialStructure
    {
        public abstract SocialRank GetRank(ISocialCreature c);
    }

    public interface IInstitution
    {
        SocialStructure SocialStructure { get; }
    }

    public abstract class Settlement
    {
        protected Dictionary<Industry, float> DevelopedIndustries;

        public void SetIndustryDevelopment(Industry i, float development)
        {
            DevelopedIndustries[i] = development;
        }

        public float GetIndustryDevelopment(Industry i)
        {
            return DevelopedIndustries[i];
        }

        // "Congregation of Iozwa"
        public string Name { get; protected set; }
        // "Opalism"
        public Religion StateReligion { get; protected set; }
        // 2195 Gold pieces
        public int Wealth { get; protected set; }
        // 20 world-map tiles
        public int Influence { get; protected set; }

        public Settlement(string name)
        {
            Name = name;
            DevelopedIndustries = new Dictionary<Industry, float>();
            foreach (Industry key in Enum.GetValues(typeof(Industry)))
            {
                DevelopedIndustries[key] = 0.0f; // 0 = undeveloped
            }
        }
    }
}
