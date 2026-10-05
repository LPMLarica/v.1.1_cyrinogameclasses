using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Faisca.EditorTools
{
    /// <summary>
    /// Ferramenta da equipe que monta o projeto Unity a partir dos assets
    /// versionados: AnimationClips, AnimatorControllers, prefabs, materiais,
    /// ScriptableObjects de configuração, as três cenas, Build Settings e o
    /// build executável.
    ///
    /// Roda sozinha na primeira vez que o projeto é aberto (quando ainda não
    /// existe Assets/Scenes/MainMenu.unity) e pode ser chamada pelo menu
    /// "Faísca" a qualquer momento.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectBootstrapper
    {
        const string ArtDir = "Assets/Art";
        const string AudioDir = "Assets/Audio";
        const string ScenesDir = "Assets/Scenes";
        const string PrefabsDir = "Assets/Prefabs";
        const string AnimDir = "Assets/Animations";
        const string MatDir = "Assets/Art/Materials";
        const string ResDir = "Assets/Resources";

        static readonly Color Navy = new Color32(13, 11, 30, 255);
        static readonly Color Yellow = new Color32(255, 243, 163, 255);
        static readonly Color Cyan = new Color32(155, 231, 255, 255);
        static readonly Color Green = new Color32(123, 211, 137, 255);
        static readonly Color TextColor = new Color32(235, 235, 245, 255);

        static Font fontRegular;
        static Font fontBold;

        static ProjectBootstrapper()
        {
            EditorApplication.delayCall += AutoRun;
        }

        static string ScenePath(string name) { return ScenesDir + "/" + name + ".unity"; }

        static void AutoRun()
        {
            if (Application.isBatchMode) return;
            if (File.Exists(ScenePath("MainMenu"))) return;
            if (SessionState.GetBool("Faisca.AutoRunDone", false)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += AutoRun; // espera a importação inicial terminar
                return;
            }
            SessionState.SetBool("Faisca.AutoRunDone", true);
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Debug.Log("[Faísca] Primeira abertura: gerando prefabs, animações e cenas...");
            Generate();
        }

        // ================================================================ menu
        [MenuItem("Faísca/Gerar projeto (prefabs, animações e cenas)", false, 1)]
        static void GenerateFromMenu()
        {
            if (File.Exists(ScenePath("MainMenu")) &&
                !EditorUtility.DisplayDialog("Faísca",
                    "Isto recria prefabs, animações, materiais e as 3 cenas.\n" +
                    "Alterações feitas à mão nesses arquivos serão perdidas.\n\nContinuar?",
                    "Recriar", "Cancelar"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Generate();
        }

        [MenuItem("Faísca/Gerar build/Windows (64 bits)", false, 20)]
        static void BuildWindows() { Build(BuildTarget.StandaloneWindows64, "Builds/Windows/Faisca.exe"); }

        [MenuItem("Faísca/Gerar build/Linux", false, 21)]
        static void BuildLinux() { Build(BuildTarget.StandaloneLinux64, "Builds/Linux/Faisca.x86_64"); }

        [MenuItem("Faísca/Gerar build/macOS", false, 22)]
        static void BuildMac() { Build(BuildTarget.StandaloneOSX, "Builds/macOS/Faisca.app"); }

        [MenuItem("Faísca/Abrir cena/Menu", false, 40)]
        static void OpenMenu() { OpenScene("MainMenu"); }

        [MenuItem("Faísca/Abrir cena/Jogo", false, 41)]
        static void OpenGame() { OpenScene("Game"); }

        [MenuItem("Faísca/Abrir cena/Final", false, 42)]
        static void OpenEnding() { OpenScene("Ending"); }

        static void OpenScene(string name)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath(name));
        }

        /// <summary>Para integração contínua: Unity -batchmode -executeMethod Faisca.EditorTools.ProjectBootstrapper.CI</summary>
        public static void CI()
        {
            Generate();
            Build(BuildTarget.StandaloneWindows64, "Builds/Windows/Faisca.exe");
        }

        // ============================================================ geração
        public static void Generate()
        {
            try
            {
                Progress("Configurando importação", 0.05f);
                EnsureLegacyInput();
                EnsureArtImported();
                fontRegular = AssetDatabase.LoadAssetAtPath<Font>(ArtDir + "/Fonts/PixelifySans-Regular.ttf");
                fontBold = AssetDatabase.LoadAssetAtPath<Font>(ArtDir + "/Fonts/PixelifySans-Bold.ttf");
                if (fontRegular == null) fontRegular = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (fontBold == null) fontBold = fontRegular;

                RecreateFolder(PrefabsDir);
                RecreateFolder(AnimDir);
                EnsureFolder(MatDir);
                EnsureFolder(ScenesDir);
                EnsureFolder(ResDir);

                Progress("Materiais", 0.15f);
                var mats = BuildMaterials();
                Progress("Animações", 0.3f);
                var ctrls = BuildAnimators();
                Progress("Prefabs", 0.5f);
                var prefabs = BuildPrefabs(ctrls, mats);
                Progress("Configurações", 0.6f);
                BuildConfigAssets(prefabs, mats);
                Progress("Cena: Menu", 0.7f);
                BuildMenuScene(ctrls);
                Progress("Cena: Jogo", 0.8f);
                BuildGameScene();
                Progress("Cena: Final", 0.9f);
                BuildEndingScene(ctrls);
                ConfigurePlayerSettings();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            if (!Application.isBatchMode) EditorSceneManager.OpenScene(ScenePath("MainMenu"));
            Debug.Log("[Faísca] Projeto gerado. Abra a cena MainMenu e aperte Play.");
        }

        static void Progress(string msg, float p)
        {
            if (!Application.isBatchMode) EditorUtility.DisplayProgressBar("Faísca", msg, p);
        }

        // ------------------------------------------------------- utilitários --
        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static void RecreateFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) AssetDatabase.DeleteAsset(path);
            EnsureFolder(path);
        }

        static T CreateOrReplace<T>(T asset, string path) where T : Object
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static Sprite S(string rel)
        {
            string path = ArtDir + "/" + rel;
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s == null) Debug.LogError("[Faísca] Sprite não encontrado: " + path);
            return s;
        }

        static Sprite[] Frames(string relPrefix, int count)
        {
            var arr = new Sprite[count];
            for (int i = 0; i < count; i++) arr[i] = S(relPrefix + "_" + i + ".png");
            return arr;
        }

        static AudioClip Clip(string rel)
        {
            string path = AudioDir + "/" + rel;
            var c = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (c == null) Debug.LogError("[Faísca] Áudio não encontrado: " + path);
            return c;
        }

        /// <summary>O jogo usa o Input Manager clássico; garante que ele está ativo.</summary>
        static void EnsureLegacyInput()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets == null || assets.Length == 0) return;
            var so = new SerializedObject(assets[0]);
            var prop = so.FindProperty("activeInputHandler");
            if (prop == null || prop.intValue != 1) return; // 0 = antigo, 1 = só o novo, 2 = ambos
            prop.intValue = 2;
            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            if (!Application.isBatchMode)
                EditorUtility.DisplayDialog("Faísca",
                    "Este projeto usa o Input Manager clássico. A opção 'Active Input Handling' foi " +
                    "alterada para 'Both'.\n\nReinicie a Unity para a mudança valer.", "OK");
        }

        /// <summary>Reimporta a arte se o pós-processador ainda não tiver sido aplicado.</summary>
        static void EnsureArtImported()
        {
            string probe = ArtDir + "/Sprites/Player/player_idle_0.png";
            var ti = AssetImporter.GetAtPath(probe) as TextureImporter;
            bool ok = ti != null && ti.textureType == TextureImporterType.Sprite &&
                      Mathf.Approximately(ti.spritePixelsPerUnit, ArtImportPostprocessor.PixelsPerUnit) &&
                      ti.filterMode == FilterMode.Point;
            if (ok) return;
            AssetDatabase.ImportAsset(ArtDir, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(AudioDir, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
        }

        // ========================================================= materiais
        class Mats
        {
            public Material spark;
            public Material dust;
            public PhysicsMaterial2D noFriction;
        }

        static Mats BuildMaterials()
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            var m = new Mats();
            m.spark = ParticleMaterial(shader, "ParticleSpark", "particle_spark.png");
            m.dust = ParticleMaterial(shader, "ParticleDust", "particle_dust.png");
            var pm = new PhysicsMaterial2D("NoFriction");
            pm.friction = 0f;
            pm.bounciness = 0f;
            m.noFriction = CreateOrReplace(pm, MatDir + "/NoFriction.physicsMaterial2D");
            return m;
        }

        static Material ParticleMaterial(Shader shader, string name, string texture)
        {
            var mat = new Material(shader);
            mat.name = name;
            mat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(ArtDir + "/Sprites/FX/" + texture);
            return CreateOrReplace(mat, MatDir + "/" + name + ".mat");
        }

        // ========================================================= animações
        class Ctrls
        {
            public RuntimeAnimatorController player, enemy, cell, arc, checkpoint, goal, goalLit;
        }

        static AnimationClip MakeClip(string name, Sprite[] frames, float fps, bool loop)
        {
            var clip = new AnimationClip();
            clip.name = name;
            clip.frameRate = fps;
            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            var keys = new ObjectReferenceKeyframe[frames.Length + 1];
            for (int i = 0; i < frames.Length; i++)
            {
                keys[i].time = i / fps;
                keys[i].value = frames[i];
            }
            // repete o último quadro para que ele dure um quadro inteiro
            keys[frames.Length].time = frames.Length / fps;
            keys[frames.Length].value = frames[frames.Length - 1];
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.CreateAsset(clip, AnimDir + "/" + name + ".anim");
            return clip;
        }

        static AnimatorController MakeController(string name)
        {
            return AnimatorController.CreateAnimatorControllerAtPath(AnimDir + "/" + name + ".controller");
        }

        static AnimatorState AddState(AnimatorStateMachine sm, string name, Motion clip, Vector2 pos)
        {
            var st = sm.AddState(name, new Vector3(pos.x, pos.y, 0f));
            st.motion = clip;
            return st;
        }

        static AnimatorStateTransition T(AnimatorState from, AnimatorState to)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = false;
            t.exitTime = 0f;
            t.duration = 0f;
            return t;
        }

        static AnimatorStateTransition AnyT(AnimatorStateMachine sm, AnimatorState to)
        {
            var t = sm.AddAnyStateTransition(to);
            t.hasExitTime = false;
            t.duration = 0f;
            t.canTransitionToSelf = false;
            return t;
        }

        static Ctrls BuildAnimators()
        {
            var c = new Ctrls();
            const string P = "Sprites/Player/player_";

            // ---------- Faísca
            var idle = MakeClip("Faisca_Idle", Frames(P + "idle", 4), 8, true);
            var run = MakeClip("Faisca_Run", Frames(P + "run", 6), 14, true);
            var jump = MakeClip("Faisca_Jump", Frames(P + "jump", 2), 10, true);
            var fall = MakeClip("Faisca_Fall", Frames(P + "fall", 2), 10, true);
            var dash = MakeClip("Faisca_Dash", Frames(P + "dash", 3), 20, true);
            var hurt = MakeClip("Faisca_Hurt", Frames(P + "hurt", 2), 12, true);

            var pc = MakeController("Faisca");
            pc.AddParameter("Speed", AnimatorControllerParameterType.Float);
            pc.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            pc.AddParameter("VelY", AnimatorControllerParameterType.Float);
            pc.AddParameter("Dashing", AnimatorControllerParameterType.Bool);
            pc.AddParameter("Hurting", AnimatorControllerParameterType.Bool);
            var sm = pc.layers[0].stateMachine;
            var sIdle = AddState(sm, "Idle", idle, new Vector2(300, 0));
            var sRun = AddState(sm, "Run", run, new Vector2(300, 120));
            var sJump = AddState(sm, "Jump", jump, new Vector2(560, 0));
            var sFall = AddState(sm, "Fall", fall, new Vector2(560, 120));
            var sDash = AddState(sm, "Dash", dash, new Vector2(560, 260));
            var sHurt = AddState(sm, "Hurt", hurt, new Vector2(300, 260));
            sm.defaultState = sIdle;

            var t = T(sIdle, sRun); t.AddCondition(AnimatorConditionMode.Greater, 0.5f, "Speed"); t.AddCondition(AnimatorConditionMode.If, 0, "Grounded");
            t = T(sRun, sIdle); t.AddCondition(AnimatorConditionMode.Less, 0.5f, "Speed");
            foreach (var ground in new[] { sIdle, sRun })
            {
                t = T(ground, sJump); t.AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded"); t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "VelY");
                t = T(ground, sFall); t.AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded"); t.AddCondition(AnimatorConditionMode.Less, -1f, "VelY");
            }
            t = T(sJump, sFall); t.AddCondition(AnimatorConditionMode.Less, 0f, "VelY");
            t = T(sJump, sIdle); t.AddCondition(AnimatorConditionMode.If, 0, "Grounded");
            t = T(sFall, sIdle); t.AddCondition(AnimatorConditionMode.If, 0, "Grounded");
            t = AnyT(sm, sDash); t.AddCondition(AnimatorConditionMode.If, 0, "Dashing");
            t = T(sDash, sFall); t.AddCondition(AnimatorConditionMode.IfNot, 0, "Dashing");
            t = AnyT(sm, sHurt); t.AddCondition(AnimatorConditionMode.If, 0, "Hurting");
            t = T(sHurt, sIdle); t.AddCondition(AnimatorConditionMode.IfNot, 0, "Hurting");
            c.player = pc;

            // ---------- Curto
            var walk = MakeClip("Curto_Walk", Frames("Sprites/Enemy/curto_walk", 4), 8, true);
            var die = MakeClip("Curto_Die", Frames("Sprites/Enemy/curto_die", 3), 10, false);
            var ec = MakeController("Curto");
            ec.AddParameter("Die", AnimatorControllerParameterType.Trigger);
            sm = ec.layers[0].stateMachine;
            var sWalk = AddState(sm, "Walk", walk, new Vector2(300, 0));
            var sDie = AddState(sm, "Die", die, new Vector2(300, 120));
            sm.defaultState = sWalk;
            t = AnyT(sm, sDie); t.AddCondition(AnimatorConditionMode.If, 0, "Die");
            c.enemy = ec;

            // ---------- Célula
            var spin = MakeClip("Cell_Spin", Frames("Sprites/Items/cell", 6), 10, true);
            var cc = MakeController("Cell");
            AddState(cc.layers[0].stateMachine, "Spin", spin, new Vector2(300, 0));
            c.cell = cc;

            // ---------- Arco
            var arcOn = MakeClip("Arc_On", Frames("Sprites/Hazards/arc_on", 3), 14, true);
            var arcOff = MakeClip("Arc_Off", Frames("Sprites/Hazards/arc_off", 1), 1, true);
            var ac = MakeController("Arc");
            ac.AddParameter("Active", AnimatorControllerParameterType.Bool);
            sm = ac.layers[0].stateMachine;
            var sOff = AddState(sm, "Off", arcOff, new Vector2(300, 0));
            var sOn = AddState(sm, "On", arcOn, new Vector2(300, 120));
            sm.defaultState = sOff;
            t = T(sOff, sOn); t.AddCondition(AnimatorConditionMode.If, 0, "Active");
            t = T(sOn, sOff); t.AddCondition(AnimatorConditionMode.IfNot, 0, "Active");
            c.arc = ac;

            // ---------- Checkpoint
            var cpOff = MakeClip("Checkpoint_Off", Frames("Sprites/Items/checkpoint_off", 1), 1, true);
            var cpOn = MakeClip("Checkpoint_On", Frames("Sprites/Items/checkpoint_on", 2), 4, true);
            var kc = MakeController("Checkpoint");
            kc.AddParameter("Active", AnimatorControllerParameterType.Bool);
            sm = kc.layers[0].stateMachine;
            var kOff = AddState(sm, "Off", cpOff, new Vector2(300, 0));
            var kOn = AddState(sm, "On", cpOn, new Vector2(300, 120));
            sm.defaultState = kOff;
            t = T(kOff, kOn); t.AddCondition(AnimatorConditionMode.If, 0, "Active");
            c.checkpoint = kc;

            // ---------- Transformador
            var gOff = MakeClip("Goal_Off", Frames("Sprites/Items/goal_off", 1), 1, true);
            var gReady = MakeClip("Goal_Ready", Frames("Sprites/Items/goal_ready", 2), 3, true);
            var gActive = MakeClip("Goal_Active", Frames("Sprites/Items/goal_active", 4), 10, true);
            var gc = MakeController("Goal");
            gc.AddParameter("Ready", AnimatorControllerParameterType.Bool);
            gc.AddParameter("Activate", AnimatorControllerParameterType.Trigger);
            sm = gc.layers[0].stateMachine;
            var g0 = AddState(sm, "Off", gOff, new Vector2(300, 0));
            var g1 = AddState(sm, "Ready", gReady, new Vector2(300, 120));
            var g2 = AddState(sm, "Active", gActive, new Vector2(560, 60));
            sm.defaultState = g0;
            t = T(g0, g1); t.AddCondition(AnimatorConditionMode.If, 0, "Ready");
            t = T(g1, g0); t.AddCondition(AnimatorConditionMode.IfNot, 0, "Ready");
            t = AnyT(sm, g2); t.AddCondition(AnimatorConditionMode.If, 0, "Activate");
            c.goal = gc;

            // transformador sempre aceso (cena final)
            var lit = MakeController("Goal_Lit");
            AddState(lit.layers[0].stateMachine, "Active", gActive, new Vector2(300, 0));
            c.goalLit = lit;

            AssetDatabase.SaveAssets();
            return c;
        }

        // =========================================================== prefabs
        class Prefabs
        {
            public GameObject player, enemy, cell, spikes, arc, checkpoint, goal, platform, spark, dust;
        }

        static SpriteRenderer AddSprite(GameObject go, Sprite sprite, int order)
        {
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        static Animator AddAnimator(GameObject go, RuntimeAnimatorController ctrl)
        {
            var an = go.AddComponent<Animator>();
            an.runtimeAnimatorController = ctrl;
            return an;
        }

        static GameObject SavePrefab(GameObject go)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabsDir + "/" + go.name + ".prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        static T Box<T>(GameObject go, Vector2 size, Vector2 offset, bool trigger) where T : Collider2D
        {
            var col = go.AddComponent<T>();
            col.isTrigger = trigger;
            col.offset = offset;
            var box = col as BoxCollider2D;
            if (box != null) box.size = size;
            return col;
        }

        static Prefabs BuildPrefabs(Ctrls ctrls, Mats mats)
        {
            var p = new Prefabs();

            // ---------- Faísca
            var go = new GameObject("Player");
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3.5f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
            var cap = go.AddComponent<CapsuleCollider2D>();
            cap.direction = CapsuleDirection2D.Vertical;
            cap.size = new Vector2(0.58f, 0.62f);
            cap.offset = new Vector2(0f, -0.125f);
            cap.sharedMaterial = mats.noFriction;
            var vis = new GameObject("Visual");
            vis.transform.SetParent(go.transform, false);
            var sr = AddSprite(vis, S("Sprites/Player/player_idle_0.png"), 20);
            var an = AddAnimator(vis, ctrls.player);
            var pc = go.AddComponent<PlayerController>();
            pc.visual = vis.transform;
            pc.spriteRenderer = sr;
            pc.animator = an;
            p.player = SavePrefab(go);

            // ---------- Curto
            go = new GameObject("Curto");
            rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            var body = Box<BoxCollider2D>(go, new Vector2(0.75f, 0.6f), new Vector2(0f, -0.19f), false);
            body.sharedMaterial = mats.noFriction;
            var hit = Box<BoxCollider2D>(go, new Vector2(0.8f, 0.62f), new Vector2(0f, -0.18f), true);
            sr = AddSprite(go, S("Sprites/Enemy/curto_walk_0.png"), 10);
            an = AddAnimator(go, ctrls.enemy);
            var ep = go.AddComponent<EnemyPatrol>();
            ep.bodyCollider = body;
            ep.hitbox = hit;
            ep.spriteRenderer = sr;
            ep.animator = an;
            p.enemy = SavePrefab(go);

            // ---------- Célula de energia
            go = new GameObject("EnergyCell");
            var circle = go.AddComponent<CircleCollider2D>();
            circle.isTrigger = true;
            circle.radius = 0.38f;
            vis = new GameObject("Visual");
            vis.transform.SetParent(go.transform, false);
            AddSprite(vis, S("Sprites/Items/cell_0.png"), 8);
            AddAnimator(vis, ctrls.cell);
            go.AddComponent<Collectible>().visual = vis.transform;
            p.cell = SavePrefab(go);

            // ---------- Sucata (espinhos)
            go = new GameObject("Spikes");
            AddSprite(go, S("Sprites/Hazards/spikes.png"), 5);
            Box<BoxCollider2D>(go, new Vector2(0.8f, 0.42f), new Vector2(0f, -0.28f), true);
            go.AddComponent<Hazard>().source = DamageSource.Spikes;
            p.spikes = SavePrefab(go);

            // ---------- Arco elétrico
            go = new GameObject("ElectricArc");
            sr = AddSprite(go, S("Sprites/Hazards/arc_off_0.png"), 6);
            an = AddAnimator(go, ctrls.arc);
            var arcCol = Box<BoxCollider2D>(go, new Vector2(0.4f, 1f), Vector2.zero, true);
            var arc = go.AddComponent<ArcHazard>();
            arc.damageCollider = arcCol;
            arc.animator = an;
            arc.spriteRenderer = sr;
            p.arc = SavePrefab(go);

            // ---------- Checkpoint
            go = new GameObject("Checkpoint");
            AddSprite(go, S("Sprites/Items/checkpoint_off_0.png"), 4);
            an = AddAnimator(go, ctrls.checkpoint);
            Box<BoxCollider2D>(go, new Vector2(0.8f, 1.6f), new Vector2(0f, 0.3f), true);
            go.AddComponent<Checkpoint>().animator = an;
            p.checkpoint = SavePrefab(go);

            // ---------- Transformador
            go = new GameObject("Transformer");
            AddSprite(go, S("Sprites/Items/goal_off_0.png"), 4);
            an = AddAnimator(go, ctrls.goal);
            Box<BoxCollider2D>(go, new Vector2(1.5f, 1.8f), Vector2.zero, true);
            go.AddComponent<Goal>().animator = an;
            p.goal = SavePrefab(go);

            // ---------- Plataforma móvel
            go = new GameObject("MovingPlatform");
            AddSprite(go, S("Sprites/Tiles/moving_platform.png"), 3);
            rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            var plat = Box<BoxCollider2D>(go, new Vector2(3f, 0.4f), new Vector2(0f, 0.04f), false);
            plat.sharedMaterial = mats.noFriction;
            go.AddComponent<MovingPlatform>();
            p.platform = SavePrefab(go);

            // ---------- Efeitos de partículas
            p.spark = BuildParticles("FX_SparkBurst", mats.spark, 16, new Vector2(2f, 6f), new Vector2(0.25f, 0.55f),
                0.35f, 0.4f, new Color32(255, 243, 163, 255), new Color32(155, 231, 255, 255));
            p.dust = BuildParticles("FX_Dust", mats.dust, 6, new Vector2(0.5f, 1.5f), new Vector2(0.2f, 0.4f),
                0.3f, -0.1f, new Color32(154, 165, 171, 255), new Color32(92, 107, 115, 255));
            return p;
        }

        static GameObject BuildParticles(string name, Material mat, int count, Vector2 speed, Vector2 life,
            float size, float gravity, Color a, Color b)
        {
            var go = new GameObject(name);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = 0.6f;
            main.loop = false;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life.x, life.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
            main.startColor = new ParticleSystem.MinMaxGradient(a, b);
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.15f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);
            var sizeOver = ps.sizeOverLifetime;
            sizeOver.enabled = true;
            sizeOver.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));
            var rend = go.GetComponent<ParticleSystemRenderer>();
            rend.sharedMaterial = mat;
            rend.sortingOrder = 30;
            return SavePrefab(go);
        }

        // ================================================== ScriptableObjects
        static void BuildConfigAssets(Prefabs p, Mats mats)
        {
            var cfg = ScriptableObject.CreateInstance<GameConfig>();
            cfg.levels = new[]
            {
                AssetDatabase.LoadAssetAtPath<TextAsset>(ResDir + "/Levels/fase1.txt"),
                AssetDatabase.LoadAssetAtPath<TextAsset>(ResDir + "/Levels/fase2.txt"),
                AssetDatabase.LoadAssetAtPath<TextAsset>(ResDir + "/Levels/fase3.txt"),
            };
            cfg.groundByMask = new Sprite[8];
            for (int m = 0; m < 8; m++) cfg.groundByMask[m] = S("Sprites/Tiles/ground_" + m + ".png");
            cfg.metal = S("Sprites/Tiles/metal.png");
            cfg.insulator = S("Sprites/Tiles/insulator.png");
            cfg.oneWay = S("Sprites/Tiles/oneway.png");
            cfg.playerPrefab = p.player;
            cfg.enemyPrefab = p.enemy;
            cfg.cellPrefab = p.cell;
            cfg.spikesPrefab = p.spikes;
            cfg.arcPrefab = p.arc;
            cfg.checkpointPrefab = p.checkpoint;
            cfg.goalPrefab = p.goal;
            cfg.movingPlatformPrefab = p.platform;
            cfg.sparkBurstPrefab = p.spark;
            cfg.dustPrefab = p.dust;
            cfg.noFriction = mats.noFriction;
            CreateOrReplace(cfg, ResDir + "/GameConfig.asset");

            var lib = ScriptableObject.CreateInstance<AudioLibrary>();
            lib.musicMenu = Clip("Music/music_menu.wav");
            lib.musicGame = Clip("Music/music_game.wav");
            lib.ambience = Clip("Music/ambience_substation.wav");
            lib.jump = Clip("SFX/sfx_jump.wav");
            lib.land = Clip("SFX/sfx_land.wav");
            lib.dash = Clip("SFX/sfx_dash.wav");
            lib.collect = Clip("SFX/sfx_collect.wav");
            lib.stomp = Clip("SFX/sfx_stomp.wav");
            lib.hurt = Clip("SFX/sfx_hurt.wav");
            lib.checkpoint = Clip("SFX/sfx_checkpoint.wav");
            lib.goal = Clip("SFX/sfx_goal.wav");
            lib.levelComplete = Clip("SFX/sfx_level_complete.wav");
            lib.gameOver = Clip("SFX/sfx_game_over.wav");
            lib.victory = Clip("SFX/sfx_victory.wav");
            lib.uiClick = Clip("SFX/sfx_ui_click.wav");
            lib.uiHover = Clip("SFX/sfx_ui_hover.wav");
            lib.denied = Clip("SFX/sfx_denied.wav");
            lib.arc = Clip("SFX/sfx_arc.wav");
            CreateOrReplace(lib, ResDir + "/AudioLibrary.asset");
        }

        // ============================================================ cenas
        static Camera MakeCamera()
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 7.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Navy;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;
            go.transform.position = new Vector3(0f, 0f, -10f);
            go.AddComponent<AudioListener>();
            return cam;
        }

        static void MakeBackground(Camera cam, string skySprite, bool withLayers, float scrollTowers, float scrollStation)
        {
            var sky = new GameObject("Sky");
            sky.transform.SetParent(cam.transform, false);
            sky.transform.localPosition = new Vector3(0f, 0f, 20f);
            sky.transform.localScale = Vector3.one * 1.8f;
            AddSprite(sky, S("Backgrounds/" + skySprite), -100);
            if (!withLayers) return;
            ParallaxLayer(cam, "BG_Towers", "bg_towers.png", -90, 0.85f, 0.7f, new Vector2(0f, -1.5f), scrollTowers);
            ParallaxLayer(cam, "BG_Station", "bg_station.png", -80, 0.6f, 0.45f, new Vector2(0f, -2.9f), scrollStation);
        }

        static void ParallaxLayer(Camera cam, string name, string sprite, int order, float fx, float fy, Vector2 offset, float scroll)
        {
            var go = new GameObject(name);
            var sr = AddSprite(go, S("Backgrounds/" + sprite), order);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(400f, 11.25f);
            var p = go.AddComponent<Parallax>();
            p.cameraTransform = cam.transform;
            p.factorX = fx;
            p.factorY = fy;
            p.offsetFromCamera = offset;
            p.autoScroll = scroll;
            p.wrapWidth = 20f;
            go.transform.position = cam.transform.position + new Vector3(offset.x, offset.y, 10f);
        }

        static GameObject DisplaySprite(string name, RuntimeAnimatorController ctrl, Sprite sprite, Vector3 pos, float scale, bool flip)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * scale;
            var sr = AddSprite(go, sprite, 10);
            sr.flipX = flip;
            if (ctrl != null) AddAnimator(go, ctrl);
            return go;
        }

        // ---------------------------------------------------------------- UI --
        static Canvas MakeCanvas(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        static void MakeEventSystem()
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        static RectTransform Stretch(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        static Text Label(Transform parent, string name, string content, int size, TextAnchor align, Color color,
            bool bold, Vector2 anchor, Vector2 pos, Vector2 box)
        {
            var rt = Rect(name, parent, anchor, pos, box);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = bold ? fontBold : fontRegular;
            t.fontSize = size;
            t.alignment = align;
            t.color = color;
            t.text = content;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.lineSpacing = 1.1f;
            t.raycastTarget = false;
            var outline = rt.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.03f, 0.02f, 0.08f, 0.9f);
            outline.effectDistance = new Vector2(2f, -2f);
            return t;
        }

        static Image Img(Transform parent, string name, Sprite sprite, Color color, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var rt = Rect(name, parent, anchor, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        static Button Btn(Transform parent, string name, string label, Vector2 pos, Vector2 size)
        {
            var rt = Rect(name, parent, new Vector2(0.5f, 0.5f), pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = S("UI/button.png");
            img.type = Image.Type.Sliced;
            var btn = rt.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = new Color32(70, 96, 170, 255);
            colors.highlightedColor = new Color32(63, 167, 214, 255);
            colors.selectedColor = new Color32(63, 167, 214, 255);
            colors.pressedColor = new Color32(255, 210, 63, 255);
            colors.disabledColor = new Color32(90, 90, 100, 200);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.05f;
            btn.colors = colors;
            btn.targetGraphic = img;
            var text = Label(rt, "Label", label, 26, TextAnchor.MiddleCenter, Color.white, true,
                new Vector2(0.5f, 0.5f), Vector2.zero, size);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            rt.gameObject.AddComponent<UIButtonSounds>();
            return btn;
        }

        static RectTransform Panel(Transform parent, string name, Vector2 size, bool dim)
        {
            var root = Stretch(name, parent);
            if (dim)
            {
                var bg = root.gameObject.AddComponent<Image>();
                bg.color = new Color(0.02f, 0.01f, 0.06f, 0.6f);
            }
            var box = Rect("Box", root, new Vector2(0.5f, 0.5f), Vector2.zero, size);
            var img = box.gameObject.AddComponent<Image>();
            img.sprite = S("UI/panel.png");
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            return box;
        }

        static Slider MakeSlider(Transform parent, string name, Vector2 pos)
        {
            var go = DefaultControls.CreateSlider(new DefaultControls.Resources());
            go.name = name;
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(360f, 24f);
            var slider = go.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            var bg = go.transform.Find("Background");
            if (bg != null) bg.GetComponent<Image>().color = new Color32(27, 31, 59, 255);
            if (slider.fillRect != null) slider.fillRect.GetComponent<Image>().color = new Color32(63, 167, 214, 255);
            if (slider.handleRect != null)
            {
                slider.handleRect.GetComponent<Image>().color = new Color32(255, 210, 63, 255);
                slider.handleRect.sizeDelta = new Vector2(24f, 0f);
            }
            var colors = slider.colors;
            colors.highlightedColor = new Color32(255, 243, 163, 255);
            colors.selectedColor = new Color32(255, 243, 163, 255);
            slider.colors = colors;
            go.AddComponent<UIButtonSounds>();
            return slider;
        }

        static void SaveScene(UnityEngine.SceneManagement.Scene scene, string name)
        {
            EditorSceneManager.SaveScene(scene, ScenePath(name));
        }

        // --------------------------------------------------------- MainMenu --
        static void BuildMenuScene(Ctrls ctrls)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = MakeCamera();
            MakeBackground(cam, "bg_sky.png", true, 0.4f, 0.9f);
            DisplaySprite("Faisca (menu)", ctrls.player, S("Sprites/Player/player_idle_0.png"), new Vector3(-8.5f, -2f, 0f), 5f, false);
            DisplaySprite("Curto (menu)", ctrls.enemy, S("Sprites/Enemy/curto_walk_0.png"), new Vector3(9f, -4.6f, 0f), 3f, true);

            var canvas = MakeCanvas("Canvas");
            var root = canvas.transform;
            var logo = Img(root, "Logo", S("UI/logo.png"), Color.white, new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(678f, 216f));
            logo.preserveAspect = true;
            Label(root, "Subtitle", "Religue a Subestação Vale Verde", 26, TextAnchor.MiddleCenter, Cyan, false,
                new Vector2(0.5f, 1f), new Vector2(0f, -262f), new Vector2(900f, 40f));

            var menu = root.gameObject.AddComponent<MainMenu>();

            // painel principal
            var main = Stretch("MainPanel", root);
            var bsize = new Vector2(280f, 52f);
            menu.playButton = Btn(main, "Btn_Jogar", "Jogar", new Vector2(0f, 0f), bsize);
            menu.howToButton = Btn(main, "Btn_ComoJogar", "Como jogar", new Vector2(0f, -64f), bsize);
            menu.optionsButton = Btn(main, "Btn_Opcoes", "Opções", new Vector2(0f, -128f), bsize);
            menu.creditsButton = Btn(main, "Btn_Creditos", "Créditos", new Vector2(0f, -192f), bsize);
            menu.quitButton = Btn(main, "Btn_Sair", "Sair", new Vector2(0f, -256f), bsize);
            menu.mainPanel = main.gameObject;

            // como jogar
            var how = Panel(root, "HowToPanel", new Vector2(980f, 560f), true);
            Label(how, "Title", "Como jogar", 40, TextAnchor.MiddleCenter, Yellow, true, new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(900f, 60f));
            Label(how, "Body",
                "<b>OBJETIVO</b>\nColete <color=#7BD389>células de energia</color> e leve-as ao <b>transformador</b> no fim de cada fase. " +
                "A meta de células aparece no topo da tela.\n\n" +
                "<b>CONTROLES</b>\nMover: A / D ou setas  ·  Pular: Espaço, W ou seta para cima (segure para ir mais alto)\n" +
                "<color=#9BE7FF>Pulso</color>: Shift, X ou J  ·  Pausar: Esc ou P\n" +
                "Controle: analógico, A (pular), X (Pulso), Start (pausa)\n\n" +
                "<b>PERIGOS</b>\nSucata pontiaguda, <color=#9BE7FF>arcos elétricos</color> ligados, Curtos e buracos tiram 1 carga.\n" +
                "Pule em cima dos Curtos para desligá-los. Durante o Pulso você atravessa arcos e derruba Curtos.",
                22, TextAnchor.UpperLeft, TextColor, false, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(880f, 380f));
            var backHow = Btn(how, "Btn_Voltar", "Voltar", new Vector2(0f, -230f), new Vector2(220f, 48f));
            menu.howToPanel = how.parent.gameObject;

            // opções
            var opt = Panel(root, "OptionsPanel", new Vector2(640f, 400f), true);
            Label(opt, "Title", "Opções", 40, TextAnchor.MiddleCenter, Yellow, true, new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(600f, 60f));
            Label(opt, "MusicLabel", "Música", 26, TextAnchor.MiddleCenter, TextColor, true, new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), new Vector2(400f, 36f));
            menu.musicSlider = MakeSlider(opt, "MusicSlider", new Vector2(0f, 30f));
            Label(opt, "SfxLabel", "Efeitos sonoros", 26, TextAnchor.MiddleCenter, TextColor, true, new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(400f, 36f));
            menu.sfxSlider = MakeSlider(opt, "SfxSlider", new Vector2(0f, -70f));
            var backOpt = Btn(opt, "Btn_Voltar", "Voltar", new Vector2(0f, -150f), new Vector2(220f, 48f));
            menu.optionsPanel = opt.parent.gameObject;

            // créditos
            var cred = Panel(root, "CreditsPanel", new Vector2(980f, 560f), true);
            Label(cred, "Title", "Créditos", 40, TextAnchor.MiddleCenter, Yellow, true, new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(900f, 60f));
            Label(cred, "Body",
                "<b>FAÍSCA</b> — projeto-faisca de jogo 2D em Unity e C#\n\n" +
                "Game design, programação, arte, animação, level design e áudio:\n" +
                "<color=#FFD23F>Equipe Faísca</color> Larissa Campos Cardoso\n\n" +
                "Sprites, cenários, músicas e efeitos produzidos pela equipe\n(ferramentas na pasta Tools/ do repositório).\n\n" +
                "<b>Recursos externos</b>\n" +
                "Fonte Pixelify Sans — The Pixelify Sans Project Authors — SIL Open Font License 1.1\n" +
                "Motor: Unity — Unity Technologies\n\n" +
                "Obrigado por jogar!",
                22, TextAnchor.UpperCenter, TextColor, false, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(880f, 380f));
            var backCred = Btn(cred, "Btn_Voltar", "Voltar", new Vector2(0f, -230f), new Vector2(220f, 48f));
            menu.creditsPanel = cred.parent.gameObject;

            menu.backButtons = new[] { backHow, backOpt, backCred };
            menu.bestTimeText = Label(root, "BestTime", "Melhor tempo: --:--", 20, TextAnchor.LowerLeft, Yellow, true,
                new Vector2(0f, 0f), new Vector2(220f, 30f), new Vector2(400f, 30f));
            Label(root, "Version", "v1.0 · projeto-faisca", 18, TextAnchor.LowerRight, TextColor, false,
                new Vector2(1f, 0f), new Vector2(-170f, 30f), new Vector2(300f, 30f));

            how.parent.gameObject.SetActive(false);
            opt.parent.gameObject.SetActive(false);
            cred.parent.gameObject.SetActive(false);
            MakeEventSystem();
            SaveScene(scene, "MainMenu");
        }

        // ------------------------------------------------------------- Game --
        static void BuildGameScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = MakeCamera();
            var follow = cam.gameObject.AddComponent<CameraFollow>();
            MakeBackground(cam, "bg_sky.png", true, 0f, 0f);

            var gmGo = new GameObject("GameManager");
            var loader = gmGo.AddComponent<LevelLoader>();
            var gm = gmGo.AddComponent<GameManager>();
            gm.levelLoader = loader;
            gm.cameraFollow = follow;

            var canvas = MakeCanvas("HUD");
            var root = canvas.transform;
            var hud = root.gameObject.AddComponent<HUD>();

            // barra superior
            var bar = Rect("TopBar", root, new Vector2(0.5f, 1f), new Vector2(0f, -32f), new Vector2(0f, 64f));
            bar.anchorMin = new Vector2(0f, 1f);
            bar.anchorMax = new Vector2(1f, 1f);
            bar.sizeDelta = new Vector2(0f, 64f);
            var barImg = bar.gameObject.AddComponent<Image>();
            barImg.color = new Color(0.02f, 0.01f, 0.08f, 0.55f);
            barImg.raycastTarget = false;

            var tl = new Vector2(0f, 1f);
            Img(root, "IconLives", S("UI/icon_life.png"), Color.white, tl, new Vector2(42f, -32f), new Vector2(44f, 44f));
            hud.livesText = Label(root, "LivesText", "x5", 30, TextAnchor.MiddleLeft, Yellow, true, tl, new Vector2(120f, -32f), new Vector2(100f, 40f));
            Img(root, "IconCells", S("UI/icon_cell.png"), Color.white, tl, new Vector2(200f, -32f), new Vector2(44f, 44f));
            hud.cellsText = Label(root, "CellsText", "0/0", 30, TextAnchor.MiddleLeft, Yellow, true, tl, new Vector2(282f, -32f), new Vector2(110f, 40f));
            hud.dashIcon = Img(root, "IconDash", S("UI/icon_dash.png"), Color.white, tl, new Vector2(370f, -32f), new Vector2(44f, 44f));
            hud.levelText = Label(root, "LevelText", "Fase", 24, TextAnchor.MiddleCenter, TextColor, true,
                new Vector2(0.5f, 1f), new Vector2(0f, -32f), new Vector2(560f, 40f));
            hud.timerText = Label(root, "TimerText", "00:00", 30, TextAnchor.MiddleRight, TextColor, true,
                new Vector2(1f, 1f), new Vector2(-90f, -32f), new Vector2(150f, 40f));

            // mensagens
            var msg = Rect("Message", root, new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(1100f, 60f));
            hud.messageGroup = msg.gameObject.AddComponent<CanvasGroup>();
            hud.messageGroup.alpha = 0f;
            hud.messageGroup.blocksRaycasts = false;
            hud.messageText = Label(msg, "MessageText", "", 30, TextAnchor.MiddleCenter, Yellow, true,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 60f));

            // pausa
            var pause = Panel(root, "PausePanel", new Vector2(520f, 340f), true);
            Label(pause, "Title", "Pausado", 44, TextAnchor.MiddleCenter, Yellow, true, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(480f, 60f));
            hud.resumeButton = Btn(pause, "Btn_Continuar", "Continuar", new Vector2(0f, 0f), new Vector2(280f, 52f));
            hud.pauseMenuButton = Btn(pause, "Btn_Menu", "Menu principal", new Vector2(0f, -70f), new Vector2(280f, 52f));
            Label(pause, "Hint", "Esc / P para voltar ao jogo", 18, TextAnchor.MiddleCenter, Cyan, false,
                new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(480f, 30f));
            hud.pausePanel = pause.parent.gameObject;

            // derrota
            var over = Panel(root, "GameOverPanel", new Vector2(600f, 400f), true);
            Label(over, "Title", "Sem energia!", 48, TextAnchor.MiddleCenter, new Color32(255, 107, 107, 255), true,
                new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(560f, 60f));
            hud.gameOverStats = Label(over, "Stats", "", 24, TextAnchor.MiddleCenter, TextColor, false,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 50f), new Vector2(540f, 80f));
            hud.retryButton = Btn(over, "Btn_TentarDeNovo", "Tentar de novo", new Vector2(0f, -50f), new Vector2(300f, 52f));
            hud.gameOverMenuButton = Btn(over, "Btn_Menu", "Menu principal", new Vector2(0f, -120f), new Vector2(300f, 52f));
            hud.gameOverPanel = over.parent.gameObject;

            // fase concluída
            var done = Panel(root, "LevelCompletePanel", new Vector2(600f, 420f), true);
            Label(done, "Title", "Fase concluída!", 48, TextAnchor.MiddleCenter, Green, true,
                new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(560f, 60f));
            hud.levelCompleteStats = Label(done, "Stats", "", 26, TextAnchor.MiddleCenter, TextColor, false,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(540f, 120f));
            hud.continueButton = Btn(done, "Btn_Continuar", "Continuar", new Vector2(0f, -110f), new Vector2(280f, 52f));
            hud.levelCompletePanel = done.parent.gameObject;

            pause.parent.gameObject.SetActive(false);
            over.parent.gameObject.SetActive(false);
            done.parent.gameObject.SetActive(false);
            MakeEventSystem();
            SaveScene(scene, "Game");
        }

        // ----------------------------------------------------------- Ending --
        static void BuildEndingScene(Ctrls ctrls)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = MakeCamera();
            MakeBackground(cam, "bg_city_lit.png", false, 0f, 0f);
            DisplaySprite("Faisca (final)", ctrls.player, S("Sprites/Player/player_idle_0.png"), new Vector3(-9f, -3.5f, 0f), 5f, false);
            DisplaySprite("Transformador (final)", ctrls.goalLit, S("Sprites/Items/goal_active_0.png"), new Vector3(9f, -3.2f, 0f), 3f, false);

            var canvas = MakeCanvas("Canvas");
            var root = canvas.transform;
            var end = root.gameObject.AddComponent<EndingScreen>();
            Label(root, "Title", "A luz voltou!", 72, TextAnchor.MiddleCenter, Yellow, true,
                new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(1000f, 100f));
            Label(root, "Subtitle", "A cidade está iluminada graças à Faísca.", 26, TextAnchor.MiddleCenter, TextColor, false,
                new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(1000f, 40f));
            end.statsText = Label(root, "Stats", "", 30, TextAnchor.MiddleCenter, TextColor, true,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(700f, 140f));
            end.recordText = Label(root, "Record", "", 32, TextAnchor.MiddleCenter, Green, true,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -90f), new Vector2(700f, 50f));
            end.playAgainButton = Btn(root, "Btn_JogarNovamente", "Jogar novamente", new Vector2(-170f, -220f), new Vector2(300f, 52f));
            end.menuButton = Btn(root, "Btn_Menu", "Menu", new Vector2(170f, -220f), new Vector2(300f, 52f));
            MakeEventSystem();
            SaveScene(scene, "Ending");
        }

        // ======================================================== build/player
        static void ConfigurePlayerSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath("MainMenu"), true),
                new EditorBuildSettingsScene(ScenePath("Game"), true),
                new EditorBuildSettingsScene(ScenePath("Ending"), true),
            };
            PlayerSettings.productName = "Faísca";
            PlayerSettings.companyName = "Equipe Faisca";
            PlayerSettings.bundleVersion = "1.0";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = false;
        }

        static void Build(BuildTarget target, string path)
        {
            if (!File.Exists(ScenePath("MainMenu"))) Generate();
            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = path,
                target = target,
                options = BuildOptions.None,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log("[Faísca] Build concluída: " + path + " (" + (report.summary.totalSize / (1024 * 1024)) + " MB)");
                if (!Application.isBatchMode) EditorUtility.RevealInFinder(path);
            }
            else
            {
                Debug.LogError("[Faísca] Build falhou: " + report.summary.result +
                               ". Verifique se o módulo de build para " + target + " está instalado no Unity Hub.");
            }
        }
    }
}
