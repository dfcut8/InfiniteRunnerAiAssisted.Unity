using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;

namespace PixelRunner.Editor
{
    public static class RunnerArtBuilder
    {
        public const string Art = "Assets/Runner/Art/";
        public static readonly Color32[] Palette = {
            new Color32(16,20,25,255), new Color32(37,45,50,255), new Color32(89,97,93,255),
            new Color32(89,75,54,255), new Color32(181,138,67,255), new Color32(98,133,72,255),
            new Color32(181,207,131,255), new Color32(243,239,217,255)
        };
        public static Color32 Quantize(Color32 c)
        {
            if (c.a < 128) return new Color32(0,0,0,0);
            int best = int.MaxValue, index = 0;
            for (int i = 0; i < Palette.Length; i++)
            {
                int r = c.r - Palette[i].r, g = c.g - Palette[i].g, b = c.b - Palette[i].b;
                int distance = r*r + g*g + b*b;
                if (distance < best) { best = distance; index = i; }
            }
            return Palette[index];
        }
        public static Sprite[] PrepareSprites()
        {
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            source.LoadImage(File.ReadAllBytes(Art + "UnicornRuinsSource.png"));
            var pixels = source.GetPixels32();
            var sprites = new Sprite[32];
            for (int cell = 0; cell < 32; cell++)
            {
                int x0 = cell % 8 * source.width / 8, x1 = (cell % 8 + 1) * source.width / 8;
                int y0 = (3 - cell / 8) * source.height / 4, y1 = (4 - cell / 8) * source.height / 4;
                int minX = x1, maxX = x0, minY = y1, maxY = y0;
                for (int y = y0; y < y1; y++) for (int x = x0; x < x1; x++)
                    if (pixels[y * source.width + x].a > 200)
                    { minX = Math.Min(minX,x); maxX = Math.Max(maxX,x); minY = Math.Min(minY,y); maxY = Math.Max(maxY,y); }
                var normalized = new Texture2D(32,32,TextureFormat.RGBA32,false);
                var output = new Color32[1024];
                bool fillTile = cell == 16 || cell == 17;
                int targetW = fillTile ? 32 : cell < 12 ? 30 : cell == 22 ? 10 : cell == 20 || cell == 21 ? 18 : 30;
                int targetH = fillTile ? 32 : cell < 12 ? 25 : cell == 22 ? 14 : 30;
                int ox = (32 - targetW) / 2, oy = cell < 12 ? 0 : cell == 22 ? 9 : 0;
                for (int y = 0; y < targetH; y++) for (int x = 0; x < targetW; x++)
                {
                    int sx = Mathf.Clamp(minX + (int)((x + 0.5f) * (maxX - minX + 1) / targetW), x0, x1-1);
                    int sy = Mathf.Clamp(minY + (int)((y + 0.5f) * (maxY - minY + 1) / targetH), y0, y1-1);
                    Color32 c = Quantize(pixels[sy * source.width + sx]);
                    if (fillTile && c.a == 0) c = Palette[1];
                    output[(y+oy)*32+x+ox] = c;
                }
                normalized.SetPixels32(output); normalized.Apply();
                string name = cell < 8 ? "UnicornRun" + cell : new[] {"UnicornJump","UnicornFall","UnicornDash","UnicornDeath","Spark0","Spark1","Spark2","Spark3","Brick","MossPlatform","LedgeLeft","LedgeRight","Runestone","RunestoneCracked","Relic","Debris","Arch","Pillar","Ruin0","Ruin1","Ruin2","Ruin3","Ruin4","Ruin5"}[cell-8];
                Vector2 pivot = cell < 12 || cell == 20 || cell == 21 || cell >= 24 ? new Vector2(0.5f,0) : cell == 16 || cell == 17 ? Vector2.zero : new Vector2(0.5f,0.5f);
                sprites[cell] = SaveSprite(normalized, name, pivot);
                UnityEngine.Object.DestroyImmediate(normalized);
            }
            UnityEngine.Object.DestroyImmediate(source);
            return sprites;
        }
        public static Sprite SaveSprite(Texture2D texture, string name, Vector2 pivot)
        {
            string path = Art + name + ".png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = true;
            importer.isReadable = true;
            importer.maxTextureSize = 2048;
            var textureSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(textureSettings);
            textureSettings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(textureSettings);
            importer.SaveAndReimport();
            var factory = new SpriteDataProviderFactories(); factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            if (provider == null) throw new InvalidOperationException("Sprite data provider unavailable: " + path);
            provider.InitSpriteEditorDataProvider();
            var capability = provider.GetDataProvider<ISpriteFrameEditCapability>();
            if (capability == null || !capability.GetEditCapability().HasCapability(EEditCapability.EditPivot))
                throw new InvalidOperationException("Importer does not support pivot editing: " + path);
            var rects = provider.GetSpriteRects();
            foreach (var rect in rects) { rect.alignment = SpriteAlignment.Custom; rect.pivot = pivot; }
            provider.SetSpriteRects(rects); provider.Apply(); importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        public static TMP_FontAsset BuildFont()
        {
            string path = Art + "RuinsPixelFont.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing != null)
            {
                existing.material.shader = Shader.Find("PixelRunner/Bitmap");
                EditorUtility.SetDirty(existing.material);
                AssetDatabase.SaveAssets();
                return existing;
            }
            // Original 5x7 bitmap glyphs. No OS font or dynamic atlas dependency in the build.
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789:/.-+ ";
            string[] rows = {
                "01110/10001/10001/11111/10001/10001/10001", "11110/10001/10001/11110/10001/10001/11110",
                "01111/10000/10000/10000/10000/10000/01111", "11110/10001/10001/10001/10001/10001/11110",
                "11111/10000/10000/11110/10000/10000/11111", "11111/10000/10000/11110/10000/10000/10000",
                "01111/10000/10000/10111/10001/10001/01110", "10001/10001/10001/11111/10001/10001/10001",
                "11111/00100/00100/00100/00100/00100/11111", "00111/00010/00010/00010/10010/10010/01100",
                "10001/10010/10100/11000/10100/10010/10001", "10000/10000/10000/10000/10000/10000/11111",
                "10001/11011/10101/10101/10001/10001/10001", "10001/11001/10101/10011/10001/10001/10001",
                "01110/10001/10001/10001/10001/10001/01110", "11110/10001/10001/11110/10000/10000/10000",
                "01110/10001/10001/10001/10101/10010/01101", "11110/10001/10001/11110/10100/10010/10001",
                "01111/10000/10000/01110/00001/00001/11110", "11111/00100/00100/00100/00100/00100/00100",
                "10001/10001/10001/10001/10001/10001/01110", "10001/10001/10001/10001/10001/01010/00100",
                "10001/10001/10001/10101/10101/10101/01010", "10001/10001/01010/00100/01010/10001/10001",
                "10001/10001/01010/00100/00100/00100/00100", "11111/00001/00010/00100/01000/10000/11111",
                "01110/10001/10011/10101/11001/10001/01110", "00100/01100/00100/00100/00100/00100/01110",
                "01110/10001/00001/00010/00100/01000/11111", "11110/00001/00001/01110/00001/00001/11110",
                "00010/00110/01010/10010/11111/00010/00010", "11111/10000/10000/11110/00001/00001/11110",
                "01110/10000/10000/11110/10001/10001/01110", "11111/00001/00010/00100/01000/01000/01000",
                "01110/10001/10001/01110/10001/10001/01110", "01110/10001/10001/01111/00001/00001/01110",
                "00000/00100/00100/00000/00100/00100/00000", "00001/00010/00010/00100/01000/01000/10000",
                "00000/00000/00000/00000/00000/00100/00100", "00000/00000/00000/11111/00000/00000/00000",
                "00000/00100/00100/11111/00100/00100/00000", "00000/00000/00000/00000/00000/00000/00000"
            };
            var font = ScriptableObject.CreateInstance<TMP_FontAsset>();
            font.name = "Ruins Pixel";
            font.faceInfo = new FaceInfo { familyName = "Ruins Pixel", styleName = "Regular", pointSize = 8, scale = 1, lineHeight = 10, ascentLine = 7, capLine = 7, meanLine = 5, baseline = 0, descentLine = -1, underlineOffset = -1, underlineThickness = 1, strikethroughOffset = 3, strikethroughThickness = 1, tabWidth = 24 };
            font.atlasPopulationMode = AtlasPopulationMode.Static;
            var texture = new Texture2D(128,32,TextureFormat.Alpha8,false) { name = "Ruins Pixel Atlas", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[128*32];
            var glyphs = new List<Glyph>();
            var characters = new List<TMP_Character>();
            for (int i = 0; i < chars.Length; i++)
            {
                int gx = i % 16 * 8, gy = i / 16 * 10;
                var pattern = rows[i].Split('/');
                for (int y = 0; y < 7; y++) for (int x = 0; x < 5; x++)
                    if (pattern[y][x] == '1') pixels[(gy+6-y)*128+gx+x] = new Color32(255,255,255,255);
                var glyph = new Glyph((uint)(i+1),new GlyphMetrics(5,7,0,7,6),new GlyphRect(gx,gy,5,7),1,0);
                glyphs.Add(glyph); characters.Add(new TMP_Character(chars[i],glyph));
            }
            texture.SetPixels32(pixels); texture.Apply();
            font.atlasTextures = new[] { texture };
            font.glyphTable.AddRange(glyphs); font.characterTable.AddRange(characters);
            font.material = new Material(Shader.Find("PixelRunner/Bitmap")) { name = "Ruins Pixel Bitmap", mainTexture = texture };
            var serialized = new SerializedObject(font);
            serialized.FindProperty("m_Version").stringValue = "1.1.0";
            serialized.FindProperty("m_AtlasRenderMode").intValue = (int)GlyphRenderMode.RASTER;
            serialized.FindProperty("m_AtlasWidth").intValue = 128;
            serialized.FindProperty("m_AtlasHeight").intValue = 32;
            serialized.FindProperty("m_AtlasPadding").intValue = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(font,path);
            AssetDatabase.AddObjectToAsset(texture,font); AssetDatabase.AddObjectToAsset(font.material,font);
            font.ReadFontAssetDefinition();
            Directory.CreateDirectory("Assets/Runner/Resources");
            var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>("Assets/Runner/Resources/TMP Settings.asset");
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<TMP_Settings>();
                AssetDatabase.CreateAsset(settings,"Assets/Runner/Resources/TMP Settings.asset");
            }
            var serializedSettings = new SerializedObject(settings);
            serializedSettings.FindProperty("assetVersion").stringValue = "2";
            serializedSettings.FindProperty("m_defaultFontAsset").objectReferenceValue = font;
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings); EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();
            return font;
        }
    }
}
