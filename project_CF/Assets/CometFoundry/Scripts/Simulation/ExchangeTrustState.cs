using System;

namespace CometFoundry.Simulation
{
    [Serializable]
    public sealed class ExchangeTrustState
    {
        public const int MinLevel = 0;
        public const int MaxLevel = 3;

        private static readonly int[] requiredExperienceByLevel = { 100, 300, 900, 27000 };

        public int Level { get; private set; }
        public int Experience { get; private set; }
        public int RequiredExperience => GetRequiredExperience(Level);

        public void AddExperience(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Experience += amount;
            while (Level < MaxLevel && Experience >= GetRequiredExperience(Level))
            {
                Experience -= GetRequiredExperience(Level);
                Level++;
            }

            if (Level >= MaxLevel)
            {
                Level = MaxLevel;
                Experience = Math.Min(Experience, GetRequiredExperience(MaxLevel));
            }
        }

        public static int GetRequiredExperience(int level)
        {
            int clampedLevel = Math.Max(MinLevel, Math.Min(MaxLevel, level));
            return requiredExperienceByLevel[clampedLevel];
        }
    }
}
