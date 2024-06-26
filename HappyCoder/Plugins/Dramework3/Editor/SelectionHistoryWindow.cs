﻿using System;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEditor.SceneManagement;

using UnityEngine;
using UnityEngine.SceneManagement;

using Object = UnityEngine.Object;


namespace IG.HappyCoder.Dramework3.Editor
{
    [Serializable]
    public class RecentSelection
    {
        #region ================================ FIELDS

        public string Path;
        public bool Pinned;
        public bool Scene;

        #endregion
    }

    public class SelectionHistoryWindow : EditorWindow
    {
        #region ================================ FIELDS

        private const int MaxHistory = 50;

        [SerializeField]
        private List<RecentSelection> values = new List<RecentSelection>();

        private Vector2 scrollPos;

        #endregion

        #region ================================ PROPERTIES AND INDEXERS

        private static SelectionHistoryWindow Instance { get; set; }

        #endregion

        #region ================================ METHODS

        private static GameObject GetGameObjectAtPath(string path)
        {
            var names = path.Split("/");

            var currentPrefabStage = PrefabStageUtility.GetCurrentPrefabStage();

            var rootGameObjects = currentPrefabStage != null
                ? new[] { currentPrefabStage.prefabContentsRoot }
                : SceneManager.GetActiveScene().GetRootGameObjects();

            var root = rootGameObjects.FirstOrDefault(o => o.name == names[0]);
            if (root == null)
                return null;

            var obj = root;

            for (var i = 1; i < names.Length; i++)
            {
                var found = false;
                for (var j = 0; j < obj.transform.childCount; j++)
                {
                    var child = obj.transform.GetChild(j);
                    if (child.name == names[i])
                    {
                        obj = child.gameObject;
                        found = true;
                        break;
                    }
                }

                if (!found)
                    return null;
            }

            return obj;
        }

        private static string GetGameObjectPath(GameObject obj)
        {
            var path = obj.name;

            var currentPrefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            var prefabRoot = currentPrefabStage != null ? currentPrefabStage.prefabContentsRoot : null;

            while (obj.transform.parent != null)
            {
                if (prefabRoot && obj == prefabRoot)
                    break;
                obj = obj.transform.parent.gameObject;
                path = obj.name + "/" + path;
            }

            return path;
        }

        [MenuItem("Window/Dramework 3/Selection History")]
        private static void ShowWindow()
        {
            var window = GetWindow<SelectionHistoryWindow>();
            window.titleContent = new GUIContent("Selection History");
            window.Show();
        }

        private void AddToList(RecentSelection selection)
        {
            var index = values.FindIndex(s => s.Scene == selection.Scene && s.Path == selection.Path);
            if (index >= 0)
            {
                selection = values[index];
                values.RemoveAt(index);
            }

            values.Insert(selection.Pinned ? 0 : values.TakeWhile(s => s.Pinned).Count(), selection);

            var i = 0;
            while (values.Count > MaxHistory)
            {
                if (values[^(1 + i)].Pinned)
                {
                    i++;
                }
                else
                {
                    values.RemoveAt(values.Count - 1 - i);
                }
            }

            Repaint();
        }

        private void HandleSelectionChange()
        {
            var activeObject = Selection.activeObject;
            if (activeObject == null) return;

            var selection = new RecentSelection();
            if (AssetDatabase.Contains(activeObject))
            {
                selection.Scene = false;
                selection.Path = AssetDatabase.GetAssetPath(activeObject);
            }
            else
            {
                if (activeObject is GameObject gameObject)
                {
                    selection.Scene = true;
                    selection.Path = GetGameObjectPath(gameObject);
                }
                else
                {
                    return;
                }
            }

            AddToList(selection);
        }

        private void OnCreateAsset(string assetPath)
        {
            AddToList(new RecentSelection
            {
                Path = assetPath
            });
        }

        private void OnDisable()
        {
            if (Instance == this)
                Instance = null;

            Selection.selectionChanged -= HandleSelectionChange;
        }

        private void OnEnable()
        {
            if (Instance == null)
                Instance = this;
            Selection.selectionChanged += HandleSelectionChange;
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            var labelWidth = GUILayout.Width(20);
            var buttonWidth = GUILayout.Width(30);
            for (var i = 0; i < values.Count; i++)
            {
                var recentSelection = values[i];

                var obj = recentSelection.Scene
                    ? GetGameObjectAtPath(recentSelection.Path)
                    : AssetDatabase.LoadAssetAtPath<Object>(recentSelection.Path);
                if (obj == null && !recentSelection.Pinned)
                    continue;

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(i.ToString(), labelWidth);

                if (GUILayout.Button(recentSelection.Pinned ? "★" : ".", buttonWidth))
                {
                    recentSelection.Pinned = !recentSelection.Pinned;
                    AddToList(recentSelection);
                }

                if (obj != null)
                {
                    EditorGUILayout.ObjectField(obj, typeof(Object), true);


                    if (GUILayout.Button("▼", buttonWidth))
                    {
                        EditorGUIUtility.PingObject(obj);
                    }

                    if (GUILayout.Button("►", buttonWidth))
                    {
                        Selection.activeObject = obj;
                    }

                    var dragHandleWidth = GUILayout.Width(20);
                    EditorGUILayout.LabelField(" ≡ ", dragHandleWidth);
                    var e = Event.current;
                    if (GUILayoutUtility.GetLastRect().Contains(e.mousePosition) && e.type == EventType.MouseDrag)
                    {
                        DragAndDrop.PrepareStartDrag();
                        DragAndDrop.objectReferences = new[] { obj };
                        DragAndDrop.StartDrag("drag");
                        Event.current.Use();
                    }
                }
                else
                {
                    EditorGUILayout.TextField(recentSelection.Path);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        #endregion

        #region ================================ NESTED TYPES

        public class CustomAssetModificationProcessor : AssetModificationProcessor
        {
            #region ================================ METHODS

            private static void OnWillCreateAsset(string assetPath)
            {
                if (Instance)
                    Instance.OnCreateAsset(assetPath);
            }

            private static AssetMoveResult OnWillMoveAsset(string sourcePath, string destinationPath)
            {
                if (Instance)
                    Instance.OnCreateAsset(destinationPath);
                return AssetMoveResult.DidNotMove;
            }

            #endregion
        }

        #endregion
    }
}