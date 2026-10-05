using System.Collections;
using UnityEngine;

namespace Faisca
{
    /// <summary>
    /// Toca músicas (com crossfade), ambiência e efeitos sonoros.
    /// Singleton criado sob demanda e mantido entre cenas (DontDestroyOnLoad),
    /// então qualquer script pode chamar <c>AudioManager.Play(Sfx.Jump)</c>.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        const string MusicVolumeKey = "faisca.musicVolume";
        const string SfxVolumeKey = "faisca.sfxVolume";
        const int SfxVoices = 10;

        static AudioManager instance;

        AudioLibrary library;
        AudioSource musicA;
        AudioSource musicB;
        AudioSource ambience;
        AudioSource[] voices;
        int nextVoice;
        bool aIsActive = true;
        Music currentMusic = Music.None;
        float duck = 1f;
        Coroutine fadeRoutine;

        public float MusicVolume { get; private set; }
        public float SfxVolume { get; private set; }

        public static AudioManager Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("AudioManager");
                    DontDestroyOnLoad(go);
                    instance = go.AddComponent<AudioManager>();
                    instance.Init();
                }
                return instance;
            }
        }

        AudioSource ActiveMusic { get { return aIsActive ? musicA : musicB; } }

        void Init()
        {
            library = Resources.Load<AudioLibrary>("AudioLibrary");
            if (library == null) Debug.LogWarning("AudioLibrary não encontrada em Resources.");
            MusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 0.7f);
            SfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 0.9f);
            musicA = CreateSource("MusicA", true);
            musicB = CreateSource("MusicB", true);
            ambience = CreateSource("Ambience", true);
            voices = new AudioSource[SfxVoices];
            for (int i = 0; i < SfxVoices; i++) voices[i] = CreateSource("Sfx" + i, false);
        }

        AudioSource CreateSource(string name, bool loop)
        {
            var child = new GameObject(name);
            child.transform.SetParent(transform, false);
            var src = child.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = loop;
            src.spatialBlend = 0f;
            return src;
        }

        // ------------------------------------------------------- efeitos ----
        /// <summary>Atalho estático: toca um efeito sonoro.</summary>
        public static void Play(Sfx sfx, float volume = 1f, float pitchVariation = 0.05f)
        {
            Instance.PlaySfx(sfx, volume, pitchVariation);
        }

        public void PlaySfx(Sfx sfx, float volume, float pitchVariation)
        {
            if (library == null) return;
            var clip = library.Get(sfx);
            if (clip == null) return;
            var src = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;
            src.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            src.PlayOneShot(clip, volume * SfxVolume);
        }

        // -------------------------------------------------------- música ----
        public void PlayMusic(Music music, float fadeTime = 1f)
        {
            if (music == currentMusic && ActiveMusic.isPlaying) return;
            currentMusic = music;
            var clip = library != null ? library.Get(music) : null;
            var from = ActiveMusic;
            aIsActive = !aIsActive;
            var to = ActiveMusic;
            to.clip = clip;
            to.volume = 0f;
            if (clip != null) to.Play();
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(Crossfade(from, to, fadeTime));
        }

        public void StopMusic(float fadeTime = 0.5f)
        {
            currentMusic = Music.None;
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(Crossfade(ActiveMusic, null, fadeTime));
        }

        IEnumerator Crossfade(AudioSource from, AudioSource to, float time)
        {
            float fromStart = from != null ? from.volume : 0f;
            float t = 0f;
            while (t < time)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / time);
                if (from != null) from.volume = Mathf.Lerp(fromStart, 0f, k);
                if (to != null) to.volume = Mathf.Lerp(0f, TargetMusicVolume, k);
                yield return null;
            }
            if (from != null && from != to) from.Stop();
            if (to != null) to.volume = TargetMusicVolume;
            fadeRoutine = null;
        }

        float TargetMusicVolume { get { return MusicVolume * duck; } }

        public void PlayAmbience(bool on)
        {
            if (!on || library == null || library.ambience == null)
            {
                ambience.Stop();
                return;
            }
            if (ambience.isPlaying) return;
            ambience.clip = library.ambience;
            ambience.volume = 0.5f * SfxVolume;
            ambience.Play();
        }

        /// <summary>Abaixa a música (ex.: jogo pausado).</summary>
        public void SetDuck(bool ducked)
        {
            duck = ducked ? 0.35f : 1f;
            if (fadeRoutine == null) ActiveMusic.volume = TargetMusicVolume;
        }

        // ------------------------------------------------------- volumes ----
        public void SetMusicVolume(float v)
        {
            MusicVolume = Mathf.Clamp01(v);
            PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
            if (fadeRoutine == null) ActiveMusic.volume = TargetMusicVolume;
        }

        public void SetSfxVolume(float v)
        {
            SfxVolume = Mathf.Clamp01(v);
            PlayerPrefs.SetFloat(SfxVolumeKey, SfxVolume);
            ambience.volume = 0.5f * SfxVolume;
        }
    }
}
