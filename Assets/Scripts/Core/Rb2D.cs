using UnityEngine;

namespace Faisca
{
    /// <summary>
    /// Compatibilidade entre versões: na Unity 6 Rigidbody2D.velocity passou a se
    /// chamar linearVelocity. Usar estes métodos mantém o projeto compilando
    /// tanto na 2022.3 LTS quanto na Unity 6.
    /// </summary>
    public static class Rb2D
    {
        public static Vector2 GetVelocity(this Rigidbody2D rb)
        {
#if UNITY_6000_0_OR_NEWER
            return rb.linearVelocity;
#else
            return rb.velocity;
#endif
        }

        public static void SetVelocity(this Rigidbody2D rb, Vector2 v)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = v;
#else
            rb.velocity = v;
#endif
        }
    }
}
