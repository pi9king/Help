using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Help.EditorTools
{
    // Applies the same 64 px grid and bottom-center pivot every time the generated sheets are imported.
    public sealed class ECharacterSpriteImport : AssetPostprocessor
    {
        private const string Folder = "Assets/Sprites/E_Character/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder, System.StringComparison.Ordinal)) return;

            var file = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            int columns;
            int rows;
            switch (file)
            {
                case "E_Master": columns = 4; rows = 1; break;
                case "E_Idle": columns = 4; rows = 4; break;
                case "E_Walk": columns = 6; rows = 4; break;
                case "E_Attack": columns = 6; rows = 4; break;
                case "E_Hit": columns = 3; rows = 4; break;
                case "E_Death": columns = 8; rows = 1; break;
                case "E_Skill": columns = 6; rows = 1; break;
                case "E_Equipment": columns = 7; rows = 1; break;
                default: return;
            }

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = PixelImport.PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            var existing = new Dictionary<string, UnityEditor.GUID>();
            foreach (var sprite in provider.GetSpriteRects()) existing[sprite.name] = sprite.spriteID;

            var sprites = new List<SpriteRect>();
            var pairs = new List<SpriteNameFileIdPair>();
            for (int row = 0; row < rows; row++)
            for (int column = 0; column < columns; column++)
            {
                string name = file + "_" + row + "_" + column;
                UnityEditor.GUID id;
                if (!existing.TryGetValue(name, out id)) id = UnityEditor.GUID.Generate();
                var sprite = new SpriteRect
                {
                    name = name,
                    spriteID = id,
                    rect = new Rect(column * 64, (rows - row - 1) * 64, 64, 64),
                    alignment = SpriteAlignment.Custom,
                    pivot = new Vector2(0.5f, 0f)
                };
                sprites.Add(sprite);
                pairs.Add(new SpriteNameFileIdPair(name, id));
            }

            provider.SetSpriteRects(sprites.ToArray());
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(pairs);
            provider.Apply();
        }
    }
}
