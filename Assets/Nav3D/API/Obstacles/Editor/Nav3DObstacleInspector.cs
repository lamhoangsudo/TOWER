using UnityEngine;
using UnityEditor;

namespace Nav3D.API.Editor
{
    [CustomEditor(typeof(Nav3DObstacle))]
    public class Nav3DObstacleInspector : UnityEditor.Editor
    {
        #region Constants

        const string SELECT_PROCESSING_TYPE = "Select processing type:";

        const string DRAW_ALL_BUTTON          = "All";
        const string DRAW_SPECIFIC_ONE_BUTTON = "Specific";

        const string OBSTACLE_DATA_SOURCE_SERIALIZATION = "Load from binary";
        const string OBSTACLE_DATA_SOURCE_RUNTIME       = "Runtime";

        const string TIP_AUTO_ADD_CHILDREN = "Process children";

        const string TIP_AUTO_UPDATE_ENABLED = "Auto-update on change"; //Update automatically on transform change
        const string TIP_AUTO_UPDATE_PERIOD  = "Update min period(s)";

        const string TIP_DRAW_OCCUPIED_LEAVES_SET_UP = "Set up drawing occupied leaves:";
        const string TIP_DRAW_FREE_LEAVES_SET_UP     = "Set up drawing free leaves:";
        const string TIP_DRAW_GRAPH_NODES_SET_UP     = "Set up drawing graph nodes:";

        const string TIP_DRAW_OCCUPIED_LEAVES = "Draw occupied leaves";
        const string TIP_DRAW_FREE_LEAVES     = "Draw free leaves";
        const string TIP_DRAW_GRAPH_NODES     = "Draw graph nodes";
        const string TIP_DRAW_ROOTS           = "Draw root nodes";

        readonly string SERIALIZING_STATUS_SERIALIZED_BY_LOADER =
            $"The obstacle is successfully baked and serialized in {nameof(Nav3DObstacleLoader)}.";

        readonly string SERIALIZING_STATUS_NOT_SERIALIZED_BY_LOADER = $"Obstacle is not serialized by {nameof(Nav3DObstacleLoader)}";

        readonly string PERFORM_OBSTACLES_BAKING_TIP =
            $"Bake obstacles in {nameof(Nav3DObstacleLoader)}. Otherwise, the obstacle will not be loaded in play mode.";

        readonly string OBSTACLES_SUCCESSFULLY_SERIALIZED_TIP = $"Changing one of the values:\n\n"                          +
                                                                $"\t-Transform.Scale,\n"                                    +
                                                                $"\t-Trasnfosrm.Postion,\n"                                 +
                                                                $"\t-Transform.Rotation,\n"                                 +
                                                                $"\t-{nameof(Nav3DObstacle.DataSource)}(Processing type)\n" +
                                                                $"\t-Auto add childs\n\n"                                   +
                                                                $"Will invalidate baked Nav3DObstacleLoader data. You will need to re-bake and re-serialize the obstacles on the scene.";

        #endregion

        #region Attributes

        Nav3DObstacle m_Obstacle;

        SerializedProperty m_ProcessChildren;
        SerializedProperty m_AutoUpdateIfChanged;
        SerializedProperty m_AutoUpdateMinPeriod;
        SerializedProperty m_DrawOccupiedLeaves;
        SerializedProperty m_DrawFreeLeaves;
        SerializedProperty m_DrawGraph;
        SerializedProperty m_DrawRoots;
        SerializedProperty m_ObstacleDataSource;

        SerializedProperty m_OccupiedLeavesDrawingMode;
        SerializedProperty m_OccupiedLeavesDrawingLayerNumber;
        SerializedProperty m_FreeLeavesDrawingMode;
        SerializedProperty m_FreeLeavesDrawingLayerNumber;
        SerializedProperty m_GraphNodesDrawingMode;
        SerializedProperty m_GraphNodesDrawingLayerNumber;

        bool m_AdditionalInfoFoldout;
        bool m_DebugDrawingFoldout;

        #endregion

        #region Unity methods

        void OnEnable()
        {
            m_ProcessChildren                  = serializedObject.FindProperty("m_ProcessChildren");
            m_AutoUpdateIfChanged              = serializedObject.FindProperty("m_AutoUpdateIfChanged");
            m_AutoUpdateMinPeriod              = serializedObject.FindProperty("m_AutoUpdateMinPeriod");
            m_DrawOccupiedLeaves               = serializedObject.FindProperty("m_DrawOccupiedLeaves");
            m_DrawFreeLeaves                   = serializedObject.FindProperty("m_DrawFreeLeaves");
            m_DrawGraph                        = serializedObject.FindProperty("m_DrawGraph");
            m_DrawRoots                        = serializedObject.FindProperty("m_DrawRoots");
            m_ObstacleDataSource               = serializedObject.FindProperty("m_ObstacleDataSource");
            m_OccupiedLeavesDrawingMode        = serializedObject.FindProperty("m_OccupiedLeavesDrawingMode");
            m_OccupiedLeavesDrawingLayerNumber = serializedObject.FindProperty("m_OccupiedLeavesDrawingLayerNumber");
            m_FreeLeavesDrawingMode            = serializedObject.FindProperty("m_FreeLeavesDrawingMode");
            m_FreeLeavesDrawingLayerNumber     = serializedObject.FindProperty("m_FreeLeavesDrawingLayerNumber");
            m_GraphNodesDrawingMode            = serializedObject.FindProperty("m_GraphNodesDrawingMode");
            m_GraphNodesDrawingLayerNumber     = serializedObject.FindProperty("m_GraphNodesDrawingLayerNumber");
        }

        public override void OnInspectorGUI()
        {
            m_Obstacle = (Nav3DObstacle)target;

            serializedObject.Update();

            EditorGUILayout.BeginVertical();

            ProcessingSettings();

            EditorGUILayout.Space();

            BehaviorSettings();

            EditorGUILayout.Space();
            UtilsEditor.SeparatorVertical();

            DebugDrawing();

            if (Application.isPlaying && m_Obstacle.enabled)
            {
                AdditionalInfo();
            }

            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }

        #endregion

        #region Service methods

        void ProcessingSettings()
        {
            EditorGUILayout.LabelField(SELECT_PROCESSING_TYPE);

            m_ObstacleDataSource.enumValueIndex = GUILayout.SelectionGrid(
                m_ObstacleDataSource.enumValueIndex,
                new[] { OBSTACLE_DATA_SOURCE_RUNTIME, OBSTACLE_DATA_SOURCE_SERIALIZATION },
                2);

            Nav3DObstacleLoader obstacleLoader = FindObjectOfType<Nav3DObstacleLoader>();

            SerializationStatus(obstacleLoader);

            ProcessingChildren();
        }

        void SerializationStatus(Nav3DObstacleLoader _ObstacleLoader)
        {
            if (m_Obstacle.DataSource != Nav3DObstacle.ObstacleDataSource.LOAD_FROM_BINARY)
                return;

            UtilsEditor.SeparatorVertical();

            if (_ObstacleLoader != null && _ObstacleLoader.IsObstaclesSerialized && _ObstacleLoader.IsObstacleSerialized(m_Obstacle))
            {
                EditorGUILayout.BeginHorizontal();
                UtilsEditor.GetCheckIcon(32);
                EditorGUILayout.LabelField(SERIALIZING_STATUS_SERIALIZED_BY_LOADER, UtilsEditor.BoldAndWrappedStyle);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();
                EditorGUILayout.LabelField(OBSTACLES_SUCCESSFULLY_SERIALIZED_TIP, UtilsEditor.BoldAndWrappedStyle);
            }
            else
            {
                EditorGUILayout.LabelField(SERIALIZING_STATUS_NOT_SERIALIZED_BY_LOADER, UtilsEditor.BoldAndWrappedStyle);
                EditorGUILayout.HelpBox(PERFORM_OBSTACLES_BAKING_TIP, MessageType.Warning);
            }
        }

        void ProcessingChildren()
        {
            UtilsEditor.SeparatorVertical();

            bool oldProcessingChildren = m_ProcessChildren.boolValue;
            m_ProcessChildren.boolValue = EditorGUILayout.Toggle(TIP_AUTO_ADD_CHILDREN, m_ProcessChildren.boolValue);

            if (oldProcessingChildren != m_ProcessChildren.boolValue)
                m_Obstacle.MarkTransformHasChangedTrue();
        }

        void BehaviorSettings()
        {
            //Auto update is unavailable for huge obstacles
            if (m_Obstacle.DataSource != Nav3DObstacle.ObstacleDataSource.RUNTIME_PROCESSING)
                return;

            m_AutoUpdateIfChanged.boolValue = EditorGUILayout.Toggle(TIP_AUTO_UPDATE_ENABLED, m_AutoUpdateIfChanged.boolValue);

            if (!m_AutoUpdateIfChanged.boolValue)
                return;

            m_AutoUpdateMinPeriod.floatValue = EditorGUILayout.FloatField(TIP_AUTO_UPDATE_PERIOD, m_AutoUpdateMinPeriod.floatValue);
        }

        void OccupiedLeavesDrawing()
        {
            bool oldDrawOccupiedLeaves = m_DrawOccupiedLeaves.boolValue;

            m_DrawOccupiedLeaves.boolValue = EditorGUILayout.Toggle(TIP_DRAW_OCCUPIED_LEAVES, m_DrawOccupiedLeaves.boolValue);

            if (m_DrawOccupiedLeaves.boolValue != oldDrawOccupiedLeaves)
                m_Obstacle.ClearGizmosCache();

            if (!m_DrawOccupiedLeaves.boolValue)
                return;

            EditorGUILayout.LabelField(TIP_DRAW_OCCUPIED_LEAVES_SET_UP);

            int oldMode = m_OccupiedLeavesDrawingMode.intValue;

            m_OccupiedLeavesDrawingMode.intValue =
                GUILayout.SelectionGrid(m_OccupiedLeavesDrawingMode.intValue, new[] { DRAW_ALL_BUTTON, DRAW_SPECIFIC_ONE_BUTTON }, 2);

            if (m_OccupiedLeavesDrawingMode.intValue != oldMode)
                m_Obstacle.ClearGizmosCache();

            if (m_OccupiedLeavesDrawingMode.intValue == 1)
            {
                int octreeLevelCount = m_Obstacle.OctreeLayersCount;

                int oldLayerNumber = m_OccupiedLeavesDrawingLayerNumber.intValue;

                m_OccupiedLeavesDrawingLayerNumber.intValue =
                    EditorGUILayout.IntSlider(m_OccupiedLeavesDrawingLayerNumber.intValue, 0, octreeLevelCount - 1);

                if (m_OccupiedLeavesDrawingLayerNumber.intValue != oldLayerNumber)
                    m_Obstacle.ClearGizmosCache();
            }
        }

        void FreeLeavesDrawing()
        {
            bool oldDrawFreeLeaves = m_DrawFreeLeaves.boolValue;

            m_DrawFreeLeaves.boolValue = EditorGUILayout.Toggle(TIP_DRAW_FREE_LEAVES, m_DrawFreeLeaves.boolValue);

            if (m_DrawFreeLeaves.boolValue != oldDrawFreeLeaves)
                m_Obstacle.ClearGizmosCache();

            if (!m_DrawFreeLeaves.boolValue)
                return;

            EditorGUILayout.LabelField(TIP_DRAW_FREE_LEAVES_SET_UP);

            int oldMode = m_FreeLeavesDrawingMode.intValue;

            m_FreeLeavesDrawingMode.intValue =
                GUILayout.SelectionGrid(m_FreeLeavesDrawingMode.intValue, new[] { DRAW_ALL_BUTTON, DRAW_SPECIFIC_ONE_BUTTON }, 2);

            if (m_FreeLeavesDrawingMode.intValue != oldMode)
                m_Obstacle.ClearGizmosCache();

            if (m_FreeLeavesDrawingMode.intValue == 1)
            {
                int octreeLevelCount = m_Obstacle.OctreeLayersCount;

                int oldLayerNumber = m_FreeLeavesDrawingLayerNumber.intValue;

                m_FreeLeavesDrawingLayerNumber.intValue =
                    EditorGUILayout.IntSlider(m_FreeLeavesDrawingLayerNumber.intValue, 0, octreeLevelCount - 1);

                if (m_FreeLeavesDrawingLayerNumber.intValue != oldLayerNumber)
                    m_Obstacle.ClearGizmosCache();
            }
        }

        void GraphNodesDrawing()
        {
            bool oldDrawGraph = m_DrawGraph.boolValue;

            m_DrawGraph.boolValue = EditorGUILayout.Toggle(TIP_DRAW_GRAPH_NODES, m_DrawGraph.boolValue);

            if (m_DrawGraph.boolValue != oldDrawGraph)
                m_Obstacle.ClearGizmosCache();

            if (!m_DrawGraph.boolValue)
                return;

            EditorGUILayout.LabelField(TIP_DRAW_GRAPH_NODES_SET_UP);

            int oldMode = m_GraphNodesDrawingMode.intValue;

            m_GraphNodesDrawingMode.intValue =
                GUILayout.SelectionGrid(m_GraphNodesDrawingMode.intValue, new[] { DRAW_ALL_BUTTON, DRAW_SPECIFIC_ONE_BUTTON }, 2);

            if (m_GraphNodesDrawingMode.intValue != oldMode)
                m_Obstacle.ClearGizmosCache();

            if (m_GraphNodesDrawingMode.intValue == 1)
            {
                int octreeLevelCount = m_Obstacle.OctreeLayersCount;

                int oldLayerNumber = m_GraphNodesDrawingLayerNumber.intValue;

                m_GraphNodesDrawingLayerNumber.intValue =
                    EditorGUILayout.IntSlider(m_GraphNodesDrawingLayerNumber.intValue, 0, octreeLevelCount - 1);

                if (m_GraphNodesDrawingLayerNumber.intValue != oldLayerNumber)
                    m_Obstacle.ClearGizmosCache();
            }
        }

        void RootsSettings()
        {
            bool oldDrawRoots = m_DrawRoots.boolValue;

            m_DrawRoots.boolValue = EditorGUILayout.Toggle(TIP_DRAW_ROOTS, m_DrawRoots.boolValue);

            if (m_DrawRoots.boolValue != oldDrawRoots)
                m_Obstacle.ClearGizmosCache();

            if (!m_DrawRoots.boolValue)
                return;
        }

        void DebugDrawing()
        {
            // ReSharper disable once AssignmentInConditionalExpression
            if (m_DebugDrawingFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_DebugDrawingFoldout, new GUIContent("Debug drawing")))
            {
                OccupiedLeavesDrawing();
                FreeLeavesDrawing();
                GraphNodesDrawing();
                RootsSettings();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void AdditionalInfo()
        {
            // ReSharper disable once AssignmentInConditionalExpression
            if (m_AdditionalInfoFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_AdditionalInfoFoldout, new GUIContent("Additional info")))
            {
                EditorGUILayout.LabelField("Nodes count:", $"{m_Obstacle.NodesCount}");
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        #endregion
    }
}