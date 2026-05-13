using System;
using System.Collections.Generic;
using System.Linq;
using Eflatun.SceneReference;
using UnityEngine;

namespace Game.Core.Global
{
    [CreateAssetMenu(fileName = "SceneGroup", menuName = "SceneManagement/SceneGroup")]
    public class SceneGroup : ScriptableObject
    {
        public string groupName;
        public List<SceneData> scenes;

        public string FindSceneNameByType(SceneType sceneType)
        {
            return scenes.FirstOrDefault(scene => scene.sceneType == sceneType)?.Name;
        }
    }

    [Serializable]
    public class SceneData
    {
        public SceneReference reference;
        public SceneType sceneType;
        public bool alwaysReload;

        public string Name => reference.Name;
    }

    [Serializable]
    public enum SceneType
    {
        Active,
        MainMenu,
        UI,
        Level
    }
}