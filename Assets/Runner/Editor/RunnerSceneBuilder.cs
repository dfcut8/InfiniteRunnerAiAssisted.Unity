using System;
using System.IO;
using System.Linq;
using TMPro;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PixelRunner.Editor
{
    public static class RunnerSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/MainGameplay.unity";
        static Material spriteMaterial, silhouetteMaterial;
        static Sprite[] art;
        static GameObject Child(string name, Transform parent = null)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent,false);
            return go;
        }
        static SpriteRenderer SpriteObject(string name, Sprite sprite, Transform parent, Vector3 position, int order, Material material = null)
        {
            var renderer = Child(name,parent).AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = material != null ? material : spriteMaterial;
            renderer.transform.localPosition = position;
            renderer.sortingOrder = order;
            return renderer;
        }
        static Material MaterialAsset(string name, bool silhouette)
        {
            string path = RunnerArtBuilder.Art + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("PixelRunner/FlatSprite"));
                AssetDatabase.CreateAsset(material,path);
            }
            material.SetFloat("_Silhouette",silhouette ? 1 : 0);
            EditorUtility.SetDirty(material);
            return material;
        }
        [CliCommand("runner_build_scene", "Build the pixel runner assets, prefabs, and main gameplay scene")]
        public static string Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before building the scene.");
            var pipeline = GraphicsSettings.currentRenderPipeline;
            if (!(pipeline is UniversalRenderPipelineAsset)) throw new InvalidOperationException("This scene requires the project's URP quality pipeline.");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            Directory.CreateDirectory("Assets/Runner/Prefabs");
            art = RunnerArtBuilder.PrepareSprites();
            var font = RunnerArtBuilder.BuildFont();
            spriteMaterial = MaterialAsset("PixelUnlit",false);
            silhouetteMaterial = MaterialAsset("PixelSilhouette",true);
            var settings = AssetDatabase.LoadAssetAtPath<RunnerSettings>("Assets/Runner/RunnerSettings.asset");
            if (settings == null) { settings = ScriptableObject.CreateInstance<RunnerSettings>(); AssetDatabase.CreateAsset(settings,"Assets/Runner/RunnerSettings.asset"); }
            var input = BuildInput();
            var prefab = BuildSection();
            var run = Child("Run State").AddComponent<RunnerRun>();
            var world = Child("Level Generator").AddComponent<RunnerWorld>();
            world.settings = settings; world.sectionPrefab = prefab;
            var effects = Child("Pixel Effects").AddComponent<RunnerEffects>();
            var pixel = new Texture2D(2,2,TextureFormat.RGBA32,false);
            pixel.SetPixels(Enumerable.Repeat(Color.white,4).ToArray()); pixel.Apply();
            effects.particle = RunnerArtBuilder.SaveSprite(pixel,"Particle",new Vector2(0.5f,0.5f));
            UnityEngine.Object.DestroyImmediate(pixel);
            effects.material = silhouetteMaterial;
            var playerObject = Child("Unicorn");
            var body = playerObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 0;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var physicsMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Runner/NoFriction.physicsMaterial2D");
            if (physicsMaterial == null)
            {
                physicsMaterial = new PhysicsMaterial2D("No Friction") { friction = 0, bounciness = 0 };
                AssetDatabase.CreateAsset(physicsMaterial,"Assets/Runner/NoFriction.physicsMaterial2D");
            }
            var collider = playerObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1.05f,1.1f); collider.offset = new Vector2(0,0.56f);
            collider.sharedMaterial = physicsMaterial;
            var player = playerObject.AddComponent<RunnerPlayer>();
            player.run = run; player.input = input; player.frames = art.Take(12).ToArray();
            player.visual = SpriteObject("Unicorn Sprite",art[0],playerObject.transform,Vector3.zero,10);
            var cameraObject = Child("Main Camera"); cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true; camera.orthographicSize = 5.625f;
            camera.backgroundColor = RunnerArtBuilder.Palette[0];
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.allowHDR = camera.allowMSAA = camera.allowDynamicResolution = false;
            var cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = false;
            cameraData.antialiasing = AntialiasingMode.None;
            var pixelCamera = cameraObject.AddComponent<PixelPerfectCamera>();
            pixelCamera.assetsPPU = 16;
            pixelCamera.refResolutionX = 320; pixelCamera.refResolutionY = 180;
            pixelCamera.gridSnapping = PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
            pixelCamera.cropFrame = PixelPerfectCamera.CropFrame.Windowbox;
            var follow = cameraObject.AddComponent<RunnerCamera>(); follow.player = player;
            follow.layers = new[] { BuildParallax("Distant Arches",0.15f,true), BuildParallax("Near Masonry",0.4f,false) };
            follow.ResetView();
            run.settings = settings; run.player = player; run.world = world; run.effects = effects; run.followCamera = follow;
            BuildHud(run,camera,pixelCamera,font);
            // A preview platform makes the saved scene immediately understandable outside Play mode.
            var preview = PrefabUtility.InstantiatePrefab(prefab.gameObject) as GameObject;
            preview.name = "Opening Platform (Editor Preview)";
            preview.GetComponent<RunnerSection>().Configure(new SectionPlan { start = -8, end = 24, height = 0 },true);
            preview.AddComponent<RunnerPreview>();
            ConfigureProject(pipeline as UniversalRenderPipelineAsset);
            EditorSceneManager.SaveScene(scene,ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath,true) };
            AssetDatabase.SaveAssets();
            return "Created " + ScenePath;
        }
        static InputActionAsset BuildInput()
        {
            const string path = "Assets/Settings/InputSystem_Actions.inputactions";
            var input = InputActionAsset.FromJson(File.ReadAllText(path));
            var existing = input.FindActionMap("Runner");
            if (existing != null) input.RemoveActionMap(existing);
            var map = input.AddActionMap("Runner");
            var jump = map.AddAction("Jump",InputActionType.Button);
            jump.AddBinding("<Keyboard>/space"); jump.AddBinding("<Keyboard>/z");
            var dash = map.AddAction("Dash",InputActionType.Button);
            dash.AddBinding("<Keyboard>/leftShift"); dash.AddBinding("<Keyboard>/rightShift"); dash.AddBinding("<Keyboard>/x");
            map.AddAction("Restart",InputActionType.Button,"<Keyboard>/r");
            File.WriteAllText(path,input.ToJson());
            UnityEngine.Object.DestroyImmediate(input);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
        }
        static RunnerSection BuildSection()
        {
            var root = Child("Ruins Platform Section"); root.layer = RunnerWorld.GroundLayer;
            var section = root.AddComponent<RunnerSection>();
            section.ground = root.AddComponent<BoxCollider2D>();
            section.tiles = new SpriteRenderer[16]; section.trim = new SpriteRenderer[16];
            for (int i = 0; i < 16; i++)
            {
                section.tiles[i] = SpriteObject("Moss Surface " + i,art[17],root.transform,new Vector3(i*2,-2,0),1);
                section.tiles[i].drawMode = SpriteDrawMode.Tiled;
                section.trim[i] = SpriteObject("Foundation " + i,art[16],root.transform,new Vector3(i*2,-3,0),1);
                section.trim[i].drawMode = SpriteDrawMode.Tiled;
                section.trim[i].size = new Vector2(2,1);
            }
            section.decorations = new SpriteRenderer[3];
            for (int i = 0; i < 3; i++)
            {
                section.decorations[i] = SpriteObject("Broken Ruins " + i,art[28+i],root.transform,new Vector3(2+i*5,0,0),0,silhouetteMaterial);
                section.decorations[i].color = RunnerArtBuilder.Palette[1];
            }
            section.obstacle = SpriteObject("Breakable Runestone",art[20],root.transform,new Vector3(6,0,0),5).gameObject.AddComponent<RunnerItem>();
            section.obstacle.obstacle = true; section.obstacle.gameObject.layer = RunnerWorld.ItemLayer;
            var ob = section.obstacle.gameObject.AddComponent<BoxCollider2D>();
            ob.isTrigger = true; ob.size = new Vector2(0.75f,1.75f); ob.offset = new Vector2(0,0.9f);
            section.relics = new RunnerItem[5];
            for (int i = 0; i < section.relics.Length; i++)
            {
                var relic = SpriteObject("Gold Relic " + i,art[22],root.transform,new Vector3(i+1,1,0),6).gameObject;
                relic.layer = RunnerWorld.ItemLayer;
                var trigger = relic.AddComponent<CircleCollider2D>(); trigger.radius = 0.35f; trigger.isTrigger = true;
                section.relics[i] = relic.AddComponent<RunnerItem>();
            }
            section.Configure(new SectionPlan { start = 0,end = 16,height = 0,obstacle = true,obstacleX = 6 });
            var prefab = PrefabUtility.SaveAsPrefabAsset(root,"Assets/Runner/Prefabs/RuinsSection.prefab").GetComponent<RunnerSection>();
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }
        static RunnerParallax BuildParallax(string name,float ratio,bool far)
        {
            var layer = Child(name).AddComponent<RunnerParallax>();
            layer.scrollRatio = ratio; layer.tileWidth = 20; layer.tiles = new Transform[3];
            for (int i = 0; i < 3; i++)
            {
                var tile = Child("Loop " + i,layer.transform).transform; layer.tiles[i] = tile;
                if (far)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        var arch = SpriteObject("Ancient Arch " + j,art[24],tile,new Vector3(j*7,-2.5f,0),-30,silhouetteMaterial);
                        arch.transform.localScale = new Vector3(3,4,1); arch.color = RunnerArtBuilder.Palette[1];
                    }
                }
                else
                {
                    for (int j = 0; j < 10; j++)
                    {
                        var ruin = SpriteObject("Masonry " + j,art[26+j%6],tile,new Vector3(j*2,-2.5f,0),-20,silhouetteMaterial);
                        ruin.color = RunnerArtBuilder.Palette[j%4 == 0 ? 3 : 1];
                        ruin.transform.localScale = new Vector3(1,1.5f + j%3 * 0.5f,1);
                    }
                    for (int j = 0; j < 2; j++)
                    {
                        var pillar = SpriteObject("Near Pillar " + j,art[25],tile,new Vector3(j*11+3,-3,0),-19,silhouetteMaterial);
                        pillar.color = RunnerArtBuilder.Palette[3]; pillar.transform.localScale = new Vector3(1,3,1);
                    }
                }
            }
            return layer;
        }
        static RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 position)
        {
            var go = new GameObject(name,typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>(); rect.SetParent(parent,false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f,0.5f);
            rect.sizeDelta = size; rect.anchoredPosition = position;
            return rect;
        }
        static TextMeshProUGUI Text(string name,Transform parent,TMP_FontAsset font,Vector2 size,Vector2 position,string value,TextAlignmentOptions alignment = TextAlignmentOptions.Left)
        {
            var rect = Rect(name,parent,size,position);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.fontSharedMaterial = font.material;
            text.fontSize = 8; text.enableAutoSizing = false; text.color = RunnerArtBuilder.Palette[7];
            text.alignment = alignment; text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow; text.raycastTarget = false;
            text.text = value; return text;
        }
        static void BuildHud(RunnerRun run,Camera camera,PixelPerfectCamera pixelCamera,TMP_FontAsset font)
        {
            var go = new GameObject("Gameplay HUD",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler));
            var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.pixelPerfect = true;
            var scaler = go.GetComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ConstantPixelSize;
            var viewport = Rect("320 x 180 Viewport",go.transform,new Vector2(320,180),Vector2.zero);
            var hud = go.AddComponent<RunnerHud>(); hud.run = run; hud.gameCamera = camera; hud.pixelCamera = pixelCamera; hud.canvas = canvas; hud.viewport = viewport;
            var top = Rect("Header",viewport,new Vector2(320,20),new Vector2(0,80)).gameObject.AddComponent<UnityEngine.UI.Image>();
            top.color = RunnerArtBuilder.Palette[0]; top.raycastTarget = false;
            hud.score = Text("Score",viewport,font,new Vector2(144,10),new Vector2(-80,79),"SCORE 000000");
            hud.relics = Text("Relics",viewport,font,new Vector2(108,10),new Vector2(98,79),"RELICS 000",TextAlignmentOptions.Right);
            hud.relics.color = RunnerArtBuilder.Palette[4];
            hud.dash = Text("Dash State",viewport,font,new Vector2(72,10),new Vector2(-116,-74),"DASH READY");
            hud.dash.color = RunnerArtBuilder.Palette[6];
            var fill = Rect("Dash Charge",viewport,new Vector2(38,2),new Vector2(-152,-83));
            fill.pivot = new Vector2(0,0.5f);
            hud.dashFill = fill.gameObject.AddComponent<UnityEngine.UI.Image>(); hud.dashFill.color = RunnerArtBuilder.Palette[5]; hud.dashFill.raycastTarget = false;
            hud.hint = Text("Opening Hint",viewport,font,new Vector2(204,22),new Vector2(51,-76),"SPACE / Z  JUMP\nSHIFT / X  DASH",TextAlignmentOptions.Right);
            var panel = Rect("Run End Panel",viewport,new Vector2(144,54),new Vector2(0,9));
            var panelImage = panel.gameObject.AddComponent<UnityEngine.UI.Image>(); panelImage.color = RunnerArtBuilder.Palette[0]; panelImage.raycastTarget = false;
            hud.deathPanel = panel.gameObject;
            hud.death = Text("Retry",panel,font,new Vector2(140,48),Vector2.zero,"RUN ENDED\n\nR TO RETRY",TextAlignmentOptions.Center);
            panel.gameObject.SetActive(false);
        }
        static void ConfigureProject(UniversalRenderPipelineAsset pipeline)
        {
            Time.fixedDeltaTime = 1f/60f;
            int selected = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i,false);
                QualitySettings.antiAliasing = 0;
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
            }
            QualitySettings.SetQualityLevel(selected,false);
            pipeline.supportsHDR = false; pipeline.msaaSampleCount = 1;
            GraphicsSettings.defaultRenderPipeline = pipeline;
            EditorUtility.SetDirty(pipeline);
            PlayerSettings.defaultScreenWidth = 1280; PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            tags.FindProperty("layers").GetArrayElementAtIndex(RunnerWorld.GroundLayer).stringValue = "RunnerGround";
            tags.FindProperty("layers").GetArrayElementAtIndex(RunnerWorld.ItemLayer).stringValue = "RunnerItems";
            tags.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
