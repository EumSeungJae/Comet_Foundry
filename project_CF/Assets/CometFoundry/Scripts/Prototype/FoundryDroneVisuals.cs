using System.Linq;
using CometFoundry.Simulation;
using UnityEngine;

namespace CometFoundry.Prototype
{
    public sealed partial class FoundryPlayableController
    {
        private Vector2 DroneScreenPoint(Vector2 tilePoint) => new Vector2(MapRect.x + tilePoint.x * MapCell, MapRect.yMax - tilePoint.y * MapCell);
        private void DrawActiveDrones()
        {
            var providers = session.Comet.Facilities.Where(f => f.Definition.IsDroneHangar).ToList();
            foreach (var flight in session.DroneFlights)
            {
                Color color = flight.Phase == FoundryDronePhase.Delivery ? Orange : flight.Phase == FoundryDronePhase.Returning ? Green : Ink;
                var route = flight.CurrentPath;
                for (int point = 1; point < route.Count; point++)
                {
                    Vector2 from = DroneScreenPoint(route[point - 1]), to = DroneScreenPoint(route[point]);
                    int dots = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(from, to) / 9));
                    for (int dot = 0; dot < dots; dot++)
                    {
                        var position = Vector2.Lerp(from, to, dot / (float)dots);
                        Fill(new Rect(position.x, position.y, 2, 2), new Color(color.r, color.g, color.b, .35f));
                    }
                }
                int rows = (flight.Provider.Definition.DroneCapacity + 1) / 2;
                var p = DroneScreenPoint(flight.Position) + new Vector2(flight.Slot % 2 == 0 ? -5 : 5, (flight.Slot / 2 - (rows - 1) / 2f) * 10);
                Fill(new Rect(p.x - 6, p.y - 4, 12, 8), color);
                foreach (int x in new[] { -8, 5 }) foreach (int y in new[] { -6, 3 }) Fill(new Rect(p.x + x, p.y + y, 4, 4), Ink);
                Fill(new Rect(p.x - 2, p.y - 2, 4, 4), Panel);
                if (flight.Phase == FoundryDronePhase.Delivery) Fill(new Rect(p.x - 3, p.y + 6, 6, 4), Orange);
                int number = providers.TakeWhile(provider => provider != flight.Provider).Sum(provider => provider.Definition.DroneCapacity) + flight.Slot + 1;
                Label(new Rect(p.x + 9, p.y - 8, 26, 16), number.ToString(), Style(10, color));
            }
        }
    }
}
