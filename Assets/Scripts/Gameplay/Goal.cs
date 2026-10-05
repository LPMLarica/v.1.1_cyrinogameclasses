using UnityEngine;

namespace Faisca
{
    /// <summary>
    /// Transformador: objetivo da fase. Fica piscando quando a meta de células
    /// foi atingida; ao ser tocado nessa condição, conclui a fase.
    /// </summary>
    public class Goal : MonoBehaviour
    {
        public Animator animator;

        bool done;
        float deniedCooldown;
        static readonly int ReadyHash = Animator.StringToHash("Ready");
        static readonly int ActivateHash = Animator.StringToHash("Activate");

        void Start()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            gm.CellsChanged += OnCellsChanged;
            OnCellsChanged(gm.CellsCollected, gm.CellsRequired);
        }

        void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (gm != null) gm.CellsChanged -= OnCellsChanged;
        }

        void OnCellsChanged(int have, int need)
        {
            if (animator != null) animator.SetBool(ReadyHash, have >= need);
        }

        void OnTriggerStay2D(Collider2D other)
        {
            if (done) return;
            var player = PlayerController.From(other);
            var gm = GameManager.Instance;
            if (player == null || player.IsDead || gm == null || gm.State != GameState.Playing) return;

            if (gm.CanCompleteLevel)
            {
                done = true;
                if (animator != null) animator.SetTrigger(ActivateHash);
                var cfg = GameConfig.Instance;
                if (cfg != null) GameConfig.SpawnFx(cfg.sparkBurstPrefab, transform.position + Vector3.up * 0.8f);
                gm.CompleteLevel();
            }
            else if (Time.time > deniedCooldown)
            {
                deniedCooldown = Time.time + 2f;
                int missing = gm.CellsRequired - gm.CellsCollected;
                AudioManager.Play(Sfx.Denied, 0.8f, 0f);
                gm.ShowMessage("Faltam " + missing + (missing == 1 ? " célula" : " células") + " para religar!", 2f);
            }
        }
    }
}
