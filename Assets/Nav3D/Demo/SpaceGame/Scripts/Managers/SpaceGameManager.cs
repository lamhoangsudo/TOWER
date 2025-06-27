using System;
using Nav3D.API;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Nav3D.Demo
{
    public class SpaceGameManager : MonoBehaviour
    {
        #region Constants

        const int MINERS_COUNT_DEFAULT    = 140;
        const int DEFENDERS_COUNT_DEFAULT = 15;

        #endregion

        #region Serialized fields

        [Space, Header("Agents configs")]
        [SerializeField] Nav3DAgentConfig m_MinerConfig;
        [SerializeField] Nav3DAgentConfig m_DefenderConfig;

        [Space, Header("Agents prefabs")] [SerializeField]
        GameObject m_DefenderPrefab;

        [Space, Header("Scene objects")] [SerializeField]
        Nav3DObstacle m_ObstaclesRoot;

        [SerializeField] ContactPointsController       m_Planet;
        [SerializeField] List<ContactPointsController> m_Mines;
        [SerializeField] List<Transform>               m_GuardWaypoints;

        [Space, Header("UI")] [SerializeField] Text m_LoaderText;

        [SerializeField] bool m_VisualizeMinersPaths;

        #endregion

        #region Attributes

        bool             m_Inited;
        List<Nav3DAgent> m_MinerAgents = new List<Nav3DAgent>(MINERS_COUNT_DEFAULT);

        #endregion

        #region Unity events

        // Start is called before the first frame update
        void Start()
        {
            Nav3DManager.OnNav3DInit += () => Nav3DPathfindingManager.MaxPathfindingTasks = (int) (Environment.ProcessorCount * 0.6f);

            //process obstacles group
            m_ObstaclesRoot.OnObstacleAdded += () => { Debug.Log(m_ObstaclesRoot.AdditionProgress.GetResultStats()); };

            Nav3DManager.OnNav3DInit += InitGame;
        }

        #if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (!Application.isPlaying || !enabled)
                return;

            if (m_VisualizeMinersPaths)
                m_MinerAgents.ForEach(_Miner => _Miner.Draw(false, true, false));
        }
        #endif

        #endregion

        #region Service methods

        //init game actions
        void InitGame()
        {
            if (m_Inited)
                return;

            //disable loader label
            m_LoaderText.gameObject.SetActive(false);

            //begin spawn miners routine
            StartCoroutine(SpawnMiners(null));
            //spawn defenders
            SpawnDefenders(null);

            m_Inited = true;
        }

        //miners spawn routine
        IEnumerator SpawnMiners(GameObject _Prefab)
        {
            //create root object for miners on scene
            GameObject agentsRoot = new GameObject("MinersRoot");

            for (int i = 0; i < MINERS_COUNT_DEFAULT; i++)
            {
                yield return new WaitForSeconds(0.125f);

                //obtain miner config variant
                Nav3DAgentConfig configVariant = m_MinerConfig.Copy();
                //instantiate miner prefab
                GameObject agentGO = DemoHelper.InstantiateAgentConeBody($"miner_{i}", configVariant.Radius, out _, _BodyColor: OrangeTone());
                agentGO.transform.SetParent(agentsRoot.transform);

                //add agent to miner prefab
                Nav3DAgent agent = agentGO.AddComponent<Nav3DAgent>();
                //add controller to miner prefab
                MinerController minerController = agentGO.AddComponent<MinerController>();
                //attach miner config variant
                agent.SetConfig(configVariant);
                //init controller
                minerController.Init(m_Planet, m_Mines[Random.Range(0, m_Mines.Count)]);
                //add agent to agents list
                m_MinerAgents.Add(agent);
            }
        }

        //defenders spawn method
        void SpawnDefenders(GameObject _Prefab)
        {
            //create root object for defenders on scene
            GameObject agentsRoot = new GameObject("DefendersRoot");

            for (int i = 0; i < DEFENDERS_COUNT_DEFAULT; i++)
            {
                //obtain defender config variant
                Nav3DAgentConfig configVariant = m_DefenderConfig.Copy();
                //instantiate defender prefab
                GameObject agentGO = Instantiate(m_DefenderPrefab, Vector3.zero, Quaternion.identity);
                agentGO.transform.SetParent(agentsRoot.transform);

                Nav3DAgent agent = agentGO.AddComponent<Nav3DAgent>();
                //add controller to defender prefab
                DefenderController defenderController = agentGO.AddComponent<DefenderController>();
                agentGO.name = $"Defender_{agent.GetInstanceID()}";
                //attach defender config variant
                agent.SetConfig(configVariant);

                int index = (int) (m_GuardWaypoints.Count * ((float) i / DEFENDERS_COUNT_DEFAULT));
                //init controller
                defenderController.Init(m_GuardWaypoints.ToArray(), index);
            }
        }

        //get orange tone for miner body
        Color OrangeTone()
        {
            return Random.ColorHSV(0.1127f, 0.1805f, 1f, 1f, 1, 1f);
        }

        #endregion
    }
}