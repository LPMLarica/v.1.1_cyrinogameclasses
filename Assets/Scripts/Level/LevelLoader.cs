using UnityEngine;
using UnityEngine.Tilemaps;

namespace Faisca
{
    /// <summary>
    /// Constrói a fase a partir de um <see cref="LevelData"/>:
    ///  1) desenha os tiles num Tilemap;
    ///  2) cria colisores retangulares agrupados (evita "quinas fantasmas");
    ///  3) instancia os prefabs 
    /// </summary>
    public class LevelLoader : MonoBehaviour
    {
        [Tooltip("Espessura do colisor das passarelas atravessáveis")]
        public float oneWayThickness = 0.25f;

        Transform root;
        Transform entities;

        public PlayerController Build(LevelData data, GameConfig cfg)
        {
            root = new GameObject("Level - " + data.name).transform;
            entities = new GameObject("Entities").transform;
            entities.SetParent(root, false);

            BuildTiles(data, cfg);
            BuildSolidColliders(data, cfg);
            BuildOneWayColliders(data);
            BuildBoundaries(data);
            return SpawnEntities(data, cfg);
        }

        void BuildTiles(LevelData data, GameConfig cfg)
        {
            var gridGo = new GameObject("Grid", typeof(Grid));
            gridGo.transform.SetParent(root, false);
            var tmGo = new GameObject("Tiles", typeof(Tilemap), typeof(TilemapRenderer));
            tmGo.transform.SetParent(gridGo.transform, false);
            var tilemap = tmGo.GetComponent<Tilemap>();
            tmGo.GetComponent<TilemapRenderer>().sortingOrder = 0;

            var ground = new Tile[8];
            for (int m = 0; m < 8; m++) ground[m] = MakeTile(cfg.groundByMask[m]);
            var metal = MakeTile(cfg.metal);
            var insulator = MakeTile(cfg.insulator);
            var oneWay = MakeTile(cfg.oneWay);

            for (int row = 0; row < data.height; row++)
            {
                for (int x = 0; x < data.width; x++)
                {
                    Tile tile = null;
                    switch (data.Get(x, row))
                    {
                        case '#': tile = ground[GroundMask(data, x, row)]; break;
                        case 'B': tile = metal; break;
                        case 'I': tile = insulator; break;
                        case '-': tile = oneWay; break;
                    }
                    if (tile != null) tilemap.SetTile(new Vector3Int(x, data.RowToY(row), 0), tile);
                }
            }
        }

        static Tile MakeTile(Sprite sprite)
        {
            var t = ScriptableObject.CreateInstance<Tile>();
            t.sprite = sprite;
            t.colliderType = Tile.ColliderType.None;
            return t;
        }

        /// <summary>Auto-tile: bit 1 = topo exposto, 2 = esquerda, 4 = direita.</summary>
        static int GroundMask(LevelData d, int x, int row)
        {
            bool up = row > 0 && d.Get(x, row - 1) == '#';
            bool left = x <= 0 || d.Get(x - 1, row) == '#';
            bool right = x >= d.width - 1 || d.Get(x + 1, row) == '#';
            return (up ? 0 : 1) | (left ? 0 : 2) | (right ? 0 : 4);
        }

        //  colisão
        /// <summary>Agrupa tiles sólidos em retângulos (guloso: estende na horizontal, depois na vertical). Menos colisores = física mais estável e rápida.</summary>
        void BuildSolidColliders(LevelData data, GameConfig cfg)
        {
            var go = new GameObject("Solids");
            go.transform.SetParent(root, false);
            var used = new bool[data.width, data.height];

            for (int row = 0; row < data.height; row++)
            {
                for (int x = 0; x < data.width; x++)
                {
                    if (used[x, row] || !data.IsSolid(x, row)) continue;

                    int w = 1;
                    while (x + w < data.width && !used[x + w, row] && data.IsSolid(x + w, row)) w++;

                    int h = 1;
                    while (row + h < data.height && RowIsFree(data, used, x, row + h, w)) h++;

                    for (int yy = row; yy < row + h; yy++)
                        for (int xx = x; xx < x + w; xx++)
                            used[xx, yy] = true;

                    var box = go.AddComponent<BoxCollider2D>();
                    box.size = new Vector2(w, h);
                    float bottomY = data.RowToY(row + h - 1);
                    box.offset = new Vector2(x + w * 0.5f, bottomY + h * 0.5f);
                    if (cfg.noFriction != null) box.sharedMaterial = cfg.noFriction;
                }
            }
        }

        static bool RowIsFree(LevelData d, bool[,] used, int x, int row, int w)
        {
            for (int i = 0; i < w; i++)
                if (used[x + i, row] || !d.IsSolid(x + i, row)) return false;
            return true;
        }

        void BuildOneWayColliders(LevelData data)
        {
            var parent = new GameObject("OneWayPlatforms").transform;
            parent.SetParent(root, false);
            for (int row = 0; row < data.height; row++)
            {
                int x = 0;
                while (x < data.width)
                {
                    if (data.Get(x, row) != '-') { x++; continue; }
                    int start = x;
                    while (x < data.width && data.Get(x, row) == '-') x++;
                    int len = x - start;

                    var go = new GameObject("OneWay");
                    go.transform.SetParent(parent, false);
                    var box = go.AddComponent<BoxCollider2D>();
                    box.size = new Vector2(len, oneWayThickness);
                    box.offset = new Vector2(start + len * 0.5f, data.RowToY(row) + 1f - oneWayThickness * 0.5f);
                    box.usedByEffector = true;
                    var eff = go.AddComponent<PlatformEffector2D>();
                    eff.useOneWay = true;
                    eff.surfaceArc = 160f;
                    eff.useSideFriction = false;
                    eff.useSideBounce = false;
                }
            }
        }

        void BuildBoundaries(LevelData data)
        {
            var go = new GameObject("Boundaries");
            go.transform.SetParent(root, false);
            float h = data.height + 20f;
            var left = go.AddComponent<BoxCollider2D>();
            left.size = new Vector2(1f, h);
            left.offset = new Vector2(-0.5f, h * 0.5f - 5f);
            var right = go.AddComponent<BoxCollider2D>();
            right.size = new Vector2(1f, h);
            right.offset = new Vector2(data.width + 0.5f, h * 0.5f - 5f);
        }

        // entity
        PlayerController SpawnEntities(LevelData data, GameConfig cfg)
        {
            PlayerController player = null;

            // o jogador primeiro: outros objetos procuram por ele no Start
            for (int row = 0; row < data.height && player == null; row++)
                for (int x = 0; x < data.width; x++)
                    if (data.Get(x, row) == 'P')
                    {
                        var go = Instantiate(cfg.playerPrefab, data.CellCenter(x, row), Quaternion.identity);
                        go.name = "Faisca";
                        player = go.GetComponent<PlayerController>();
                        break;
                    }

            if (player == null) Debug.LogError("A fase não tem 'P' (início do jogador).");

            for (int row = 0; row < data.height; row++)
            {
                for (int x = 0; x < data.width; x++)
                {
                    char c = data.Get(x, row);
                    Vector3 pos = data.CellCenter(x, row);
                    switch (c)
                    {
                        case 'E': Spawn(cfg.enemyPrefab, pos); break;
                        case 'C': Spawn(cfg.cellPrefab, pos); break;
                        case '^': Spawn(cfg.spikesPrefab, pos); break;
                        case 'K': Spawn(cfg.checkpointPrefab, pos); break;
                        case '~':
                        case '!':
                            var arc = Spawn(cfg.arcPrefab, pos);
                            if (arc != null) arc.GetComponent<ArcHazard>().phaseB = c == '!';
                            break;
                        case 'G':
                            Spawn(cfg.goalPrefab, new Vector3(x + 1f, data.RowToY(row) + 1f, 0f));
                            break;
                        case 'M':
                            SpawnPlatform(data, cfg, x, row, FindInRow(data, row, x, 'm'), row);
                            break;
                        case 'V':
                            SpawnPlatform(data, cfg, x, row, x, FindInColumn(data, x, row, 'v'));
                            break;
                        default:
                            if (char.IsDigit(c)) SpawnHint(data, c, pos);
                            break;
                    }
                }
            }
            return player;
        }

        GameObject Spawn(GameObject prefab, Vector3 pos)
        {
            if (prefab == null) return null;
            return Instantiate(prefab, pos, Quaternion.identity, entities);
        }

        void SpawnPlatform(LevelData data, GameConfig cfg, int x0, int row0, int x1, int row1)
        {
            var go = Spawn(cfg.movingPlatformPrefab, data.CellCenter(x0, row0));
            if (go == null) return;
            go.GetComponent<MovingPlatform>().Setup(data.CellCenter(x0, row0), data.CellCenter(x1, row1));
        }

        static int FindInRow(LevelData d, int row, int fromX, char c)
        {
            for (int i = 1; i < d.width; i++)
            {
                if (d.Get(fromX + i, row) == c) return fromX + i;
                if (d.Get(fromX - i, row) == c) return fromX - i;
            }
            return fromX;
        }

        static int FindInColumn(LevelData d, int x, int fromRow, char c)
        {
            for (int i = 1; i < d.height; i++)
            {
                if (d.Get(x, fromRow - i) == c) return fromRow - i;
                if (d.Get(x, fromRow + i) == c) return fromRow + i;
            }
            return fromRow;
        }

        void SpawnHint(LevelData data, char digit, Vector3 pos)
        {
            string text = data.Hint(digit);
            if (string.IsNullOrEmpty(text)) return;
            var go = new GameObject("Hint " + digit);
            go.transform.SetParent(entities, false);
            go.transform.position = pos + Vector3.up;
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(1f, 5f);
            go.AddComponent<HintTrigger>().message = text;
        }
    }
}
