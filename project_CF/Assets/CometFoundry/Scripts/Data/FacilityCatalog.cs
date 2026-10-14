using System.Collections.Generic;
using UnityEngine;

namespace CometFoundry.Data
{
    public static class FacilityCatalog
    {
        public static IReadOnlyList<FacilityDefinition> CreatePrototypeDefinitions()
        {
            return new[]
            {
                Create("mine-small", "소형 채굴기", FacilityType.Mine, 1, 1),
                Create("mine-large", "대형 채굴기", FacilityType.Mine, 1, 2),
                Create("mine-deep", "심층 채굴기", FacilityType.Mine, 3, 3),

                Create("refinery-basic", "기초 정제소", FacilityType.Refinery, 1, 2),
                Create("refinery-chemical", "화학 정제소", FacilityType.Refinery, 1, 3),
                Create("refinery-cryogenic", "극저온 처리소", FacilityType.Refinery, 2, 3),

                Create("factory-metal", "금속 가공소", FacilityType.Factory, 2, 2),
                Create("factory-electronics", "전자 가공소", FacilityType.Factory, 2, 3),
                Create("factory-precision", "정밀 부품 공장", FacilityType.Factory, 2, 4),

                Create("assembly-basic", "일반 조립소", FacilityType.Assembly, 2, 2),
                Create("assembly-advanced", "고급 조립소", FacilityType.Assembly, 2, 3),
                Create("assembly-heavy", "중공업 조립소", FacilityType.Assembly, 3, 3),

                Create("warehouse-small", "소형 창고", FacilityType.Warehouse, 1, 2, 250d),
                Create("warehouse-medium", "중형 창고", FacilityType.Warehouse, 2, 2, 500d),
                Create("warehouse-large", "대형 창고", FacilityType.Warehouse, 2, 4, 1200d),

                Create("tank-small", "소형 액체/가스 탱크", FacilityType.FluidTank, 2, 2, 400d),
                Create("tank-medium", "중형 액체/가스 탱크", FacilityType.FluidTank, 2, 3, 800d),
                Create("tank-large", "대형 액체/가스 탱크", FacilityType.FluidTank, 2, 5, 1800d),

                Create("port-small", "소형 항구", FacilityType.LogisticsHub, 2, 3),
                Create("port-docking-hub", "화물선 도킹 허브", FacilityType.LogisticsHub, 2, 5),
                Create("port-large-terminal", "대형 물류 터미널", FacilityType.LogisticsHub, 3, 6),

                Create("market-terminal", "거래소 터미널", FacilityType.MarketTerminal, 1, 2),
                Create("market-branch", "거래소 지부", FacilityType.MarketTerminal, 2, 2),
                Create("market-corporate-center", "기업 무역 센터", FacilityType.MarketTerminal, 2, 3),

                Create("lab-basic", "기초 연구소", FacilityType.ResearchLab, 1, 2),
                Create("lab-industrial", "산업 연구소", FacilityType.ResearchLab, 2, 2),
                Create("lab-space-engineering", "우주공학 연구소", FacilityType.ResearchLab, 2, 4),

                FacilityDefinition.CreatePrototypeSmallPowerPlant(),
                FacilityDefinition.CreatePrototypeMediumPowerPlant(),
                FacilityDefinition.CreatePrototypeLargePowerPlant(),
                FacilityDefinition.CreatePrototypeDroneHangar()
            };
        }

        private static FacilityDefinition Create(string id, string label, FacilityType type, int width, int height, double capacityTons = 0d)
        {
            FacilityDefinition definition = ScriptableObject.CreateInstance<FacilityDefinition>();
            definition.ConfigurePrototype(id, label, type, new Vector2Int(width, height), capacityTons);
            return definition;
        }
    }
}
