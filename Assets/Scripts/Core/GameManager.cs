using System;
using System.Collections;
using UnityEngine;

namespace Faisca
{
    public enum GameState { Playing, Paused, Respawning, LevelComplete, GameOver }

    /// <summary>
    /// Regras da fase: carrega o mapa, conta células, controla vidas,
    /// checkpoint, pausa, vitória e derrota. A UI escuta os eventos C#
    /// abaixo (padrão Observer) em vez de ser chamada diretamente.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Referências da cena")]
        public LevelLoader levelLoader;
        public CameraFollow cameraFollow;

        [Header("Ajustes")]
        public float hurtDelay = 0.6f;
        public float respawnFadeTime = 0.25f;
        public float killY = -3f;

        public PlayerController Player { get; private set; }
        public LevelData Level { get; private set; }
        public GameState State { get; private set; }
        public int LevelNumber { get; private set; }
        public int CellsCollected { get; private set; }
        public float LevelTime { get; private set; }
        public bool IsLastLevel { get; private set; }

        public int CellsRequired { get { return Level != null ? Level.requiredCells : 0; } }
        public bool CanCompleteLevel { get { return CellsCollected >= CellsRequired; } }

        // eventos observados pela HUD e por objetos da fase
        public event Action<int, int> CellsChanged;   // (coletadas, meta)
        public event Action<int> LivesChanged;        // vidas restantes
        public event Action<string, float> MessageRequested; // (texto, duração)
        public event Action<GameState> StateChanged;

        Vector3 checkpointPosition;

        void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
            if (!GameSession.InProgress) GameSession.NewGame(); // permite dar Play direto na cena Game

            var cfg = GameConfig.Instance;
            int index = Mathf.Clamp(GameSession.LevelIndex, 0, cfg.levels.Length - 1);
            LevelNumber = index + 1;
            IsLastLevel = index == cfg.levels.Length - 1;
            Level = LevelData.Parse(cfg.levels[index].text);

            if (levelLoader == null) levelLoader = GetComponent<LevelLoader>();
            Player = levelLoader.Build(Level, cfg);
            checkpointPosition = Player.transform.position;

            if (cameraFollow != null)
            {
                cameraFollow.SetTarget(Player.transform);
                cameraFollow.SetBounds(new Rect(0f, 0f, Level.width, Level.height));
                cameraFollow.SnapToTarget();
            }
            State = GameState.Playing;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            if (CellsChanged != null) CellsChanged(CellsCollected, CellsRequired);
            if (LivesChanged != null) LivesChanged(GameSession.Lives);
            if (StateChanged != null) StateChanged(State);
            AudioManager.Instance.PlayMusic(Music.Game);
            AudioManager.Instance.PlayAmbience(true);
            AudioManager.Instance.SetDuck(false);
            ShowMessage("Fase " + LevelNumber + " — " + Level.name, 2.5f);
        }

        void Update()
        {
            switch (State)
            {
                case GameState.Playing:
                    LevelTime += Time.deltaTime;
                    if (InputReader.PausePressed) Pause();
                    else if (Player != null && Player.transform.position.y < killY) OnPlayerHurt(Player.transform.position, DamageSource.Fall);
                    break;
                case GameState.Paused:
                    if (InputReader.PausePressed) Resume();
                    break;
            }
        }

        void SetState(GameState s)
        {
            State = s;
            if (StateChanged != null) StateChanged(s);
        }

        // ------------------------------------------------------- mensagens --
        public void ShowMessage(string text, float duration)
        {
            if (MessageRequested != null) MessageRequested(text, duration);
        }

        // ---------------------------------------------------------- células --
        public void CollectCell()
        {
            CellsCollected++;
            if (CellsChanged != null) CellsChanged(CellsCollected, CellsRequired);
            if (CellsCollected == CellsRequired)
                ShowMessage("Meta atingida! Leve a energia ao transformador", 2.5f);
        }

        public void SetCheckpoint(Vector3 position)
        {
            checkpointPosition = position;
            ShowMessage("Checkpoint!", 1.2f);
        }

        // ------------------------------------------------------ dano/vidas --
        public void OnPlayerHurt(Vector2 from, DamageSource source)
        {
            if (State != GameState.Playing) return;
            StartCoroutine(HurtRoutine(from, source));
        }

        IEnumerator HurtRoutine(Vector2 from, DamageSource source)
        {
            SetState(GameState.Respawning);
            GameSession.Lives--;
            GameSession.Deaths++;
            if (LivesChanged != null) LivesChanged(GameSession.Lives);

            AudioManager.Play(Sfx.Hurt);
            if (cameraFollow != null) cameraFollow.Shake(0.3f, 0.35f);
            Player.BeginHurt(from, source != DamageSource.Fall);

            yield return new WaitForSeconds(hurtDelay);

            if (GameSession.Lives <= 0)
            {
                GameOver();
                yield break;
            }

            yield return SceneLoader.Instance.FadeOut(respawnFadeTime);
            Player.RespawnAt(checkpointPosition);
            if (cameraFollow != null) cameraFollow.SnapToTarget();
            yield return SceneLoader.Instance.FadeIn(respawnFadeTime);
            SetState(GameState.Playing);
        }

        void GameOver()
        {
            SetState(GameState.GameOver);
            AudioManager.Instance.StopMusic(0.3f);
            AudioManager.Instance.PlayAmbience(false);
            AudioManager.Play(Sfx.GameOver, 1f, 0f);
        }

        // --------------------------------------------------------- vitória --
        public void CompleteLevel()
        {
            if (State != GameState.Playing) return;
            SetState(GameState.LevelComplete);
            Player.Freeze();
            GameSession.TotalCells += CellsCollected;
            GameSession.TotalTime += LevelTime;
            AudioManager.Instance.StopMusic(0.3f);
            AudioManager.Play(Sfx.Goal, 1f, 0f);
            StartCoroutine(PlayLater(Sfx.LevelComplete, 1.1f));
            if (cameraFollow != null) cameraFollow.Shake(0.15f, 0.4f);
        }

        IEnumerator PlayLater(Sfx sfx, float delay)
        {
            yield return new WaitForSeconds(delay);
            AudioManager.Play(sfx, 1f, 0f);
        }

        /// <summary>Botão "Continuar" do painel de fase concluída.</summary>
        public void ContinueAfterLevel()
        {
            if (State != GameState.LevelComplete) return;
            if (IsLastLevel)
            {
                SceneLoader.Load(SceneLoader.EndingScene);
            }
            else
            {
                GameSession.LevelIndex++;
                SceneLoader.Load(SceneLoader.GameScene);
            }
        }

        // ---------------------------------------------------------- pausa --
        public void Pause()
        {
            if (State != GameState.Playing) return;
            Time.timeScale = 0f;
            AudioManager.Instance.SetDuck(true);
            SetState(GameState.Paused);
        }

        public void Resume()
        {
            if (State != GameState.Paused) return;
            Time.timeScale = 1f;
            AudioManager.Instance.SetDuck(false);
            SetState(GameState.Playing);
        }

        public void RestartLevel()
        {
            Time.timeScale = 1f;
            GameSession.RetryLevel();
            SceneLoader.Load(SceneLoader.GameScene);
        }

        public void QuitToMenu()
        {
            Time.timeScale = 1f;
            GameSession.InProgress = false;
            AudioManager.Instance.SetDuck(false);
            SceneLoader.Load(SceneLoader.MenuScene);
        }
    }
}
