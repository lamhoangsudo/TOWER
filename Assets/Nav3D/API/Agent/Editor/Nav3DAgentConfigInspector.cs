using UnityEditor;
using UnityEngine;

namespace Nav3D.API.Editor
{
    [CustomEditor(typeof(Nav3DAgentConfig))]
    public class Nav3DAgentConfigInspector : UnityEditor.Editor
    {
        #region Constants
        
        const string MAX_SPEED_OBTAIN_METHOD_VALUE      = "Absolute value";
        const string MAX_SPEED_OBTAIN_METHOD_MULTIPLIER = "Speed multiplier";

        GUIStyle BOLD_WRAPPED_WHITE_STYLE;

        #endregion

        #region Attributes

        Nav3DAgentConfig m_Config;

        bool m_RadiusFoldout;
        bool m_SpeedFoldout;
        bool m_BehaviorFoldout;
        bool m_LocalAvoidanceFoldout;
        bool m_LocalAvoidanceAdvancedFoldout;
        bool m_PathfindingFoldout;
        bool m_PathfindingAdvancedFoldout;
        bool m_MotionFoldout;
        bool m_VelocityFoldout;
        bool m_DebugFoldout;
        
        SerializedProperty m_MotionNavigationType;
        SerializedProperty m_Radius;

        SerializedProperty m_Speed;
        SerializedProperty m_MaxSpeed;
        SerializedProperty m_MaxSpeedObtainMode;
        SerializedProperty m_SpeedToMaxSpeedMultiplier;
        SerializedProperty m_UseConsideredAgentsNumberLimit;
        SerializedProperty m_ConsideredAgentsNumberLimit;
        SerializedProperty m_ORCATau;
        SerializedProperty m_AvoidStaticObstacles;

        SerializedProperty m_PathfindingTimeout;
        SerializedProperty m_SmoothPath;
        SerializedProperty m_SmoothRatio;
        SerializedProperty m_AutoUpdatePath;
        SerializedProperty m_PathAutoUpdateCooldown;
        SerializedProperty m_TryRepositionTargetIfOccupied;

        SerializedProperty m_TargetReachDistance;
        SerializedProperty m_MaxAgentDegreesRotationPerTick;
        SerializedProperty m_RotationVectorLerpFactor;

        SerializedProperty m_PathVelocityWeight;
        SerializedProperty m_PathVelocityWeight1;
        SerializedProperty m_PathVelocityWeight2;
        SerializedProperty m_AgentsAvoidanceVelocityWeight;
        SerializedProperty m_AgentsAvoidanceVelocityWeight1;
        SerializedProperty m_AgentsAvoidanceVelocityWeight2;
        SerializedProperty m_ObstacleAvoidanceVelocityWeight;
        SerializedProperty m_ObstacleAvoidanceVelocityWeight1;
        SerializedProperty m_ObstacleAvoidanceVelocityWeight2;

        SerializedProperty m_UseLog;
        SerializedProperty m_LogSize;

        #endregion

        #region Unity methods

        void OnEnable()
        {
            BOLD_WRAPPED_WHITE_STYLE = new GUIStyle
            {
                wordWrap  = true,
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Bold,
                normal    = new GUIStyleState { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
            };

            m_Radius                = serializedObject.FindProperty("m_Radius");

            m_Speed                     = serializedObject.FindProperty("m_Speed");
            m_MaxSpeed                  = serializedObject.FindProperty("m_MaxSpeed");
            m_MaxSpeedObtainMode        = serializedObject.FindProperty("m_MaxSpeedObtainMode");
            m_SpeedToMaxSpeedMultiplier = serializedObject.FindProperty("m_SpeedToMaxSpeedMultiplier");

            m_UseConsideredAgentsNumberLimit = serializedObject.FindProperty("m_UseConsideredAgentsNumberLimit");
            m_ConsideredAgentsNumberLimit    = serializedObject.FindProperty("m_ConsideredAgentsNumberLimit");
            m_ORCATau                        = serializedObject.FindProperty("m_ORCATau");
            m_AvoidStaticObstacles           = serializedObject.FindProperty("m_AvoidStaticObstacles");
            
            m_MotionNavigationType = serializedObject.FindProperty("m_MotionNavigationType");

            m_PathfindingTimeout            = serializedObject.FindProperty("m_PathfindingTimeout");
            m_SmoothPath                    = serializedObject.FindProperty("m_SmoothPath");
            m_SmoothRatio                   = serializedObject.FindProperty("m_SmoothRatio");
            m_AutoUpdatePath                = serializedObject.FindProperty("m_AutoUpdatePath");
            m_PathAutoUpdateCooldown        = serializedObject.FindProperty("m_PathAutoUpdateCooldown");
            m_TryRepositionTargetIfOccupied = serializedObject.FindProperty("m_TryRepositionTargetIfOccupied");

            m_TargetReachDistance            = serializedObject.FindProperty("m_TargetReachDistance");
            m_MaxAgentDegreesRotationPerTick = serializedObject.FindProperty("m_MaxAgentDegreesRotationPerTick");
            m_RotationVectorLerpFactor       = serializedObject.FindProperty("m_RotationVectorLerpFactor");

            m_PathVelocityWeight               = serializedObject.FindProperty("m_PathVelocityWeight");
            m_PathVelocityWeight1              = serializedObject.FindProperty("m_PathVelocityWeight1");
            m_PathVelocityWeight2              = serializedObject.FindProperty("m_PathVelocityWeight2");
            m_AgentsAvoidanceVelocityWeight    = serializedObject.FindProperty("m_AgentsAvoidanceVelocityWeight");
            m_AgentsAvoidanceVelocityWeight1   = serializedObject.FindProperty("m_AgentsAvoidanceVelocityWeight1");
            m_AgentsAvoidanceVelocityWeight2   = serializedObject.FindProperty("m_AgentsAvoidanceVelocityWeight2");
            m_ObstacleAvoidanceVelocityWeight  = serializedObject.FindProperty("m_ObstacleAvoidanceVelocityWeight");
            m_ObstacleAvoidanceVelocityWeight1 = serializedObject.FindProperty("m_ObstacleAvoidanceVelocityWeight1");
            m_ObstacleAvoidanceVelocityWeight2 = serializedObject.FindProperty("m_ObstacleAvoidanceVelocityWeight2");

            m_UseLog  = serializedObject.FindProperty("m_UseLog");
            m_LogSize = serializedObject.FindProperty("m_LogSize");
        }

        public override void OnInspectorGUI()
        {
            m_Config = (Nav3DAgentConfig)target;

            serializedObject.Update();

            HeaderControls();

            //Behavior setup
            BehaviorFoldout();
            
            //Radius setup
            RadiusFoldout();

            //Speed setup 
            SpeedFoldout();

            //Local avoidance setup
            if (m_MotionNavigationType.enumValueIndex == 0 || m_MotionNavigationType.enumValueIndex == 2)
                LocalAvoidanceFoldout();

            //Pathfinding setup
            PathfindingFoldout();

            //Motion setup
            MotionFoldout();

            if (m_MotionNavigationType.enumValueIndex == 0)
                VelocityBlendingFoldout();

            DebugFoldout();

            serializedObject.ApplyModifiedProperties();
        }

        #endregion

        #region Service methods

        void HeaderControls()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Expand All"))
            {
                m_RadiusFoldout         = true;
                m_SpeedFoldout          = true;
                m_BehaviorFoldout       = true;
                m_LocalAvoidanceFoldout = true;
                m_PathfindingFoldout    = true;
                m_MotionFoldout         = true;
                m_VelocityFoldout       = true;
                m_DebugFoldout          = true;
            }

            if (GUILayout.Button("Collapse All"))
            {
                m_RadiusFoldout         = false;
                m_SpeedFoldout          = false;
                m_BehaviorFoldout       = false;
                m_LocalAvoidanceFoldout = false;
                m_PathfindingFoldout    = false;
                m_MotionFoldout         = false;
                m_VelocityFoldout       = false;
                m_DebugFoldout          = false;
            }

            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Reset to defaults") &&
                EditorUtility.DisplayDialog("Set default config parameters?", "Are you sure?", "Yes", "Cancel"))
                ((Nav3DAgentConfig)target).SetDefaultAttributes();

            if (GUILayout.Button("Save changes"))
                Save();
        }

        void Save()
        {
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
        }

        void BehaviorFoldout()
        {
            // ReSharper disable once AssignmentInConditionalExpression
            if (m_BehaviorFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_BehaviorFoldout, "Behavior"))
            {
                EditorGUILayout.PropertyField(m_MotionNavigationType);
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        
        void RadiusFoldout()
        {
            // ReSharper disable once AssignmentInConditionalExpression
            if (m_RadiusFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_RadiusFoldout, "Radius"))
            {
                EditorGUILayout.PropertyField(m_Radius);
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void SpeedFoldout()
        {
            // ReSharper disable once AssignmentInConditionalExpression
            if (m_SpeedFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_SpeedFoldout, "Speed"))
            {
                EditorGUILayout.PropertyField(m_Speed);

                if (m_MaxSpeedObtainMode.intValue == 0)
                {
                    m_MaxSpeed.floatValue = m_Speed.floatValue * m_SpeedToMaxSpeedMultiplier.floatValue;
                }
                else if (m_MaxSpeedObtainMode.intValue == 1)
                {
                    m_Speed.floatValue = Mathf.Clamp(m_Speed.floatValue, 0, Mathf.Max(m_Speed.floatValue, m_MaxSpeed.floatValue));
                }

                if (m_MotionNavigationType.enumValueIndex == 0 || m_MotionNavigationType.enumValueIndex == 2)
                {
                    EditorGUILayout.LabelField("Select a deriving method for max speed:");
                    m_MaxSpeedObtainMode.intValue = GUILayout.SelectionGrid(
                            m_MaxSpeedObtainMode.intValue,
                            new[] { MAX_SPEED_OBTAIN_METHOD_MULTIPLIER, MAX_SPEED_OBTAIN_METHOD_VALUE },
                            2
                        );
                    
                    if (m_MaxSpeedObtainMode.intValue == 0)
                    {
                        EditorGUILayout.PropertyField(m_SpeedToMaxSpeedMultiplier);

                        EditorGUILayout.LabelField($"Max speed: {m_SpeedToMaxSpeedMultiplier.floatValue * m_Speed.floatValue}");
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(m_MaxSpeed);
                    }
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void LocalAvoidanceFoldout()
        {
            // ReSharper disable once AssignmentInConditionalExpression
            if (m_LocalAvoidanceFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_LocalAvoidanceFoldout, "Local avoidance"))
            {
                //Agents limit
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.PrefixLabel("Agents considered number limit");
                m_UseConsideredAgentsNumberLimit.boolValue = EditorGUILayout.Toggle(m_UseConsideredAgentsNumberLimit.boolValue);

                EditorGUILayout.EndHorizontal();

                if (m_UseConsideredAgentsNumberLimit.boolValue)
                {
                    EditorGUILayout.BeginHorizontal();

                    EditorGUILayout.PrefixLabel("Agents number:");
                    m_ConsideredAgentsNumberLimit.intValue = Mathf.Max(1, EditorGUILayout.IntField(m_ConsideredAgentsNumberLimit.intValue));

                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.BeginHorizontal();
                
                EditorGUILayout.PrefixLabel("Avoid static obstacles");
                m_AvoidStaticObstacles.boolValue = EditorGUILayout.Toggle(m_AvoidStaticObstacles.boolValue);
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUI.indentLevel++;
                // ReSharper disable once AssignmentInConditionalExpression
                if (m_LocalAvoidanceAdvancedFoldout = EditorGUILayout.Foldout(m_LocalAvoidanceAdvancedFoldout, "Advanced", true))
                {
                    EditorGUILayout.BeginHorizontal();

                    EditorGUILayout.PropertyField(m_ORCATau);

                    if (GUILayout.Button("Set default"))
                    {
                        m_Config.SetDefaultORCATau();

                        Save();
                    }

                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void PathfindingFoldout()
        {
            // ReSharper disable once AssignmentInConditionalExpression
            if (m_PathfindingFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_PathfindingFoldout, "Pathfinding"))
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.PrefixLabel("Pathfinding timeout (ms)");
                m_PathfindingTimeout.intValue = EditorGUILayout.IntField(m_PathfindingTimeout.intValue);

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.PrefixLabel("Smooth the path");
                m_SmoothPath.boolValue = EditorGUILayout.Toggle(m_SmoothPath.boolValue);

                EditorGUILayout.EndHorizontal();

                if (m_SmoothPath.boolValue)
                {
                    EditorGUILayout.BeginHorizontal();

                    EditorGUILayout.PrefixLabel("Samples per min bucket volume");
                    m_SmoothRatio.intValue = Mathf.Max(1, EditorGUILayout.IntField(m_SmoothRatio.intValue));

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.PrefixLabel("Auto-update path on stagnant behavior");
                m_AutoUpdatePath.boolValue = EditorGUILayout.Toggle(m_AutoUpdatePath.boolValue);

                EditorGUILayout.EndHorizontal();


                if (m_AutoUpdatePath.boolValue)
                {
                    EditorGUILayout.BeginHorizontal();

                    EditorGUILayout.PrefixLabel("Auto-update cooldown (ms)");
                    m_PathAutoUpdateCooldown.intValue = EditorGUILayout.IntField(m_PathAutoUpdateCooldown.intValue);

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUI.indentLevel++;
                // ReSharper disable once AssignmentInConditionalExpression
                if (m_PathfindingAdvancedFoldout = EditorGUILayout.Foldout(m_PathfindingAdvancedFoldout, "Advanced", true))
                {
                    EditorGUILayout.BeginHorizontal();

                    EditorGUILayout.PrefixLabel("Try reposition target if occupied");
                    m_TryRepositionTargetIfOccupied.boolValue = EditorGUILayout.Toggle(m_TryRepositionTargetIfOccupied.boolValue);

                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void MotionFoldout()
        {
            // ReSharper disable once AssignmentInConditionalExpression
            if (m_MotionFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_MotionFoldout, "Motion"))
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.PrefixLabel("Target reach distance");
                m_TargetReachDistance.floatValue = EditorGUILayout.FloatField(m_TargetReachDistance.floatValue);

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.PrefixLabel("Max rotation in degrees per fixed update tick");
                m_MaxAgentDegreesRotationPerTick.floatValue = EditorGUILayout.FloatField(m_MaxAgentDegreesRotationPerTick.floatValue);

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.PrefixLabel("Rotation vector lerp factor");
                m_RotationVectorLerpFactor.floatValue = Mathf.Clamp(EditorGUILayout.FloatField(m_RotationVectorLerpFactor.floatValue), 0.001f, 1f);

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void VelocityBlendingFoldout()
        {
            // ReSharper disable once AssignmentInConditionalExpression
            if (m_VelocityFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_VelocityFoldout, "*Velocity blending"))
            {
                if (GUILayout.Button("Set default blending weights"))
                    m_Config.SetDefaultVelocitiesBlendingWeights();
                
                EditorGUILayout.HelpBox("Below you can change blend ratios for the local avoidance velocities vectors.", MessageType.Info);

                GUILayout.Label("Agent follows global path, and there are both other agents and obstacles near.", BOLD_WRAPPED_WHITE_STYLE);

                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_PathVelocityWeight);
                EditorGUILayout.PropertyField(m_AgentsAvoidanceVelocityWeight);
                EditorGUILayout.PropertyField(m_ObstacleAvoidanceVelocityWeight);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();

                GUILayout.Label("Agent follows global path, and there are only obstacles near.", BOLD_WRAPPED_WHITE_STYLE);

                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_PathVelocityWeight1);
                EditorGUILayout.PropertyField(m_ObstacleAvoidanceVelocityWeight1);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();

                GUILayout.Label("Agent follows global path, and there are only other agents near.", BOLD_WRAPPED_WHITE_STYLE);

                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_PathVelocityWeight2);
                EditorGUILayout.PropertyField(m_AgentsAvoidanceVelocityWeight1);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();

                GUILayout.Label("Agent preform only local avoidance, and there are both other agents and obstacles near.", BOLD_WRAPPED_WHITE_STYLE);

                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_AgentsAvoidanceVelocityWeight2);
                EditorGUILayout.PropertyField(m_ObstacleAvoidanceVelocityWeight2);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void DebugFoldout()
        {
            // ReSharper disable once AssignmentInConditionalExpression
            if (m_DebugFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_DebugFoldout, "Debug"))
            {
                EditorGUILayout.PropertyField(m_UseLog);

                if (m_UseLog.boolValue)
                {
                    EditorGUILayout.BeginHorizontal();

                    EditorGUILayout.PrefixLabel("Log records count:");
                    m_LogSize.intValue = EditorGUILayout.IntField(m_LogSize.intValue);

                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space();
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        #endregion
    }
}