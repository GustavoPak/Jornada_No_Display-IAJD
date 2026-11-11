#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PanicoNoDisplay.EditorTools
{
    /// <summary>
    /// Configura automaticamente a importação das artes do minijogo:
    /// sprite, 1 pixel por unidade, sem filtro (pixel art, igual ao Godot),
    /// sem compressão e sem mudar o tamanho das imagens.
    /// </summary>
    public class PNDTexturePostprocessor : AssetPostprocessor
    {
        // Funciona em qualquer pasta do projeto (Assets/MiniGames/PanicoNoDisplay, etc.)
        public const string ArtMarker = "/Resources/PanicoNoDisplay/art/";

        public static bool IsOurArt(string assetPath)
        {
            return assetPath != null && assetPath.Replace('\\', '/').IndexOf(ArtMarker, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        void OnPreprocessTexture()
        {
            string path = assetPath.Replace('\\', '/');
            if (!IsOurArt(path)) return;

            var ti = (TextureImporter)assetImporter;
            var s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);

            s.textureType = TextureImporterType.Sprite;
            s.spriteMode = (int)SpriteImportMode.Single;
            s.spritePixelsPerUnit = 1f;
            s.spriteMeshType = SpriteMeshType.FullRect;
            s.spriteAlignment = (int)SpriteAlignment.Center;
            s.mipmapEnabled = false;
            s.alphaIsTransparency = true;
            s.npotScale = TextureImporterNPOTScale.None;
            // Fundos usam filtro linear (texture_filter = 2 no Godot); o resto é pixel art
            s.filterMode = path.Contains("display_lab_background") ? FilterMode.Bilinear : FilterMode.Point;
            s.wrapMode = path.Contains("lab_floor_tile") ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            ti.SetTextureSettings(s);

            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.maxTextureSize = 2048;
        }
    }

    [InitializeOnLoad]
    static class PNDImportCheck
    {
        static PNDImportCheck()
        {
            // Se as artes foram importadas antes deste script existir, reimporta uma vez
            EditorApplication.delayCall += () =>
            {
                var guids = AssetDatabase.FindAssets("t:Texture2D");
                bool needs = false;
                foreach (var g in guids)
                {
                    var p = AssetDatabase.GUIDToAssetPath(g);
                    if (!PNDTexturePostprocessor.IsOurArt(p)) continue;
                    var ti = AssetImporter.GetAtPath(p) as TextureImporter;
                    if (ti != null && (ti.textureType != TextureImporterType.Sprite || Mathf.Abs(ti.spritePixelsPerUnit - 1f) > 0.001f))
                    {
                        needs = true;
                        break;
                    }
                }
                if (needs) PNDMenu.Reimport();
            };
        }
    }

    static class PNDMenu
    {
        [MenuItem("Tools/Pânico no Display/Reimportar artes do minijogo")]
        public static void Reimport()
        {
            foreach (var g in AssetDatabase.FindAssets("t:Texture2D"))
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (PNDTexturePostprocessor.IsOurArt(p))
                    AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate);
            }
            Debug.Log("[Pânico no Display] Artes reimportadas.");
        }

        [MenuItem("Tools/Pânico no Display/Criar gatilho (apertar E) na cena aberta")]
        static void CreateTrigger()
        {
            if (!CheckNotPlaying()) return;

            var go = new GameObject("IniciarPanicoNoDisplay");
            Undo.RegisterCreatedObjectUndo(go, "Criar gatilho do Pânico no Display");
            var trig = go.AddComponent<IniciarPanicoNoDisplay>();

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                trig.jogador = player.transform;
                PlaceNextTo(go.transform, player.transform);
            }
            else if (SceneView.lastActiveSceneView != null)
            {
                go.transform.position = SceneView.lastActiveSceneView.pivot;
            }

            FinishAndSave(go, "Gatilho criado em " + go.transform.position);
        }

        [MenuItem("Tools/Pânico no Display/Colocar gatilho ao lado do jogador")]
        static void MoveTriggerToPlayer()
        {
            if (!CheckNotPlaying()) return;

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                EditorUtility.DisplayDialog("Pânico no Display", "Não achei nenhum objeto com a tag \"Player\" na cena aberta.", "OK");
                return;
            }

            var trig = Object.FindAnyObjectByType<IniciarPanicoNoDisplay>(FindObjectsInactive.Include);
            if (trig == null)
            {
                CreateTrigger();
                return;
            }

            Undo.RecordObject(trig.transform, "Mover gatilho do Pânico no Display");
            Undo.RecordObject(trig, "Mover gatilho do Pânico no Display");
            trig.jogador = player.transform;
            trig.mensagem = "Aperte E para jogar";
            PlaceNextTo(trig.transform, player.transform);
            FinishAndSave(trig.gameObject, "Gatilho movido para " + trig.transform.position);
        }

        static bool CheckNotPlaying()
        {
            if (!EditorApplication.isPlaying) return true;
            EditorUtility.DisplayDialog("Pânico no Display",
                "Pare o Play primeiro. Mudanças feitas durante o Play são desfeitas quando ele para.", "OK");
            return false;
        }

        // 2 m na frente do jogador, apoiado no chão
        static void PlaceNextTo(Transform t, Transform player)
        {
            Vector3 fwd = player.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            Vector3 pos = player.position + fwd.normalized * 2f;
            if (Physics.Raycast(pos + Vector3.up * 2f, Vector3.down, out var hit, 10f))
                pos.y = hit.point.y;
            t.position = pos;
        }

        static void FinishAndSave(GameObject go, string msg)
        {
            Selection.activeGameObject = go;
            if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();
            EditorSceneManager.MarkSceneDirty(go.scene);
            EditorSceneManager.SaveScene(go.scene);
            Debug.Log("[Pânico no Display] " + msg + " e cena salva. Pode arrastar o cubo amarelo para outro lugar (fora do Play) e salvar com Ctrl+S.");
        }
    }
}
#endif
