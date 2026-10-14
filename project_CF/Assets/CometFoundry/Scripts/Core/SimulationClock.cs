using System;

namespace CometFoundry.Core
{
    [Serializable]
    public sealed class SimulationClock
    {
        public const double SecondsPerDay = 86400d;
        public const double DefaultRealSecondsPerGameDay = 300d;

        public double Day { get; private set; }
        public double TimeScale { get; set; } = SecondsPerDay / DefaultRealSecondsPerGameDay;

        public void AdvanceSeconds(double deltaSeconds)
        {
            if (deltaSeconds <= 0d || TimeScale <= 0d)
            {
                return;
            }

            Day += deltaSeconds * TimeScale / SecondsPerDay;
        }
    }
}
