using System;
using UnityEngine;

namespace PixelRunner
{
    [DefaultExecutionOrder(-200)]
    public sealed class RunnerRun : MonoBehaviour
    {
        public RunnerSettings settings;
        public RunnerPlayer player;
        public RunnerWorld world;
        public RunnerCamera followCamera;
        public RunnerEffects effects;
        public event Action ScoreChanged;
        public event Action<bool> RunStateChanged;
        public bool IsRunning { get; private set; }
        public float Elapsed { get; private set; }
        public double Distance { get; private set; }
        public int Relics { get; private set; }
        public int Bonus { get; private set; }
        public int Seed { get; private set; }
        public int RebaseCount { get; private set; }
        public int Score => (int)Math.Min(int.MaxValue, Math.Floor(Distance * 10) + Bonus);
        public float Speed => settings.SpeedAt(Elapsed);
        float previousX;
        int lastScore;
        void Awake() => Time.fixedDeltaTime = 1f / 60f;
        void Start() => Restart();
        public void Restart() => Restart(unchecked(Environment.TickCount ^ (Seed * 397 + 17)));
        public void Restart(int seed)
        {
            Seed = seed;
            Elapsed = previousX = 0;
            Distance = 0;
            Relics = Bonus = lastScore = RebaseCount = 0;
            IsRunning = true;
            world.ResetWorld(seed);
            player.ResetPlayer();
            effects.Clear();
            followCamera.ResetView();
            Physics2D.SyncTransforms();
            ScoreChanged?.Invoke();
            RunStateChanged?.Invoke(true);
        }
        void FixedUpdate()
        {
            if (!IsRunning) return;
            Elapsed += Time.fixedDeltaTime;
            Distance += Mathf.Max(0, player.Body.position.x - previousX);
            previousX = player.Body.position.x;
            if (previousX >= settings.rebaseDistance)
            {
                float shift = settings.rebaseDistance;
                player.Rebase(shift);
                world.Rebase(shift);
                effects.Rebase(shift);
                followCamera.Rebase(shift);
                previousX -= shift;
                RebaseCount++;
                Physics2D.SyncTransforms();
            }
            world.Tick(player.Body.position.x, Elapsed);
            if (lastScore != Score) { lastScore = Score; ScoreChanged?.Invoke(); }
        }
        public void AwardRelic()
        {
            if (!IsRunning) return;
            Relics++;
            Bonus += 25;
            ScoreChanged?.Invoke();
        }
        public void AwardObstacle()
        {
            if (!IsRunning) return;
            Bonus += 100;
            ScoreChanged?.Invoke();
        }
        public void Die()
        {
            if (!IsRunning) return;
            Distance += Mathf.Max(0, player.Body.position.x - previousX);
            previousX = player.Body.position.x;
            IsRunning = false;
            player.Stop();
            effects.Burst(player.transform.position + Vector3.up * 0.7f, true);
            ScoreChanged?.Invoke();
            RunStateChanged?.Invoke(false);
        }
    }
}
