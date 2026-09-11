using UnityEngine;

namespace PixelRunner
{
    public sealed class RunnerSection : MonoBehaviour
    {
        public BoxCollider2D ground;
        public SpriteRenderer[] tiles;
        public SpriteRenderer[] trim;
        public SpriteRenderer[] decorations;
        public RunnerItem obstacle;
        public RunnerItem[] relics;
        public float End { get; private set; }
        public SectionPlan Plan { get; private set; }
        public void Configure(SectionPlan plan, bool opening = false)
        {
            Plan = plan;
            transform.position = new Vector3(plan.start, plan.height, 0);
            float width = plan.end - plan.start;
            End = plan.end;
            ground.size = new Vector2(width, 3);
            ground.offset = new Vector2(width * 0.5f, -1.5f);
            for (int i = 0; i < tiles.Length; i++)
            {
                tiles[i].gameObject.SetActive(i * 2 < width);
                tiles[i].transform.localPosition = new Vector3(i * 2, -2, 0);
                tiles[i].size = new Vector2(Mathf.Min(2, width - i * 2), 2);
                trim[i].gameObject.SetActive(i * 2 < width);
                trim[i].transform.localPosition = new Vector3(i * 2, -3, 0);
            }
            for (int i = 0; i < decorations.Length; i++)
                decorations[i].transform.localPosition = new Vector3(2 + i * 5, 0, 0);
            obstacle.transform.localPosition = new Vector3(plan.obstacleX - plan.start, 0, 0);
            obstacle.ResetItem();
            obstacle.gameObject.SetActive(plan.obstacle);
            for (int i = 0; i < relics.Length; i++)
            {
                relics[i].ResetItem();
                // Trails occupy the clear entry/exit runway, never an obstacle's hitbox.
                float localX = opening ? 12 + i : 1.25f + i * 0.85f;
                float localY = plan.arc ? 0.8f + Mathf.Sin(i / (float)(relics.Length - 1) * Mathf.PI) * 1.5f : 0.9f;
                relics[i].transform.localPosition = new Vector3(localX, localY, 0);
            }
            gameObject.SetActive(true);
        }
        public void Rebase(float shift)
        {
            transform.position -= Vector3.right * shift;
            End -= shift;
            var plan = Plan;
            plan.start -= shift; plan.end -= shift; plan.obstacleX -= shift;
            Plan = plan;
        }
    }
}
