using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieryOpal.src.procgen
{
    public enum Ethics
    {
        MurderIsWrong,
        TheftIsWrong,
        DeceptionIsWrong,
        AdulteryIsWrong,
        HeresyIsWrong,
    }

    public enum DivineSphere
    {
        Music,
        Poetry,
        Art,
        Love,
        Beauty,
        Desire,
        Pleasure,
        Knowledge,
        Healing,
        Prophecy,
        Chaos,
        War,
        Bloodshed,
        Violence,
        Hunt,
        Wilderness,
        Animals,
        Fertility,
        Reason,
        Wisdom,
        Skill,
        Peace,
        Strategy,
        Handicrafts,
        Merchants,
        Harvest,
        Growth,
        Nourishment,
        Party,
        Death,
        Life,
        Wealth,
        Metalworking,
        Marriage,
        Travel,
        Language,
        Home,
        Sea,
        Rivers,
        Floods,
        Droughts,
        Earthquakes,
        Justice,
        Order,
        Consciousness,
        Empathy,
        Beggars,
        Theft,
        Sun,
        Day,
        Moon,
        Night,
        Stars,
        Magic,
        Occult,
        Rebellion,
        Faith,
        Mountains,
        Forests,
        Light,
        Freedom,
        Lightning,
        Gluttony,
        Restraint,
        Perseverance,
        Vigilance,
        Vengeance,
        Retribution,
        Darkness,
        Hate,
        Separation,
        Wholsomeness,
        Passion,
        Torture,
        Power,
        Wickedness,
        Pain,
        Suicide,
        Despair,
    }

    public class DeityBase : ISocialCreature
    {
        public string Name { get; protected set; }
        public IEnumerable<DivineSphere> Spheres { get; }

        public DeityBase(string name, IEnumerable<DivineSphere> spheres)
        {
            Name = name;
            Spheres = spheres.ToList();
        }
    }

    public class Pantheon : SocialStructure
    {
        protected Dictionary<DeityBase, SocialRank> GodRanks;

        public Pantheon(IEnumerable<Tuple<DeityBase, SocialRank>> deities)
        {
            GodRanks = new Dictionary<DeityBase, SocialRank>();
            foreach (var god_rank in deities)
            {
                GodRanks.Add(god_rank.Item1, god_rank.Item2);
            }
        }

        public override SocialRank GetRank(ISocialCreature c)
        {
            if (c is DeityBase && GodRanks.ContainsKey(c as DeityBase))
            {
                return GodRanks[c as DeityBase];
            }

            return null;
        }

        public IEnumerable<DeityBase> Deities => GodRanks.Keys.AsEnumerable();
    }

    public abstract class Religion : IInstitution
    {
        public string Name { get; protected set; }
        public Pantheon Pantheon { get; protected set; }
        public SocialStructure SocialStructure { get; protected set; }

        protected Dictionary<Ethics, float> EthicalStandings;

        public void SetEthicalStanding(Ethics i, float standing)
        {
            EthicalStandings[i] = standing;
        }

        public float GetEthicalStanding(Ethics i)
        {
            return EthicalStandings[i];
        }

        public Religion(string name)
        {
            Name = name;
            foreach (Ethics key in Enum.GetValues(typeof(Ethics)))
            {
                EthicalStandings[key] = 0.0f; // 0 = neutral
            }
        }
    }

    public abstract class DeityGenerator<T>
        where T : DeityBase
    {
        public DeityNameGenerator NameGenerator { get; protected set; }

        public DeityGenerator()
        {
            NameGenerator = new DeityNameGenerator();
        }

        public abstract T Generate();
    }

    public class GoodDeityGenerator : DeityGenerator<DeityBase>
    {
        public static DivineSphere[] AllowedSpheres = new[]
        {
            DivineSphere.Faith,
            DivineSphere.Empathy,
            DivineSphere.Light,
            DivineSphere.Love,
            DivineSphere.Marriage,
            DivineSphere.Peace,
            DivineSphere.Prophecy,
            DivineSphere.Poetry,
            DivineSphere.Art,
            DivineSphere.Music,
            DivineSphere.Fertility,
            DivineSphere.Freedom,
            DivineSphere.Healing,
            DivineSphere.Sun,
            DivineSphere.Day,
            DivineSphere.Life,
            DivineSphere.Order,
            DivineSphere.Wisdom,
            DivineSphere.Justice,
            DivineSphere.Perseverance,
            DivineSphere.Consciousness,
            DivineSphere.Wholsomeness,
        };

        public override DeityBase Generate()
        {
            DeityNameGenerator.DeityNameGeneratorInfo dinfo = new DeityNameGenerator.DeityNameGeneratorInfo()
            {
                Gender = Util.RandomTernary(),
                Spheres = Util.ChooseN(AllowedSpheres, Util.GlobalRng.Next(1, 5))
            };

            return new DeityBase(NameGenerator.GetName(dinfo), dinfo.Spheres);
        }
    }

    public class BadDeityGenerator : DeityGenerator<DeityBase>
    {
        public static DivineSphere[] AllowedSpheres = new[]
        {
            DivineSphere.Bloodshed,
            DivineSphere.Chaos,
            DivineSphere.Death,
            DivineSphere.Darkness,
            DivineSphere.Hate,
            DivineSphere.War,
            DivineSphere.Desire,
            DivineSphere.Droughts,
            DivineSphere.Lightning,
            DivineSphere.Pleasure,
            DivineSphere.Violence,
            DivineSphere.Suicide,
            DivineSphere.Despair,
            DivineSphere.Moon,
            DivineSphere.Night,
            DivineSphere.Wealth,
            DivineSphere.Torture,
            DivineSphere.Pain,
            DivineSphere.Wickedness,
            DivineSphere.Power,
            DivineSphere.Passion,
        };

        public override DeityBase Generate()
        {
            DeityNameGenerator.DeityNameGeneratorInfo dinfo = new DeityNameGenerator.DeityNameGeneratorInfo()
            {
                Gender = Util.RandomTernary(),
                Spheres = Util.ChooseN(AllowedSpheres, Util.GlobalRng.Next(1, 5))
            };

            return new DeityBase(NameGenerator.GetName(dinfo), dinfo.Spheres);
        }
    }

}
