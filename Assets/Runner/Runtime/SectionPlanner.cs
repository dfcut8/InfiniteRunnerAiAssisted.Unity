using System;
using UnityEngine;

namespace PixelRunner
{
    public struct SectionPlan
    {
        public float start, end, height, gap, obstacleX;
        public bool obstacle, arc;
    }
    public sealed class SectionPlanner
    {
        readonly System.Random random;
        readonly RunnerSettings settings;
        float end = 24, height, lastObstacle = -100;
        public SectionPlanner(int seed, RunnerSettings settings) { random = new System.Random(seed); this.settings = settings; }
        float Range(float a, float b) => Mathf.Lerp(a, b, (float)random.NextDouble());
        public static float SingleJumpFlight(RunnerSettings settings, float heightChange)
        {
            float discriminant = settings.jumpVelocity * settings.jumpVelocity - 2 * settings.gravity * heightChange;
            return discriminant <= 0 ? 0 : (settings.jumpVelocity + Mathf.Sqrt(discriminant)) / settings.gravity;
        }
        public static bool IsReachable(RunnerSettings settings, float gap, float heightChange, float speed)
        {
            // Conservative single-jump bound, reserving runway/landing margins. Double jump remains a recovery tool.
            return gap + 0.9f <= speed * SingleJumpFlight(settings, heightChange) && Mathf.Abs(heightChange) <= 0.75f;
        }
        public float MinimumObstacleSpacing => settings.maximumSpeed * (settings.dashSeconds * settings.dashMultiplier + settings.dashCooldown) + 3f;
        public SectionPlan Next(float elapsed)
        {
            float speed = settings.SpeedAt(elapsed);
            float nextHeight = elapsed < 20 ? 0 : Mathf.Clamp(height + (random.Next(3) - 1) * 0.5f, -1, 1);
            float gap = elapsed < 8 ? 1.25f : Range(1.5f, Mathf.Lerp(2, 3.5f, Mathf.Clamp01(elapsed / 120)));
            gap = Mathf.Floor(gap * 16) / 16;
            if (!IsReachable(settings, gap, nextHeight - height, speed))
                gap = Mathf.Max(1, Mathf.Floor((speed * SingleJumpFlight(settings, nextHeight - height) - 1f) * 16) / 16);
            var plan = new SectionPlan { start = end + gap, height = nextHeight, gap = gap, arc = random.Next(2) == 0 };
            // Integral even widths keep modular tiles seamless.
            plan.end = plan.start + random.Next(7, 10) * 2;
            plan.obstacleX = plan.start + 6;
            plan.obstacle = elapsed >= 4 && plan.obstacleX - lastObstacle >= MinimumObstacleSpacing;
            if (plan.obstacle) lastObstacle = plan.obstacleX;
            end = plan.end;
            height = nextHeight;
            return plan;
        }
        public void Rebase(float shift) { end -= shift; lastObstacle -= shift; }
    }
}
