using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Faisca
{
    /// <summary>Menu principal: jogar, como jogar, opções de volume, créditos e sair.</summary>
    public class MainMenu : MonoBehaviour
    {
        [Header("Painéis")]
        public GameObject mainPanel;
        public GameObject howToPanel;
        public GameObject optionsPanel;
        public GameObject creditsPanel;

        [Header("Botões")]
        public Button playButton;
        public Button howToButton;
        public Button optionsButton;
        public Button creditsButton;
        public Button quitButton;
        public Button[] backButtons;

        [Header("Opções")]
        public Slider musicSlider;
        public Slider sfxSlider;

        [Header("Textos")]
        public Text bestTimeText;

        void Awake()
        {
            playButton.onClick.AddListener(Play);
            howToButton.onClick.AddListener(() => Show(howToPanel));
            optionsButton.onClick.AddListener(() => Show(optionsPanel));
            creditsButton.onClick.AddListener(() => Show(creditsPanel));
            quitButton.onClick.AddListener(Quit);
            foreach (var b in backButtons) b.onClick.AddListener(() => Show(mainPanel));
        }

        void Start()
        {
            Time.timeScale = 1f;
            GameSession.InProgress = false;
            var audio = AudioManager.Instance;
            audio.PlayAmbience(false);
            audio.SetDuck(false);
            audio.PlayMusic(Music.Menu);

            musicSlider.value = audio.MusicVolume;
            sfxSlider.value = audio.SfxVolume;
            musicSlider.onValueChanged.AddListener(v => AudioManager.Instance.SetMusicVolume(v));
            sfxSlider.onValueChanged.AddListener(v =>
            {
                AudioManager.Instance.SetSfxVolume(v);
                AudioManager.Play(Sfx.UiHover, 1f, 0f);
            });

            float best = GameSession.BestTime;
            bestTimeText.text = best > 0f ? "Melhor tempo: " + GameSession.FormatTime(best) : "Melhor tempo: --:--";
            Show(mainPanel);
        }

        void Update()
        {
            if (!mainPanel.activeSelf && InputReader.PausePressed) Show(mainPanel);
        }

        void Show(GameObject panel)
        {
            mainPanel.SetActive(panel == mainPanel);
            howToPanel.SetActive(panel == howToPanel);
            optionsPanel.SetActive(panel == optionsPanel);
            creditsPanel.SetActive(panel == creditsPanel);

            GameObject first = playButton.gameObject;
            if (panel == optionsPanel) first = musicSlider.gameObject;
            else if (panel != mainPanel)
            {
                foreach (var b in backButtons)
                    if (b.transform.IsChildOf(panel.transform)) first = b.gameObject;
            }
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(first);
        }

        void Play()
        {
            GameSession.NewGame();
            SceneLoader.Load(SceneLoader.GameScene);
        }

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
