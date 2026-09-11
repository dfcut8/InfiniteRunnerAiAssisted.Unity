using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace PixelRunner
{
    public sealed class RunnerHud : MonoBehaviour
    {
        public RunnerRun run;
        public Camera gameCamera;
        public PixelPerfectCamera pixelCamera;
        public Canvas canvas;
        public RectTransform viewport;
        public TextMeshProUGUI score, relics, dash, hint, death;
        public GameObject deathPanel;
        public UnityEngine.UI.Image dashFill;
        void OnEnable()
        {
            run.ScoreChanged += RefreshScore;
            run.RunStateChanged += RefreshState;
        }
        void OnDisable()
        {
            run.ScoreChanged -= RefreshScore;
            run.RunStateChanged -= RefreshState;
        }
        void Start() { RefreshScore(); RefreshState(run.IsRunning); }
        void RefreshScore()
        {
            score.text = "SCORE " + run.Score.ToString("D6");
            relics.text = "RELICS " + run.Relics.ToString("D3");
        }
        void RefreshState(bool alive)
        {
            deathPanel.SetActive(!alive);
            death.text = "RUN ENDED\n\nR TO RETRY";
        }
        void LateUpdate()
        {
            int scale = Mathf.Max(1, Mathf.Min(Screen.width / 320, Screen.height / 180));
            canvas.scaleFactor = scale;
            viewport.sizeDelta = new Vector2(320, 180);
            hint.gameObject.SetActive(run.IsRunning && run.Elapsed < 6);
            if (run.player.Motor == null) return;
            bool ready = run.player.Motor.CanDash;
            dash.text = ready ? "DASH READY" : "DASH";
            dashFill.rectTransform.sizeDelta = new Vector2(Mathf.Round(38 * run.player.Motor.DashReadiness), 2);
        }
    }
}
