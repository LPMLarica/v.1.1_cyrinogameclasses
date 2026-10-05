using UnityEditor;
using UnityEngine;

namespace Faisca.EditorTools
{
    /// <summary>
    /// Configura automaticamente a importação da arte e do áudio:
    /// pixel art nítida (Point, sem compressão, 16 px por unidade) e
    /// música em streaming. Assim ninguém da equipe precisa lembrar de
    /// ajustar o Inspector a cada sprite novo.
    /// </summary>
    public class ArtImportPostprocessor : AssetPostprocessor
    {
        public const int PixelsPerUnit = 16;
        public const int UiPixelsPerUnit = 32;

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Art/")) return;
            var ti = (TextureImporter)assetImporter;
            bool isUI = assetPath.StartsWith("Assets/Art/UI/");
            bool isBackground = assetPath.StartsWith("Assets/Art/Backgrounds/");

            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = isUI ? UiPixelsPerUnit : PixelsPerUnit;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.wrapMode = isBackground ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;

            var settings = new TextureImporterSettings();
            ti.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; // necessário para o modo Tiled
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spriteExtrude = 0;
            ti.SetTextureSettings(settings);

            if (assetPath.EndsWith("panel.png")) ti.spriteBorder = new Vector4(8, 8, 8, 8);
            if (assetPath.EndsWith("button.png")) ti.spriteBorder = new Vector4(5, 5, 5, 5);
        }

        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith("Assets/Audio/")) return;
            var ai = (AudioImporter)assetImporter;
            var s = ai.defaultSampleSettings;
            if (assetPath.StartsWith("Assets/Audio/Music/"))
            {
                s.loadType = AudioClipLoadType.Streaming;
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = 0.6f;
            }
            else
            {
                s.loadType = AudioClipLoadType.DecompressOnLoad;
                s.compressionFormat = AudioCompressionFormat.PCM;
            }
            ai.defaultSampleSettings = s;
        }
    }
}
