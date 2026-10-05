using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Faisca
{
    /// <summary>
    /// Troca de cenas com fade para preto. Também expõe FadeOut/FadeIn para
    /// outras transições (ex.: respawn). Singleton persistente criado sob demanda.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        public const string MenuScene = "MainMenu";
        public const string GameScene = "Game";
        public const string EndingScene = "Ending";

        static SceneLoader instance;
        CanvasGroup group;
        bool loading;

        public static SceneLoader Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("SceneLoader");
                    DontDestroyOnLoad(go);
                    instance = go.AddComponent<SceneLoader>();
                    instance.Build();
                }
                return instance;
            }
        }

        void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            gameObject.AddComponent<GraphicRaycaster>();
            group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;

            var img = new GameObject("Fade", typeof(RectTransform), typeof(Image));
            img.transform.SetParent(transform, false);
            var rt = (RectTransform)img.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            img.GetComponent<Image>().color = new Color(0.04f, 0.03f, 0.1f, 1f);
        }

        /// <summary>Carrega uma cena com fade.</summary>
        public static void Load(string sceneName)
        {
            Instance.StartCoroutine(Instance.LoadRoutine(sceneName));
        }

        IEnumerator LoadRoutine(string sceneName)
        {
            if (loading) yield break;
            loading = true;
            yield return FadeOut(0.35f);
            Time.timeScale = 1f;
            var op = SceneManager.LoadSceneAsync(sceneName);
            while (!op.isDone) yield return null;
            yield return FadeIn(0.35f);
            loading = false;
        }

        public IEnumerator FadeOut(float duration)
        {
            group.blocksRaycasts = true;
            yield return Fade(group.alpha, 1f, duration);
        }

        public IEnumerator FadeIn(float duration)
        {
            yield return Fade(group.alpha, 0f, duration);
            group.blocksRaycasts = false;
        }

        IEnumerator Fade(float from, float to, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
                yield return null;
            }
            group.alpha = to;
        }
    }
}
