using UnityEngine;

namespace PixelRunner
{
    public sealed class RunnerItem : MonoBehaviour
    {
        public bool obstacle;
        public bool Consumed { get; private set; }
        public void ResetItem() { Consumed = false; gameObject.SetActive(true); }
        public void Touch(RunnerPlayer player)
        {
            if (Consumed || !player.run.IsRunning) return;
            if (obstacle && !player.DashActiveThisStep) { player.run.Die(); return; }
            Consumed = true;
            if (obstacle) player.run.AwardObstacle(); else player.run.AwardRelic();
            player.run.effects.Burst(transform.position + (obstacle ? Vector3.up : Vector3.zero), false);
            gameObject.SetActive(false);
        }
    }
}
