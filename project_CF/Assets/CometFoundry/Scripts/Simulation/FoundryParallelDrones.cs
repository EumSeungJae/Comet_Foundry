using System;
using System.Collections.Generic;
using System.Linq;
using CometFoundry.Data;
using UnityEngine;

namespace CometFoundry.Simulation
{
    internal sealed class FoundryDroneRoute
    {
        internal readonly List<Vector2> Points;
        internal readonly double Seconds;
        internal FoundryDroneRoute(List<Vector2> points, double seconds) { Points = points; Seconds = seconds; }
    }

    public sealed partial class FoundrySession
    {
        public const int MaxProductionDroneQueue = 2;
        public IReadOnlyList<FoundryDroneFlight> DroneFlights => colonies[activeColony].DroneFlights;
        public int DroneWorkerCapacity => Comet.Facilities.Where(f => f.Definition.IsDroneHangar).Sum(f => f.Definition.DroneCapacity);
        public int ActiveDroneCount => DroneFlights.Count;
        public int CompletedDroneDeliveries => colonies[activeColony].DroneDeliveries;
        public double DeliveredDroneStacks => colonies[activeColony].DroneDeliveredStacks;
        private static bool IsProductionFacility(FacilityInstance facility) => facility.Definition.SupportedRecipes.Count > 0;
        public int DroneQueueCount(FacilityInstance facility) => DroneFlights.Count(f => !f.Delivered &&
            (f.Request.Source == facility || f.Request.Destination == facility));
        public int ProviderActiveDrones(FacilityInstance provider) => DroneFlights.Count(f => f.Provider == provider);

        private double ReservedOutbound(FacilityInstance source, ResourceDefinition resource)
            => DroneFlights.Where(f => !f.Delivered && f.Request.Source == source && f.Request.Resource == resource).Sum(f => f.Request.RequestedStacks);
        private double ReservedInbound(FacilityInstance destination, ResourceDefinition resource = null)
            => DroneFlights.Where(f => !f.Delivered && f.Request.Destination == destination && (resource == null || f.Request.Resource == resource)).Sum(f => f.Request.RequestedStacks);

        private FoundryDroneRoute DroneRoute(FacilityInstance source, FacilityInstance destination)
        {
            var cache = colonies[activeColony].DroneRoutes;
            if (cache.TryGetValue((source, destination), out var cached)) return cached;
            var tiles = logistics.FindNavigablePath(Comet, source, destination);
            if (tiles == null) { cache[(source, destination)] = null; return null; }
            var points = new List<Vector2> { (Vector2)source.Anchor + (Vector2)source.Definition.FootprintSize / 2 };
            points.AddRange(tiles.Select(t => (Vector2)t + Vector2.one * .5f));
            points.Add((Vector2)destination.Anchor + (Vector2)destination.Definition.FootprintSize / 2);
            double length = 0;
            for (int i = 1; i < points.Count; i++) length += Vector2.Distance(points[i - 1], points[i]);
            double speed = logistics.GetPathSpeedMultiplier(Comet, source, destination);
            var route = new FoundryDroneRoute(points, (.18 + length * .065) / Math.Max(1, speed));
            cache[(source, destination)] = route;
            return route;
        }

        private void InvalidateDroneNavigation()
        {
            colonies[activeColony].DroneRoutes.Clear();
            colonies[activeColony].DroneFlights.RemoveAll(f => !FlightPathClear(f));
        }
        private bool FlightPathClear(FoundryDroneFlight flight)
        {
            var owners = new[] { flight.Provider, flight.Request.Source, flight.Request.Destination };
            if (owners.Any(owner => !Comet.Facilities.Contains(owner))) return false;
            foreach (var point in flight.PickupPath.Concat(flight.DeliveryPath).Concat(flight.ReturnPath))
            {
                var tile = new Vector2Int(Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.y));
                if (owners.Any(f => tile.x >= f.Anchor.x && tile.y >= f.Anchor.y &&
                    tile.x < f.Anchor.x + f.Definition.FootprintSize.x && tile.y < f.Anchor.y + f.Definition.FootprintSize.y)) continue;
                if (Comet.Grid.GetTile(tile)?.IsDroneNavigable != true) return false;
            }
            return true;
        }
        private void CancelDroneJobsFor(FacilityInstance facility)
        {
            colonies[activeColony].DroneFlights.RemoveAll(f => f.Provider == facility || f.Request.Source == facility || f.Request.Destination == facility);
            colonies[activeColony].DroneRoutes.Clear();
        }

        private void TickDrones(double seconds)
        {
            var colony = colonies[activeColony];
            var flights = colony.DroneFlights;
            if (!HasLogistics) { flights.Clear(); return; }
            // Cargo remains in its source inventory until delivery. Reservations prevent double assignment.
            // A save or cancellation can therefore rebuild work without losing or duplicating any material.
            for (int i = flights.Count - 1; i >= 0; i--)
            {
                var flight = flights[i]; flight.Elapsed += seconds;
                if (!flight.Delivered && flight.Elapsed + 1e-9 >= flight.PickupSeconds + flight.DeliverySeconds)
                {
                    var request = flight.Request;
                    double quantity = Math.Min(request.RequestedStacks, Math.Min(request.Source.Inventory.GetAmount(request.Resource), request.Destination.Inventory.FreeStacks));
                    double removed = request.Source.Inventory.RemoveStacks(request.Resource, quantity);
                    double added = request.Destination.Inventory.AddStacks(request.Resource, removed);
                    if (added < removed) request.Source.Inventory.AddStacks(request.Resource, removed - added);
                    flight.DeliveredStacks = added; flight.Delivered = true;
                    if (added > 0) { colony.DroneDeliveries++; colony.DroneDeliveredStacks += added; }
                }
                if (flight.Elapsed + 1e-9 >= flight.Duration) flights.RemoveAt(i);
            }
            foreach (var provider in Comet.Facilities.Where(f => f.Definition.IsDroneHangar))
                for (int slot = 0; slot < provider.Definition.DroneCapacity; slot++)
                {
                    if (flights.Any(f => f.Provider == provider && f.Slot == slot)) continue;
                    // Rebuild requests after every assignment so queue limits, cargo and space reservations include it.
                    var requests = DroneRequests();
                    DroneHaulRequest request;
                    FoundryDroneRoute pickup = null, delivery = null, returning = null;
                    while ((request = planner.SelectNextRequest(requests)) != null)
                    {
                        pickup = DroneRoute(provider, request.Source);
                        delivery = DroneRoute(request.Source, request.Destination);
                        returning = DroneRoute(request.Destination, provider);
                        if (pickup != null && delivery != null && returning != null) break;
                        requests.Remove(request);
                    }
                    if (request == null) continue;
                    flights.Add(new FoundryDroneFlight(provider, slot, request, pickup.Points, delivery.Points, returning.Points,
                        pickup.Seconds, delivery.Seconds, returning.Seconds));
                    lastSupply[request.Destination] = Seconds;
                    colony.LastHaul[(request.Source, request.Destination, request.Resource)] = Seconds;
                }
        }

        private List<DroneHaulRequest> DroneRequests()
        {
            var requests = new List<DroneHaulRequest>();
            foreach (var destination in Comet.Facilities)
            {
                if (destination.ActiveRecipe != null)
                    foreach (var input in destination.ActiveRecipe.Inputs)
                    {
                        double wanted = input.Stacks * 3 - destination.Inventory.GetAmount(input.Resource) - ReservedInbound(destination, input.Resource);
                        if (wanted > 0) AddRequests(requests, destination, input.Resource, wanted, DroneHaulRequestType.ProductionInput);
                    }
                if (destination.Definition.FacilityType == FacilityType.MarketTerminal && AutoSell)
                    foreach (var resource in Content.Resources)
                        if (resource.ProcessingGrade == 5) AddRequests(requests, destination, resource, 4, DroneHaulRequestType.MarketReservation);
                if (destination.Definition.IsWarehouse)
                    foreach (var resource in Content.Resources) AddRequests(requests, destination, resource, 4, DroneHaulRequestType.Storage);
            }
            return requests;
        }
        private void AddRequests(List<DroneHaulRequest> requests, FacilityInstance destination, ResourceDefinition resource, double wanted, DroneHaulRequestType type)
        {
            double free = destination.Inventory.FreeStacks - ReservedInbound(destination);
            if (free <= 1e-8 || (IsProductionFacility(destination) && DroneQueueCount(destination) >= MaxProductionDroneQueue)) return;
            foreach (var source in Comet.Facilities)
            {
                if (source == destination || (IsProductionFacility(source) && DroneQueueCount(source) >= MaxProductionDroneQueue)) continue;
                if (type == DroneHaulRequestType.Storage && source.Definition.IsWarehouse) continue;
                double reserve = source.ActiveRecipe?.Inputs.Where(input => input.Resource == resource).Sum(input => input.Stacks * 3) ?? 0;
                double quantity = Math.Min(4, Math.Min(wanted, Math.Min(source.Inventory.GetAmount(resource) - reserve - ReservedOutbound(source, resource), free)));
                if (quantity <= .0001 || DroneRoute(source, destination) == null) continue;
                bool urgent = destination.ActiveRecipe?.Inputs.Any(input => input.Resource == resource &&
                    destination.Inventory.GetAmount(resource) + ReservedInbound(destination, resource) < input.Stacks) == true;
                lastSupply.TryGetValue(destination, out double last);
                int priority = (urgent ? 5 : 0) + (int)Math.Min(4, Math.Max(0, Seconds - last) / 3);
                colonies[activeColony].LastHaul.TryGetValue((source, destination, resource), out double lastHaul);
                requests.Add(new DroneHaulRequest(type, source, destination, resource, quantity, priority, Seconds - lastHaul));
            }
        }
    }
}
