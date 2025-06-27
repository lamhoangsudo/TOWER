using System.Linq;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace Nav3D.Common
{
    public static class UtilsEditor
    {
        #region Constants

        static readonly string ASSETS_WITH_NAME_NOT_FOUND_ERROR = "Assets with name ({0}) were not found.";

        #endregion

        #region Public methods

        /// <summary>
        /// Instantiates prefab instance (not independant clone!)
        /// </summary>
        public static void InstantiatePrefab(string _PrefabName)
        {
            string GUID = AssetDatabase.FindAssets($"{_PrefabName} t:Prefab", null).FirstOrDefault();

            if (string.IsNullOrEmpty(GUID))
            {
                UnityEngine.Debug.LogError(string.Format(ASSETS_WITH_NAME_NOT_FOUND_ERROR, _PrefabName));
                return;
            }

            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(GUID), typeof(GameObject)));
        }

        /// <summary>
        /// Creates a GameObject and attaches an instance of component T to it.
        /// </summary>
        public static void InstantiateGOWithComponent<T>(string _Name) where T : MonoBehaviour
        {
            GameObject gameObject = new GameObject(_Name);
            T component = gameObject.AddComponent<T>();
        }

        #endregion
    }
}
#endif