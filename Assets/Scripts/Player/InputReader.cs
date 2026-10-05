using UnityEngine;

namespace Faisca
{
    /// <summary>Ponto único de leitura de entrada (Input Manager clássico). Centralizar aqui facilita trocar o sistema de input no futuro e mapear teclado + controle ao mesmo tempo.</summary>
    public static class InputReader
    {
        const float DeadZone = 0.35f;

        /// <summary>-1, 0 ou 1 (movimento digital, típico de plataforma).</summary>
        public static float Horizontal
        {
            get
            {
                float h = Input.GetAxisRaw("Horizontal");
                if (h > DeadZone) return 1f;
                if (h < -DeadZone) return -1f;
                return 0f;
            }
        }

        public static bool JumpPressed
        {
            get
            {
                return Input.GetButtonDown("Jump")
                    || Input.GetKeyDown(KeyCode.W)
                    || Input.GetKeyDown(KeyCode.UpArrow)
                    || Input.GetKeyDown(KeyCode.Z)
                    || Input.GetKeyDown(KeyCode.JoystickButton0);
            }
        }

        public static bool JumpHeld
        {
            get
            {
                return Input.GetButton("Jump")
                    || Input.GetKey(KeyCode.W)
                    || Input.GetKey(KeyCode.UpArrow)
                    || Input.GetKey(KeyCode.Z)
                    || Input.GetKey(KeyCode.JoystickButton0);
            }
        }

        public static bool DashPressed
        {
            get
            {
                return Input.GetKeyDown(KeyCode.LeftShift)
                    || Input.GetKeyDown(KeyCode.RightShift)
                    || Input.GetKeyDown(KeyCode.X)
                    || Input.GetKeyDown(KeyCode.J)
                    || Input.GetKeyDown(KeyCode.K)
                    || Input.GetKeyDown(KeyCode.JoystickButton2)
                    || Input.GetKeyDown(KeyCode.JoystickButton5);
            }
        }

        public static bool PausePressed
        {
            get
            {
                return Input.GetKeyDown(KeyCode.Escape)
                    || Input.GetKeyDown(KeyCode.P)
                    || Input.GetKeyDown(KeyCode.JoystickButton7);
            }
        }

        public static bool SubmitPressed
        {
            get
            {
                return Input.GetKeyDown(KeyCode.Return)
                    || Input.GetKeyDown(KeyCode.KeypadEnter)
                    || Input.GetKeyDown(KeyCode.Space)
                    || Input.GetKeyDown(KeyCode.JoystickButton0);
            }
        }
    }
}
