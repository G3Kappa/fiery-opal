using System;
using System.Collections.Generic;

namespace FieryOpal.src.procgen
{

    public abstract class NameGenerator<T>
    {
        public abstract class NameGeneratorInfo<Y>
            where Y : T
        { }

        public abstract string GetName(NameGeneratorInfo<T> info);
    }

    public class DeityNameGenerator : NameGenerator<DeityBase>
    {
        public class DeityNameGeneratorInfo : NameGeneratorInfo<DeityBase>
        {
            public IEnumerable<DivineSphere> Spheres;
            public bool? Gender;

            public bool IsMale    => Gender.HasValue && Gender.Value;
            public bool IsFemale  => Gender.HasValue && !Gender.Value;
            public bool IsAsexual => !Gender.HasValue;
        }

        private string JoinSpheres(DeityNameGeneratorInfo dinfo)
        {
            List<string> spheres = new List<string>();
            foreach (DivineSphere sphere in dinfo.Spheres)
            {
                spheres.Add(Enum.GetName(typeof(DivineSphere), sphere).ToLower());
            }

            if (spheres.Count == 1) return spheres[0];

            var s_spheres_2 = spheres[spheres.Count - 1];
            spheres.RemoveAt(spheres.Count - 1);

            var s_spheres_1 = String.Join(", ", spheres);

            return s_spheres_1 + " and " + s_spheres_2;
        }

        private string GetGenderedNoun(DeityNameGeneratorInfo dinfo)
        {
            if (dinfo.IsMale) return "god";
            if (dinfo.IsFemale) return "goddes";
            else return "spirit";
        }

        private string GetProperNoun(DeityNameGeneratorInfo dinfo)
        {
            string[] syllables = new[]
            {
                "al", "sh", "ox", "mo", "pi", "ba", "ku", "ze", "wa", "ru",
                "trum", "flo", "kro", "xus", "ur", "ist", "ka", "nos", "dio",
                "ne", "ko", "por", "qin", "-", "'"
            };

            int length = Util.GlobalRng.Next(2, 5);
            return String.Join("", Util.ChooseN(syllables, length)).CapitalizeFirst();
        }

        public override string GetName(NameGeneratorInfo<DeityBase> info)
        {
            var dinfo = (info as DeityNameGeneratorInfo);
            return String.Format(
                "{0}, {1} of {2}.",
                GetProperNoun(dinfo), 
                GetGenderedNoun(dinfo), 
                JoinSpheres(dinfo)
            );
        }
    }

}
