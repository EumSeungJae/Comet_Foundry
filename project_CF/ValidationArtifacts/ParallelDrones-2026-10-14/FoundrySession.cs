using System;
using System.Collections.Generic;
using CometFoundry.Core;
using CometFoundry.Data;
using UnityEngine;

namespace CometFoundry.Simulation
{
    public sealed partial class FoundrySession : IDisposable
    {
        public const int Width = 16, Height = 12;
        public const string SaveKey = "CometFoundry.Playable.v1";
        public FoundryContent Content { get; } = new FoundryContent();
        public CometState Comet => colonies[activeColony].State;
        public CompanyState Company { get; private set; }
        public double Seconds { get; private set; }
        public double Sales { get; private set; }
        public int EquipmentDelivered { get; private set; }
        public bool AutoSell { get; set; }
        public DroneHaulRequest Flight => DroneFlights.Count > 0 ? DroneFlights[0].Request : null;
        public float FlightProgress => DroneFlights.Count > 0 ? DroneFlights[0].Progress : 0;
        public PowerReport Power
        {
            get
            {
                var report = powerSystem.Calculate(Comet);
                return new PowerReport(report.GeneratedPower, report.ConsumedPower + DroneWorkerCapacity / DroneWorkersPerProvider);
            }
        }
        public bool HasHangar => FindType(FacilityType.DroneHangar) != null;
        public bool HasLogistics => DroneWorkerCapacity > 0;
        private readonly PlacementSystem placement = new PlacementSystem();
        private readonly PowerSystem powerSystem = new PowerSystem();
        private readonly DroneLogisticsSystem logistics = new DroneLogisticsSystem();
        private readonly DroneWorkPlanner planner = new DroneWorkPlanner();
        private Dictionary<FacilityInstance, double> lastSupply => colonies[activeColony].LastSupply;
        private double pending, saleTimer;
        private int nextId;
        // Runtime observations for guided production; purchases never count as production.
        private readonly Dictionary<FacilityInstance, Dictionary<string, double>> guidedProduction = new Dictionary<FacilityInstance, Dictionary<string, double>>();
        public double ProducedBy(FacilityInstance facility, string recipeId)
            => facility != null && guidedProduction.TryGetValue(facility, out var recipes) && recipes.TryGetValue(recipeId, out var amount) ? amount : 0;

        public FoundrySession() { NewGame(); }
        public void NewGame()
        {
            guidedProduction.Clear();
            Company = new CompanyState(80000);
            colonies.Clear(); activeColony = 0;
            colonies.Add(new FoundryColony(new CometState(new SimulationId("home"), "헤스티아 / HESTIA", Vector2.zero, 8, Width, Height), "헤스티아"));
            Company.AddComet(Comet);
            Seconds = Sales = pending = saleTimer = 0;
            EquipmentDelivered = nextId = 0;
            lastSupply.Clear(); AutoSell = false;
            // Fixed starter comet, with open corridors around every starter building.
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    if ((x < 2 || x > 13) && (y < 2 || y > 9)) Comet.Grid.GetTile(new Vector2Int(x, y)).SetTerrain(CometTileTerrain.Void);
            AddDeposit("iron-ore", 2, 3); AddDeposit("copper-ore", 2, 7); AddDeposit("ice", 3, 9);
            AddDeposit("iron-ore", 11, 9); AddDeposit("copper-ore", 13, 4); AddDeposit("ice", 6, 1);
            Starter("power-plant-large", 12, 0);
            Starter("warehouse-small", 7, 7);
            Starter("drone-storage", 6, 8);
            Starter("mine-small", 2, 3, "extract-iron");
            Starter("mine-small", 2, 7, "extract-copper");
            ResetExpansion();
        }
        private void AddDeposit(string resource, int x, int y)
        {
            Comet.AddDeposit(new ResourceDeposit(Content.FindResource(resource), 2400, Comet.Deposits.Count, new Vector2Int(x, y)));
        }
        private FacilityInstance Starter(string id, int x, int y, string recipe = null)
        {
            if (!placement.TryPlaceFacility(Comet, new SimulationId("facility-" + nextId++), Content.FindBuilding(id), new Vector2Int(x, y), out var facility))
                throw new InvalidOperationException("Invalid starter placement: " + id);
            if (recipe != null) facility.AssignRecipe(Content.FindRecipe(recipe));
            return facility;
        }
        public ResourceDeposit DepositAt(Vector2Int tile)
        {
            foreach (var deposit in Comet.Deposits) if (deposit.Coordinate == tile) return deposit;
            return null;
        }
        public FacilityInstance FacilityAt(Vector2Int tile)
        {
            foreach (var facility in Comet.Facilities)
            {
                var size = facility.Definition.FootprintSize;
                if (tile.x >= facility.Anchor.x && tile.x < facility.Anchor.x + size.x && tile.y >= facility.Anchor.y && tile.y < facility.Anchor.y + size.y) return facility;
            }
            return null;
        }
        public string PlacementError(FacilityDefinition definition, Vector2Int tile)
        {
            if (!Comet.Grid.CanOccupy(tile, definition.FootprintSize)) return "빈 건설 가능 타일에 배치하세요.";
            var deposit = DepositAt(tile);
            if (definition.FacilityType == FacilityType.Mine && (deposit == null || deposit.IsDepleted)) return "채굴기는 자원 노드 위에 배치하세요.";
            if (definition.FacilityType != FacilityType.Mine)
                foreach (var footprintTile in Comet.Grid.GetFootprintTiles(tile, definition.FootprintSize))
                    if (DepositAt(new Vector2Int(footprintTile.X, footprintTile.Y)) != null) return "자원 노드는 채굴기용으로 남겨주세요.";
            if (Company.Credits < definition.BuildCost) return "건설 솔라가 부족합니다.";
            return null;
        }
        public bool Build(FacilityDefinition definition, Vector2Int tile, out string message)
        {
            message = PlacementError(definition, tile);
            if (message != null) return false;
            if (!placement.TryPlaceFacility(Comet, new SimulationId("facility-" + nextId++), definition, tile, out var facility)) return false;
            Company.Spend(definition.BuildCost);
            InvalidateDroneNavigation();
            if (definition.FacilityType == FacilityType.Mine)
                foreach (var recipe in definition.SupportedRecipes)
                    if (recipe.Outputs[0].Resource == DepositAt(tile).Resource) facility.AssignRecipe(recipe);
            message = definition.DisplayName + " 건설 완료. 클릭해서 생산품을 설정하세요.";
            RecordBalance(); RefreshTutorial();
            return true;
        }
        public bool CanSelect(FacilityInstance facility, RecipeDefinition recipe)
        {
            if (facility.Definition.FacilityType != FacilityType.Mine) return true;
            var deposit = DepositAt(facility.Anchor);
            return deposit != null && recipe.Outputs.Count > 0 && recipe.Outputs[0].Resource == deposit.Resource;
        }
        public string Status(FacilityInstance facility)
        {
            if (facility.ActiveRecipe == null) return facility.Definition.SupportedRecipes.Count > 0 ? "생산품 미설정" : "가동 중";
            if (Power.HasDeficit) return "전력 부족";
            if (facility.Definition.FacilityType == FacilityType.Mine)
            {
                var deposit = DepositAt(facility.Anchor);
                if (deposit == null || deposit.RemainingStacks < facility.ActiveRecipe.Outputs[0].Stacks) return "매장량 고갈";
            }
            return facility.GetProductionBlockReason(Company) ?? "생산 중";
        }
        public void Advance(double seconds)
        {
            if (seconds <= 0 || double.IsNaN(seconds) || double.IsInfinity(seconds)) return;
            pending += seconds;
            while (pending >= 0.1d - 1e-9)
            {
                pending = Math.Max(0, pending - 0.1d); Seconds += 0.1d;
                int viewing = activeColony;
                for (int c = 0; c < colonies.Count; c++)
                {
                    activeColony = c;
                    if (Power.HasDeficit) continue;
                    foreach (var facility in Comet.Facilities)
                    {
                        if (facility.ActiveRecipe == null) continue;
                        ResourceDeposit deposit = facility.Definition.FacilityType == FacilityType.Mine ? DepositAt(facility.Anchor) : null;
                        if (facility.Definition.FacilityType == FacilityType.Mine && (deposit == null || deposit.RemainingStacks < facility.ActiveRecipe.Outputs[0].Stacks)) continue;
                        if (facility.TickProduction(0.1d / 300d, Company))
                        {
                            if (!guidedProduction.TryGetValue(facility, out var observed))
                                guidedProduction[facility] = observed = new Dictionary<string, double>();
                            var recipeId = facility.ActiveRecipe.RecipeId;
                            observed.TryGetValue(recipeId, out var made);
                            observed[recipeId] = made + facility.ActiveRecipe.Outputs[0].Stacks;
                            RecordPracticeProduction(facility.ActiveRecipe);
                            if (deposit != null) deposit.ExtractStacks(facility.ActiveRecipe.Outputs[0].Stacks);
                            if (c == 0) foreach (var output in facility.ActiveRecipe.Outputs)
                            { if (output.Resource.ResourceId == "iron") TutorialIronMade += output.Stacks; if (output.Resource.ResourceId == "plate") TutorialPlateMade += output.Stacks; }
                        }
                    }
                    TickDrones(0.1d);
                }
                activeColony = viewing;
                TickExpansion(0.1d);
                saleTimer += 0.1d;
                if (saleTimer >= 1d) { saleTimer -= 1d; SellTerminalStock(); }
                RecordBalance(); RefreshTutorial();
            }
        }
        private void SellTerminalStock()
        {
            if (!AutoSell) return;
            foreach (var facility in Comet.Facilities)
            {
                if (facility.Definition.FacilityType != FacilityType.MarketTerminal) continue;
                foreach (var resource in Content.Resources)
                {
                    if (resource.ProcessingGrade != 5) continue;
                    double quantity = facility.Inventory.RemoveStacks(resource, facility.Inventory.GetAmount(resource));
                    double revenue = quantity * Market.GetSaleUnitPrice(Company, resource);
                    Company.Earn(revenue); Sales += revenue;
                    colonies[activeColony].Revenue += revenue;
                    if (resource.ResourceId == "equipment") EquipmentDelivered += (int)quantity;
                }
            }
        }
        private FacilityInstance FindType(FacilityType type)
        {
            foreach (var facility in Comet.Facilities) if (facility.Definition.FacilityType == type) return facility;
            return null;
        }
        private void SaveLegacy()
        {
            var save = new FoundrySave { version = 1, credits = Company.Credits, seconds = Seconds, sales = Sales, delivered = EquipmentDelivered, autoSell = AutoSell };
            foreach (var deposit in Comet.Deposits) save.deposits.Add(deposit.RemainingStacks);
            foreach (var facility in Comet.Facilities)
            {
                var saved = new SavedFacility { building = facility.Definition.FacilityId, x = facility.Anchor.x, y = facility.Anchor.y, recipe = facility.ActiveRecipe?.RecipeId, progress = facility.ProgressDays };
                foreach (var amount in facility.Inventory.Amounts) saved.stock.Add(new SavedStock { resource = amount.Key.ResourceId, amount = amount.Value });
                save.facilities.Add(saved);
            }
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(save)); PlayerPrefs.Save();
        }
        private bool LoadLegacy()
        {
            if (!PlayerPrefs.HasKey(SaveKey)) return false;
            FoundrySave save;
            try { save = JsonUtility.FromJson<FoundrySave>(PlayerPrefs.GetString(SaveKey)); }
            catch (Exception) { return false; }
            if (save == null || save.version != 1 || save.facilities == null || save.deposits == null
                || save.deposits.Count != 6 || save.credits < 0 || double.IsNaN(save.credits) || double.IsInfinity(save.credits)) return false;
            // Validate using a separate board before replacing the current session.
            var home = colonies[0].State;
            var candidate = new CometState(new SimulationId("home"), home.DisplayName, Vector2.zero, 8, Width, Height);
            for (int x = 0; x < Width; x++) for (int y = 0; y < Height; y++) candidate.Grid.GetTile(new Vector2Int(x, y)).SetTerrain(home.Grid.GetTile(new Vector2Int(x, y)).Terrain);
            int id = 0;
            foreach (var saved in save.facilities)
            {
                var definition = Content.FindBuilding(saved.building);
                if (definition == null || !placement.TryPlaceFacility(candidate, new SimulationId("facility-" + id++), definition, new Vector2Int(saved.x, saved.y), out var facility)) return false;
                if (saved.recipe != null && saved.recipe.Length > 0 && !facility.AssignRecipe(Content.FindRecipe(saved.recipe))) return false;
                if (saved.stock == null) return false;
                foreach (var stock in saved.stock)
                {
                    var resource = Content.FindResource(stock.resource);
                    if (resource == null || stock.amount < 0 || double.IsNaN(stock.amount) || double.IsInfinity(stock.amount) || facility.Inventory.FreeStacks + 1e-6 < stock.amount) return false;
                    facility.Inventory.AddStacks(resource, stock.amount);
                }
                facility.RestoreProgress(saved.progress);
            }
            for (int i = 0; i < home.Deposits.Count; i++)
            {
                var original = home.Deposits[i]; double remaining = save.deposits[i];
                if (remaining < 0 || remaining > original.InitialStacks || double.IsNaN(remaining)) return false;
                var deposit = new ResourceDeposit(original.Resource, original.InitialStacks, i, original.Coordinate);
                deposit.ExtractStacks(original.InitialStacks - remaining); candidate.AddDeposit(deposit);
            }
            Company = new CompanyState(save.credits); colonies.Clear(); colonies.Add(new FoundryColony(candidate, "헤스티아")); activeColony = 0; Company.AddComet(candidate);
            ResetExpansion(); TutorialStep = FoundryTutorialStep.Complete;
            Seconds = Math.Max(0, save.seconds) / 300d * RealSecondsPerDay; Sales = Math.Max(0, save.sales); EquipmentDelivered = Math.Max(0, save.delivered);
            AutoSell = save.autoSell; nextId = id; lastSupply.Clear(); pending = saleTimer = 0; return true;
        }
        public void Dispose() => Content.Dispose();
    }
    [Serializable] public sealed class FoundrySave
    {
        public int version, delivered;
        public double credits, seconds, sales;
        public bool autoSell;
        public List<double> deposits = new List<double>();
        public List<SavedFacility> facilities = new List<SavedFacility>();
    }
    [Serializable] public sealed class SavedFacility
    {
        public string building, recipe;
        public int x, y;
        public double progress;
        public List<SavedStock> stock = new List<SavedStock>();
    }
    [Serializable] public sealed class SavedStock { public string resource; public double amount; }
}
