using UnityEngine;

namespace PixelRunner
{
    [DefaultExecutionOrder(100)]
    public sealed class RunnerCamera : MonoBehaviour
    {
        public RunnerPlayer player;
        public RunnerParallax[] layers;
        public float lookAhead = 5f;
        public float fixedY = 1.5f;
        void LateUpdate() => PositionView();
        void PositionView()
        {
            transform.position = new Vector3(player.transform.position.x + lookAhead, fixedY, -10);
            foreach (var layer in layers) layer.Position(transform.position.x);
        }
        public void ResetView()
        {
            foreach (var layer in layers) layer.ResetLayer();
            PositionView();
        }
        public void Rebase(float shift)
        {
            foreach (var layer in layers) layer.Rebase(shift);
            transform.position -= Vector3.right * shift;
        }
    }
}
