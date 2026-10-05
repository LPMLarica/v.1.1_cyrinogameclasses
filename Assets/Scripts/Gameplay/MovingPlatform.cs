using UnityEngine;

namespace Faisca
{
    /// <summary>
    /// Plataforma que vai e volta entre dois pontos. Usa Rigidbody2D cinemático
    /// (MovePosition) e expõe a própria velocidade para o jogador "pegar carona".
    /// Roda antes do PlayerController para a velocidade já estar atualizada.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(Rigidbody2D))]
    public class MovingPlatform : MonoBehaviour
    {
        public Vector2 pointA;
        public Vector2 pointB;
        public float speed = 2.5f;
        public float waitTime = 0.5f;

        public Vector2 Velocity { get; private set; }

        Rigidbody2D rb;
        Vector2 target;
        float wait;
        bool configured;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        public void Setup(Vector2 a, Vector2 b)
        {
            pointA = a;
            pointB = b;
            transform.position = a;
            if (rb != null) rb.position = a;
            target = b;
            configured = true;
        }

        void Start()
        {
            if (!configured) Setup(transform.position, pointB);
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            if (wait > 0f)
            {
                wait -= dt;
                Velocity = Vector2.zero;
                return;
            }
            Vector2 pos = rb.position;
            Vector2 next = Vector2.MoveTowards(pos, target, speed * dt);
            Velocity = (next - pos) / dt;
            rb.MovePosition(next);
            if ((next - target).sqrMagnitude < 0.0001f)
            {
                target = target == pointB ? pointA : pointB;
                wait = waitTime;
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(pointA, pointB);
        }
    }
}
