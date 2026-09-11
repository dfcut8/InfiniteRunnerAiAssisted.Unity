using System.Collections.Generic;
using UnityEngine;

namespace PixelRunner
{
    public sealed class RunnerWorld : MonoBehaviour
    {
        public const int GroundLayer = 6;
        public const int ItemLayer = 7;
        public RunnerSettings settings;
        public RunnerSection sectionPrefab;
        public int ActiveCount => active.Count;
        public IReadOnlyList<RunnerSection> ActiveSections => active;
        readonly List<RunnerSection> active = new List<RunnerSection>();
        readonly Queue<RunnerSection> available = new Queue<RunnerSection>();
        SectionPlanner planner;
        float generatedEnd;
        void EnsurePool()
        {
            if (active.Count + available.Count > 0) return;
            for (int i = 0; i < settings.poolSize; i++)
            {
                var section = Instantiate(sectionPrefab, transform);
                section.name = "Platform Section " + i;
                section.gameObject.SetActive(false);
                available.Enqueue(section);
            }
        }
        public void ResetWorld(int seed)
        {
            EnsurePool();
            foreach (var section in active) { section.gameObject.SetActive(false); available.Enqueue(section); }
            active.Clear();
            planner = new SectionPlanner(seed, settings);
            var first = available.Dequeue();
            first.Configure(new SectionPlan { start = -8, end = 24, height = 0 }, true);
            active.Add(first);
            generatedEnd = 24;
            Tick(0, 0);
        }
        public void Tick(float playerX, float elapsed)
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                if (active[i].End >= playerX - 12) continue;
                var section = active[i];
                active.RemoveAt(i);
                section.gameObject.SetActive(false);
                available.Enqueue(section);
            }
            while (generatedEnd < playerX + settings.generateAhead && available.Count > 0)
            {
                // Estimate arrival time so prefetched sections follow the intended introduction schedule.
                var plan = planner.Next(elapsed + Mathf.Max(0, generatedEnd - playerX) / settings.SpeedAt(elapsed));
                var section = available.Dequeue();
                section.Configure(plan);
                active.Add(section);
                generatedEnd = plan.end;
            }
        }
        public void Rebase(float shift)
        {
            generatedEnd -= shift;
            planner.Rebase(shift);
            foreach (var section in active) section.Rebase(shift);
        }
    }
}
