using System;
using System.Collections.Generic;
using System.Linq;
using CometFoundry.Data;
using CometFoundry.Simulation;
using UnityEngine;

namespace CometFoundry.Prototype
{
    public enum FoundryPage { Board, Exchange, Management }
    public sealed partial class FoundryPlayableController
    {
        private FoundryPage page;
        private int materialIndex, marketMonths = 6, cashMonths = 6, tradeQuantity = 1;
        private int prospectIndex = -1, shipIndex, shipFrom, shipTo = 1, cargoIndex, cargoQuantity = 1;
        private bool buying, fleet;
        private Vector2 materialScroll, colonyScroll;
        private Texture2D priceChartTexture, balanceChartTexture;
        private string priceChartKey, balanceChartKey;
        private void ResetPanelSelection() { demolishMode = false; demolitionTarget = null; missionList = false; page = FoundryPage.Board; selected = null; materialIndex = cargoIndex = shipIndex = shipFrom = 0; shipTo = 1; prospectIndex = -1; priceChartKey = balanceChartKey = null; }
        private void OpenExchange() { if (!session.HasMarket) return; page = FoundryPage.Exchange; selected = null; build = null; }
        private void PageHeader(string caption)
        {
            Fill(new Rect(24, 120, 1392, 674), new Color32(10, 18, 25, 255));
            Label(new Rect(42, 124, 1100, 30), caption, heading);
            if (Btn(new Rect(1266, 121, 150, 34), "개척지로 돌아가기")) page = FoundryPage.Board;
        }
        private int RangeButtons(int value, float x, float y)
        {
            foreach (int months in new[] { 6, 12, 18, 24 }) { if (Btn(new Rect(x, y, 70, 30), months + "개월", value == months)) value = months; x += 78; }
            return value;
        }
        private void DrawExchange()
        {
            PageHeader("거래소 터미널  /  " + session.ActiveColony.Name + " · " + session.Comet.DistanceCode);
            Fill(new Rect(24, 164, 292, 630), Panel); Fill(new Rect(330, 164, 788, 630), Panel); Fill(new Rect(1132, 164, 284, 630), Panel);
            Label(new Rect(42, 178, 260, 28), "보유 소재 · 모든 등급", heading);
            Label(new Rect(42, 211, 260, 35), "현재 혜성 / 전체 혜성 재고 (스택)", small);
            materialScroll = GUI.BeginScrollView(new Rect(38, 252, 268, 521), materialScroll, new Rect(0, 0, 242, session.Content.Resources.Count * 55));
            for (int i = 0; i < session.Content.Resources.Count; i++)
            {
                var r = session.Content.Resources[i]; float y = i * 55;
                if (Btn(new Rect(0, y, 238, 50), "", materialIndex == i)) { materialIndex = i; tradeQuantity = 1; }
                Label(new Rect(10, y + 3, 225, 23), r.DisplayName, text);
                Label(new Rect(10, y + 26, 225, 20), $"{session.Stock(session.ActiveColony, r):0.#} / {session.TotalStock(r):0.#}", small);
            }
            GUI.EndScrollView();
            var resource = session.Content.Resources[materialIndex];
            double ratio = session.PriceRatio(resource, session.Day);
            Color quoteColor = ratio >= 1 ? new Color32(245, 84, 91, 255) : new Color32(76, 155, 255, 255);
            Label(new Rect(350, 182, 730, 30), $"{resource.DisplayName} · 정가 {resource.BaseMarketPrice:N0} · 시세 {resource.BaseMarketPrice * ratio:N2} 솔라", heading);
            marketMonths = RangeButtons(marketMonths, 350, 228);
            Label(new Rect(678, 230, 414, 28), $"정가 대비 {(ratio - 1) * 100:+0.00;-0.00;0.00}%", Style(16, quoteColor));
            DrawHistory(new Rect(350, 281, 746, 386), session.PriceHistory(resource, marketMonths), Orange, "솔라 / 스택", resource.BaseMarketPrice, resource);
            Label(new Rect(350, 678, 735, 40),  $"전월 대비 {MonthlyChange(session.PriceHistory(resource, 6))}  ·  다음 갱신 {session.DaysUntilMarketRefresh}일 후\n월별 시세 · 곡선 연결 · 빨강: 정가 이상 / 파랑: 정가 미만 · 일반 시세 ±30%", small);
            Label(new Rect(350, 722, 735, 58), "현재 이벤트: " + (session.MarketEventAt(resource, session.Day) ?? "없음 · 일반 변동 제한 적용") + "\n실제 구입·판매 단가는 우측에 표시합니다. 거래는 현재 혜성 재고에 반영됩니다.", small);
            Label(new Rect(1150, 182, 244, 26), "개당 거래 가격 (1스택)", heading);
            Label(new Rect(1150, 224, 244, 35), $"판매  {session.SellUnitPrice(resource):N2} 솔라", Style(18, Green));
            Label(new Rect(1150, 266, 244, 35), $"구입  {session.BuyUnitPrice(resource):N2} 솔라", Style(18, Orange));
            if (Btn(new Rect(1150, 330, 116, 42), "구입", buying)) { buying = true; tradeQuantity = 1; }
            if (Btn(new Rect(1276, 330, 120, 42), "판매", !buying)) { buying = false; tradeQuantity = 1; }
            int max = session.MaxTradeQuantity(resource, buying);
            tradeQuantity = Mathf.Clamp(tradeQuantity, 1, Math.Max(1, max));
            Label(new Rect(1150, 402, 244, 29), $"거래 수량  {tradeQuantity:N0} 스택", heading);
            GUI.enabled = (max > 0) && !GameOverlayOpen;
            tradeQuantity = Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(1150, 450, 246, 25), tradeQuantity, 1, Math.Max(1, max)));
            if (Btn(new Rect(1150, 489, 75, 32), "− 1")) tradeQuantity = Math.Max(1, tradeQuantity - 1);
            if (Btn(new Rect(1232, 489, 75, 32), "+ 1")) tradeQuantity = Math.Min(max, tradeQuantity + 1);
            if (Btn(new Rect(1314, 489, 82, 32), "최대")) tradeQuantity = max;
            GUI.enabled = (true) && !GameOverlayOpen;
            Label(new Rect(1150, 548, 244, 55), $"합계 {(buying ? session.BuyUnitPrice(resource) : session.SellUnitPrice(resource)) * tradeQuantity:N2} 솔라\n거래 가능 최대 {max:N0} 스택", text);
            string error = session.TradeError(resource, buying, tradeQuantity);
            Label(new Rect(1150, 620, 244, 67), error ?? (buying ? "구매 소재는 창고·항구·거래소에 입고됩니다." : "현재 혜성의 재고에서 판매합니다."), small);
            GUI.enabled = (error == null) && !GameOverlayOpen;
            if (Btn(new Rect(1150, 710, 246, 54), "거래 확인", true)) session.Trade(resource, buying, tradeQuantity, out notice);
            GUI.enabled = (true) && !GameOverlayOpen;
        }
        private void DrawManagement()
        {
            PageHeader("혜성관리  /  보유 혜성 " + session.Colonies.Count + "개");
            Fill(new Rect(24, 164, 720, 630), Panel); Fill(new Rect(758, 164, 658, 630), Panel);
            Label(new Rect(42, 178, 660, 28), "보유 혜성 · 선택하여 개발 구역 전환", heading);
            colonyScroll = GUI.BeginScrollView(new Rect(40, 217, 687, 285), colonyScroll, new Rect(0, 0, 662, session.Colonies.Count * 95));
            for (int i = 0; i < session.Colonies.Count; i++)
            {
                var c = session.Colonies[i]; float y = i * 95;
                if (Btn(new Rect(0, y, 654, 88), "", i == session.ActiveColonyIndex)) { session.SwitchColony(i); build = null; selected = null; demolishMode = false; }
                Label(new Rect(12, y + 5, 625, 24), c.Name + "  /  " + c.State.DistanceCode, heading);
                Label(new Rect(12, y + 32, 625, 23), "자원: " + string.Join(" · ", c.State.Deposits.Select(d => d.Resource.DisplayName + " " + d.RemainingStacks.ToString("N0"))), small);
                Label(new Rect(12, y + 58, 625, 23), $"누적 판매 수익 {c.Revenue:N0} 솔라  ·  시설 {c.State.Facilities.Count}개", text);
            }
            GUI.EndScrollView();
            Label(new Rect(42, 515, 660, 28), "경영지표 · 월초 소지금 (30일 기록)", heading);
            cashMonths = RangeButtons(cashMonths, 42, 550);
            Label(new Rect(380, 545, 340, 44), $"현재 {session.Company.Credits:N0} 솔라\n전월 대비 {MonthlyChange(session.BalanceHistory(6))}", small);
            DrawHistory(new Rect(42, 597, 681, 176), session.BalanceHistory(cashMonths), Green, "솔라");
            if (Btn(new Rect(778, 178, 300, 37), "혜성 탐사", !fleet)) fleet = false;
            if (Btn(new Rect(1088, 178, 308, 37), "수송선 관리", fleet)) fleet = true;
            if (fleet) DrawFleet(); else DrawExploration();
        }
        private void DrawExploration()
        {
            Label(new Rect(780, 230, 610, 54), $"탐사 비용 {FoundrySession.SurveyCost:N0} 솔라 · {FoundrySession.SurveySeconds:0}초\n발견한 3개 후보 중 하나를 선택해 항구와 함께 개척합니다.", text);
            GUI.enabled = (!session.IsExploring && session.Company.Credits >= FoundrySession.SurveyCost) && !GameOverlayOpen;
            if (Btn(new Rect(780, 297, 610, 40), session.IsExploring ? $"탐사 중 · {session.SurveyRemainingSeconds:0.0}초" : "새 탐사 시작", true)) { session.Explore(out notice); prospectIndex = -1; }
            GUI.enabled = (true) && !GameOverlayOpen;
            for (int i = 0; i < session.Prospects.Count; i++)
            {
                var p = session.Prospects[i]; float y = 352 + i * 107;
                if (Btn(new Rect(780, y, 610, 99), "", prospectIndex == i)) prospectIndex = i;
                Label(new Rect(794, y + 6, 580, 25), $"후보 {i + 1}  ·  {p.State.DistanceCode}  ·  {p.State.Grid.Width}×{p.State.Grid.Height}", heading);
                Label(new Rect(794, y + 35, 580, 31), string.Join(" · ", p.State.Deposits.Select(d => d.Resource.DisplayName + " " + d.RemainingStacks.ToString("N0"))), small);
                Label(new Rect(794, y + 69, 580, 23), $"개척 비용 {p.Cost:N0} 솔라 (항구 포함)", text);
            }
            if (session.Prospects.Count == 0) Label(new Rect(792, 397, 590, 90), "탐사를 시작하면 발견한 혜성이 표시됩니다.\n개척 후 좌측 혜성 슬롯을 선택하여 건설할 수 있습니다.", small);
            GUI.enabled = (prospectIndex >= 0 && prospectIndex < session.Prospects.Count && session.Company.Credits >= session.Prospects[prospectIndex].Cost) && !GameOverlayOpen;
            if (Btn(new Rect(780, 698, 610, 52), "선택한 혜성 개척하기", true)) { session.Colonize(prospectIndex, out notice); prospectIndex = -1; }
            GUI.enabled = (true) && !GameOverlayOpen;
        }
        private int CyclePicker(string label, string value, int index, int count, float y)
        {
            Label(new Rect(780, y, 133, 32), label, small);
            if (Btn(new Rect(915, y, 43, 32), "◀")) index = (index + count - 1) % Math.Max(1, count);
            Label(new Rect(970, y, 359, 32), value, text);
            if (Btn(new Rect(1347, y, 43, 32), "▶")) index = (index + 1) % Math.Max(1, count);
            return index;
        }
        private void DrawFleet()
        {
            shipIndex = Mathf.Clamp(shipIndex, 0, session.Company.Ships.Count - 1);
            var ship = session.Company.Ships[shipIndex];
            shipIndex = CyclePicker("수송선", $"수송선 {shipIndex + 1} · {ship.Status} · 화물 {ship.Cargo.UsedStacks:0.#}/100", shipIndex, session.Company.Ships.Count, 239);
            shipFrom = Mathf.Clamp(shipFrom, 0, session.Colonies.Count - 1); shipTo = Mathf.Clamp(shipTo, 0, session.Colonies.Count - 1);
            shipFrom = CyclePicker("출발 혜성", session.Colonies[shipFrom].Name, shipFrom, session.Colonies.Count, 292);
            shipTo = CyclePicker("도착 혜성", session.Colonies[shipTo].Name, shipTo, session.Colonies.Count, 337);
            cargoIndex = CyclePicker("운송 물자", session.Content.Resources[cargoIndex].DisplayName, cargoIndex, session.Content.Resources.Count, 382);
            var r = session.Content.Resources[cargoIndex]; var from = session.Colonies[shipFrom]; var to = session.Colonies[shipTo];
            int max = (int)Math.Min(100, Math.Floor(session.Stock(from, r))); cargoQuantity = Mathf.Clamp(cargoQuantity, 1, Math.Max(1, max));
            Label(new Rect(780, 433, 610, 27), $"운송 수량 {cargoQuantity} 스택 · 출발 재고 {session.Stock(from, r):0.#}", heading);
            cargoQuantity = Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(782, 480, 606, 24), cargoQuantity, 1, Math.Max(1, max)));
            Label(new Rect(780, 522, 610, 35), $"운송 비용 {session.ShippingCost(from, to, cargoQuantity):N0} 솔라 · 소요 {session.ShippingSeconds(from, to):0.0}초", text);
            string error = session.ShippingError(shipIndex, shipFrom, shipTo, r, cargoQuantity);
            Label(new Rect(780, 568, 610, 60), error ?? "운송 중에도 다른 혜성을 개발할 수 있습니다. 도착 항구가 차면 화물을 선내에 보관하며 하역을 기다립니다.", small);
            GUI.enabled = (error == null) && !GameOverlayOpen;
            if (Btn(new Rect(780, 644, 610, 44), "운송 시작", true)) session.Dispatch(shipIndex, shipFrom, shipTo, r, cargoQuantity, out notice);
            GUI.enabled = (session.Company.Credits >= FoundrySession.ShipPurchaseCost) && !GameOverlayOpen;
            if (Btn(new Rect(780, 708, 610, 42), $"수송선 추가 구매 · {FoundrySession.ShipPurchaseCost:N0} 솔라")) session.PurchaseShip(out notice);
            GUI.enabled = (true) && !GameOverlayOpen;
        }
        private static string MonthlyChange(List<FoundryHistoryPoint> points)
        {
            if (points.Count < 2) return "첫 월간 기록 대기";
            double previous = points[points.Count - 2].value, current = points[points.Count - 1].value;
            double delta = current - previous;
            string amount = $"{delta:+#,##0.00;-#,##0.00;0.00} 솔라";
            return previous > 0 ? amount + $" ({delta / previous * 100:+0.00;-0.00;0.00}%)" : amount + " (이전 소지금 0)";
        }
        private void DrawHistory(Rect rect, List<FoundryHistoryPoint> points, Color color, string unit, double reference = 0, ResourceDefinition resource = null)
        {
            Fill(rect, new Color32(13, 25, 32, 255));
            if (points.Count == 0) return;
            double min = points.Min(p => p.value), max = points.Max(p => p.value), pad = Math.Max(1, (max - min) * .12);
            min = Math.Max(0, min - pad); max += pad;
            if (reference > 0) { min = reference * .70; max = reference * 1.30; }
            Rect plot = new Rect(rect.x + 80, rect.y + 36, rect.width - 148, rect.height - 78);
            if (reference <= 0)
                for (int i = 0; i <= 4; i++) { float y = plot.y + plot.height * i / 4; Fill(new Rect(plot.x, y, plot.width, 1), new Color32(46, 65, 75, 255)); Label(new Rect(rect.x + 5, y - 9, 69, 19), (max - (max - min) * i / 4).ToString("N0"), small); }
            if (reference > 0)
            {
                foreach (double change in new[] { -.30, -.15, 0, .15, .30 })
                {
                    float y = plot.yMax - (float)((reference * (1 + change) - min) / (max - min)) * plot.height;
                    Fill(new Rect(plot.x, y, plot.width, change == 0 ? 2 : 1), change == 0 ? Ink : new Color32(76, 94, 104, 255));
                    Label(new Rect(rect.x + 5, y - 10, 71, 20), (reference * (1 + change)).ToString("0.##"), change == 0 ? text : small);
                    Label(new Rect(plot.xMax + 8, y - 10, 58, 20), change == 0 ? "정가" : $"{change * 100:+0;-0}%", change == 0 ? text : small);
                }
            }
            double start = points[0].day, end = Math.Max(start + 1, points[points.Count - 1].day);
            int firstMonth = (int)Math.Floor(start / FoundrySession.DaysPerMonth);
            int lastMonth = (int)Math.Floor(points[points.Count - 1].day / FoundrySession.DaysPerMonth);
            int stride = Math.Max(1, (int)Math.Ceiling((lastMonth - firstMonth) / 6d));
            for (int month = firstMonth; month <= lastMonth; month++)
            {
                if ((month - firstMonth) % stride != 0 && month != lastMonth) continue;
                double day = month * FoundrySession.DaysPerMonth;
                float x = plot.x + (float)((day - start) / (end - start)) * plot.width;
                Fill(new Rect(x, plot.y, 1, plot.height), new Color32(32, 48, 59, 255));
                Label(new Rect(x - 35, plot.yMax + 8, 88, 22), FoundrySession.MonthAt(day), small);
            }
            string key = $"{resource?.ResourceId}/{points.Count}/{points[0].day:R}/{points[points.Count - 1].day:R}/{points[points.Count - 1].value:R}/{reference:R}";
            if (reference > 0)
            {
                if (priceChartTexture == null || priceChartKey != key) { if (priceChartTexture != null) Destroy(priceChartTexture); priceChartTexture = FoundryChartRaster.Create(points, (int)plot.width, (int)plot.height, min, max, reference, color, true); priceChartKey = key; }
                GUI.DrawTexture(plot, priceChartTexture);
            }
            else
            {
                if (balanceChartTexture == null || balanceChartKey != key) { if (balanceChartTexture != null) Destroy(balanceChartTexture); balanceChartTexture = FoundryChartRaster.Create(points, (int)plot.width, (int)plot.height, min, max, reference, color); balanceChartKey = key; }
                GUI.DrawTexture(plot, balanceChartTexture);
            }
            Label(new Rect(rect.x + 8, rect.y + 5, rect.width - 16, 23), unit + (points.Count == 1 ? " · 30일 경과 후 첫 월간 비교" : ""), small);
            if (plot.Contains(Event.current.mousePosition))
            {
                double day = start + (Event.current.mousePosition.x - plot.x) / plot.width * (end - start);
                var point = points.OrderBy(p => Math.Abs(p.day - day)).First();
                float cursorX = plot.x + (float)((point.day - start) / (end - start)) * plot.width;
                Fill(new Rect(cursorX, plot.y, 1, plot.height), new Color32(190, 203, 212, 160));
                int pointIndex = points.IndexOf(point);
                string change = pointIndex > 0 ? "전월 " + MonthlyChange(points.GetRange(pointIndex - 1, 2)) : "이전 월 기록 없음";
                string detail = reference > 0 ? $" · 정가 {(point.value / reference - 1) * 100:+0.00;-0.00;0.00}%" : "";
                string context = reference > 0 ? " · " + (session.MarketEventAt(resource, point.day) ?? "일반 시세") : "";
                Fill(new Rect(rect.x + 5, rect.y + 2, rect.width - 10, 50), Panel);
                Label(new Rect(rect.x + 9, rect.y + 4, rect.width - 18, 44), $"{FoundrySession.DateAt(point.day)} · {point.value:N2} {unit}" + detail + "\n" + change + context, small);

            }
        }
    }
}
