using UnityEngine;

namespace Faisca
{
    /// <summary>
    /// Curto: anda de um lado para o outro e vira ao encontrar parede ou borda.
    /// Usa dois colisores no mesmo objeto: um sólido (corpo, para andar no chão)
    /// e um trigger (hitbox, para detectar a Faísca).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyPatrol : MonoBehaviour
    {
        public float speed = 2f;
        public bool startFacingRight;
        [Tooltip("Margem para considerar que a Faísca pisou por cima")]
        public float stompTolerance = 0.25f;

        [Header("Referências")]
        public Collider2D bodyCollider;
        public Collider2D hitbox;
        public SpriteRenderer spriteRenderer;
        public Animator animator;

        Rigidbody2D rb;
        ContactFilter2D filter;
        readonly RaycastHit2D[] hits = new RaycastHit2D[6];
        int dir;
        float flipCooldown;
        bool dead;

        static readonly int DieHash = Animator.StringToHash("Die");

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.freezeRotation = true;
            filter = new ContactFilter2D();
            filter.useTriggers = false;
            filter.useLayerMask = false;
            dir = startFacingRight ? 1 : -1;
        }

        void Start()
        {
            // o corpo do Curto não empurra a Faísca: o contato é tratado pela hitbox
            var gm = GameManager.Instance;
            if (gm != null && gm.Player != null && bodyCollider != null)
                Physics2D.IgnoreCollision(bodyCollider, gm.Player.BodyCollider, true);
        }

        void FixedUpdate()
        {
            if (dead) return;
            if (flipCooldown > 0f) flipCooldown -= Time.fixedDeltaTime;

            Bounds b = bodyCollider.bounds;
            float frontX = dir > 0 ? b.max.x + 0.02f : b.min.x - 0.02f;
            bool wall = Probe(new Vector2(frontX, b.center.y), new Vector2(dir, 0f), 0.08f);
            bool groundAhead = Probe(new Vector2(frontX + dir * 0.05f, b.min.y + 0.05f), Vector2.down, 0.35f);
            bool grounded = Probe(new Vector2(b.center.x, b.min.y + 0.05f), Vector2.down, 0.15f);

            if (grounded && flipCooldown <= 0f && (wall || !groundAhead))
            {
                dir = -dir;
                flipCooldown = 0.2f;
            }

            var v = rb.GetVelocity();
            rb.SetVelocity(new Vector2(grounded ? dir * speed : v.x, v.y));
            if (spriteRenderer != null) spriteRenderer.flipX = dir < 0; // o sprite olha para a direita
        }

        bool Probe(Vector2 origin, Vector2 direction, float distance)
        {
            int n = Physics2D.Raycast(origin, direction, filter, hits, distance);
            for (int i = 0; i < n; i++)
            {
                var c = hits[i].collider;
                if (c == null || c == bodyCollider || c == hitbox) continue;
                if (PlayerController.From(c) != null) continue;
                return true;
            }
            return false;
        }

        void OnTriggerStay2D(Collider2D other)
        {
            if (dead) return;
            var player = PlayerController.From(other);
            if (player == null || player.IsDead) return;

            if (player.IsDashing)
            {
                Die();
                return;
            }

            bool fromAbove = other.bounds.min.y > bodyCollider.bounds.max.y - stompTolerance;
            if (fromAbove && player.Velocity.y <= 0.5f)
            {
                Die();
                player.Bounce();
                return;
            }
            player.TakeHit(transform.position, DamageSource.Enemy);
        }

        void Die()
        {
            dead = true;
            foreach (var c in GetComponents<Collider2D>()) c.enabled = false;
            rb.simulated = false;
            if (animator != null) animator.SetTrigger(DieHash);
            AudioManager.Play(Sfx.Stomp);
            var cfg = GameConfig.Instance;
            if (cfg != null) GameConfig.SpawnFx(cfg.sparkBurstPrefab, transform.position);
            var gm = GameManager.Instance;
            if (gm != null && gm.cameraFollow != null) gm.cameraFollow.Shake(0.12f, 0.15f);
            Destroy(gameObject, 0.6f);
        }
    }
}
