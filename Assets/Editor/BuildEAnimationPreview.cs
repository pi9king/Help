using System;
using System.Collections.Generic;
using System.IO;
using Help.Animation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Help.EditorTools
{
    public static class BuildEAnimationPreview
    {
        private const string SpriteDir = "Assets/Sprites/E_Character";
        private const string AnimationDir = "Assets/Animations/E_Character";
        private const string ScenePath = "Assets/Scenes/EAnimationPreview.unity";

        private struct ActionSpec
        {
            public string Name;
            public int Columns;
            public int Rows;
            public int Fps;
            public bool Loop;
            public bool Directional;

            public ActionSpec(string name, int columns, int rows, int fps, bool loop, bool directional)
            {
                Name = name;
                Columns = columns;
                Rows = rows;
                Fps = fps;
                Loop = loop;
                Directional = directional;
            }
        }

        private static readonly string[] Directions = { "Down", "Left", "Right", "Up" };
        private static readonly ActionSpec[] Actions =
        {
            new ActionSpec("Idle", 4, 4, 7, true, true),
            new ActionSpec("Walk", 6, 4, 11, true, true),
            new ActionSpec("Attack", 6, 4, 12, false, true),
            new ActionSpec("Hit", 3, 4, 11, false, true),
            new ActionSpec("Death", 8, 1, 9, false, false),
            new ActionSpec("Skill", 6, 1, 11, false, false)
        };

        [MenuItem("Tools/E Character/Build Animation Preview")]
        public static void Build()
        {
            Directory.CreateDirectory(AnimationDir);
            var clips = new List<AnimationClip>();

            // Reimport after the importer compiles, then verify every expected slice.
            ImportAndCheck("Master", 4, 1);
            ImportAndCheck("Equipment", 7, 1);
            foreach (var action in Actions) ImportAndCheck(action.Name, action.Columns, action.Rows);

            foreach (var action in Actions)
            {
                var sprites = LoadSprites(action.Name, action.Columns, action.Rows);
                int directionCount = action.Directional ? 4 : 1;
                for (int direction = 0; direction < directionCount; direction++)
                {
                    string clipName = action.Name + "_" + Directions[direction];
                    string clipPath = AnimationDir + "/E_" + clipName + ".anim";
                    var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                    if (clip == null)
                    {
                        clip = new AnimationClip { name = "E_" + clipName };
                        AssetDatabase.CreateAsset(clip, clipPath);
                    }
                    SetFrames(clip, sprites, direction, action.Columns, action.Fps, action.Loop);
                    clips.Add(clip);
                }
            }

            string controllerPath = AnimationDir + "/E_Character.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            if (controller.layers.Length == 0) controller.AddLayer("Base Layer");
            var stateMachine = controller.layers[0].stateMachine;
            foreach (var state in stateMachine.states) stateMachine.RemoveState(state.state);
            foreach (var clip in clips)
            {
                var state = stateMachine.AddState(clip.name.Substring(2));
                state.motion = clip;
                if (state.name == "Idle_Down") stateMachine.defaultState = state;
            }

            BuildScene(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("E animation preview built: 18 clips, controller, scene, 101 imported sprite cells.");
        }

        private static void ImportAndCheck(string name, int columns, int rows)
        {
            string path = SpriteDir + "/E_" + name + ".png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || importer.spriteImportMode != SpriteImportMode.Multiple ||
                importer.spritePixelsPerUnit != PixelImport.PixelsPerUnit || importer.filterMode != FilterMode.Point)
                throw new InvalidOperationException("Invalid import settings: " + path);
            LoadSprites(name, columns, rows);
        }

        private static Sprite[,] LoadSprites(string name, int columns, int rows)
        {
            string path = SpriteDir + "/E_" + name + ".png";
            var found = new Dictionary<string, Sprite>();
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                var sprite = asset as Sprite;
                if (sprite != null) found[sprite.name] = sprite;
            }
            if (found.Count != columns * rows)
                throw new InvalidOperationException(path + ": expected " + (columns * rows) + " sprites, found " + found.Count);

            var result = new Sprite[rows, columns];
            for (int row = 0; row < rows; row++)
            for (int column = 0; column < columns; column++)
            {
                string spriteName = "E_" + name + "_" + row + "_" + column;
                Sprite sprite;
                if (!found.TryGetValue(spriteName, out sprite))
                    throw new InvalidOperationException("Missing sprite " + spriteName + " in " + path);
                if (sprite.rect.width != 64 || sprite.rect.height != 64 ||
                    Mathf.Abs(sprite.pivot.x - 32f) > 0.01f || Mathf.Abs(sprite.pivot.y) > 0.01f)
                    throw new InvalidOperationException("Invalid size or pivot: " + spriteName);
                result[row, column] = sprite;
            }
            return result;
        }

        private static void SetFrames(AnimationClip clip, Sprite[,] sprites, int row, int columns, int fps, bool loop)
        {
            clip.frameRate = fps;
            clip.wrapMode = loop ? WrapMode.Loop : WrapMode.Once;
            // 스프라이트 커브는 마지막 키 뒤로 한 프레임을 더 보여 준다. 루프 클립에 첫 프레임을 한 번 더
            // 넣으면 경계에서 첫 프레임이 두 번 연속 나와 걸음이 멈칫한다. 루프가 아닐 때만 마지막
            // 프레임 유지 키를 붙인다.
            var keys = new ObjectReferenceKeyframe[loop ? columns : columns + 1];
            for (int frame = 0; frame < columns; frame++)
                keys[frame] = new ObjectReferenceKeyframe { time = (float)frame / fps, value = sprites[row, frame] };
            if (!loop)
                keys[columns] = new ObjectReferenceKeyframe { time = (float)columns / fps, value = sprites[row, columns - 1] };
            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
        }

        private static void BuildScene(AnimatorController controller)
        {
            var previous = SceneManager.GetActiveScene();
            bool replaceUntitled = string.IsNullOrEmpty(previous.path);
            if (replaceUntitled && previous.isDirty)
                throw new InvalidOperationException("Save the current untitled scene before building the E preview scene.");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                replaceUntitled ? NewSceneMode.Single : NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 4.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.11f, 0.12f, 0.15f);
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            var actor = new GameObject("E Character Preview");
            actor.transform.position = Vector3.zero;
            var renderer = actor.AddComponent<SpriteRenderer>();
            renderer.sprite = LoadSprites("Master", 4, 1)[0, 0];
            renderer.sortingOrder = 10;
            actor.AddComponent<Animator>().runtimeAnimatorController = controller;
            actor.AddComponent<EAnimationPreview>();

            // 애니메이션 관련 테스트는 이 씬에 모은다(사용자 결정 2026-10-05).
            // 팔 리그는 EAnimationPreview가 시작할 때 ArmRigPreview로 직접 붙인다(버튼으로 조작).
            // 그 텍스처 임포트 설정은 ArmRigTextureImport가 맞춘다. 설정이 생기기 전에 들어온 파일도 다시 읽힌다.
            if (AssetDatabase.IsValidFolder("Assets/Resources/ArmRig"))
                AssetDatabase.ImportAsset("Assets/Resources/ArmRig",
                    ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);

            EditorSceneManager.SaveScene(scene, ScenePath);
            if (!replaceUntitled)
            {
                EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }
    }
}
