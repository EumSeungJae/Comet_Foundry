using System;

namespace CometFoundry.Simulation
{
    public readonly struct PowerReport
    {
        public PowerReport(double generatedPower, double consumedPower)
        {
            GeneratedPower = Math.Max(0d, generatedPower);
            ConsumedPower = Math.Max(0d, consumedPower);
        }

        public double GeneratedPower { get; }
        public double ConsumedPower { get; }
        public double AvailablePower => GeneratedPower - ConsumedPower;
        public bool HasDeficit => AvailablePower < 0d;
    }

    public sealed class PowerSystem
    {
        public const int DronesPerPower = 4;

        public PowerReport Calculate(CometState comet)
        {
            if (comet == null)
            {
                return new PowerReport(0d, 0d);
            }

            double generated = 0d;
            double consumed = 0d;

            for (int i = 0; i < comet.Facilities.Count; i++)
            {
                FacilityInstance facility = comet.Facilities[i];
                generated += facility.Definition?.PowerOutput ?? 0d;
                consumed += facility.Definition?.BasePowerConsumption ?? 0d;
            }

            for (int i = 0; i < comet.DroneLinks.Count; i++)
            {
                consumed += GetDronePowerConsumption(comet.DroneLinks[i].AssignedDroneCount);
            }

            return new PowerReport(generated, consumed);
        }

        public static int GetDronePowerConsumption(int droneCount)
        {
            if (droneCount <= 0)
            {
                return 0;
            }

            return (int)Math.Ceiling(droneCount / (double)DronesPerPower);
        }
    }
}
