#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>Configura as artes do minigame para que Resources.Load<Sprite> funcione.</summary>
public class PanicoTextureImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.Contains("/PanicoNoLaboratorio/Resources/art/")) return;
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.spritePixelsPerUnit = 1f;
    }

    [MenuItem("Tools/Panico no Laboratorio/Reimportar artes como Sprites")]
    static void ReimportarArtes()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/PanicoNoLaboratorio/Resources/art" });
        int count = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.spritePixelsPerUnit = 1f;
            importer.SaveAndReimport();
            count++;
        }
        Debug.Log("Pânico no Laboratório: " + count + " artes reimportadas como Sprites.");
    }
}
#endif
