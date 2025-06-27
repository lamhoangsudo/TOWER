using UnityEditor;
using UnityEngine;

namespace Nav3D.API.Editor
{
    [CustomEditor(typeof(Nav3DEvader)), CanEditMultipleObjects]
    public class Nav3DEvaderInspector : UnityEditor.Editor
    {
        #region Attributes

        SerializedProperty m_Radius;
        SerializedProperty m_MaxSpeed;
        SerializedProperty m_SpeedDecayFactor;
        SerializedProperty m_ORCATau;

        SerializedProperty m_DrawRadius;
        SerializedProperty m_DrawNearestAgents;
        SerializedProperty m_DrawNearestTriangles;
        SerializedProperty m_DrawTrianglesUpdateInfo;

        Nav3DEvader m_Evader;

        bool m_AdvancedFoldout;

        #endregion

        #region Unity methods

        void OnEnable()
        {
            m_Radius                  = serializedObject.FindProperty("m_Radius");
            m_MaxSpeed                = serializedObject.FindProperty("m_MaxSpeed");
            m_SpeedDecayFactor        = serializedObject.FindProperty("m_SpeedDecayFactor");
            m_ORCATau                 = serializedObject.FindProperty("m_ORCATau");
            m_DrawRadius              = serializedObject.FindProperty("m_DrawRadius");
            m_DrawNearestAgents       = serializedObject.FindProperty("m_DrawNearestAgents");
            m_DrawNearestTriangles    = serializedObject.FindProperty("m_DrawNearestTriangles");
            m_DrawTrianglesUpdateInfo = serializedObject.FindProperty("m_DrawTrianglesUpdateInfo");

            m_Evader = (Nav3DEvader) target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawSettings();

            EditorGUILayout.Space();
            
            DebugDrawContent(m_Evader);

            serializedObject.ApplyModifiedProperties();
        }

        #endregion

        #region Service methods

        void DrawSettings()
        {
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(m_Radius);
            EditorGUILayout.PropertyField(m_MaxSpeed);
            EditorGUILayout.PropertyField(m_SpeedDecayFactor);

            EditorGUI.indentLevel++;
            // ReSharper disable once AssignmentInConditionalExpression
            if (m_AdvancedFoldout = EditorGUILayout.Foldout(m_AdvancedFoldout, "Advanced", true))
            {
                EditorGUILayout.BeginHorizontal();
                
                EditorGUILayout.PrefixLabel("ORCA Tau");
                m_ORCATau.floatValue = EditorGUILayout.FloatField(m_ORCATau.floatValue);
                
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }

        void DebugDrawContent(Nav3DEvader _Evader)
        {
            if (EditorUtility.IsPersistent(_Evader))
                return;

            EditorGUILayout.LabelField("Debug drawing", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Draw radius:");
            m_DrawRadius.boolValue = EditorGUILayout.Toggle(m_DrawRadius.boolValue);

            EditorGUILayout.EndHorizontal();

            if (!Application.isPlaying)
                return;
            
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Highlight nearest agents: ");
            m_DrawNearestAgents.boolValue = EditorGUILayout.Toggle(m_DrawNearestAgents.boolValue);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Highlight nearest triangles: ");
            m_DrawNearestTriangles.boolValue = EditorGUILayout.Toggle(m_DrawNearestTriangles.boolValue);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Highlight triangles update info: ");
            m_DrawTrianglesUpdateInfo.boolValue = EditorGUILayout.Toggle(m_DrawTrianglesUpdateInfo.boolValue);

            EditorGUILayout.EndHorizontal();
        }

        #endregion
    }
}