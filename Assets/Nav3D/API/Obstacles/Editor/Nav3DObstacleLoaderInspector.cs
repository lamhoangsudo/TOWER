using System.Linq;
using System.Collections.Generic;
using Nav3D.Obstacles.Serialization;
using Nav3D.Common;
using UnityEditor;
using UnityEngine;
using LoaderState = Nav3D.API.Nav3DObstacleLoader.LoaderState;

namespace Nav3D.API.Editor
{
    [CustomEditor(typeof(Nav3DObstacleLoader))]
    public class Nav3DObstacleLoaderInspector : UnityEditor.Editor
    {
        #region Constants

        const string SERIALIZABLE_OBSTACLES_HEADER = "Serializable obstacles";
        const string SERIALIZABLE_OBSTACLES_LIST   = "Found serializable obstacles on scene:";

        readonly string NO_SERIALIZABLE_OBSTACLES = $"There are no {nameof(Nav3DObstacle)} on scene to be serialized." +
                                                    $"For obstacles that you want to serialize, set the {nameof(Nav3DObstacle.DataSource)} parameter to {nameof(Nav3DObstacle.ObstacleDataSource.LOAD_FROM_BINARY)}.";

        const string BAKE_OBSTACLES = "Bake and serialize obstacles";

        const string FOUND_NOT_SERIALIZED_OBSTACLES = "Serializable obstacles were found in the scene that have not yet been serialized. " +
                                                      "Re-bake obstacles, otherwise obstacles in the list below will not be taken into account in play mode.";

        const string SAVE_DIALOG_TITLE   = "Select save location";
        const string SAVE_DIALOG_MESSAGE = "Enter the file name";

        const string DIALOG_RESERIALIZE_TITLE = "Bake and serialize again?";

        const string DIALOG_RESERIALIZE_MSG =
            "It looks like you've already selected the obstacle data binary, are you sure you want to bake and serialize the data again?";

        const string DIALOG_RESERIALIZE_YES    = "Yes, go ahead";
        const string DIALOG_RESERIALIZE_CANCEL = "Cancel";

        readonly string NAV3D_INITIALIZER_NOT_FOUND =
            $"The {nameof(Nav3DInitializer)} component is not found on the scene. Without it, Nav3D will not initialize and load obstacles.";

        readonly string NAV3D_INITIALIZER_INCORRECT_SETTINGS = $"Incorrect parameter settings in {nameof(Nav3DInitializer)}. Check for correctness.";

        readonly string SERIALIZATION_SUCCESSFULLY_COMPLETED_MSG = $"Obstacles baking successfully completed. " +
                                                                   $"All serialized obstacles will be loaded at runtime on {nameof(Nav3DInitializer)}.{nameof(Nav3DInitializer.Init)}()";

        const string BINARY_LABEL         = "Binary:";
        const string SERIALIZE_BUTTON_TIP = "By clicking on the button below you can bake and serialize obstacles in the scene from the list above.";

        const string SERIALIZED_OBSTACLES_HAS_CHANGED_TIP =
            "The last serialized data is not valid because one of the baked obstacles was changed or removed. Re-bake obstacles.";

        readonly string SERIALIZED_INITIALIZER_PARAMETERS_HAS_CHANGED_TIP =
            $"Since the last serialization, the size of the minimum bucket cell in Nav3DInitializer has changed. " +
            $"Re-bake the obstacles, or set the Nav3DInitializer to the value used during serialization ({{0}}).";

        readonly LoaderState[] PERSISTENT_FLAGS = new LoaderState[]
        {
            LoaderState.SERIALIZATION_IN_PROGRESS,
            LoaderState.SERIALIZED_OBSTACLES_HAS_CHANGED,
            LoaderState.SERIALIZED_INITIALIZER_PARAMETERS_HAS_CHANGED
        };

        #endregion

        #region Attributes

        LoaderState m_State = LoaderState.NONE;

        Nav3DObstacleLoader m_Loader;
        Nav3DObstacle[]     m_SerializableObstaclesOnScene;
        Nav3DObstacle[]     m_NotSerializedObstaclesOnScene;

        Nav3DInitializer m_Initializer;

        SerializedProperty m_BinaryData;
        SerializedProperty m_MinBucketSizeSerialized;
        SerializedProperty m_SerializedObstacles;
        SerializedProperty m_ValidateSerializedData;

        Vector2 scrollSerializableObstaclesListPhase = new Vector2(0, 0);
        Vector2 scrollNoSerializedObstaclesListPhase = new Vector2(0, 0);

        ObstacleSerializingProgress m_SerializingProgress;

        string m_SerializedFilePath;

        Nav3DObstacle[] m_JustSerializedObstacles;

        bool m_ObstaclesOnSceneFoldout = true;

        #endregion

        #region Properties

        bool SerializationEnabled =>
            (m_State & LoaderState.INITIALIZER_NOT_FOUND)        == LoaderState.NONE &&
            (m_State & LoaderState.INITIALIZER_INVALID_SETTINGS) == LoaderState.NONE &&
            (m_State & LoaderState.SERIALIZATION_IN_PROGRESS)    == LoaderState.NONE &&
            (m_State & LoaderState.SERIALIZABLE_OBSTACLES_FOUND) == LoaderState.SERIALIZABLE_OBSTACLES_FOUND;

        public LoaderState[] PERSISTENT_FLAGS1 => PERSISTENT_FLAGS;

        #endregion

        #region Unity events

        void OnEnable()
        {
            m_Loader = (Nav3DObstacleLoader)target;

            m_BinaryData              = serializedObject.FindProperty("m_BinaryData");
            m_MinBucketSizeSerialized = serializedObject.FindProperty("m_MinBucketSizeSerialized");
            m_SerializedObstacles     = serializedObject.FindProperty("m_SerializedObstacles");
            m_ValidateSerializedData  = serializedObject.FindProperty("m_ValidateSerializedData");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DetermineState();

            EditorGUILayout.BeginVertical();

            if (m_State.HasFlag(LoaderState.INITIALIZER_NOT_FOUND))
                InitializerNotFoundMessage();

            if (m_State.HasFlag(LoaderState.INITIALIZER_INVALID_SETTINGS))
                InitializerInvalidSettingsMessage();

            if (m_State.HasFlag(LoaderState.SERIALIZATION_IN_PROGRESS))
                SerializationInProgress();

            if (m_State.HasFlag(LoaderState.SERIALIZABLE_OBSTACLES_IS_NOT_FOUND))
                SerializableObstaclesNotFound();

            if (m_State.HasFlag(LoaderState.SERIALIZED_OBSTACLES_HAS_CHANGED))
                SerializedObstaclesChanged();

            if (m_State.HasFlag(LoaderState.SERIALIZED_INITIALIZER_PARAMETERS_HAS_CHANGED))
                SerializedInitializerParametersChanged();

            if (m_State.HasFlag(LoaderState.NOT_SERIALIZED_OBSTACLES_FOUND))
                NotSerializedObstaclesList(m_NotSerializedObstaclesOnScene);

            if (SerializationEnabled)
            {
                SerializableObstaclesList();
                ObstaclesSerializingControls();
            }

            if (m_State.HasFlag(LoaderState.SERIALIZATION_COMPLETED)           &&
                !m_State.HasFlag(LoaderState.SERIALIZED_OBSTACLES_HAS_CHANGED) &&
                !m_State.HasFlag(LoaderState.SERIALIZED_INITIALIZER_PARAMETERS_HAS_CHANGED))
            {
                SerializationCompleted();
            }

            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }

        #endregion

        #region Service methods

        void ApplyStatePersistentFlags()
        {
            LoaderState newState = LoaderState.NONE;

            foreach (LoaderState flag in PERSISTENT_FLAGS)
            {
                if (!m_State.HasFlag(flag))
                    continue;

                newState |= flag;
            }

            m_State = newState;
        }

        void DetermineState()
        {
            ApplyStatePersistentFlags();

            m_Initializer = FindObjectOfType<Nav3DInitializer>();

            if (m_Initializer == null)
                m_State |= LoaderState.INITIALIZER_NOT_FOUND;
            else if (!m_Initializer.IsSettingsValid)
                m_State |= LoaderState.INITIALIZER_INVALID_SETTINGS;

            m_SerializableObstaclesOnScene = m_Loader.GetAllSerializableObstacles();

            if (m_SerializableObstaclesOnScene.Any())
                m_State |= LoaderState.SERIALIZABLE_OBSTACLES_FOUND;
            else
                m_State |= LoaderState.SERIALIZABLE_OBSTACLES_IS_NOT_FOUND;

            if (m_Loader.IsObstaclesSerialized)
                m_State |= LoaderState.SERIALIZATION_COMPLETED;

            if (m_Loader.IsObstaclesSerialized)
            {
                if (!m_Loader.ValidateSerializedObstacles())
                {
                    m_State |= LoaderState.SERIALIZED_OBSTACLES_HAS_CHANGED;
                }
                //reset flag
                else if (m_State.HasFlag(LoaderState.SERIALIZED_OBSTACLES_HAS_CHANGED))
                {
                    m_State ^= LoaderState.SERIALIZED_OBSTACLES_HAS_CHANGED;
                }

                if (m_Initializer != null && m_Loader.MinBucketSizeSerialized != m_Initializer.MinBucketSize)
                {
                    m_State |= LoaderState.SERIALIZED_INITIALIZER_PARAMETERS_HAS_CHANGED;
                }
                //reset flag
                else if (m_State.HasFlag(LoaderState.SERIALIZED_INITIALIZER_PARAMETERS_HAS_CHANGED))
                {
                    m_State ^= LoaderState.SERIALIZED_INITIALIZER_PARAMETERS_HAS_CHANGED;
                }
            }

            if (m_Loader.IsObstaclesSerialized && m_Loader.SerializedObstacles.Length < m_SerializableObstaclesOnScene.Length)
            {
                List<Nav3DObstacle> obstacles = m_SerializableObstaclesOnScene.ToList();
                obstacles.RemoveAll(_Obstacle => m_Loader.SerializedObstacles.Contains(_Obstacle));
                m_NotSerializedObstaclesOnScene = obstacles.ToArray();

                m_State |= LoaderState.NOT_SERIALIZED_OBSTACLES_FOUND;
            }
        }

        void InitializerNotFoundMessage()
        {
            EditorGUILayout.HelpBox(NAV3D_INITIALIZER_NOT_FOUND, MessageType.Error);
        }

        void InitializerInvalidSettingsMessage()
        {
            EditorGUILayout.HelpBox(NAV3D_INITIALIZER_INCORRECT_SETTINGS, MessageType.Error);
        }

        void SerializationCompleted()
        {
            UtilsEditor.SeparatorVertical();

            EditorGUILayout.BeginHorizontal();

            UtilsEditor.GetCheckIcon(32);
            EditorGUILayout.LabelField(SERIALIZATION_SUCCESSFULLY_COMPLETED_MSG, UtilsEditor.BoldAndWrappedStyle);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUI.BeginDisabledGroup(true);
            m_BinaryData.objectReferenceValue =
                (TextAsset)EditorGUILayout.ObjectField(BINARY_LABEL, m_BinaryData.objectReferenceValue, typeof(TextAsset), false);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();

            SerializedObstaclesArray();

            UtilsEditor.SeparatorVertical();

            SerializedDataValidationSettings();
        }

        void SerializableObstaclesList()
        {
            UtilsEditor.SeparatorVertical();

            if (m_ObstaclesOnSceneFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_ObstaclesOnSceneFoldout, SERIALIZABLE_OBSTACLES_HEADER))
            {
                EditorGUILayout.LabelField(SERIALIZABLE_OBSTACLES_LIST);

                string obstaclesNamesText =
                    string.Join("\n", m_SerializableObstaclesOnScene.Select(_Obstacle => $"- {_Obstacle.name}, [InstanceID: {_Obstacle.GetInstanceID()}]"));

                scrollSerializableObstaclesListPhase = EditorGUILayout.BeginScrollView(scrollSerializableObstaclesListPhase,
                                                                                       GUILayout.MaxHeight(
                                                                                           Mathf.Min(m_SerializableObstaclesOnScene.Length * 17, 150)));

                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextArea(obstaclesNamesText);
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void SerializableObstaclesNotFound()
        {
            EditorGUILayout.HelpBox(NO_SERIALIZABLE_OBSTACLES, MessageType.Error);
        }

        void SerializedObstaclesChanged()
        {
            EditorGUILayout.HelpBox(SERIALIZED_OBSTACLES_HAS_CHANGED_TIP, MessageType.Error);
        }

        void SerializedInitializerParametersChanged()
        {
            EditorGUILayout.HelpBox(string.Format(SERIALIZED_INITIALIZER_PARAMETERS_HAS_CHANGED_TIP, m_Loader.MinBucketSizeSerialized), MessageType.Error);
        }

        void NotSerializedObstaclesList(Nav3DObstacle[] _Obstacles)
        {
            EditorGUILayout.HelpBox(FOUND_NOT_SERIALIZED_OBSTACLES, MessageType.Warning);

            string obstaclesNamesText = string.Join("\n", _Obstacles.Select(_Obstacle => $"- {_Obstacle.name}, [InstanceID: {_Obstacle.GetInstanceID()}]"));

            scrollNoSerializedObstaclesListPhase =
                EditorGUILayout.BeginScrollView(scrollNoSerializedObstaclesListPhase, GUILayout.MaxHeight(Mathf.Min(_Obstacles.Length * 17, 150)));

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextArea(obstaclesNamesText);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndScrollView();
        }

        void SerializationInProgress()
        {
            while ((m_State & LoaderState.SERIALIZATION_IN_PROGRESS) == LoaderState.SERIALIZATION_IN_PROGRESS)
            {
                if (EditorUtility.DisplayCancelableProgressBar(m_SerializingProgress.GetTitle(), m_SerializingProgress.GetInfo(),
                                                               m_SerializingProgress.GetProgress()))
                {
                    m_SerializingProgress.CancelSerialization();
                    m_State ^= LoaderState.SERIALIZATION_IN_PROGRESS;
                }
            }

            Nav3DManager.Dispose3DNavEditMode();

            EditorUtility.ClearProgressBar();

            AssetDatabase.Refresh();
            m_BinaryData.objectReferenceValue = (TextAsset)AssetDatabase.LoadAssetAtPath(m_SerializedFilePath, typeof(TextAsset));

            SaveSerializedDatas();
        }

        void ObstaclesSerializingControls()
        {
            UtilsEditor.SeparatorVertical();

            EditorGUILayout.LabelField(SERIALIZE_BUTTON_TIP, UtilsEditor.BoldAndWrappedStyle);

            if (GUILayout.Button(BAKE_OBSTACLES))
            {
                if (m_Loader.IsSerializedBinaryAttached)
                {
                    if (EditorUtility.DisplayDialog(DIALOG_RESERIALIZE_TITLE, DIALOG_RESERIALIZE_MSG, DIALOG_RESERIALIZE_YES, DIALOG_RESERIALIZE_CANCEL))
                    {
                        StartSerializing();
                    }
                }
                else
                {
                    StartSerializing();
                }
            }
        }

        void SerializedObstaclesArray()
        {
            if (m_Loader.IsObstaclesSerialized)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.PropertyField(m_SerializedObstacles, true);
                EditorGUI.EndDisabledGroup();
            }
        }

        void StartSerializing()
        {
            UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

            string filePath = EditorUtility.SaveFilePanelInProject(
                SAVE_DIALOG_TITLE,
                scene.name,
                "bytes",
                SAVE_DIALOG_MESSAGE,
                scene.path);

            if (!string.IsNullOrEmpty(filePath))
            {
                void onFinish(Nav3DObstacle[] _SerializedObstacles)
                {
                    m_State ^= LoaderState.SERIALIZATION_IN_PROGRESS;

                    m_JustSerializedObstacles = _SerializedObstacles;
                }

                m_SerializedFilePath =  filePath;
                m_State              |= LoaderState.SERIALIZATION_IN_PROGRESS;

                m_SerializingProgress = m_Loader.SerializeObstacles(filePath, onFinish);
            }
        }

        void SaveSerializedDatas()
        {
            if (m_JustSerializedObstacles.IsNullOrEmpty())
                return;

            m_SerializedObstacles.arraySize = m_JustSerializedObstacles.Length;

            for (int i = 0; i < m_JustSerializedObstacles.Length; i++)
            {
                SerializedProperty elementProperty = m_SerializedObstacles.GetArrayElementAtIndex(i);
                elementProperty.objectReferenceValue = m_JustSerializedObstacles[i];
            }

            m_MinBucketSizeSerialized.floatValue = m_Initializer.MinBucketSize;

            serializedObject.ApplyModifiedProperties();

            m_Loader.SerializedObstacles.ForEach(_Obstacle => _Obstacle.MarkAsSerialized());
            m_JustSerializedObstacles = null;

            AssetDatabase.SaveAssets();
        }

        void SerializedDataValidationSettings()
        {
            m_ValidateSerializedData.boolValue = EditorGUILayout.Toggle("Validate serialized data", m_ValidateSerializedData.boolValue);
        }

        #endregion
    }
}