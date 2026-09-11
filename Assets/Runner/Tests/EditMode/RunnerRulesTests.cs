using NUnit.Framework;
using UnityEngine;

namespace PixelRunner.Tests
{
    public sealed class RunnerRulesTests
    {
        RunnerSettings settings;
        RunnerMotor motor;
        const float Dt = 1f/60f;
        [SetUp] public void Setup() { settings = ScriptableObject.CreateInstance<RunnerSettings>(); motor = new RunnerMotor(settings); }
        [TearDown] public void TearDown() => Object.DestroyImmediate(settings);
        Vector2 Tick(bool grounded = false,bool jump = false,bool release = false,bool dash = false) => motor.Step(Dt,5,grounded,jump,release,dash);
        [Test] public void GroundJumpThenAirJumpButNoThirdJump()
        {
            Tick(true,true); Assert.That(motor.JumpsUsed,Is.EqualTo(1));
            Tick(false,true); Assert.That(motor.JumpsUsed,Is.EqualTo(2));
            float before = motor.VerticalSpeed;
            Tick(false,true); Assert.That(motor.JumpsUsed,Is.EqualTo(2));
            Assert.That(motor.VerticalSpeed,Is.LessThan(before));
        }
        [Test] public void LandingRestoresBothJumps()
        {
            Tick(true,true); Tick(false,true);
            for (int i = 0; i < 40; i++) Tick();
            Tick(true); Assert.That(motor.JumpsUsed,Is.Zero);
            Tick(true,true); Assert.That(motor.JumpsUsed,Is.EqualTo(1));
        }
        [Test] public void ReleaseCutsJumpHeight()
        {
            float full = Tick(true,true).y;
            Assert.That(Tick(false,false,true).y,Is.LessThan(full * 0.5f));
        }
        [Test] public void CoyoteJumpKeepsAirJump()
        {
            Tick(true);
            for (int i = 0; i < 4; i++) Tick();
            Tick(false,true); Assert.That(motor.JumpsUsed,Is.EqualTo(1));
        }
        [Test] public void LeavingGroundPastCoyoteOnlyAllowsOneAirJump()
        {
            Tick(true);
            for (int i = 0; i < 8; i++) Tick();
            Tick(false,true); Assert.That(motor.JumpsUsed,Is.EqualTo(2));
        }
        [Test] public void BufferedJumpFiresOnLanding()
        {
            Tick(true,true); Tick(false,true);
            for (int i = 0; i < 40; i++) Tick();
            Tick(false,true);
            for (int i = 0; i < 3; i++) Tick();
            var velocity = Tick(true);
            Assert.That(velocity.y,Is.GreaterThan(0)); Assert.That(motor.JumpsUsed,Is.EqualTo(1));
        }
        [Test] public void ExpiredBufferDoesNotJumpOnLanding()
        {
            Tick(true,true); Tick(false,true);
            for (int i = 0; i < 40; i++) Tick();
            Tick(false,true);
            for (int i = 0; i < 9; i++) Tick();
            Assert.That(Tick(true).y,Is.LessThanOrEqualTo(0));
        }
        [Test] public void DashLastsTwelvePhysicsStepsAndRestoresVerticalSpeed()
        {
            Tick(true,true); float vertical = motor.VerticalSpeed;
            for (int i = 0; i < 12; i++)
            {
                var velocity = Tick(false,false,false,i == 0);
                Assert.That(velocity.x,Is.EqualTo(10)); Assert.That(velocity.y,Is.Zero);
            }
            Assert.That(motor.IsDashing,Is.False); Assert.That(motor.CanDash,Is.False);
            Assert.That(motor.VerticalSpeed,Is.EqualTo(vertical).Within(0.001));
            Assert.That(Tick().x,Is.EqualTo(5));
            Assert.That(motor.JumpsUsed,Is.EqualTo(1));
        }
        [Test] public void DashCannotRetriggerUntilCooldownEnds()
        {
            for (int i = 0; i < 12; i++) Tick(false,false,false,i == 0);
            for (int i = 0; i < 40; i++) Assert.That(Tick(false,false,false,true).x,Is.EqualTo(5));
            for (int i = 0; i < 3; i++) Tick();
            Assert.That(motor.CanDash,Is.True);
            Assert.That(Tick(false,false,false,true).x,Is.EqualTo(10));
        }
        [Test] public void ResetClearsAllMovementState()
        {
            Tick(true,true); Tick(false,true); Tick(false,false,false,true); motor.Reset();
            Assert.That(motor.JumpsUsed,Is.Zero); Assert.That(motor.VerticalSpeed,Is.Zero);
            Assert.That(motor.CanDash,Is.True); Assert.That(motor.IsDashing,Is.False);
        }
        [Test] public void SpeedRampsAndCaps()
        {
            Assert.That(settings.SpeedAt(0),Is.EqualTo(5));
            Assert.That(settings.SpeedAt(60),Is.EqualTo(7));
            Assert.That(settings.SpeedAt(120),Is.EqualTo(9));
            Assert.That(settings.SpeedAt(10000),Is.EqualTo(9));
        }
        [TestCase(0f)] [TestCase(30f)] [TestCase(120f)]
        public void ThousandsOfSeededTransitionsHaveLandingAndDashMargins(float elapsed)
        {
            for (int seed = 0; seed < 64; seed++)
            {
                var planner = new SectionPlanner(seed,settings);
                float previousHeight = 0, previousEnd = 24, previousObstacle = -100;
                for (int i = 0; i < 128; i++)
                {
                    var section = planner.Next(elapsed);
                    Assert.That(section.start-previousEnd,Is.EqualTo(section.gap).Within(0.001));
                    Assert.That(SectionPlanner.IsReachable(settings,section.gap,section.height-previousHeight,settings.SpeedAt(elapsed)),Is.True);
                    Assert.That(section.end-section.start,Is.GreaterThanOrEqualTo(14));
                    if (section.obstacle)
                    {
                        Assert.That(section.obstacleX-previousObstacle,Is.GreaterThanOrEqualTo(planner.MinimumObstacleSpacing));
                        Assert.That(section.obstacleX-section.start,Is.GreaterThanOrEqualTo(6));
                        Assert.That(section.end-section.obstacleX,Is.GreaterThanOrEqualTo(8));
                        previousObstacle = section.obstacleX;
                    }
                    previousEnd = section.end; previousHeight = section.height;
                }
            }
        }
        [Test] public void SeedsReproduceLayoutAndRebasePreservesIt()
        {
            var a = new SectionPlanner(123,settings); var b = new SectionPlanner(123,settings);
            for (int i = 0; i < 10; i++) Assert.That(a.Next(30),Is.EqualTo(b.Next(30)));
            a.Rebase(100);
            var pa = a.Next(30); var pb = b.Next(30);
            Assert.That(pa.start+100,Is.EqualTo(pb.start).Within(0.001));
            Assert.That(pa.obstacle,Is.EqualTo(pb.obstacle));
        }
        [TestCase(0.15f)] [TestCase(0.4f)]
        public void ParallaxMovesAtItsRatioAndRebaseKeepsItsPhase(float ratio)
        {
            var root = new GameObject("Parallax Test");
            try
            {
                var layer = root.AddComponent<RunnerParallax>();
                var tile = new GameObject("Tile"); tile.transform.SetParent(root.transform);
                layer.tiles = new[] { tile.transform }; layer.scrollRatio = ratio;
                layer.Position(5); float before = tile.transform.position.x-5;
                layer.Position(9); float after = tile.transform.position.x-9;
                Assert.That(after-before,Is.EqualTo(-4*ratio).Within(1f/16));
                float original = tile.transform.position.x;
                layer.Rebase(512); layer.Position(9-512);
                Assert.That(tile.transform.position.x+512,Is.EqualTo(original).Within(1f/16));
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
