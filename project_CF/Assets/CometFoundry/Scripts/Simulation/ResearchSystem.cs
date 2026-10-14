using System.Collections.Generic;
using CometFoundry.Data;

namespace CometFoundry.Simulation
{
    public sealed class ResearchSystem
    {
        private readonly HashSet<ResearchDefinition> completed = new HashSet<ResearchDefinition>();

        public IReadOnlyCollection<ResearchDefinition> Completed => completed;

        public bool IsCompleted(ResearchDefinition research)
        {
            return research != null && completed.Contains(research);
        }

        public bool CanResearch(ResearchDefinition research)
        {
            if (research == null)
            {
                return false;
            }

            foreach (ResearchDefinition prerequisite in research.Prerequisites)
            {
                if (!completed.Contains(prerequisite))
                {
                    return false;
                }
            }

            return true;
        }

        public bool Complete(CompanyState company, ResearchDefinition research)
        {
            if (company == null || !CanResearch(research) || completed.Contains(research))
            {
                return false;
            }

            if (!company.Spend(research.ResearchCost))
            {
                return false;
            }

            completed.Add(research);
            return true;
        }
    }
}
