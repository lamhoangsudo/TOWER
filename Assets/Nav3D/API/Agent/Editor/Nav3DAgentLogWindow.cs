using UnityEngine;
using UnityEditor;

namespace Nav3D.API.Editor
{
    class Nav3DAgentLogWindow : EditorWindow
    {
        #region constants

        const float WINDOW_MIN_SIZE = 200;

        #endregion

        #region Attributes

        Nav3DAgent m_Agent;

        Vector2 m_ScrollPosition;

        string     m_CachedContent;
        GUIContent m_GUIContent;

        #endregion

        #region Public methods

        public static void Open(Nav3DAgent _Agent)
        {
            Nav3DAgentLogWindow window = GetWindow<Nav3DAgentLogWindow>();
            window.minSize = new Vector2(WINDOW_MIN_SIZE, WINDOW_MIN_SIZE);
            window.Init(_Agent);
            window.ShowModalUtility();
        }

        #endregion

        #region Service methods

        void Init(Nav3DAgent _Agent)
        {
            m_Agent = _Agent;
        }

        #endregion

        #region Unity events

        void OnGUI()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                m_CachedContent = m_Agent.GetLogText();
                m_GUIContent    = new GUIContent(m_CachedContent);
            }

            GUIStyle style  = UtilsEditor.WrappedStyle;
            float    height = style.CalcHeight(m_GUIContent, position.width);

            Vector2 size = new Vector2(position.size.x, height);
            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition, GUILayout.Width(size.x), GUILayout.Height(position.height));

            EditorGUILayout.LabelField(m_GUIContent, style, GUILayout.Height(size.y), GUILayout.Width(size.x - EditorGUIUtility.singleLineHeight * 2));

            EditorGUILayout.EndScrollView();
            Repaint();
        }

        #endregion
    }
}

