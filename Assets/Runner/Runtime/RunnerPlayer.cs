using UnityEngine;
using UnityEngine.InputSystem;

namespace PixelRunner
{
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class RunnerPlayer : MonoBehaviour
    {
        public RunnerRun run;
        public InputActionAsset input;
        public SpriteRenderer visual;
        public Sprite[] frames;
        public Rigidbody2D Body { get; private set; }
        public RunnerMotor Motor { get; private set; }
        public bool Grounded { get; private set; }
        public bool DashActiveThisStep { get; private set; }
        public bool ExternalInput { get; set; }
        InputActionAsset ownedInput;
        InputAction jump, dash, restart;
        BoxCollider2D hitbox;
        readonly RaycastHit2D[] hits = new RaycastHit2D[24];
        bool jumpPressed, jumpReleased, dashPressed;
        float deathTime, trailTimer;
        void Awake()
        {
            Body = GetComponent<Rigidbody2D>();
            hitbox = GetComponent<BoxCollider2D>();
            Motor = new RunnerMotor(run.settings);
            ownedInput = Instantiate(input);
            var map = ownedInput.FindActionMap("Runner", true);
            jump = map.FindAction("Jump", true);
            dash = map.FindAction("Dash", true);
            restart = map.FindAction("Restart", true);
            map.Enable();
        }
        void OnDestroy()
        {
            if (ownedInput == null) return;
            ownedInput.Disable();
            Destroy(ownedInput);
        }
        void OnDisable() { if (ownedInput != null) ownedInput.Disable(); }
        void OnEnable() { if (ownedInput != null) ownedInput.FindActionMap("Runner").Enable(); }
        void Update()
        {
            if (!ExternalInput)
            {
                if (!run.IsRunning && restart.WasPressedThisFrame()) run.Restart();
                jumpPressed |= jump.WasPressedThisFrame();
                jumpReleased |= jump.WasReleasedThisFrame();
                dashPressed |= dash.WasPressedThisFrame();
            }
            if (!run.IsRunning)
            {
                deathTime += Time.deltaTime;
                visual.sprite = frames[11];
                visual.enabled = deathTime < 0.65f;
                return;
            }
            visual.enabled = true;
            visual.sprite = DashActiveThisStep ? frames[10] : !Grounded ? frames[Body.linearVelocity.y > 0 ? 8 : 9] : frames[(int)(run.Elapsed * 12f * run.Speed / 5f) % 8];
            trailTimer -= Time.deltaTime;
            if (DashActiveThisStep && trailTimer <= 0)
            {
                trailTimer = 0.045f;
                run.effects.Trail(transform.position, visual.sprite);
            }
        }
        public void QueueInput(bool pressJump = false, bool releaseJump = false, bool pressDash = false)
        {
            jumpPressed |= pressJump;
            jumpReleased |= releaseJump;
            dashPressed |= pressDash;
        }
        void FixedUpdate()
        {
            if (!run.IsRunning) { jumpPressed = jumpReleased = dashPressed = false; return; }
            Grounded = false;
            var groundFilter = new ContactFilter2D { useLayerMask = true, layerMask = 1 << RunnerWorld.GroundLayer, useTriggers = false };
            int count = hitbox.Cast(Vector2.down, groundFilter, hits, 0.065f);
            for (int i = 0; i < count; i++) if (hits[i].normal.y > 0.7f) Grounded = true;
            bool wasDashing = Motor.IsDashing;
            bool startsDash = dashPressed && Motor.CanDash;
            Vector2 velocity = Motor.Step(Time.fixedDeltaTime, run.Speed, Grounded, jumpPressed, jumpReleased, dashPressed);
            DashActiveThisStep = wasDashing || startsDash;
            jumpPressed = jumpReleased = dashPressed = false;
            // Sweep triggers as well as using callbacks: a fast dash must not skip thin objects.
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = 1 << RunnerWorld.ItemLayer, useTriggers = true };
            count = hitbox.Cast(velocity.normalized, filter, hits, velocity.magnitude * Time.fixedDeltaTime + 0.01f);
            for (int i = 0; i < count && run.IsRunning; i++)
                if (hits[i].collider.TryGetComponent<RunnerItem>(out var item)) item.Touch(this);
            if (!run.IsRunning) return;
            Body.linearVelocity = velocity;
            if (Body.position.y < run.settings.deathHeight) run.Die();
        }
        void OnCollisionEnter2D(Collision2D collision) => CheckTerrain(collision);
        void OnCollisionStay2D(Collision2D collision) => CheckTerrain(collision);
        void CheckTerrain(Collision2D collision)
        {
            if (!run.IsRunning) return;
            for (int i = 0; i < collision.contactCount; i++)
                if (collision.GetContact(i).normal.x < -0.5f) { run.Die(); return; }
        }
        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent<RunnerItem>(out var item)) item.Touch(this);
        }
        public void Stop()
        {
            Body.linearVelocity = Vector2.zero;
            Body.simulated = false;
            DashActiveThisStep = false;
            jumpPressed = jumpReleased = dashPressed = false;
        }
        public void ResetPlayer()
        {
            Motor.Reset();
            Body.simulated = true;
            Body.position = new Vector2(0, 0.03f);
            transform.position = new Vector3(0, 0.03f, 0);
            Body.linearVelocity = Vector2.zero;
            Grounded = true;
            DashActiveThisStep = false;
            deathTime = trailTimer = 0;
            visual.enabled = true;
            visual.sprite = frames[0];
            jumpPressed = jumpReleased = dashPressed = false;
        }
        public void Rebase(float shift)
        {
            Vector2 position = Body.position - Vector2.right * shift;
            Body.position = position;
            transform.position = position;
        }
    }
}
