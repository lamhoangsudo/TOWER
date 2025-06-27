using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using System.Threading.Tasks;
using Nav3D.Common;
using Nav3D.Obstacles;
using Nav3D.Obstacles.Serialization;
using ObstacleSerializingStatus = Nav3D.Obstacles.Serialization.ObstacleSerializingProgress.ObstacleSerializingStatus;
using ObstacleDeserializingStatus = Nav3D.Obstacles.Serialization.ObstacleDeserializingProgress.ObstacleDeserializingStatus;

namespace Nav3D.API
{
    [ExecuteInEditMode]
    public class Nav3DObstacleLoader : MonoBehaviour
    {
        #region Nested types

        [Flags]
        public enum LoaderState
        {
            NONE                                          = 0,
            INITIALIZER_NOT_FOUND                         = 1,
            INITIALIZER_INVALID_SETTINGS                  = 2,
            SERIALIZABLE_OBSTACLES_FOUND                  = 4,
            SERIALIZABLE_OBSTACLES_IS_NOT_FOUND           = 8,
            SERIALIZED_OBSTACLES_HAS_CHANGED              = 16,
            SERIALIZED_INITIALIZER_PARAMETERS_HAS_CHANGED = 32,
            NOT_SERIALIZED_OBSTACLES_FOUND                = 64,
            SERIALIZATION_IN_PROGRESS                     = 128,
            SERIALIZATION_COMPLETED                       = 256
        }

        #endregion

        #region Constants

        readonly string INITIALIZER_NOT_FOUND_ERROR = $"Serialization error. {nameof(Nav3DInitializer)} component is not found on scene.";

        readonly string NO_SERIALIZABLE_OBSTACLES = $"There are no {nameof(Nav3DObstacle)} on scene to be serialized." +
                                                    $"For obstacles that you want to serialize, set the {nameof(Nav3DObstacle.DataSource)} parameter to {nameof(Nav3DObstacle.ObstacleDataSource.LOAD_FROM_BINARY)}.";

        const string BINARY_DATA_IS_NULL_OR_EMPTY_ERROR = "Unable to deserialize obstacle. Binary data is empty or corrupt. Rebake binary.";

        readonly string BINARY_DATA_IS_NOT_ATTACHED_ERROR =
            $"Unable to deserialize obstacle. There is no binary data attached into the field {nameof(m_BinaryData)}. Attach a binary.";

        const string NO_SERIALIZED_OBSTACLES = "No serialized obstacles. Bake obstacles in editor mode.";

        const string SERIALIZED_OBSTACLES_HAS_CHANGED =
            "The last serialized data is not valid because one of the baked obstacles was changed or removed. Re-bake obstacles in editor mode.";

        const string IMPRINT_MISMATCH_ERROR = "Mismatch between the actual obstacles on the scene and the loaded data. " +
                                              "The obstacles on the scene were likely changed after baking and serialization. Re-bake scene obstacles.";

        readonly string MIN_BUCKET_MISMATCH_ERROR =
            $"Mismatch between the actual {nameof(Nav3DInitializer)}.{nameof(Nav3DInitializer.MinBucketSize)}: {{0}} " +
            $"and loaded {nameof(MinBucketSizeSerialized)}: {{1}}. Re-bake obstacles with the current minimum bucket size.";

        const string MENU_PATH = "Nav3D/Create Obstacle loader";

        readonly string INSTANCE_HAS_EXIST_ON_SCENE_ERROR =
            $"An instance of {nameof(Nav3DObstacleLoader)} is already present in the scene, duplication is not possible.";

        static readonly string ON_INIT_UNSUBSCRIBE_ERROR = $"There is no need to unsubscribe from the {nameof(OnLoadingFinished)} event. " +
                                                           $"All subscriptions will be invoked and unsubscribed after {nameof(Nav3DObstacleLoader)} performs obstacles loading.";

        #endregion

        #region Attributes

        readonly List<Action> m_OnLoadSubscribers = new List<Action>();

        event Action m_OnLoadingFinished;

        #endregion

        #region Events

        public event Action OnLoadingFinished
        {
            add
            {
                if (value == null)
                    return;

                if (LoadIsFinished)
                {
                    ThreadDispatcher.BeginInvoke(value);

                    return;
                }

                Action subscriber = () => ThreadDispatcher.BeginInvoke(value);

                m_OnLoadSubscribers.Add(subscriber);

                m_OnLoadingFinished += subscriber;
            }
            remove { Debug.LogError(ON_INIT_UNSUBSCRIBE_ERROR); }
        }

        #endregion

        #region Serialized fields

        [SerializeField] TextAsset       m_BinaryData;
        [SerializeField] float           m_MinBucketSizeSerialized;
        [SerializeField] Nav3DObstacle[] m_SerializedObstacles;
        [SerializeField] bool            m_ValidateSerializedData = true;

        #endregion

        #region Properties

        public ObstacleDeserializingProgress DeserializingProgress { get; private set; }

        /// <summary>
        /// Indicates that the obstacles are successfully baked and serialized.
        /// </summary>
        public bool IsObstaclesSerialized => m_BinaryData != null && !m_SerializedObstacles.IsNullOrEmpty();

        public bool IsSerializedBinaryAttached => m_BinaryData != null;

        public Nav3DObstacle[] SerializedObstacles     => m_SerializedObstacles;
        public float           MinBucketSizeSerialized => m_MinBucketSizeSerialized;
        public bool            LoadIsFinished          { get; private set; }

        #endregion

        #region Public methods

        #if UNITY_EDITOR

        [UnityEditor.MenuItem(MENU_PATH)]
        public static void CreateOnScene()
        {
            UtilsEditor.InstantiateGOWithComponent<Nav3DObstacleLoader>(nameof(Nav3DObstacleLoader));
        }

        #endif

        public bool IsObstacleSerialized(Nav3DObstacle _Obstacle)
        {
            return m_SerializedObstacles.Contains(_Obstacle);
        }

        public Nav3DObstacle[] GetAllSerializableObstacles()
        {
            return
                FindObjectsOfType<Nav3DObstacle>()
                   .Where(
                            _Obstacle => _Obstacle.gameObject.activeInHierarchy &&
                                         _Obstacle.DataSource == Nav3DObstacle.ObstacleDataSource.LOAD_FROM_BINARY
                        )
                   .ToArray();
        }

        public ObstacleSerializingProgress SerializeObstacles(string _FilePath, Action<Nav3DObstacle[]> _OnFinish)
        {
            Nav3DInitializer initializer = FindObjectOfType<Nav3DInitializer>();

            if (initializer == null)
                throw new Exception(INITIALIZER_NOT_FOUND_ERROR);

            Nav3DObstacle[] obstaclesToSerialize = GetAllSerializableObstacles();

            if (!obstaclesToSerialize.Any())
                throw new Exception(NO_SERIALIZABLE_OBSTACLES);

            Nav3DManager.InitNav3DEditMode(initializer.MinBucketSize);

            ObstacleSerializingProgress obstacleSerializingProgress = ObstacleSerializingProgress.INITIAL;

            List<ObstacleInfoBase> preparedObstacleInfos = new List<ObstacleInfoBase>(obstaclesToSerialize.Length);
            List<string>           resultingImprint      = new List<string>(obstaclesToSerialize.Length);

            for (int i = 0; i < obstaclesToSerialize.Length; i++)
            {
                Nav3DObstacle obstacle = obstaclesToSerialize[i];

                List<ObstacleInfoBase> obstaclePreparedInfos = obstacle.PrepareObstacleInfos(out string[] processablesImprint);

                //Replace current InstanceIDs to element array index
                //On deserializing it will need to do opposite replacement: index -> InstanceID
                obstaclePreparedInfos.ForEach(_Info => _Info.ReplaceID(obstacle.InstanceID, i));

                preparedObstacleInfos.AddRange(obstaclePreparedInfos);
                resultingImprint.AddRange(processablesImprint);
            }

            Task.Factory.StartNew(
                    () =>
                    {
                        try
                        {
                            obstacleSerializingProgress.SetStatus(ObstacleSerializingStatus.BAKING_OBSTACLES);

                            ObstacleAdditionProgress additionProgress = null;

                            additionProgress = ObstacleManager.Instance.AddObstacles(
                                    preparedObstacleInfos,
                                    () =>
                                    {
                                        if (obstacleSerializingProgress.CancellationToken.IsCancellationRequested)
                                            return;

                                        obstacleSerializingProgress.SetStatus(
                                                ObstacleSerializingStatus
                                                   .PACKING_BAKED_DATA
                                            );

                                        Dictionary<ObstacleInfoBase, Obstacle> obstacleDatas =
                                            ObstacleManager.Instance.GetObstacleDatas();

                                        //packed data
                                        ObstacleDataSerializable obstacleDataSerializable = new ObstacleDataSerializable(obstacleDatas, resultingImprint.ToArray(), obstacleSerializingProgress);

                                        if (obstacleSerializingProgress.CancellationToken.IsCancellationRequested)
                                            return;

                                        try
                                        {
                                            UtilsSerialization.SerializeObstacleData(
                                                    obstacleDataSerializable,
                                                    _FilePath,
                                                    obstacleSerializingProgress
                                                );
                                        }
                                        catch (Exception _Exception)
                                        {
                                            Debug.LogException(_Exception);
                                        }

                                        if (obstacleSerializingProgress.CancellationToken
                                                                       .IsCancellationRequested)
                                            return;

                                        obstacleSerializingProgress.SetStatus(
                                                ObstacleSerializingStatus.FINISHED
                                            );

                                        Debug.Log(obstacleSerializingProgress.GetResultStats());

                                        _OnFinish?.Invoke(obstaclesToSerialize);
                                    },
                                    true
                                );

                            obstacleSerializingProgress.SetObstacleAdditionProgress(additionProgress);
                        }
                        catch (Exception _Exception)
                        {
                            Debug.LogException(_Exception);
                        }
                    },
                    TaskCreationOptions.LongRunning
                );

            return obstacleSerializingProgress;
        }

        public bool ValidateSerializedObstacles(bool _OnAwake = false)
        {
            return !m_SerializedObstacles.Any(
                    _Obstacle =>
                        _Obstacle == null                       || //obstacle has removed
                        !_Obstacle.gameObject.activeInHierarchy || //obstacle has disabled
                        //if check occurs on Awake, then some obstacles may have not yet reset the TransformHasChanged flag, because they also reset it on Awake.
                        (!_OnAwake && _Obstacle.TransformHasChanged) || //obstacle's transform has changed
                        _Obstacle.DataSource !=
                        Nav3DObstacle.ObstacleDataSource.LOAD_FROM_BINARY //obstacle datasource was changed
                );
        }

        #endregion

        #region Service methods

        void LoadObstacles()
        {
            if (!m_SerializedObstacles.Any())
            {
                Debug.LogError(NO_SERIALIZED_OBSTACLES);

                FinishLoading(true);

                return;
            }

            if (!ValidateSerializedObstacles(true))
            {
                Debug.LogError(SERIALIZED_OBSTACLES_HAS_CHANGED);

                FinishLoading(true);

                return;
            }

            if (m_BinaryData == null)
            {
                Debug.LogError(BINARY_DATA_IS_NOT_ATTACHED_ERROR);

                FinishLoading(true);

                return;
            }

            if (m_BinaryData.bytes.IsNullOrEmpty())
            {
                Debug.LogError(BINARY_DATA_IS_NULL_OR_EMPTY_ERROR);

                FinishLoading(true);

                return;
            }

            m_SerializedObstacles.ForEach(_Obstacle => _Obstacle.Init());

            Nav3DManager.OnNav3DPreInit += () =>
            {
                if (!ValidateInitializerParameters(out float initializerMinBucketSize))
                    throw new Exception(
                            string.Format(
                                    MIN_BUCKET_MISMATCH_ERROR,
                                    initializerMinBucketSize,
                                    m_MinBucketSizeSerialized
                                )
                        );

                byte[] binary = m_BinaryData.bytes;

                List<string> actualImprint = new List<string>();

                if (m_ValidateSerializedData)
                    foreach (Nav3DObstacle serializedObstacle in m_SerializedObstacles)
                    {
                        actualImprint.AddRange(serializedObstacle.GetImprint());
                    }

                Task.Factory.StartNew(
                        () =>
                        {
                            try
                            {
                                if (ObstacleManager.Doomed)
                                    return;

                                Dictionary<ObstacleInfoBase, Obstacle> deserializedData = LoadObstacleData(binary, actualImprint.ToArray());

                                //Replace array element index to current InstanceID
                                foreach (ObstacleInfoBase obstacleInfo in
                                         deserializedData.Keys)
                                {
                                    for (int i = 0; i < m_SerializedObstacles.Length; i++)
                                        obstacleInfo
                                           .ReplaceID(i, m_SerializedObstacles[i].InstanceID);
                                }

                                ObstacleManager.Instance.AddBakedObstacles(
                                        deserializedData,
                                        () =>
                                        {
                                            foreach (Nav3DObstacle serializedObstacle in m_SerializedObstacles)
                                            {
                                                serializedObstacle.InvokeOnObstacleAdded();
                                            }
                                            
                                            FinishLoading();
                                        }
                                    );
                            }
                            catch (Exception _Exception)
                            {
                                DeserializingProgress.Fail();

                                Debug.LogException(_Exception);

                                FinishLoading(true);
                            }
                        },
                        TaskCreationOptions.LongRunning
                    );
            };
        }

        void FinishLoading(bool _Failed = false)
        {
            if (_Failed)
                DeserializingProgress = ObstacleDeserializingProgress.FAILED;

            MarkAsLoaded();
        }

        void MarkAsLoaded()
        {
            LoadIsFinished = true;

            m_OnLoadingFinished?.Invoke();

            UnsubscribeOnLoadSubscribers();
        }

        bool ValidateImprints(string[] _ActualImprint, ObstacleDataSerializable _LoadedData)
        {
            List<string> loadedImprint = new List<string>(_LoadedData.ImprintData);

            foreach (string imprintHunk in _ActualImprint)
            {
                if (loadedImprint.RemoveAll(_String => string.CompareOrdinal(_String, imprintHunk) == 0) == 0)
                    return false;
            }

            return loadedImprint.Count == 0;
        }

        bool ValidateInitializerParameters(out float _InitializerMinBucketSize)
        {
            Nav3DInitializer initializer = FindObjectOfType<Nav3DInitializer>();

            _InitializerMinBucketSize = initializer.MinBucketSize;

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            return m_MinBucketSizeSerialized == initializer.MinBucketSize;
        }

        Dictionary<ObstacleInfoBase, Obstacle> LoadObstacleData(byte[] _Binary, string[] _ActualImprint)
        {
            DeserializingProgress = new ObstacleDeserializingProgress();

            ObstacleDataSerializable data = UtilsSerialization.DeserializeObstacleData(_Binary, DeserializingProgress);

            if (m_ValidateSerializedData && !ValidateImprints(_ActualImprint, data))
                throw new Exception(IMPRINT_MISMATCH_ERROR);

            DeserializingProgress.SetStatus(ObstacleDeserializingStatus.UNPACKING);

            Dictionary<ObstacleInfoBase, Obstacle> unpackedData = data.Unpack(DeserializingProgress);

            DeserializingProgress.SetStatus(ObstacleDeserializingStatus.FINISHED);

            return unpackedData;
        }

        void UnsubscribeOnLoadSubscribers()
        {
            foreach (Action subscriber in m_OnLoadSubscribers)
            {
                m_OnLoadingFinished -= subscriber;
            }

            m_OnLoadSubscribers.Clear();
        }

        #endregion

        #region Unity events

        void Awake()
        {
            #if UNITY_EDITOR

            if (FindObjectsOfType<Nav3DObstacleLoader>().Any(_Instance => _Instance != this))
            {
                Debug.LogError(INSTANCE_HAS_EXIST_ON_SCENE_ERROR);

                UtilsCommon.SmartDestroy(this);

                return;
            }

            if (!Application.isPlaying || !enabled)
                return;

            #endif

            LoadObstacles();
        }

        #endregion
    }
}