using UnityEngine;

namespace Faisca
{
    /// <summary>Gatilho invisível que mostra uma dica de tutorial uma única vez.</summary>
    public class HintTrigger : MonoBehaviour
    {
        [TextArea] public string message;
        public float duration = 3.5f;
        bool shown;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (shown || PlayerController.From(other) == null) return;
            shown = true;
            if (GameManager.Instance != null) GameManager.Instance.ShowMessage(message, duration);
        }
    }
}
