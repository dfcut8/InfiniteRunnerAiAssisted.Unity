using UnityEngine;

namespace PixelRunner
{
    public sealed class RunnerParallax : MonoBehaviour
    {
        [Range(0, 1)] public float scrollRatio = 0.15f;
        public float tileWidth = 20f;
        public Transform[] tiles;
        double distanceOffset;
        public void Position(float cameraX)
        {
            double travel = (cameraX + distanceOffset) * scrollRatio;
            float phase = (float)(travel % tileWidth);
            for (int i = 0; i < tiles.Length; i++)
            {
                var p = tiles[i].position;
                p.x = Mathf.Round((cameraX - phase + (i - 1) * tileWidth) * 16) / 16;
                tiles[i].position = p;
            }
        }
        public void ResetLayer() => distanceOffset = 0;
        public void Rebase(float shift) => distanceOffset += shift;
    }
}
