using UnityEngine;

namespace Faisca
{
    /// <summary>Perigo estático (sucata pontiaguda): causa dano ao toque.</summary>
    public class Hazard : MonoBehaviour
    {
        public DamageSource source = DamageSource.Spikes;

        void OnTriggerStay2D(Collider2D other)
        {
            var player = PlayerController.From(other);
            if (player != null) player.TakeHit(transform.position, source);
        }
    }
}
