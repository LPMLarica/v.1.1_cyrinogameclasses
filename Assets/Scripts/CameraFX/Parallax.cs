using UnityEngine;

namespace Faisca
{
    /// <summary>
    /// Camada de fundo com parallax: acompanha a câmera numa fração do
    /// movimento (fator 1 = parece infinitamente longe; 0 = parado no mundo).
    /// Também pode rolar sozinha (usado no menu).
    /// Roda depois da câmera para não tremer.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class Parallax : MonoBehaviour
    {
        public Transform cameraTransform;
        [Range(0f, 1f)] public float factorX = 0.5f;
        [Range(0f, 1f)] public float factorY = 0.3f;
        [Tooltip("Posição inicial relativa à câmera")]
        public Vector2 offsetFromCamera;
        [Tooltip("Rolagem automática em unidades/segundo (0 = desligada)")]
        public float autoScroll;
        [Tooltip("Largura de um ciclo da imagem (para a rolagem automática dar a volta)")]
        public float wrapWidth = 20f;

        Vector3 cameraStart;
        Vector3 layerStart;
        float scrolled;

        void Start()
        {
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
            if (cameraTransform == null) return;
            cameraStart = cameraTransform.position;
            layerStart = new Vector3(cameraStart.x + offsetFromCamera.x, cameraStart.y + offsetFromCamera.y, transform.position.z);
            transform.position = layerStart;
        }

        void LateUpdate()
        {
            if (cameraTransform == null) return;
            if (autoScroll != 0f) scrolled = Mathf.Repeat(scrolled + autoScroll * Time.deltaTime, wrapWidth);
            Vector3 d = cameraTransform.position - cameraStart;
            transform.position = new Vector3(
                layerStart.x + d.x * factorX - scrolled,
                layerStart.y + d.y * factorY,
                layerStart.z);
        }
    }
}
