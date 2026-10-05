using UnityEngine;

namespace Faisca
{
    /// <summary>
    /// Câmera que segue o alvo com suavização, olha um pouco à frente na
    /// direção do movimento, respeita os limites da fase e treme (screen shake).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public Vector2 offset = new Vector2(0f, 1.5f);
        public float smoothTime = 0.15f;
        public float lookAhead = 2f;
        public float lookAheadSmoothTime = 0.5f;

        Camera cam;
        Rect bounds;
        bool hasBounds;
        Vector3 basePosition;
        Vector3 velocity;
        float look;
        float lookVelocity;
        float shakeTime;
        float shakeDuration;
        float shakeMagnitude;
        PlayerController player;
        bool snapped;

        void Awake()
        {
            if (cam == null) cam = GetComponent<Camera>();
            if (!snapped) basePosition = transform.position;
        }

        public void SetTarget(Transform t)
        {
            target = t;
            player = t != null ? t.GetComponent<PlayerController>() : null;
        }

        public void SetBounds(Rect r)
        {
            bounds = r;
            hasBounds = true;
        }

        public void SnapToTarget()
        {
            if (target == null) return;
            look = player != null && !player.FacingRight ? -lookAhead : lookAhead;
            if (cam == null) cam = GetComponent<Camera>();
            basePosition = Clamp(Desired());
            velocity = Vector3.zero;
            snapped = true;
            transform.position = basePosition;
        }

        public void Shake(float magnitude, float duration)
        {
            if (magnitude < shakeMagnitude && shakeTime > 0f) return;
            shakeMagnitude = magnitude;
            shakeDuration = duration;
            shakeTime = duration;
        }

        Vector3 Desired()
        {
            return new Vector3(target.position.x + offset.x + look, target.position.y + offset.y, transform.position.z);
        }

        void LateUpdate()
        {
            if (target == null) return;
            float desiredLook = player != null ? (player.FacingRight ? lookAhead : -lookAhead) : 0f;
            look = Mathf.SmoothDamp(look, desiredLook, ref lookVelocity, lookAheadSmoothTime);
            basePosition = Clamp(Vector3.SmoothDamp(basePosition, Desired(), ref velocity, smoothTime));

            Vector3 shake = Vector3.zero;
            if (shakeTime > 0f)
            {
                shakeTime -= Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(shakeTime / shakeDuration);
                Vector2 r = Random.insideUnitCircle * shakeMagnitude * k;
                shake = new Vector3(r.x, r.y, 0f);
                if (shakeTime <= 0f) shakeMagnitude = 0f;
            }
            transform.position = basePosition + shake;
        }

        Vector3 Clamp(Vector3 p)
        {
            if (!hasBounds || cam == null) return p;
            float h = cam.orthographicSize;
            float w = h * cam.aspect;
            p.x = bounds.width <= 2f * w ? bounds.center.x : Mathf.Clamp(p.x, bounds.xMin + w, bounds.xMax - w);
            p.y = bounds.height <= 2f * h ? bounds.yMin + h : Mathf.Clamp(p.y, bounds.yMin + h, bounds.yMax - h);
            return p;
        }
    }
}
