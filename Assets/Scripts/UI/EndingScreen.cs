using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Faisca
{
    /// <summary>Cena final: estatísticas da partida, recorde e retorno ao menu.</summary>
    public class EndingScreen : MonoBehaviour
    {
        public Text statsText;
        public Text recordText;
        public Button playAgainButton;
        public Button menuButton;

        void Awake()
        {
            playAgainButton.onClick.AddListener(() =>
            {
                GameSession.NewGame();
                SceneLoader.Load(SceneLoader.GameScene);
            });
            menuButton.onClick.AddListener(() => SceneLoader.Load(SceneLoader.MenuScene));
        }

        void Start()
        {
            float time = GameSession.TotalTime;
            statsText.text =
                "Tempo total: " + GameSession.FormatTime(time) + "\n" +
                "Células entregues: " + GameSession.TotalCells + "\n" +
                "Quedas: " + GameSession.Deaths;

            bool record = time > 0f && GameSession.InProgress && GameSession.TrySaveBestTime(time);
            recordText.text = record
                ? "NOVO RECORDE!"
                : "Recorde: " + GameSession.FormatTime(GameSession.BestTime);
            GameSession.InProgress = false;

            var audio = AudioManager.Instance;
            audio.PlayAmbience(false);
            audio.SetDuck(false);
            audio.StopMusic(0.2f);
            AudioManager.Play(Sfx.Victory, 1f, 0f);
            StartCoroutine(MusicLater(4.2f));

            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(menuButton.gameObject);
        }

        IEnumerator MusicLater(float delay)
        {
            yield return new WaitForSeconds(delay);
            AudioManager.Instance.PlayMusic(Music.Menu, 2f);
        }
    }
}
