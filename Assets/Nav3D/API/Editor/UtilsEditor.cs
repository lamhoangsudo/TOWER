using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
namespace Nav3D.API.Editor
{
    public static class UtilsEditor
    {
        #region Attributes

        static Texture m_CheckTexture;

        #endregion

        #region Properties

        static Texture CheckTexture => m_CheckTexture ??= (Texture)AssetDatabase.LoadAssetAtPath("Assets/Nav3D/API/Obstacles/Editor/Resources/check.png", typeof(Texture));

        public static GUIStyle BoldAndWrappedStyle
        {
            get
            {
                GUIStyle style = EditorStyles.wordWrappedLabel;

                style.fontStyle = FontStyle.Bold;
                style.alignment = TextAnchor.MiddleLeft;

                return style;
            }
        }

        public static GUIStyle WrappedStyle
        {
            get
            {
                GUIStyle style = EditorStyles.wordWrappedLabel;

                style.fontStyle = FontStyle.Normal;
                style.alignment = TextAnchor.MiddleLeft;

                return style;
            }
        }

        #endregion

        #region Public methods

        public static void GetCheckIcon(float _Size = 32)
        {
            Rect rect = GUILayoutUtility.GetRect(_Size, _Size);
            GUI.DrawTexture(rect, CheckTexture, ScaleMode.ScaleToFit);
        }

        public static void SeparatorVertical(int _Thickness = 1)
        {
            EditorGUILayout.Space();

            Rect rect = EditorGUILayout.GetControlRect(false, _Thickness);
            rect.height = _Thickness;

            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));

            EditorGUILayout.Space();
        }

        #endregion
    }
}
#endif