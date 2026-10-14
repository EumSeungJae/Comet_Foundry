using System.Collections.Generic;
using System.Linq;

namespace CometFoundry.Simulation
{
    public sealed class DroneWorkPlanner
    {
        public DroneHaulRequest SelectNextRequest(IEnumerable<DroneHaulRequest> requests)
        {
            if (requests == null)
            {
                return null;
            }

            return requests
                .Where(IsExecutable)
                .OrderByDescending(request => request.PriorityScore)
                .FirstOrDefault();
        }

        public bool IsExecutable(DroneHaulRequest request)
        {
            if (request == null
                || request.Source == null
                || request.Destination == null
                || request.Resource == null
                || request.RequestedStacks <= 0d)
            {
                return false;
            }

            return request.Source.Inventory.HasStacks(request.Resource, request.RequestedStacks)
                && request.Destination.Inventory.FreeStacks >= request.RequestedStacks;
        }
    }
}
