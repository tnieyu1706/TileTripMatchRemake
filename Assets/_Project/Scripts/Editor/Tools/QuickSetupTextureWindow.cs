using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TnieYuPackage.Tools
{
    public class QuickSetupTextureWindow : EditorWindow
    {
        private class TextureItem
        {
            public Texture2D Texture;
            public int Width;
            public int Height;
            public int CurrentMaxSize;
            public float PixelUnit;
        }

        private readonly List<TextureItem> textures = new();

        private Vector2 scroll;
        private float elementSize = 32;

        private static readonly int[] UnitySizes =
        {
            32, 64, 128, 256, 512, 1024, 2048, 4096
        };

        private TextureImporterCompression compression = TextureImporterCompression.Compressed;

        private TextureResizeAlgorithm resizeAlgorithm = TextureResizeAlgorithm.Mitchell;

        private TextureImporterFormat format = TextureImporterFormat.Automatic;

        [MenuItem("Tools/TnieYu/Texture/Quick Setup Texture")]
        public static void Open()
        {
            GetWindow<QuickSetupTextureWindow>("Quick Setup Texture");
        }

        void OnGUI()
        {
            DrawTopSettings();
            DrawImportSettings();
            DrawDropArea();
            DrawList();
            DrawExecute();
        }

        void DrawTopSettings()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            elementSize = EditorGUILayout.Slider(
                "Element Size",
                elementSize,
                8,
                128
            );

            GUILayout.Space(10);

            GUILayout.Label($"Selected: {textures.Count}");

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Clear All", GUILayout.Width(90)))
            {
                textures.Clear();
            }

            EditorGUILayout.EndHorizontal();
        }

        void DrawImportSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            GUILayout.Label("Import Settings", EditorStyles.boldLabel);

            compression = (TextureImporterCompression)EditorGUILayout.EnumPopup(
                "Compression",
                compression
            );

            resizeAlgorithm = (TextureResizeAlgorithm)EditorGUILayout.EnumPopup(
                "Resize Algorithm",
                resizeAlgorithm
            );

            format = (TextureImporterFormat)EditorGUILayout.EnumPopup(
                "Format",
                format
            );

            EditorGUILayout.EndVertical();
        }

        void DrawDropArea()
        {
            Rect rect = GUILayoutUtility.GetRect(0, 60, GUILayout.ExpandWidth(true));

            GUI.Box(rect, "Drag & Drop Texture or Folder Here");

            Event evt = Event.current;

            if (!rect.Contains(evt.mousePosition))
                return;

            if (evt.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            }

            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();

                foreach (var obj in DragAndDrop.objectReferences)
                {
                    if (obj is Texture2D tex)
                    {
                        AddTexture(tex);
                    }
                    else if (obj is DefaultAsset folder)
                    {
                        AddFolder(folder);
                    }
                }
            }
        }

        void AddTexture(Texture2D tex)
        {
            if (tex == null)
                return;

            if (textures.Exists(t => t.Texture == tex))
                return;

            string path = AssetDatabase.GetAssetPath(tex);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer == null)
                return;

            textures.Add(new TextureItem
            {
                Texture = tex,
                Width = tex.width,
                Height = tex.height,
                CurrentMaxSize = importer.maxTextureSize,
                PixelUnit = importer.spritePixelsPerUnit
            });
        }

        void AddFolder(DefaultAsset folder)
        {
            string folderPath = AssetDatabase.GetAssetPath(folder);

            if (!AssetDatabase.IsValidFolder(folderPath))
                return;

            string fullPath = Path.Combine(Application.dataPath, folderPath.Replace("Assets/", ""));

            string[] files = Directory.GetFiles(fullPath);

            foreach (string file in files)
            {
                if (file.EndsWith(".meta"))
                    continue;

                if (!(file.EndsWith(".png") ||
                      file.EndsWith(".jpg") ||
                      file.EndsWith(".tga") ||
                      file.EndsWith(".psd") ||
                      file.EndsWith(".jpeg")))
                    continue;

                string assetPath = "Assets" + file.Replace(Application.dataPath, "").Replace("\\", "/");

                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);

                if (tex != null)
                {
                    AddTexture(tex);
                }
            }
        }

        void DrawList()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            for (int i = 0; i < textures.Count; i++)
            {
                var item = textures[i];

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                GUILayout.Label(
                    AssetPreview.GetAssetPreview(item.Texture),
                    GUILayout.Width(elementSize),
                    GUILayout.Height(elementSize)
                );

                GUILayout.Space(6);

                GUILayout.Label(
                    $"{item.Texture.name}: " +
                    $"w&h({item.Width} x {item.Height}) | " +
                    $"Max:{item.CurrentMaxSize} | " +
                    $"Pixel: {item.PixelUnit}",
                    GUILayout.ExpandWidth(true)
                );

                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    textures.RemoveAt(i);
                    i--;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        void DrawExecute()
        {
            GUILayout.Space(8);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Auto ReMaxSize", GUILayout.Height(35)))
            {
                ExecuteAutoMaxSize();
            }

            if (GUILayout.Button("Auto Pixels", GUILayout.Height(35)))
            {
                ExecuteAutoSetPixel();
            }

            if (GUILayout.Button("Apply Import Settings", GUILayout.Height(35)))
            {
                ApplyImportSettings();
            }

            EditorGUILayout.EndHorizontal();
        }

        void ExecuteAutoMaxSize()
        {
            foreach (var item in textures)
            {
                string path = AssetDatabase.GetAssetPath(item.Texture);

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;

                if (importer == null)
                    continue;

                int size = Mathf.Max(item.Width, item.Height);

                int targetSize = GetUnitySize(size);

                if (importer.maxTextureSize != targetSize)
                {
                    importer.maxTextureSize = targetSize;
                    importer.SaveAndReimport();
                }

                item.CurrentMaxSize = importer.maxTextureSize;
            }
        }

        void ExecuteAutoSetPixel()
        {
            foreach (var item in textures)
            {
                string path = AssetDatabase.GetAssetPath(item.Texture);

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;

                if (importer == null)
                    continue;

                // Only Sprite
                if (importer.textureType != TextureImporterType.Sprite)
                    continue;

                int size = Mathf.Max(item.Width, item.Height);

                if (importer.spritePixelsPerUnit != size)
                {
                    importer.spritePixelsPerUnit = size;
                    importer.SaveAndReimport();
                }
            }
        }

        void ApplyImportSettings()
        {
            foreach (var item in textures)
            {
                string path = AssetDatabase.GetAssetPath(item.Texture);

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;

                if (importer == null)
                    continue;

                importer.textureCompression = compression;

                // Resize Algorithm (internal serialized property)
                var so = new SerializedObject(importer);
                var resizeProp = so.FindProperty("m_ResizeAlgorithm");

                if (resizeProp != null)
                {
                    resizeProp.intValue = (int)resizeAlgorithm;
                }

                so.ApplyModifiedPropertiesWithoutUndo();

                // Format
                var platform = importer.GetDefaultPlatformTextureSettings();

                if (format != TextureImporterFormat.Automatic)
                {
                    platform.format = format;
                }

                importer.SetPlatformTextureSettings(platform);

                importer.SaveAndReimport();
            }
        }

        int GetUnitySize(int size)
        {
            foreach (var s in UnitySizes)
            {
                if (s >= size)
                    return s;
            }

            return 4096;
        }
    }
}