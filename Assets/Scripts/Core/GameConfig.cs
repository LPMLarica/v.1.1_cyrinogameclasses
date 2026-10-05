using UnityEngine;

namespace Faisca
{
    /// <summary>
    /// Configuração central do jogo: lista de fases, sprites dos tiles e
    /// prefabs usados pelo <see cref="LevelLoader"/>. Fica em
    /// Assets/Resources/GameConfig.asset para ser carregada em qualquer cena.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Faísca/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Fases (na ordem em que são jogadas)")]
        public TextAsset[] levels;

        [Header("Tiles")]
        [Tooltip("Chão indexado pela máscara: topo=1, esquerda=2, direita=4")]
        public Sprite[] groundByMask = new Sprite[8];
        public Sprite metal;
        public Sprite insulator;
        public Sprite oneWay;

        [Header("Prefabs")]
        public GameObject playerPrefab;
        public GameObject enemyPrefab;
        public GameObject cellPrefab;
        public GameObject spikesPrefab;
        public GameObject arcPrefab;
        public GameObject checkpointPrefab;
        public GameObject goalPrefab;
        public GameObject movingPlatformPrefab;

        [Header("Efeitos")]
        public GameObject sparkBurstPrefab;
        public GameObject dustPrefab;

        [Header("Física")]
        public PhysicsMaterial2D noFriction;

        static GameConfig instance;

        public static GameConfig Instance
        {
            get
            {
                if (instance == null) instance = Resources.Load<GameConfig>("GameConfig");
                if (instance == null) Debug.LogError("GameConfig não encontrado em Resources. Rode o menu Faísca ▸ Gerar projeto.");
                return instance;
            }
        }

        /// <summary>Instancia um efeito (se existir) na posição indicada.</summary>
        public static void SpawnFx(GameObject prefab, Vector3 position)
        {
            if (prefab != null) Instantiate(prefab, position, Quaternion.identity);
        }
    }
}
