using UnityEditor;

namespace Nav3D.API.Editor
{
    [CustomEditor(typeof(Nav3DSphereShell)), CanEditMultipleObjects]
    public class Nav3DSphereShellInspector: UnityEditor.Editor
    {
        #region Attributes

        SerializedProperty m_Radius;
        SerializedProperty m_DrawRadius;

        #endregion
        
        #region Unity events

        void OnEnable()
        {
            m_Radius     = serializedObject.FindProperty("m_Radius");
            m_DrawRadius = serializedObject.FindProperty("m_DrawRadius");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            DrawSettings();
            
            EditorGUILayout.Space();
            
            DebugDrawContent();
            
            serializedObject.ApplyModifiedProperties();
        }

        #endregion
        
        #region Service methods

        void DrawSettings()
        {
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_Radius);
        }

        void DebugDrawContent()
        {
            EditorGUILayout.LabelField("Debug drawing", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.LabelField("Draw radius:");
            m_DrawRadius.boolValue = EditorGUILayout.Toggle(m_DrawRadius.boolValue);
            
            EditorGUILayout.EndHorizontal();
        }
        
        #endregion
    }
}