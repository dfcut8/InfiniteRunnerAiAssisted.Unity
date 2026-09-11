using UnityEngine;

namespace PixelRunner
{
    // Pure movement state, shared by gameplay and deterministic tests.
    public sealed class RunnerMotor
    {
        readonly RunnerSettings settings;
        float coyote, buffer, dashLeft, cooldown, savedVertical;
        bool jumpCut;
        public float VerticalSpeed { get; private set; }
        public int JumpsUsed { get; private set; }
        public bool IsDashing => dashLeft > 0;
        public bool CanDash => !IsDashing && cooldown <= 0;
        public float DashReadiness => IsDashing ? 0 : 1f - Mathf.Clamp01(cooldown / settings.dashCooldown);
        public RunnerMotor(RunnerSettings settings) { this.settings = settings; }
        public void Reset()
        {
            coyote = buffer = dashLeft = cooldown = savedVertical = VerticalSpeed = 0;
            JumpsUsed = 0;
            jumpCut = false;
        }
        public Vector2 Step(float dt, float speed, bool grounded, bool jumpPressed, bool jumpReleased, bool dashPressed)
        {
            cooldown = Mathf.Max(0, cooldown - dt);
            buffer = jumpPressed ? settings.jumpBufferSeconds : Mathf.Max(0, buffer - dt);
            if (grounded && VerticalSpeed <= 0 && !IsDashing)
            {
                coyote = settings.coyoteSeconds;
                JumpsUsed = 0;
                VerticalSpeed = 0;
            }
            else coyote = Mathf.Max(0, coyote - dt);

            if (dashPressed && CanDash)
            {
                savedVertical = VerticalSpeed;
                dashLeft = settings.dashSeconds;
            }
            if (jumpReleased) jumpCut = true;
            if (IsDashing)
            {
                dashLeft = Mathf.Max(0, dashLeft - dt);
                if (dashLeft < 0.00001f)
                {
                    dashLeft = 0;
                    cooldown = settings.dashCooldown;
                    VerticalSpeed = savedVertical;
                }
                // The final dash tick still moves at dash speed, but restores gravity next tick.
                return new Vector2(speed * settings.dashMultiplier, 0);
            }
            if (buffer > 0 && (coyote > 0 || JumpsUsed < 2))
            {
                // Walking off a ledge consumes the ground jump when coyote time expires.
                if (coyote <= 0 && JumpsUsed == 0) JumpsUsed = 1;
                JumpsUsed++;
                VerticalSpeed = settings.jumpVelocity;
                coyote = buffer = 0;
                jumpCut = jumpReleased;
            }
            if (jumpCut && VerticalSpeed > 0)
            {
                VerticalSpeed *= settings.jumpReleaseMultiplier;
                jumpCut = false;
            }
            VerticalSpeed -= settings.gravity * dt;
            return new Vector2(speed, VerticalSpeed);
        }
    }
}
