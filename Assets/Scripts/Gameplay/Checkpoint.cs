using UnityEngine;

namespace Faisca
{
    /// <summary>Poste de checkpoint: acende ao toque e vira o ponto de retorno.</summary>
    public class Checkpoint : MonoBehaviour
    {
        public Animator animator;
        public Vector3 respawnOffset = new Vector3(0f, 0.1f, 0f);

        bool active;
        static readonly int ActiveHash = Animator.StringToHash("Active");

        void OnTriggerEnter2D(Collider2D other)
        {
            if (active || PlayerController.From(other) == null) return;
            active = true;
            if (animator != null) animator.SetBool(ActiveHash, true);
            AudioManager.Play(Sfx.Checkpoint, 0.9f, 0f);
            var cfg = GameConfig.Instance;
            if (cfg != null) GameConfig.SpawnFx(cfg.sparkBurstPrefab, transform.position + Vector3.up * 0.4f);
            if (GameManager.Instance != null) GameManager.Instance.SetCheckpoint(transform.position + respawnOffset);
        }
    }
}
