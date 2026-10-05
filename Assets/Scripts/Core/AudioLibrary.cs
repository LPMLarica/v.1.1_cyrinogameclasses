using UnityEngine;

namespace Faisca
{
    public enum Sfx
    {
        Jump, Land, Dash, Collect, Stomp, Hurt, Checkpoint, Goal,
        LevelComplete, GameOver, Victory, UiClick, UiHover, Denied, Arc
    }

    public enum Music { None, Menu, Game }

    /// <summary>
    /// Catálogo de clipes de áudio. Separar os clipes num ScriptableObject
    /// permite trocar sons pelo Inspector sem mexer no código.
    /// Fica em Assets/Resources/AudioLibrary.asset.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioLibrary", menuName = "Faísca/Audio Library")]
    public class AudioLibrary : ScriptableObject
    {
        [Header("Música e ambiência")]
        public AudioClip musicMenu;
        public AudioClip musicGame;
        public AudioClip ambience;

        [Header("Efeitos")]
        public AudioClip jump;
        public AudioClip land;
        public AudioClip dash;
        public AudioClip collect;
        public AudioClip stomp;
        public AudioClip hurt;
        public AudioClip checkpoint;
        public AudioClip goal;
        public AudioClip levelComplete;
        public AudioClip gameOver;
        public AudioClip victory;
        public AudioClip uiClick;
        public AudioClip uiHover;
        public AudioClip denied;
        public AudioClip arc;

        public AudioClip Get(Sfx sfx)
        {
            switch (sfx)
            {
                case Sfx.Jump: return jump;
                case Sfx.Land: return land;
                case Sfx.Dash: return dash;
                case Sfx.Collect: return collect;
                case Sfx.Stomp: return stomp;
                case Sfx.Hurt: return hurt;
                case Sfx.Checkpoint: return checkpoint;
                case Sfx.Goal: return goal;
                case Sfx.LevelComplete: return levelComplete;
                case Sfx.GameOver: return gameOver;
                case Sfx.Victory: return victory;
                case Sfx.UiClick: return uiClick;
                case Sfx.UiHover: return uiHover;
                case Sfx.Denied: return denied;
                case Sfx.Arc: return arc;
                default: return null;
            }
        }

        public AudioClip Get(Music music)
        {
            switch (music)
            {
                case Music.Menu: return musicMenu;
                case Music.Game: return musicGame;
                default: return null;
            }
        }
    }
}
