using System;
using System.Collections.Generic;
using System.Linq;
using CometFoundry.Core;
using UnityEngine;

namespace CometFoundry.Simulation
{
    public sealed partial class FoundrySession
    {
        public void Save() { PlayerPrefs.SetString(SaveKey, ExportState()); PlayerPrefs.Save(); }
        public bool Load()
        {
            if (!PlayerPrefs.HasKey(SaveKey)) return false;
            string json = PlayerPrefs.GetString(SaveKey);
            try
            {
                var header = JsonUtility.FromJson<FoundryWorldSave>(json);
                return header != null && header.version == 1 ? LoadLegacy() : ImportState(json);
            }
            catch (Exception) { return false; }
        }
        public string ExportState()
        {
            var save = new FoundryWorldSave
            {
                version = 3, calendarSecondsPerDay = RealSecondsPerDay, credits = Company.Credits, seconds = Seconds, sales = Sales, delivered = EquipmentDelivered,
                autoSell = AutoSell, active = activeColony, nextId = nextId, surveyCount = surveyCount,
                surveyRemaining = surveyRemaining, shipCount = Company.Ships.Count, tutorial = (int)TutorialStep,
                ironMade = TutorialIronMade, plateMade = TutorialPlateMade, manualSales = ManualSales, manualPurchases = ManualPurchases
            };
            save.missionPractice = (int)MissionPracticeStep; save.practicePlateMade = PracticePlateMade;
            save.activeMission = activeMissionId; save.completedMissions = completedMissionIds.ToList();
            foreach (var colony in colonies) save.colonies.Add(WriteColony(colony));
            foreach (var prospect in prospects)
                save.prospects.Add(new SavedProspect { colony = WriteColony(new FoundryColony(prospect.State, prospect.State.DisplayName)), cost = prospect.Cost, x = prospect.PortAnchor.x, y = prospect.PortAnchor.y });
            foreach (var voyage in voyages)
            {
                var saved = new SavedVoyage { ship = Company.Ships.ToList().IndexOf(voyage.Ship), from = colonies.IndexOf(voyage.Source), to = colonies.IndexOf(voyage.Destination), duration = voyage.Duration, progress = voyage.Ship.TravelProgress };
                foreach (var stock in voyage.Ship.Cargo.Amounts) saved.stock.Add(new SavedStock { resource = stock.Key.ResourceId, amount = stock.Value });
                save.voyages.Add(saved);
            }
            save.balances = balances.Select(p => new FoundryHistoryPoint(p.day, p.value)).ToList();
            return JsonUtility.ToJson(save);
        }
        private SavedColony WriteColony(FoundryColony colony)
        {
            var state = colony.State;
            var saved = new SavedColony { id = state.Id.Value, name = colony.Name, seed = state.SeedCode, coordinate = state.DistanceCode,
                minutes = state.DistanceMinutes, grade = (int)state.Grade, sizeGrade = (int)state.SizeGrade, resourceTypes = state.ResourceTypeCount,
                buildable = state.BuildableRatio, px = state.Position.x, py = state.Position.y, radius = state.Radius,
                width = state.Grid.Width, height = state.Grid.Height, revenue = colony.Revenue };
            for (int y = 0; y < state.Grid.Height; y++) for (int x = 0; x < state.Grid.Width; x++) saved.terrain.Add((int)state.Grid.GetTile(new Vector2Int(x, y)).Terrain);
            foreach (var deposit in state.Deposits) saved.deposits.Add(new SavedDeposit { resource = deposit.Resource.ResourceId, initial = deposit.InitialStacks, remaining = deposit.RemainingStacks, x = deposit.Coordinate.x, y = deposit.Coordinate.y });
            foreach (var facility in state.Facilities)
            {
                var item = new SavedFacility { building = facility.Definition.FacilityId, x = facility.Anchor.x, y = facility.Anchor.y, recipe = facility.ActiveRecipe?.RecipeId, progress = facility.ProgressDays };
                foreach (var amount in facility.Inventory.Amounts) item.stock.Add(new SavedStock { resource = amount.Key.ResourceId, amount = amount.Value });
                saved.facilities.Add(item);
            }
            return saved;
        }
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private FoundryColony ReadColony(SavedColony saved, ref int id)
        {
            if (saved == null || string.IsNullOrEmpty(saved.id) || string.IsNullOrEmpty(saved.name)
                || saved.width < 1 || saved.height < 1 || saved.width > 16 || saved.height > 16
                || saved.terrain == null || saved.terrain.Count != saved.width * saved.height || saved.deposits == null || saved.facilities == null
                || !Finite(saved.revenue) || saved.revenue < 0 || !Finite(saved.radius) || saved.radius <= 0
                || !Finite(saved.px) || !Finite(saved.py) || saved.grade < 0 || saved.grade > 6 || saved.sizeGrade < 0 || saved.sizeGrade > 6
                || !Finite(saved.buildable) || saved.buildable < 0 || saved.buildable > 1) throw new ArgumentException("Invalid colony");
            var state = new CometState(new SimulationId(saved.id), saved.name, new Vector2(saved.px, saved.py), saved.radius,
                (CometGrade)saved.grade, (CometGrade)saved.sizeGrade, saved.seed, saved.buildable, saved.resourceTypes, saved.coordinate, saved.minutes, saved.width, saved.height);
            for (int y = 0; y < saved.height; y++) for (int x = 0; x < saved.width; x++)
            {
                int terrain = saved.terrain[y * saved.width + x];
                if (terrain < 0 || terrain > 3) throw new ArgumentException("Invalid terrain");
                state.Grid.GetTile(new Vector2Int(x, y)).SetTerrain((CometTileTerrain)terrain);
            }
            foreach (var d in saved.deposits)
            {
                var resource = Content.FindResource(d.resource); var coordinate = new Vector2Int(d.x, d.y);
                if (resource == null || !resource.CanBeMined || !state.Grid.Contains(coordinate) || state.Grid.GetTile(coordinate).Terrain == CometTileTerrain.Void
                    || !Finite(d.initial) || !Finite(d.remaining) || d.initial < 0 || d.remaining < 0 || d.remaining > d.initial) throw new ArgumentException("Invalid deposit");
                var deposit = new ResourceDeposit(resource, d.initial, state.Deposits.Count, coordinate);
                deposit.ExtractStacks(d.initial - d.remaining); state.AddDeposit(deposit);
            }
            foreach (var savedFacility in saved.facilities)
            {
                var definition = Content.FindBuilding(savedFacility.building);
                if (definition == null || !placement.TryPlaceFacility(state, new SimulationId("facility-" + id++), definition, new Vector2Int(savedFacility.x, savedFacility.y), out var facility)) throw new ArgumentException("Invalid facility");
                if (!string.IsNullOrEmpty(savedFacility.recipe) && !facility.AssignRecipe(Content.FindRecipe(savedFacility.recipe))) throw new ArgumentException("Invalid recipe");
                if (definition.FacilityType == Data.FacilityType.Mine && facility.ActiveRecipe != null && !state.Deposits.Any(d => d.Coordinate == facility.Anchor && d.Resource == facility.ActiveRecipe.Outputs[0].Resource)) throw new ArgumentException("Invalid mine");
                ReadInventory(facility.Inventory, savedFacility.stock);
                if (!Finite(savedFacility.progress) || savedFacility.progress < 0) throw new ArgumentException("Invalid progress");
                facility.RestoreProgress(savedFacility.progress);
            }
            return new FoundryColony(state, saved.name) { Revenue = saved.revenue };
        }
        private void ReadInventory(Inventory inventory, List<SavedStock> stocks)
        {
            if (stocks == null) throw new ArgumentException("Missing inventory");
            foreach (var stock in stocks)
            {
                var resource = Content.FindResource(stock.resource);
                if (resource == null || !Finite(stock.amount) || stock.amount < 0 || inventory.FreeStacks + 1e-8 < stock.amount) throw new ArgumentException("Invalid inventory");
                inventory.AddStacks(resource, stock.amount);
            }
        }
        public bool ImportState(string json)
        {
            try
            {
                var saved = JsonUtility.FromJson<FoundryWorldSave>(json);
                if (saved == null || (saved.version != 2 && saved.version != 3) || !Finite(saved.credits) || saved.credits < 0 || !Finite(saved.seconds) || saved.seconds < 0
                    || !Finite(saved.sales) || saved.sales < 0 || saved.colonies == null || saved.colonies.Count == 0 || saved.active < 0 || saved.active >= saved.colonies.Count
                    || saved.shipCount < 1 || saved.shipCount > 128 || saved.voyages == null || saved.prospects == null || saved.balances == null
                    || saved.tutorial < 0 || saved.tutorial > 9 || !Finite(saved.surveyRemaining) || saved.surveyRemaining < 0 || saved.surveyRemaining > SurveySeconds
                    || !Finite(saved.ironMade) || saved.ironMade < 0 || !Finite(saved.plateMade) || saved.plateMade < 0 || saved.manualSales < 0 || saved.manualPurchases < 0) return false;
                double savedDayDuration = saved.version == 2 ? 300d : saved.calendarSecondsPerDay;
                if (!Finite(savedDayDuration) || savedDayDuration <= 0) return false;
                double restoredSeconds = saved.seconds / savedDayDuration * RealSecondsPerDay;
                if (!Finite(restoredSeconds)) return false;
                var missionIds = saved.completedMissions ?? new List<string>();
                if (missionIds.Distinct().Count() != missionIds.Count || missionIds.Any(id => id != "first-iron" && !missions.Any(m => m.Id == id))
                    || (!string.IsNullOrEmpty(saved.activeMission) && saved.activeMission != "first-iron" && (!missions.Any(m => m.Id == saved.activeMission) || missionIds.Contains(saved.activeMission)))) return false;
                if (saved.missionPractice < 0 || saved.missionPractice > (int)FoundryMissionPracticeStep.Complete
                    || !Finite(saved.practicePlateMade) || saved.practicePlateMade < 0) return false;
                int id = 0;
                var loaded = saved.colonies.Select(c => ReadColony(c, ref id)).ToList();
                if (loaded.Select(c => c.State.Id.Value).Distinct().Count() != loaded.Count || loaded.Select(c => c.State.DistanceCode).Distinct().Count() != loaded.Count) return false;
                var company = new CompanyState(saved.credits); foreach (var c in loaded) company.AddComet(c.State);
                for (int i = 0; i < saved.shipCount; i++) company.AddShip(new SimulationId("ship-" + i), Content.Courier);
                var loadedVoyages = new List<FoundryVoyage>(); var assigned = new HashSet<int>();
                foreach (var item in saved.voyages)
                {
                    if (item.ship < 0 || item.ship >= saved.shipCount || !assigned.Add(item.ship) || item.from < 0 || item.to < 0 || item.from >= loaded.Count || item.to >= loaded.Count || item.from == item.to
                        || !Finite(item.duration) || item.duration <= 0 || !Finite(item.progress) || item.progress < 0 || item.progress > 1) return false;
                    var source = loaded[item.from]; var destination = loaded[item.to];
                    var sourceDock = FindDock(source, item.from == 0); var destinationDock = FindDock(destination, item.to == 0);
                    if (sourceDock == null || destinationDock == null) return false;
                    var ship = company.Ships[item.ship]; ReadInventory(ship.Cargo, item.stock);
                    var route = new TransportRoute(new SimulationId("route-" + id++), sourceDock, destinationDock, item.duration / RealSecondsPerDay);
                    ship.AssignRoute(route); ship.MarkInTransit(); ship.AdvanceTravel(item.progress); company.AddRoute(route);
                    loadedVoyages.Add(new FoundryVoyage { Ship = ship, Source = source, Destination = destination, Duration = item.duration });
                }
                var loadedProspects = new List<FoundryProspect>();
                if (saved.prospects.Count != 0 && saved.prospects.Count != 3) return false;
                var coordinates = new HashSet<string>(loaded.Select(c => c.State.DistanceCode));
                foreach (var offer in saved.prospects)
                {
                    var candidate = ReadColony(offer.colony, ref id); var anchor = new Vector2Int(offer.x, offer.y);
                    if (!coordinates.Add(candidate.State.DistanceCode) || !Finite(offer.cost) || offer.cost < 0 || !candidate.State.Grid.CanOccupy(anchor, Content.FindBuilding("port-small").FootprintSize)) return false;
                    loadedProspects.Add(new FoundryProspect { State = candidate.State, PortAnchor = anchor, Cost = offer.cost });
                }
                double previous = -1;
                foreach (var point in saved.balances)
                {
                    if (!Finite(point.day) || !Finite(point.value) || point.day < 0 || point.value < 0 || point.day <= previous || point.day > saved.seconds / savedDayDuration) return false;
                    previous = point.day;
                }
                colonies.Clear(); colonies.AddRange(loaded); Company = company; activeColony = saved.active;
                voyages.Clear(); voyages.AddRange(loadedVoyages); prospects.Clear(); prospects.AddRange(loadedProspects);
                activeMissionId = saved.activeMission == "first-iron" ? null : saved.activeMission;
                MissionPracticeStep = (FoundryMissionPracticeStep)saved.missionPractice; PracticePlateMade = saved.practicePlateMade; completedMissionIds.Clear(); foreach (string missionId in missionIds) completedMissionIds.Add(missionId);
                balances.Clear(); balances.AddRange(saved.balances); Seconds = restoredSeconds; Sales = saved.sales;
                EquipmentDelivered = Math.Max(0, saved.delivered); AutoSell = saved.autoSell; nextId = Math.Max(saved.nextId, id);
                surveyCount = Math.Max(0, saved.surveyCount); surveyRemaining = saved.surveyRemaining; pending = saleTimer = 0;
                TutorialStep = (FoundryTutorialStep)saved.tutorial; TutorialIronMade = saved.ironMade; TutorialPlateMade = saved.plateMade;
                ManualSales = saved.manualSales; ManualPurchases = saved.manualPurchases;
                RefreshMarket(); RecordBalance(); RefreshTutorial(); return true;
            }
            catch (Exception) { return false; }
        }
    }
    [Serializable] public sealed class FoundryWorldSave
    {
        public int version, delivered, active, nextId, surveyCount, shipCount, tutorial, manualSales, manualPurchases;
        public double credits, seconds, sales, surveyRemaining, ironMade, plateMade, calendarSecondsPerDay;
        public int missionPractice;
        public double practicePlateMade;
        public string activeMission;
        public List<string> completedMissions = new List<string>();
        public bool autoSell;
        public List<SavedColony> colonies = new List<SavedColony>();
        public List<SavedProspect> prospects = new List<SavedProspect>();
        public List<SavedVoyage> voyages = new List<SavedVoyage>();
        public List<FoundryHistoryPoint> balances = new List<FoundryHistoryPoint>();
    }
    [Serializable] public sealed class SavedColony
    {
        public string id, name, seed, coordinate;
        public int minutes, grade, sizeGrade, resourceTypes, width, height;
        public float px, py, radius, buildable;
        public double revenue;
        public List<int> terrain = new List<int>();
        public List<SavedDeposit> deposits = new List<SavedDeposit>();
        public List<SavedFacility> facilities = new List<SavedFacility>();
    }
    [Serializable] public sealed class SavedDeposit { public string resource; public int x, y; public double initial, remaining; }
    [Serializable] public sealed class SavedProspect { public SavedColony colony; public int x, y; public double cost; }
    [Serializable] public sealed class SavedVoyage { public int ship, from, to; public double duration, progress; public List<SavedStock> stock = new List<SavedStock>(); }
}
