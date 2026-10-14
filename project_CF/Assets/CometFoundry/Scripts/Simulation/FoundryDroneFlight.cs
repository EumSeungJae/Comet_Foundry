using System;
using System.Collections.Generic;
using UnityEngine;

namespace CometFoundry.Simulation
{
    public enum FoundryDronePhase { Pickup, Delivery, Returning }

    public sealed class FoundryDroneFlight
    {
        internal FoundryDroneFlight(FacilityInstance provider, int slot, DroneHaulRequest request,
            IReadOnlyList<Vector2> pickup, IReadOnlyList<Vector2> delivery, IReadOnlyList<Vector2> returning,
            double pickupSeconds, double deliverySeconds, double returnSeconds)
        {
            Provider = provider; Slot = slot; Request = request;
            PickupPath = pickup; DeliveryPath = delivery; ReturnPath = returning;
            PickupSeconds = pickupSeconds; DeliverySeconds = deliverySeconds; ReturnSeconds = returnSeconds;
        }
        public FacilityInstance Provider { get; }
        public int Slot { get; }
        public DroneHaulRequest Request { get; }
        public IReadOnlyList<Vector2> PickupPath { get; }
        public IReadOnlyList<Vector2> DeliveryPath { get; }
        public IReadOnlyList<Vector2> ReturnPath { get; }
        public double Elapsed { get; internal set; }
        public bool Delivered { get; internal set; }
        public double DeliveredStacks { get; internal set; }
        public double PickupSeconds { get; }
        public double DeliverySeconds { get; }
        public double ReturnSeconds { get; }
        public double Duration => PickupSeconds + DeliverySeconds + ReturnSeconds;
        public float Progress => (float)Math.Min(1, Elapsed / Duration);
        public FoundryDronePhase Phase => Elapsed < PickupSeconds ? FoundryDronePhase.Pickup
            : !Delivered ? FoundryDronePhase.Delivery : FoundryDronePhase.Returning;
        public IReadOnlyList<Vector2> CurrentPath => Phase == FoundryDronePhase.Pickup ? PickupPath
            : Phase == FoundryDronePhase.Delivery ? DeliveryPath : ReturnPath;
        public Vector2 Position
        {
            get
            {
                double start = Phase == FoundryDronePhase.Pickup ? 0 : Phase == FoundryDronePhase.Delivery ? PickupSeconds : PickupSeconds + DeliverySeconds;
                double duration = Phase == FoundryDronePhase.Pickup ? PickupSeconds : Phase == FoundryDronePhase.Delivery ? DeliverySeconds : ReturnSeconds;
                var path = CurrentPath;
                float total = 0;
                for (int i = 1; i < path.Count; i++) total += Vector2.Distance(path[i - 1], path[i]);
                float remaining = total * Mathf.Clamp01((float)((Elapsed - start) / duration));
                for (int i = 1; i < path.Count; i++)
                {
                    float segment = Vector2.Distance(path[i - 1], path[i]);
                    if (remaining <= segment) return Vector2.Lerp(path[i - 1], path[i], segment > 0 ? remaining / segment : 1);
                    remaining -= segment;
                }
                return path[path.Count - 1];
            }
        }
    }
}
