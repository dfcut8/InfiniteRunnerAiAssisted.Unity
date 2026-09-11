using UnityEngine;

namespace PixelRunner
{
    public sealed class RunnerEffects : MonoBehaviour
    {
        public Sprite particle;
        public Material material;
        const int Count = 64;
        readonly SpriteRenderer[] sprites = new SpriteRenderer[Count];
        readonly Vector3[] velocities = new Vector3[Count];
        readonly float[] remaining = new float[Count];
        int cursor;
        static readonly Color Ivory = new Color32(243, 239, 217, 255);
        static readonly Color Gold = new Color32(181, 138, 67, 255);
        static readonly Color Moss = new Color32(98, 133, 72, 255);
        void Awake()
        {
            for (int i = 0; i < Count; i++)
            {
                sprites[i] = new GameObject("Pixel Effect " + i).AddComponent<SpriteRenderer>();
                sprites[i].transform.SetParent(transform);
                sprites[i].sharedMaterial = material;
                sprites[i].sortingOrder = 20;
                sprites[i].enabled = false;
            }
        }
        public void Clear()
        {
            cursor = 0;
            for (int i = 0; i < Count; i++) { remaining[i] = 0; sprites[i].enabled = false; }
        }
        void Spawn(Vector3 p, Sprite sprite, Color color, Vector3 velocity, float life, float scale)
        {
            int i = cursor++ % Count;
            sprites[i].sprite = sprite;
            sprites[i].color = color;
            sprites[i].transform.position = p;
            sprites[i].transform.localScale = Vector3.one * scale;
            sprites[i].enabled = true;
            velocities[i] = velocity;
            remaining[i] = life;
        }
        public void Burst(Vector3 p, bool death)
        {
            int count = death ? 20 : 7;
            for (int i = 0; i < count; i++)
            {
                float a = i * 2.399963f;
                Spawn(p, particle, death ? Ivory : Gold, new Vector3(Mathf.Cos(a) * 2.5f, Mathf.Sin(a) * 2.5f + 2, 0), death ? 0.75f : 0.35f, 1);
            }
        }
        public void Trail(Vector3 p, Sprite sprite) => Spawn(p, sprite, Moss, Vector3.zero, 0.13f, 1);
        void Update()
        {
            for (int i = 0; i < Count; i++)
            {
                if (remaining[i] <= 0) continue;
                remaining[i] -= Time.deltaTime;
                if (remaining[i] <= 0) { sprites[i].enabled = false; continue; }
                if (sprites[i].sprite == particle) velocities[i] += Vector3.down * (9 * Time.deltaTime);
                sprites[i].transform.position += velocities[i] * Time.deltaTime;
            }
        }
        public void Rebase(float shift)
        {
            for (int i = 0; i < Count; i++) sprites[i].transform.position -= Vector3.right * shift;
        }
    }
}
