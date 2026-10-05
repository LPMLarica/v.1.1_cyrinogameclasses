using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Faisca
{
    /// <summary>Sons de foco e clique para botões (mouse, teclado e controle).</summary>
    public class UIButtonSounds : MonoBehaviour, IPointerEnterHandler, ISelectHandler
    {
        static float lastHover;

        void Awake()
        {
            var button = GetComponent<Button>();
            if (button != null) button.onClick.AddListener(() => AudioManager.Play(Sfx.UiClick, 1f, 0f));
        }

        public void OnPointerEnter(PointerEventData eventData) { Hover(); }
        public void OnSelect(BaseEventData eventData) { Hover(); }

        static void Hover()
        {
            if (Time.unscaledTime - lastHover < 0.06f || Time.timeSinceLevelLoad < 0.3f) return;
            lastHover = Time.unscaledTime;
            AudioManager.Play(Sfx.UiHover, 0.6f, 0f);
        }
    }
}
