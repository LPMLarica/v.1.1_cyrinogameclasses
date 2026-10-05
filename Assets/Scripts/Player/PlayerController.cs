using UnityEngine;

namespace Faisca
{
    public enum DamageSource { Spikes, Arc, Enemy, Fall }

    /// <summary>
    /// Controle da Faísca: corrida com aceleração, pulo com coyote time,
    /// jump buffer e pulo variável, Pulso (dash), dano e respawn.
    /// A física fica em FixedUpdate; a leitura de input em Update.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Corrida")]
        public float moveSpeed = 7f;
        public float acceleration = 70f;
        public float deceleration = 90f;
        [Range(0f, 1f)] public float airControl = 0.8f;

        [Header("Pulo")]
        public float jumpVelocity = 16f;
        public float gravityScale = 3.5f;
        public float fallGravityMultiplier = 1.7f;
        [Range(0f, 1f)] public float jumpCutMultiplier = 0.5f;
        public float maxFallSpeed = 20f;
        public float coyoteTime = 0.10f;
        public float jumpBufferTime = 0.12f;
        public float groundCheckDistance = 0.06f;

        [Header("Pulso (dash)")]
        public float dashSpeed = 18f;
        public float dashDuration = 0.15f;
        public float dashCooldown = 0.30f;

        [Header("Dano")]
        public float invulnerableTime = 1.0f;
        public Vector2 hurtKnockback = new Vector2(5f, 9f);
        public float stompBounce = 13f;

        [Header("Visual")]
        public Transform visual;
        public SpriteRenderer spriteRenderer;
        public Animator animator;
        [Tooltip("Distância do centro do sprite até a base do corpo (para o squash não 'descolar' do chão).")]
        public float feetOffset = 0.44f;

        // estado
        Rigidbody2D rb;
        CapsuleCollider2D body;
        ContactFilter2D groundFilter;
        readonly RaycastHit2D[] hits = new RaycastHit2D[8];

        bool grounded;
        bool isJumping;
        bool canAirDash = true;
        bool wasDashing;
        bool facingRight = true;
        int dashDir = 1;
        float coyoteTimer;
        float jumpBufferTimer;
        float dashTimer;
        float dashCooldownTimer;
        float invulnTimer;
        float relativeVelX;
        MovingPlatform currentPlatform;

        static readonly int SpeedHash = Animator.StringToHash("Speed");
        static readonly int GroundedHash = Animator.StringToHash("Grounded");
        static readonly int VelYHash = Animator.StringToHash("VelY");
        static readonly int DashingHash = Animator.StringToHash("Dashing");
        static readonly int HurtingHash = Animator.StringToHash("Hurting");

        public bool IsDashing { get { return dashTimer > 0f; } }
        public bool IsInvulnerable { get { return invulnTimer > 0f; } }
        public bool IsDead { get; private set; }
        public bool ControlsEnabled { get; set; }
        public bool FacingRight { get { return facingRight; } }
        public bool IsGrounded { get { return grounded; } }
        public Vector2 Velocity { get { return rb.GetVelocity(); } }
        public Collider2D BodyCollider { get { return body; } }
        public bool DashReady { get { return dashCooldownTimer <= 0f && (grounded || canAirDash); } }

        /// <summary>Encontra o PlayerController dono de um collider (ou null).</summary>
        public static PlayerController From(Collider2D other)
        {
            if (other == null || other.attachedRigidbody == null) return null;
            return other.attachedRigidbody.GetComponent<PlayerController>();
        }

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            body = GetComponent<CapsuleCollider2D>();
            rb.gravityScale = gravityScale;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
            groundFilter = new ContactFilter2D();
            groundFilter.useTriggers = false;
            groundFilter.useLayerMask = false;
            ControlsEnabled = true;
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (visual == null && spriteRenderer != null) visual = spriteRenderer.transform;
        }

        // ------------------------------------------------------------ input --
        void Update()
        {
            if (Time.timeScale <= 0f) return;
            float dt = Time.deltaTime;
            if (jumpBufferTimer > 0f) jumpBufferTimer -= dt;
            if (invulnTimer > 0f) invulnTimer -= dt;

            if (ControlsEnabled && !IsDead)
            {
                if (InputReader.JumpPressed) jumpBufferTimer = jumpBufferTime;
                if (InputReader.DashPressed) TryDash();

                // pulo variável: soltar o botão corta a subida
                if (isJumping && !InputReader.JumpHeld)
                {
                    var v = rb.GetVelocity();
                    if (v.y > 0f) rb.SetVelocity(new Vector2(v.x, v.y * jumpCutMultiplier));
                    isJumping = false;
                }
            }
            UpdateVisuals(dt);
        }

        // ---------------------------------------------------------- física --
        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            if (dashCooldownTimer > 0f) dashCooldownTimer -= dt;
            CheckGround();

            if (IsDead)
            {
                rb.gravityScale = gravityScale;
                return;
            }

            var v = rb.GetVelocity();

            if (IsDashing)
            {
                dashTimer -= dt;
                rb.gravityScale = 0f;
                rb.SetVelocity(new Vector2(dashDir * dashSpeed, 0f));
                wasDashing = true;
                return;
            }
            if (wasDashing)
            {
                // fim do Pulso: mantém a velocidade de corrida, zera a vertical
                wasDashing = false;
                relativeVelX = dashDir * moveSpeed;
                v = new Vector2(relativeVelX, 0f);
            }

            rb.gravityScale = v.y < 0f ? gravityScale * fallGravityMultiplier : gravityScale;

            float input = ControlsEnabled ? InputReader.Horizontal : 0f;
            float target = input * moveSpeed;
            float rate = Mathf.Abs(target) > 0.01f ? acceleration : deceleration;
            if (!grounded) rate *= airControl;
            relativeVelX = Mathf.MoveTowards(relativeVelX, target, rate * dt);

            Vector2 platformVel = currentPlatform != null ? currentPlatform.Velocity : Vector2.zero;
            v.x = relativeVelX + platformVel.x;
            if (grounded && currentPlatform != null && !isJumping && platformVel.y < 0f)
                v.y = Mathf.Min(v.y, platformVel.y); // acompanha a plataforma que desce

            // pulo (com coyote time e jump buffer)
            if (jumpBufferTimer > 0f && coyoteTimer > 0f && ControlsEnabled)
            {
                relativeVelX += platformVel.x; // herda o embalo da plataforma
                v.y = jumpVelocity;
                jumpBufferTimer = 0f;
                coyoteTimer = 0f;
                isJumping = true;
                grounded = false;
                AudioManager.Play(Sfx.Jump, 0.8f);
                Squash(0.75f, 1.3f);
                SpawnDust();
            }

            if (v.y < -maxFallSpeed) v.y = -maxFallSpeed;
            if (v.y <= 0f) isJumping = false;
            rb.SetVelocity(v);

            if (input > 0f) facingRight = true;
            else if (input < 0f) facingRight = false;
        }

        void CheckGround()
        {
            bool wasGrounded = grounded;
            grounded = false;
            currentPlatform = null;
            var v = rb.GetVelocity();

            if (!(isJumping && v.y > 0.1f))
            {
                int n = body.Cast(Vector2.down, groundFilter, hits, groundCheckDistance);
                for (int i = 0; i < n; i++)
                {
                    var h = hits[i];
                    if (h.collider == null || h.collider.isTrigger) continue;
                    if (h.normal.y < 0.65f) continue;
                    if (h.collider.GetComponent<EnemyPatrol>() != null) continue;
                    grounded = true;
                    currentPlatform = h.collider.GetComponentInParent<MovingPlatform>();
                    break;
                }
            }

            if (grounded)
            {
                coyoteTimer = coyoteTime;
                canAirDash = true;
                if (!wasGrounded && !IsDead) OnLand(v.y);
            }
            else if (coyoteTimer > 0f)
            {
                coyoteTimer -= Time.fixedDeltaTime;
            }
        }

        void OnLand(float impactVelY)
        {
            if (impactVelY < -6f)
            {
                Squash(1.3f, 0.7f);
                SpawnDust();
                AudioManager.Play(Sfx.Land, 0.5f);
            }
        }

        void TryDash()
        {
            if (IsDashing || dashCooldownTimer > 0f) return;
            if (!grounded)
            {
                if (!canAirDash) return;
                canAirDash = false;
            }
            float input = InputReader.Horizontal;
            if (input != 0f) facingRight = input > 0f;
            dashDir = facingRight ? 1 : -1;
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown + dashDuration;
            isJumping = false;
            jumpBufferTimer = 0f;
            Squash(1.35f, 0.7f);
            AudioManager.Play(Sfx.Dash, 0.9f);
            var cfg = GameConfig.Instance;
            if (cfg != null) GameConfig.SpawnFx(cfg.sparkBurstPrefab, transform.position);
        }

        // ------------------------------------------------------------ dano --
        /// <summary>
        /// Tenta causar dano. Durante o Pulso, arcos e Curtos não machucam.
        /// Retorna true se o dano foi aplicado.
        /// </summary>
        public bool TakeHit(Vector2 from, DamageSource source)
        {
            if (IsDead || IsInvulnerable) return false;
            if (IsDashing && (source == DamageSource.Arc || source == DamageSource.Enemy)) return false;
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Playing) return false;
            gm.OnPlayerHurt(from, source);
            return true;
        }

        /// <summary>Chamado pelo GameManager: animação de dano e empurrão.</summary>
        public void BeginHurt(Vector2 from, bool knockback)
        {
            IsDead = true;
            dashTimer = 0f;
            wasDashing = false;
            isJumping = false;
            if (knockback)
            {
                float dir = transform.position.x >= from.x ? 1f : -1f;
                relativeVelX = 0f;
                rb.SetVelocity(new Vector2(dir * hurtKnockback.x, hurtKnockback.y));
            }
            else
            {
                rb.SetVelocity(Vector2.zero);
            }
            var cfg = GameConfig.Instance;
            if (cfg != null) GameConfig.SpawnFx(cfg.sparkBurstPrefab, transform.position);
        }

        public void RespawnAt(Vector3 position)
        {
            transform.position = position;
            rb.position = position;
            rb.SetVelocity(Vector2.zero);
            relativeVelX = 0f;
            dashTimer = 0f;
            wasDashing = false;
            isJumping = false;
            facingRight = true;
            IsDead = false;
            ControlsEnabled = true;
            invulnTimer = invulnerableTime;
            if (visual != null) visual.localScale = Vector3.one;
        }

        /// <summary>Quica depois de pisar num Curto.</summary>
        public void Bounce()
        {
            var v = rb.GetVelocity();
            rb.SetVelocity(new Vector2(v.x, stompBounce));
            isJumping = false;
            canAirDash = true;
            Squash(0.8f, 1.25f);
        }

        /// <summary>Trava o controle (ex.: fim de fase).</summary>
        public void Freeze()
        {
            ControlsEnabled = false;
            dashTimer = 0f;
            relativeVelX = 0f;
            var v = rb.GetVelocity();
            rb.SetVelocity(new Vector2(0f, Mathf.Min(0f, v.y)));
        }

        // ---------------------------------------------------------- visual --
        void Squash(float sx, float sy)
        {
            if (visual != null) visual.localScale = new Vector3(sx, sy, 1f);
        }

        void SpawnDust()
        {
            var cfg = GameConfig.Instance;
            if (cfg != null) GameConfig.SpawnFx(cfg.dustPrefab, transform.position + Vector3.down * feetOffset);
        }

        void UpdateVisuals(float dt)
        {
            if (visual != null)
            {
                visual.localScale = Vector3.Lerp(visual.localScale, Vector3.one, 12f * dt);
                // mantém a base do corpo no chão durante o squash
                visual.localPosition = new Vector3(0f, feetOffset * (visual.localScale.y - 1f), 0f);
            }
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = !facingRight;
                bool blink = IsInvulnerable && !IsDead && Mathf.Repeat(Time.time, 0.16f) < 0.08f;
                spriteRenderer.color = blink ? new Color(1f, 1f, 1f, 0.35f) : Color.white;
            }
            if (animator != null)
            {
                animator.SetFloat(SpeedHash, Mathf.Abs(relativeVelX));
                animator.SetBool(GroundedHash, grounded);
                animator.SetFloat(VelYHash, rb.GetVelocity().y);
                animator.SetBool(DashingHash, IsDashing);
                animator.SetBool(HurtingHash, IsDead);
            }
        }
    }
}
