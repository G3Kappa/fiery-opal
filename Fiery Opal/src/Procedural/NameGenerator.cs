using System;
using System.Collections.Generic;
using System.Linq;

namespace FieryOpal.Src.Procedural
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

            public bool IsMale => Gender.HasValue && Gender.Value;
            public bool IsFemale => Gender.HasValue && !Gender.Value;
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
            if (dinfo.IsFemale) return "goddess";
            else return "spirit";
        }

        private string GetProperNoun(DeityNameGeneratorInfo dinfo)
        {
            string[] syllables = new[]
            {
                "ak", "pha", "ra", "sa", "sha",
                "en", "te", "me", "les", "ve",
                "ik", "di", "phi", "bi", "zi",
                "or", "on", "ko", "go", "xo",
                "ux", "uk", "ku", "gu", "ru",
                "n", "l", "s", "t", "k",
                "a", "e", "i", "o", "u",
                "-", "'"
            };

            int length = Util.Rng.Next(2, 5);

            string name;
            var excludedExtremes = new[] { '-', '\'' };
            do
            {
                name = String.Join("", Util.ChooseN(syllables, length)).CapitalizeFirst();
            } while (excludedExtremes.Contains(name[0]) || excludedExtremes.Contains(name[name.Length - 1]));
            return name;
        }

        public override string GetName(NameGeneratorInfo<DeityBase> info)
        {
            var dinfo = (info as DeityNameGeneratorInfo);
            if (dinfo == null) return GetProperNoun(dinfo);

            return String.Format(
                "{0}, {1} of {2}.",
                GetProperNoun(dinfo),
                GetGenderedNoun(dinfo),
                JoinSpheres(dinfo)
            );
        }
    }

}
