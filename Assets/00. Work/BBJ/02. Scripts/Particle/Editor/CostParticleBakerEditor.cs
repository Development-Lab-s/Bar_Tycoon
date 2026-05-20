using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;

namespace BBJ.Particle.Editor
{
    [CustomEditor(typeof(CostParticleConfigSO))]
    public class CostParticleBakerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            if (GUILayout.Button("Bake SpriteAsset", GUILayout.Height(30)))
                BakeSpriteAsset((CostParticleConfigSO)target);
        }

        private void BakeSpriteAsset(CostParticleConfigSO config)
        {
            if (config.costTypes == null || config.costTypes.Count == 0)
            {
                Debug.LogWarning("[CostParticleBaker] costTypes가 비어 있습니다.");
                return;
            }

            // 1. 스프라이트 수집 및 텍스처 추출
            // configIndices: 각 sprite가 어느 costType[j]에서 왔는지 추적 (null 건너뜀 대응)
            List<Sprite> sprites = new List<Sprite>();
            List<Texture2D> spriteTex = new List<Texture2D>();
            List<int> configIndices = new List<int>();

            for (int j = 0; j < config.costTypes.Count; j++)
            {
                CostTypeConfig costType = config.costTypes[j];
                if (costType.icon == null)
                {
                    Debug.LogWarning($"[CostParticleBaker] index={j} icon이 없습니다. 건너뜁니다.");
                    continue;
                }

                Sprite s = costType.icon;
                string texPath = AssetDatabase.GetAssetPath(s.texture);
                TextureImporter importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
                if (importer != null && !importer.isReadable)
                {
                    Debug.LogWarning($"[CostParticleBaker] {s.texture.name} Read/Write 활성화 중...");
                    importer.isReadable = true;
                    importer.SaveAndReimport();
                }

                int x = (int)s.rect.x;
                int y = (int)s.rect.y;
                int w = (int)s.rect.width;
                int h = (int)s.rect.height;

                Texture2D extracted = new Texture2D(w, h, TextureFormat.RGBA32, false);
                extracted.SetPixels(s.texture.GetPixels(x, y, w, h));
                extracted.Apply();

                sprites.Add(s);
                spriteTex.Add(extracted);
                configIndices.Add(j);
            }

            if (sprites.Count == 0)
            {
                Debug.LogWarning("[CostParticleBaker] 유효한 스프라이트가 없습니다.");
                return;
            }

            // 2. 아틀라스 팩킹
            Texture2D atlas = new Texture2D(config.maxAtlasSize, config.maxAtlasSize, TextureFormat.RGBA32, false);
            Rect[] uvRects = atlas.PackTextures(spriteTex.ToArray(), 2, config.maxAtlasSize);

            // 3. 아틀라스 저장 (Application.dataPath = "[Project]/Assets")
            string saveDir = "Assets/00. Work/BBJ/05. SO/CostParticle";
            if (!AssetDatabase.IsValidFolder(saveDir))
                AssetDatabase.CreateFolder("Assets/00. Work/BBJ/05. SO", "CostParticle");

            string atlasPath = saveDir + "/CostParticleAtlas.png";
            string fullPath = Path.Combine(Application.dataPath, atlasPath.Substring("Assets/".Length));
            File.WriteAllBytes(fullPath, atlas.EncodeToPNG());
            AssetDatabase.ImportAsset(atlasPath);

            TextureImporter atlasImporter = AssetImporter.GetAtPath(atlasPath) as TextureImporter;
            if (atlasImporter != null)
            {
                atlasImporter.textureType = TextureImporterType.Default;
                atlasImporter.isReadable = false;
                atlasImporter.SaveAndReimport();
            }

            Texture2D atlasTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);

            // 4. TMP_SpriteAsset 생성 또는 갱신
            string spriteAssetPath = saveDir + "/CostParticleSpriteAsset.asset";
            TMP_SpriteAsset spriteAsset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(spriteAssetPath);
            if (spriteAsset == null)
            {
                spriteAsset = CreateInstance<TMP_SpriteAsset>();
                AssetDatabase.CreateAsset(spriteAsset, spriteAssetPath);
            }

            spriteAsset.spriteSheet = atlasTexture;
            spriteAsset.spriteGlyphTable.Clear();
            spriteAsset.spriteCharacterTable.Clear();

            for (int i = 0; i < sprites.Count; i++)
            {
                Rect uv = uvRects[i];
                int pw = spriteTex[i].width;
                int ph = spriteTex[i].height;

                TMP_SpriteGlyph glyph = new TMP_SpriteGlyph
                {
                    index = (uint)i,
                    metrics = new GlyphMetrics(pw, ph, 0, ph, pw),
                    glyphRect = new GlyphRect(
                        Mathf.RoundToInt(uv.x * atlas.width),
                        Mathf.RoundToInt(uv.y * atlas.height),
                        pw, ph),
                    scale = 1f,
                    atlasIndex = 0
                };
                spriteAsset.spriteGlyphTable.Add(glyph);

                TMP_SpriteCharacter character = new TMP_SpriteCharacter((uint)(0xE000 + i), glyph)
                {
                    name = sprites[i].name,
                    scale = 1f
                };
                spriteAsset.spriteCharacterTable.Add(character);

                // 5. spriteIndex 자동 기입 (configIndices로 null 건너뜀 대응)
                config.costTypes[configIndices[i]].spriteIndex = i;
            }

            spriteAsset.UpdateLookupTables();

            // 6. config에 자동 연결
            config.spriteAsset = spriteAsset;

            EditorUtility.SetDirty(spriteAsset);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[CostParticleBaker] Bake 완료: {sprites.Count}개 스프라이트 → {spriteAssetPath}");
        }
    }
}
