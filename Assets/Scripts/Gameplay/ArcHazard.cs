using UnityEngine;

namespace Faisca
{
    /// <summary>
    /// Arco elétrico rítmico: liga e desliga num ciclo fixo. Arcos da fase B
    /// ('!' no mapa) ficam ligados quando os da fase A ('~') estão desligados.
    /// Pisca um pouco antes de ligar para avisar o jogador.
    /// </summary>
    public class ArcHazard : MonoBehaviour
    {
        public bool phaseB;
        public float period = 2.4f;
        [Range(0.1f, 0.9f)] public float onFraction = 0.5f;
        public float warningTime = 0.35f;

        [Header("Referências")]
        public Collider2D damageCollider;
        public Animator animator;
        public SpriteRenderer spriteRenderer;

        bool active;
        static float lastZapTime;
        static readonly int ActiveHash = Animator.StringToHash("Active");

        void Start()
        {
            active = !IsOnNow(); // força a atualização no primeiro Update
        }

        float CycleTime()
        {
            return Mathf.Repeat(Time.timeSinceLevelLoad + (phaseB ? period * 0.5f : 0f), period);
        }

        bool IsOnNow()
        {
            return CycleTime() < period * onFraction;
        }

        void Update()
        {
            float t = CycleTime();
            bool on = t < period * onFraction;
            if (on != active)
            {
                active = on;
                if (damageCollider != null) damageCollider.enabled = on;
                if (animator != null) animator.SetBool(ActiveHash, on);
                if (on) TryPlayZap();
            }

            if (spriteRenderer != null)
            {
                // aviso: pisca nos últimos instantes antes de ligar
                bool warning = !on && t > period - warningTime;
                float a = warning ? (Mathf.Repeat(Time.time, 0.1f) < 0.05f ? 1f : 0.3f) : 1f;
                spriteRenderer.color = new Color(1f, 1f, 1f, a);
            }
        }

        void TryPlayZap()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Player == null) return;
            if (Vector2.Distance(gm.Player.transform.position, transform.position) > 12f) return;
            if (Time.time - lastZapTime < 0.25f) return;
            lastZapTime = Time.time;
            AudioManager.Play(Sfx.Arc, 0.35f, 0.1f);
        }

        void OnTriggerStay2D(Collider2D other)
        {
            if (!active) return;
            var player = PlayerController.From(other);
            if (player != null) player.TakeHit(transform.position, DamageSource.Arc);
        }
    }
}
