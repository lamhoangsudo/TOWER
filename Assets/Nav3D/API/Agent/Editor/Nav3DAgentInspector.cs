using UnityEditor;
using UnityEngine;

namespace Nav3D.API.Editor
{
    [CustomEditor(typeof(Nav3DAgent), true), CanEditMultipleObjects]
    public class Nav3DAgentInspector : UnityEditor.Editor
    {
        #region Attributes

        SerializedProperty m_DrawRadius;
        SerializedProperty m_DrawVelocities;
        SerializedProperty m_DrawCurrentPath;

        SerializedProperty m_Config;

        SerializedProperty m_ShowAgents;
        SerializedProperty m_DrawNearestTriangles;

        Nav3DAgent m_Agent;

        #endregion

        #region Unity methods

        void OnEnable()
        {
            m_DrawRadius = serializedObject.FindProperty("m_DrawRadius");
            m_DrawVelocities = serializedObject.FindProperty("m_DrawVelocities");
            m_DrawCurrentPath = serializedObject.FindProperty("m_DrawCurrentPath");

            m_Config = serializedObject.FindProperty("m_Config");

            m_ShowAgents = serializedObject.FindProperty("m_ShowAgents");
            m_DrawNearestTriangles = serializedObject.FindProperty("m_DrawNearestTriangles");

            m_Agent = (Nav3DAgent)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DebugDrawContent(m_Agent);

            SettingsContent(m_Agent.Config);

            DebugContent();

            serializedObject.ApplyModifiedProperties();
        }

        #endregion

        #region Service methods

        void DebugDrawContent(Nav3DAgent _Agent)
        {
            if (EditorUtility.IsPersistent(m_Agent))
                return;

            EditorGUILayout.LabelField("Debug drawing", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Draw radius: ");
            m_DrawRadius.boolValue = EditorGUILayout.Toggle(m_DrawRadius.boolValue);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Draw velocities: ");
            m_DrawVelocities.boolValue = EditorGUILayout.Toggle(m_DrawVelocities.boolValue);

            EditorGUILayout.EndHorizontal();

            if (_Agent.Config != null && _Agent.Config.MotionNavigationType != MotionNavigationType.LOCAL)
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField("Draw path: ");
                m_DrawCurrentPath.boolValue = EditorGUILayout.Toggle(m_DrawCurrentPath.boolValue);

                EditorGUILayout.EndHorizontal();
            }

            if (_Agent.Config != null && _Agent.Config.MotionNavigationType != MotionNavigationType.GLOBAL)
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField("Highlight nearest agents: ");
                m_ShowAgents.boolValue = EditorGUILayout.Toggle(m_ShowAgents.boolValue);

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField("Highlight nearest triangles: ");
                m_DrawNearestTriangles.boolValue = EditorGUILayout.Toggle(m_DrawNearestTriangles.boolValue);

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
        }

        void DebugContent()
        {
            if (!Application.isPlaying || !m_Agent.isActiveAndEnabled)
                return;

            EditorGUILayout.LabelField("Debug data", EditorStyles.boldLabel);

            InnerStatusContent();
            LastPathfindingStatsContent();

            if (m_Agent.Config?.UseLog ?? false)
                LogContent(m_Agent);
        }

        void InnerStatusContent()
        {
            if (GUILayout.Button("Show inner status"))
                Nav3DAgentInnerStatusWindow.Open(m_Agent);
        }

        void LastPathfindingStatsContent()
        {
            if (GUILayout.Button("Show last pathfinding stats"))
                Nav3DAgentPathfindingStatsWindow.Open(m_Agent);
        }

        void LogContent(Nav3DAgent _Agent)
        {
            EditorGUILayout.LabelField("Log", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Log is enabled for an agent.");

            if (GUILayout.Button("Show"))
                Nav3DAgentLogWindow.Open(m_Agent);

            EditorGUILayout.LabelField("Press the button below to copy log to the clipboard.");

            if (GUILayout.Button("Copy"))
                EditorGUIUtility.systemCopyBuffer = _Agent.GetLogText();
        }

        void SettingsContent(Nav3DAgentConfig _Config)
        {
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_Config);

            if (Application.isPlaying && !EditorUtility.IsPersistent(m_Agent))
            {
                EditorGUILayout.LabelField("Press the button below to copy Config instance to the clipboard.");

                if (GUILayout.Button("Copy"))
                    EditorGUIUtility.systemCopyBuffer = _Config.ToString();
            }
        }

        #endregion
    }
}
