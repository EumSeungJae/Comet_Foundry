using CometFoundry.Core;
using CometFoundry.Data;
using System.Collections.Generic;
using UnityEngine;

namespace CometFoundry.Simulation
{
    public sealed class CometFoundryGame : MonoBehaviour
    {
        [SerializeField] private double startingCredits = 1000000d;
        [SerializeField] private List<ResourceDefinition> exchangeRequestResources = new List<ResourceDefinition>();

        public SimulationClock Clock { get; } = new SimulationClock();
        public CompanyState Company { get; private set; }
        public ResearchSystem Research { get; } = new ResearchSystem();
        public RequestSystem Requests { get; } = new RequestSystem();
        public MarketSystem Market { get; } = new MarketSystem();
        public MarketEventSystem MarketEvents { get; } = new MarketEventSystem();

        private void Awake()
        {
            Company = new CompanyState(startingCredits);
        }

        private void Update()
        {
            Clock.AdvanceSeconds(Time.deltaTime);
            MarketEvents.Update(Clock, Market);
            Market.UpdatePrices(Clock, exchangeRequestResources);
            Requests.Update(Clock, Company, exchangeRequestResources);
        }
    }
}
