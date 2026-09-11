using UnityEngine;

namespace PixelRunner
{
    [CreateAssetMenu(menuName = "Pixel Runner/Settings")]
    public sealed class RunnerSettings : ScriptableObject
    {
        [Header("Running")]
        public float startSpeed = 5f;
        public float maximumSpeed = 9f;
        public float accelerationSeconds = 120f;
        [Header("Jump")]
        public float jumpVelocity = 9f;
        public float gravity = 28f;
        public float jumpReleaseMultiplier = 0.45f;
        public float coyoteSeconds = 0.1f;
        public float jumpBufferSeconds = 0.12f;
        [Header("Dash")]
        public float dashSeconds = 0.2f;
        public float dashMultiplier = 2f;
        public float dashCooldown = 0.7f;
        [Header("World")]
        public float deathHeight = -6f;
        public float rebaseDistance = 512f;
        public float generateAhead = 44f;
        public int poolSize = 10;
        public float SpeedAt(float seconds) => Mathf.Lerp(startSpeed, maximumSpeed, Mathf.Clamp01(seconds / accelerationSeconds));
    }
}
