using UnityEngine;

namespace Faisca
{
    /// <summary>Célula de Energia: flutua e é coletada ao toque.</summary>
    public class Collectible : MonoBehaviour
    {
        public Transform visual;
        public float bobAmplitude = 0.08f;
        public float bobSpeed = 3f;

        bool taken;
        float phase;

        void Start()
        {
            phase = transform.position.x * 0.37f;
        }

        void Update()
        {
            if (visual != null)
                visual.localPosition = Vector3.up * Mathf.Sin((Time.time + phase) * bobSpeed) * bobAmplitude;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (taken) return;
            var player = PlayerController.From(other);
            if (player == null || player.IsDead) return;
            taken = true;
            AudioManager.Play(Sfx.Collect, 0.9f, 0.02f);
            var cfg = GameConfig.Instance;
            if (cfg != null) GameConfig.SpawnFx(cfg.sparkBurstPrefab, transform.position);
            if (GameManager.Instance != null) GameManager.Instance.CollectCell();
            Destroy(gameObject);
        }
    }
}
