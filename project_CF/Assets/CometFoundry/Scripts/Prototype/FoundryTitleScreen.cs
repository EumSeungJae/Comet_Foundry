using CometFoundry.Simulation;
using UnityEngine;

namespace CometFoundry.Prototype
{
    public sealed partial class FoundryPlayableController
    {
        private Texture2D titleArtwork;
        private bool showingTitle = true;
        private bool hasActiveGame;
        private string titleNotice = "";

        private void DrawTitleScreen()
        {
            // Fit the entire cover so its embedded logo survives every aspect ratio.
            if (titleArtwork != null)
                GUI.DrawTexture(new Rect(0, 0, 1440, 810), titleArtwork, ScaleMode.ScaleToFit);
            else
                Label(new Rect(100, 130, 950, 110), "COMET FOUNDRY", Style(64, Ink, FontStyle.Bold));

            Fill(new Rect(0, 810, 1440, 90), new Color32(10, 18, 25, 255));
            Rect menu = new Rect(92, 382, 390, 384);
            Fill(menu, new Color32(10, 18, 25, 232));
            Fill(new Rect(menu.x, menu.y, 4, menu.height), Orange);
            Label(new Rect(118, 398, 340, 30), "A-0 관문 / 개척 사업부", Style(19, Ink, FontStyle.Bold));
            Label(new Rect(118, 435, 336, 48), "혜성을 개척하고, 생산과 물류를 연결해\n우주 산업의 다음 거점을 세우세요.", Style(15, Muted));

            if (TitleButton(new Rect(118, 505, 338, 52), hasActiveGame ? "개척 재개" : "새 개척 시작", true))
            {
                if (!hasActiveGame) tutorialChoice = true;
                showingTitle = false; hasActiveGame = true; confirmNew = false; titleNotice = "";
            }
            bool enabled = GUI.enabled;
            GUI.enabled = PlayerPrefs.HasKey(FoundrySession.SaveKey);
            if (TitleButton(new Rect(118, 568, 338, 46), "저장한 개척 불러오기"))
            {
                if (session.Load())
                {
                    build = null; ResetPanelSelection(); speed = 1; tutorialChoice = true;
                    notice = "저장한 개척 데이터를 불러왔습니다.";
                    showingTitle = false; hasActiveGame = true; titleNotice = "";
                }
                else titleNotice = "저장 데이터를 읽을 수 없습니다.";
            }
            GUI.enabled = enabled;
            if (hasActiveGame && TitleButton(new Rect(118, 625, 338, 46), confirmNew ? "새 개척 확인 · 현재 진행 초기화" : "새 개척 시작"))
            {
                if (confirmNew)
                {
                    session.NewGame(); build = null; ResetPanelSelection(); speed = 1; tutorialChoice = true;
                    notice = "새 개척지를 시작했습니다. 정제소 건설부터 진행하세요.";
                    confirmNew = false; showingTitle = false; titleNotice = "";
                }
                else { confirmNew = true; titleNotice = "현재 진행을 초기화합니다. Esc로 취소."; }
            }
            if (TitleButton(new Rect(118, hasActiveGame ? 682 : 625, 338, 46), "종료"))
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
            Label(new Rect(118, 735, 338, 22), titleNotice, Style(12, Orange));
            Label(new Rect(92, 832, 900, 26), "COMET FOUNDRY  /  혜성 산업 · 경영 · 물류 시뮬레이션", Style(14, Ink));
            Label(new Rect(92, 863, 900, 20), "서체: 넥슨 메이플스토리  ·  개발 중인 플레이 버전", small);
            if (!PlayerPrefs.HasKey(FoundrySession.SaveKey))
                Label(new Rect(1040, 836, 310, 24), "저장된 개척 기록 없음", small);
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            { confirmNew = false; titleNotice = ""; Event.current.Use(); }
        }

        private bool TitleButton(Rect rect, string label, bool primary = false)
        {
            bool hover = GUI.enabled && rect.Contains(Event.current.mousePosition);
            Color background = primary ? Orange : hover ? new Color32(52, 72, 84, 255) : new Color32(29, 45, 57, 255);
            if (!GUI.enabled) background = new Color32(24, 32, 39, 255);
            Fill(rect, background);
            var style = Style(primary ? 20 : 17, !GUI.enabled ? Muted : primary ? DarkText() : Ink, primary ? FontStyle.Bold : FontStyle.Normal);
            style.padding = new RectOffset(18, 18, 0, 0);
            // Explicit backgrounds keep keyboard focus and pointer interaction visible.
            style.normal.background = Texture2D.whiteTexture;
            style.hover.background = Texture2D.whiteTexture;
            style.active.background = Texture2D.whiteTexture;
            style.focused.background = Texture2D.whiteTexture;
            Color old = GUI.backgroundColor;
            GUI.backgroundColor = background;
            bool clicked = GUI.Button(rect, label, style);
            GUI.backgroundColor = old;
            return clicked;
        }
    }
}
