using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Faisca
{
    /// <summary>
    /// Interface durante o jogo: cargas, células, fase, tempo, Pulso,
    /// mensagens e os painéis de pausa, derrota e fase concluída.
    /// Só escuta eventos do GameManager — não altera regras do jogo.
    /// </summary>
    public class HUD : MonoBehaviour
    {
        [Header("Barra superior")]
        public Text livesText;
        public Text cellsText;
        public Text levelText;
        public Text timerText;
        public Image dashIcon;

        [Header("Mensagens")]
        public Text messageText;
        public CanvasGroup messageGroup;

        [Header("Painéis")]
        public GameObject pausePanel;
        public GameObject gameOverPanel;
        public GameObject levelCompletePanel;
        public Text gameOverStats;
        public Text levelCompleteStats;

        [Header("Botões")]
        public Button resumeButton;
        public Button pauseMenuButton;
        public Button retryButton;
        public Button gameOverMenuButton;
        public Button continueButton;

        static readonly Color GoalColor = new Color(0.48f, 0.83f, 0.54f);
        static readonly Color NormalColor = new Color(1f, 0.95f, 0.64f);

        GameManager gm;
        float messageTimer;
        float cellsPunch;
        float panelOpenedAt;

        void Awake()
        {
            if (resumeButton != null) resumeButton.onClick.AddListener(() => gm.Resume());
            if (pauseMenuButton != null) pauseMenuButton.onClick.AddListener(() => gm.QuitToMenu());
            if (retryButton != null) retryButton.onClick.AddListener(() => gm.RestartLevel());
            if (gameOverMenuButton != null) gameOverMenuButton.onClick.AddListener(() => gm.QuitToMenu());
            if (continueButton != null) continueButton.onClick.AddListener(() => gm.ContinueAfterLevel());
        }

        void OnEnable()
        {
            gm = GameManager.Instance;
            if (gm == null) return;
            gm.CellsChanged += OnCellsChanged;
            gm.LivesChanged += OnLivesChanged;
            gm.MessageRequested += OnMessage;
            gm.StateChanged += OnStateChanged;
        }

        void OnDisable()
        {
            if (gm == null) return;
            gm.CellsChanged -= OnCellsChanged;
            gm.LivesChanged -= OnLivesChanged;
            gm.MessageRequested -= OnMessage;
            gm.StateChanged -= OnStateChanged;
        }

        void Start()
        {
            if (gm == null) return;
            if (levelText != null) levelText.text = "Fase " + gm.LevelNumber + " · " + gm.Level.name;
            OnStateChanged(gm.State);
            if (messageGroup != null && messageTimer <= 0f) messageGroup.alpha = 0f;
        }

        void Update()
        {
            if (gm == null) return;
            if (timerText != null) timerText.text = GameSession.FormatTime(gm.LevelTime);
            if (dashIcon != null && gm.Player != null)
                dashIcon.color = new Color(1f, 1f, 1f, gm.Player.DashReady ? 1f : 0.25f);

            // mensagem com fade-out (tempo real, funciona com o jogo pausado)
            if (messageTimer > 0f)
            {
                messageTimer -= Time.unscaledDeltaTime;
                if (messageGroup != null) messageGroup.alpha = Mathf.Clamp01(messageTimer / 0.4f);
            }

            // "soco" de escala no contador de células
            if (cellsText != null)
            {
                cellsPunch = Mathf.MoveTowards(cellsPunch, 0f, Time.unscaledDeltaTime * 3f);
                cellsText.transform.localScale = Vector3.one * (1f + cellsPunch * 0.4f);
            }

            // atalho de teclado para continuar após concluir a fase
            if (gm.State == GameState.LevelComplete && Time.unscaledTime - panelOpenedAt > 1f && InputReader.SubmitPressed)
                gm.ContinueAfterLevel();
        }

        void OnCellsChanged(int have, int need)
        {
            if (cellsText == null) return;
            cellsText.text = have + "/" + need;
            cellsText.color = have >= need ? GoalColor : NormalColor;
            if (have > 0) cellsPunch = 1f;
        }

        void OnLivesChanged(int lives)
        {
            if (livesText != null) livesText.text = "x" + Mathf.Max(0, lives);
        }

        void OnMessage(string text, float duration)
        {
            if (messageText != null) messageText.text = text;
            messageTimer = duration;
            if (messageGroup != null) messageGroup.alpha = 1f;
        }

        void OnStateChanged(GameState state)
        {
            SetActive(pausePanel, state == GameState.Paused);
            SetActive(gameOverPanel, state == GameState.GameOver);
            SetActive(levelCompletePanel, state == GameState.LevelComplete);

            switch (state)
            {
                case GameState.Paused:
                    Select(resumeButton);
                    break;
                case GameState.GameOver:
                    if (gameOverStats != null)
                        gameOverStats.text = "As cargas acabaram.\nCélulas nesta tentativa: " + gm.CellsCollected + "/" + gm.CellsRequired;
                    Select(retryButton);
                    break;
                case GameState.LevelComplete:
                    panelOpenedAt = Time.unscaledTime;
                    if (levelCompleteStats != null)
                        levelCompleteStats.text =
                            "Células: " + gm.CellsCollected + "/" + gm.CellsRequired + "\n" +
                            "Tempo: " + GameSession.FormatTime(gm.LevelTime) + "\n" +
                            "Cargas restantes: " + GameSession.Lives;
                    Select(continueButton);
                    break;
            }
        }

        static void SetActive(GameObject go, bool on)
        {
            if (go != null && go.activeSelf != on) go.SetActive(on);
        }

        static void Select(Button b)
        {
            if (b != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(b.gameObject);
        }
    }
}
