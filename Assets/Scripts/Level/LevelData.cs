using System.Collections.Generic;
using UnityEngine;

namespace Faisca
{
    /// <summary>
    /// Representação de uma fase lida de um arquivo de texto.
    ///
    /// Formato (ver Docs/LevelDesign/LevelDesign.md):
    ///   linhas iniciadas por '@' são metadados (ex.: @nome=..., @meta=7, @dica1=...);
    ///   as demais formam o mapa, um caractere por tile, linha 0 = topo.
    /// </summary>
    public class LevelData
    {
        public string name = "Fase";
        public int requiredCells = 1;
        public int width;
        public int height;
        public readonly Dictionary<string, string> meta = new Dictionary<string, string>();
        char[,] map; // [x, linha]

        public static LevelData Parse(string text)
        {
            var data = new LevelData();
            var rows = new List<string>();
            var lines = text.Replace("\r", "").Split('\n');
            foreach (var raw in lines)
            {
                if (string.IsNullOrEmpty(raw)) continue;
                if (raw[0] == '@')
                {
                    int eq = raw.IndexOf('=');
                    if (eq > 1) data.meta[raw.Substring(1, eq - 1).Trim()] = raw.Substring(eq + 1).Trim();
                    continue;
                }
                rows.Add(raw);
            }

            data.height = rows.Count;
            foreach (var r in rows) data.width = Mathf.Max(data.width, r.Length);
            data.map = new char[data.width, data.height];
            for (int y = 0; y < data.height; y++)
                for (int x = 0; x < data.width; x++)
                    data.map[x, y] = x < rows[y].Length ? rows[y][x] : '.';

            string value;
            if (data.meta.TryGetValue("nome", out value)) data.name = value;
            if (data.meta.TryGetValue("meta", out value)) int.TryParse(value, out data.requiredCells);
            return data;
        }

        /// <summary>Caractere na posição (fora do mapa = vazio).</summary>
        public char Get(int x, int row)
        {
            if (x < 0 || x >= width || row < 0 || row >= height) return '.';
            return map[x, row];
        }

        public bool IsSolid(int x, int row)
        {
            char c = Get(x, row);
            return c == '#' || c == 'B' || c == 'I';
        }

        /// <summary>Linha do mapa (0 = topo) → coordenada y da grade (0 = base).</summary>
        public int RowToY(int row) { return height - 1 - row; }

        /// <summary>Centro do tile em coordenadas de mundo (1 tile = 1 unidade).</summary>
        public Vector3 CellCenter(int x, int row)
        {
            return new Vector3(x + 0.5f, RowToY(row) + 0.5f, 0f);
        }

        public string Hint(char digit)
        {
            string value;
            return meta.TryGetValue("dica" + digit, out value) ? value : null;
        }
    }
}
