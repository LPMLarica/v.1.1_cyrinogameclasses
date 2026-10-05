using UnityEngine;

namespace Faisca
{
    /// <summary>
    /// Dados que precisam sobreviver à troca de cenas durante uma partida
    /// (vidas, fase atual, estatísticas). Estado estático simples: não precisa
    /// de GameObject e é reiniciado em <see cref="NewGame"/>.
    /// </summary>
    public static class GameSession
    {
        public const int StartingLives = 5;
        const string BestTimeKey = "faisca.bestTime";

        public static int Lives = StartingLives;
        public static int LevelIndex;
        public static int TotalCells;
        public static int Deaths;
        public static float TotalTime;
        public static bool InProgress;

        public static void NewGame()
        {
            Lives = StartingLives;
            LevelIndex = 0;
            TotalCells = 0;
            Deaths = 0;
            TotalTime = 0f;
            InProgress = true;
        }

        /// <summary>Após um Game Over, recomeça a mesma fase com as cargas cheias.</summary>
        public static void RetryLevel()
        {
            Lives = StartingLives;
            InProgress = true;
        }

        /// <summary>Melhor tempo salvo (0 = nenhum registro).</summary>
        public static float BestTime
        {
            get { return PlayerPrefs.GetFloat(BestTimeKey, 0f); }
        }

        /// <summary>Grava o tempo se for recorde. Retorna true quando é um novo recorde.</summary>
        public static bool TrySaveBestTime(float time)
        {
            float best = BestTime;
            if (best > 0f && time >= best) return false;
            PlayerPrefs.SetFloat(BestTimeKey, time);
            PlayerPrefs.Save();
            return true;
        }

        public static string FormatTime(float seconds)
        {
            if (seconds < 0f) seconds = 0f;
            int m = Mathf.FloorToInt(seconds / 60f);
            int s = Mathf.FloorToInt(seconds % 60f);
            return string.Format("{0:00}:{1:00}", m, s);
        }
    }
}
