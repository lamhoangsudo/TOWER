using UnityEngine;
using UnityEditor;

namespace Nav3D.API.Editor
{
    class Nav3DAgentPathfindingStatsWindow : EditorWindow
    {
        #region constants

        const float WINDOW_MIN_SIZE = 200;

        #endregion

        #region Attributes

        Nav3DAgent m_Agent;

        string     m_CachedContent;
        GUIContent m_GUIContent;

        #endregion

        #region Public methods

        public static void Open(Nav3DAgent _Agent)
        {
            Nav3DAgentPathfindingStatsWindow window = GetWindow<Nav3DAgentPathfindingStatsWindow>();
            window.minSize = new Vector2(WINDOW_MIN_SIZE, WINDOW_MIN_SIZE);
            window.Init(_Agent);
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
                m_CachedContent = m_Agent.GetLastPathfindingStatsString();
                m_GUIContent    = new GUIContent(m_CachedContent);
            }

            GUIStyle style  = UtilsEditor.WrappedStyle;
            float    height = style.CalcHeight(m_GUIContent, position.width);

            EditorGUILayout.LabelField(m_GUIContent, style, GUILayout.Height(height), GUILayout.Width(position.size.x));
            Repaint();
        }

        #endregion
    }
}