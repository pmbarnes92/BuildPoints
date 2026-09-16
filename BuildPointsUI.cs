using UnityEngine;

namespace BuildPoints
{
    /// <summary>
    /// Draws a small always-on readout of the current Build Points balance,
    /// positioned near the stock Funds/Science/Reputation bar. Uses OnGUI
    /// for a first pass; a follow-up could anchor a proper UI Toolkit panel
    /// directly into the stock app bar the way RP-1 does.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class BuildPointsUI : MonoBehaviour
    {
        private GUIStyle labelStyle;
        private Rect windowRect = new Rect(260, 0, 150, 30);

        public void Update()
        {
            // Nothing per-frame needed yet; accrual lives in BuildPointsScenario.
        }

        public void OnGUI()
        {
            if (HighLogic.CurrentGame == null) return;
            if (BuildPointsScenario.Instance == null) return;
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER && HighLogic.CurrentGame.Mode != Game.Modes.SCIENCE_SANDBOX)
                return;

            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = Color.white }
                };
            }

            double current = BuildPointsScenario.Instance.CurrentPoints;
            double cap = BuildPointsScenario.Instance.GetCapacity();

            GUI.Label(windowRect, $"Build Points: {current:0.0} / {cap:0}", labelStyle);
        }
    }
}
