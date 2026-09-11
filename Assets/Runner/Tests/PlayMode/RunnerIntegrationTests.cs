using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace PixelRunner.Tests
{
    public sealed class RunnerIntegrationTests
    {
        RunnerRun run;
        SimulationMode2D previousMode;
        [UnitySetUp] public IEnumerator Setup()
        {
            previousMode = Physics2D.simulationMode;
            yield return SceneManager.LoadSceneAsync("MainGameplay");
            yield return null;
            run = Object.FindAnyObjectByType<RunnerRun>();
            run.player.ExternalInput = true;
            Physics2D.simulationMode = SimulationMode2D.Script;
            run.Restart(731);
        }
        [UnityTearDown] public IEnumerator TearDown() { Physics2D.simulationMode = previousMode; yield return null; }
        void Step(int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                run.SendMessage("FixedUpdate"); run.player.SendMessage("FixedUpdate");
                Physics2D.Simulate(Time.fixedDeltaTime);
            }
        }
        RunnerItem PlaceItem(bool obstacle,float x)
        {
            var item = run.world.ActiveSections[0].relics[0];
            if (obstacle) item = run.world.ActiveSections[0].obstacle;
            item.ResetItem(); item.transform.position = new Vector3(x,obstacle ? 0 : 0.7f,0);
            Physics2D.SyncTransforms(); return item;
        }
        [UnityTest] public IEnumerator StartsImmediatelyAndActuallyRunsOnGround()
        {
            Step(60);
            Assert.That(run.IsRunning,Is.True); Assert.That(run.player.Body.position.x,Is.InRange(4.9f,5.2f));
            Assert.That(run.player.Body.position.y,Is.InRange(-0.1f,0.1f));
            Assert.That(run.Score,Is.GreaterThan(40)); yield return null;
        }
        [UnityTest] public IEnumerator KeyboardBindingsDriveJumpDashAndRetry()
        {
            // Batch Editors have no focused Game view. Route synthetic events to game input
            // temporarily; restore the focus policy after the test.
            var originalInputSettings = InputSystem.settings;
            var originalBackground = originalInputSettings.backgroundBehavior;
            originalInputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            var originalEditorBehavior = originalInputSettings.editorInputBehaviorInPlayMode;
            originalInputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            var keyboard = InputSystem.AddDevice<Keyboard>();
            run.player.ExternalInput = false;
            try
            {
                Step(2);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space));
                InputSystem.Update(); run.player.SendMessage("Update"); Step();
                Assert.That(run.player.Motor.JumpsUsed,Is.EqualTo(1));
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());
                InputSystem.Update(); run.player.SendMessage("Update"); Step();
                Assert.That(run.player.Motor.VerticalSpeed,Is.LessThan(5));
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.X));
                InputSystem.Update(); run.player.SendMessage("Update"); Step();
                Assert.That(run.player.DashActiveThisStep,Is.True);
                run.Die();
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.R));
                InputSystem.Update(); run.player.SendMessage("Update");
                Assert.That(run.IsRunning,Is.True); Assert.That(run.Score,Is.Zero);
                Assert.That(run.player.Body.position.x,Is.Zero);
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard); run.player.ExternalInput = true;
                originalInputSettings.backgroundBehavior = originalBackground;
#if UNITY_EDITOR
                originalInputSettings.editorInputBehaviorInPlayMode = originalEditorBehavior;
#endif
            }
            yield return null;
        }
        [UnityTest] public IEnumerator PhysicalDoubleJumpAndLanding()
        {
            Step(2); run.player.QueueInput(pressJump:true); Step(12);
            Assert.That(run.player.Body.position.y,Is.GreaterThan(1));
            run.player.QueueInput(pressJump:true); Step(1);
            Assert.That(run.player.Motor.JumpsUsed,Is.EqualTo(2));
            Step(65);
            Assert.That(run.IsRunning,Is.True); Assert.That(run.player.Grounded,Is.True);
            Assert.That(run.player.Motor.JumpsUsed,Is.Zero); yield return null;
        }
        [UnityTest] public IEnumerator RelicOnlyAwardsOnce()
        {
            var item = PlaceItem(false,0.7f); Step(12);
            Assert.That(run.Relics,Is.EqualTo(1)); item.Touch(run.player);
            Assert.That(run.Relics,Is.EqualTo(1)); Assert.That(run.Bonus,Is.EqualTo(25)); yield return null;
        }
        [UnityTest] public IEnumerator ObstacleKillsWithoutDashAndDeathFreezesScore()
        {
            PlaceItem(true,1.4f); Step(30);
            Assert.That(run.IsRunning,Is.False); int score = run.Score; float x = run.player.Body.position.x;
            Step(60); run.AwardRelic(); run.AwardObstacle();
            Assert.That(run.Score,Is.EqualTo(score)); Assert.That(run.player.Body.position.x,Is.EqualTo(x)); yield return null;
        }
        [UnityTest] public IEnumerator DashBreaksObstacleAndDoesNotDoubleAward()
        {
            var obstacle = PlaceItem(true,1.25f);
            run.player.QueueInput(pressDash:true); Step(10);
            Assert.That(run.IsRunning,Is.True); Assert.That(obstacle.Consumed,Is.True);
            Assert.That(run.Bonus,Is.EqualTo(100)); obstacle.Touch(run.player);
            Assert.That(run.Bonus,Is.EqualTo(100)); yield return null;
        }
        [UnityTest] public IEnumerator FallKillsEvenDuringDashAndRetryResetsEverything()
        {
            run.AwardRelic(); run.AwardObstacle(); run.player.Body.position = new Vector2(0,-7);
            run.player.QueueInput(pressDash:true); Step(); Assert.That(run.IsRunning,Is.False);
            run.Restart(732);
            Assert.That(run.IsRunning,Is.True); Assert.That(run.Score,Is.Zero); Assert.That(run.Relics,Is.Zero);
            Assert.That(run.player.Motor.CanDash,Is.True); Assert.That(run.player.Motor.JumpsUsed,Is.Zero);
            Assert.That(run.player.Body.position.x,Is.Zero); Assert.That(run.Elapsed,Is.Zero);
            Assert.That(run.world.ActiveCount,Is.InRange(2,5));
            Assert.That(run.world.ActiveSections[0].relics[0].Consumed,Is.False); yield return null;
        }
        [UnityTest] public IEnumerator DashDoesNotProtectAgainstTerrainWalls()
        {
            var wall = new GameObject("Test Wall",typeof(BoxCollider2D)); wall.layer = RunnerWorld.GroundLayer;
            wall.transform.position = new Vector3(1.6f,0.5f,0);
            wall.GetComponent<BoxCollider2D>().size = new Vector2(0.5f,3);
            Physics2D.SyncTransforms(); run.player.QueueInput(pressDash:true); Step(20);
            Assert.That(run.IsRunning,Is.False); Object.Destroy(wall); yield return null;
        }
        [UnityTest] public IEnumerator RebaseKeepsDistanceAndParallaxAndPoolBounded()
        {
            float originalThreshold = run.settings.rebaseDistance;
            run.settings.rebaseDistance = 2;
            try
            {
                Step(60);
                Assert.That(run.IsRunning,Is.True); Assert.That(run.RebaseCount,Is.GreaterThanOrEqualTo(2));
                Assert.That(run.Distance,Is.InRange(4.8,5.2)); Assert.That(run.player.Body.position.x,Is.LessThan(2));
                Assert.That(run.world.transform.childCount,Is.EqualTo(run.settings.poolSize));
            }
            finally { run.settings.rebaseDistance = originalThreshold; }
            yield return null;
        }
        [UnityTest] public IEnumerator TenMinuteRunsAtThirtySixtyAndOneTwentyInputFrames()
        {
            foreach (int fps in new[] { 30,60,120 })
            {
                run.Restart(731);
                double accumulator = 0;
                for (int frame = 0; frame < fps * 600; frame++)
                {
                    float x = run.player.Body.position.x;
                    foreach (var section in run.world.ActiveSections)
                    {
                        if (run.player.Grounded && run.player.Motor.VerticalSpeed <= 0 && x >= section.Plan.start && x < section.End && section.End-x <= 1.05f + run.Speed / fps)
                            run.player.QueueInput(pressJump:true);
                        var obstacle = section.obstacle;
                        if (obstacle.gameObject.activeInHierarchy && obstacle.transform.position.x > x && obstacle.transform.position.x-x < 1.05f + run.Speed * 0.12f && run.player.Motor.CanDash)
                            run.player.QueueInput(pressDash:true);
                    }
                    accumulator += 1.0/fps;
                    while (accumulator + 1e-8 >= Time.fixedDeltaTime)
                    {
                        Step(); accumulator -= Time.fixedDeltaTime;
                        Assert.That(run.IsRunning,Is.True,$"Died at {fps} FPS, t={run.Elapsed:F2}, x={run.player.Body.position.x:F2}, y={run.player.Body.position.y:F2}");
                    }
                }
                Assert.That(run.Elapsed,Is.GreaterThan(599));
                Assert.That(run.RebaseCount,Is.GreaterThanOrEqualTo(9));
                Assert.That(run.world.transform.childCount,Is.EqualTo(run.settings.poolSize));
                Assert.That(run.Relics,Is.GreaterThan(100));
                Assert.That(run.Speed,Is.EqualTo(9));
            }
            yield return null;
        }
    }
}
