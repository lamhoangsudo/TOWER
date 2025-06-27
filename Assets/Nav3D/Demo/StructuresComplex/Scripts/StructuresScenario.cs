using Nav3D.API;
using Nav3D.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Nav3D.Demo
{
    public class StructuresScenario : MonoBehaviour
    {
        #region Constants

        const int AGENTS_PREFETCH_NUMBER = 150;

        #endregion

        #region Serialized fields

        [Header("Obstacles setup")]
        [SerializeField] Nav3DObstacle m_Obstacle;

        [Header("Obstacles drawing")]
        [SerializeField] bool m_DrawOccupiedLeaves;
        [SerializeField] bool m_DrawFreeLeaves;
        [SerializeField] bool m_DrawGraph;

        [Header("Agents setup")]
        [SerializeField]
        Nav3DAgentConfig m_AgentConfig;

        [Header("Scenario setup")]
        [SerializeField] int m_AgentsCount;

        [SerializeField] float m_SpawnDelay;
        [SerializeField] List<Transform> m_Targets = new List<Transform>();
        [SerializeField] Transform m_Spawn;

        [Header("Agents drawing")]
        [SerializeField] bool m_DrawPaths;
        
        [SerializeField] bool m_DrawAgents;

        [Header("UI")]
        [SerializeField] Text m_LoaderText;

        #endregion

        #region Attributes

        Queue<Nav3DAgent> m_AgentsPrefetched = new Queue<Nav3DAgent>();
        List<Nav3DAgent> m_AgentsOperating = new List<Nav3DAgent>(AGENTS_PREFETCH_NUMBER);

        #endregion

        #region Unity events

        void Start()
        {
            Nav3DManager.OnNav3DInit += () =>
            {
                //set specific pathfinding tasks limit
                Nav3DPathfindingManager.MaxPathfindingTasks = (int)(Environment.ProcessorCount * 0.8f);

                Debug.Log(m_Obstacle.AdditionProgress.GetResultStats());

                //prefetch the agents on scene
                PrefetchAgents(m_AgentsCount == 0 ? AGENTS_PREFETCH_NUMBER : m_AgentsCount);

                m_LoaderText.gameObject.SetActive(false);

                //start agents spawn
                StartCoroutine(AgentProductionRoutine());
            };
        }

        #if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (!Application.isPlaying || !enabled)
                return;

            if (m_DrawAgents || m_DrawPaths)
            {
                foreach (Nav3DAgent agent in m_AgentsOperating)
                    agent.Draw(true, m_DrawPaths, true);
            }
        }
        #endif

        #endregion

        #region Service methods

        void PrefetchAgents(int _Number)
        {
            GameObject agentsRoot = new GameObject("AgentsRoot");

            using (UtilsCommon.RandomSeedPermanence)
            {
                for (int i = 0; i < _Number; i++)
                {
                    //obtain config variant
                    Nav3DAgentConfig config = m_AgentConfig.Copy();

                    float radius = UtilsMath.GetRandomNormalValue(0.05f, 0.075f);
                    
                    config.Radius = radius;
                    //correct speed depending on the radius
                    config.Speed *= radius + 1.1f;
                    
                    //instantiate agent body
                    GameObject agentGO = DemoHelper.InstantiateAgentConeBody($"agent_{i}", radius, out _);
                    agentGO.transform.SetParent(agentsRoot.transform);

                    //add Nav3DAgent to agent's body
                    Nav3DAgent agent = agentGO.AddComponent<Nav3DAgent>();

                    //apply config variant to agent
                    agent.SetConfig(config);

                    m_AgentsPrefetched.Enqueue(agent);

                    agent.gameObject.SetActive(false);
                }
            }
        }

        IEnumerator AgentProductionRoutine()
        {
            while (true)
            {
                if (m_AgentsPrefetched.Count == 0)
                {
                    yield return null;
                    continue;
                }

                //get agent from queue
                Nav3DAgent agent = m_AgentsPrefetched.Dequeue();

                //set random position around spawn area to avoid agent crowding in one point
                agent.transform.position = m_Spawn.position + UtilsMath.GetRandomVector(0.4f);
                agent.gameObject.SetActive(true);

                //start moving to random target
                MoveToRandomTarget(agent);

                m_AgentsOperating.Add(agent);

                yield return new WaitForSeconds(m_SpawnDelay == 0 ? 0.5f : m_SpawnDelay);
            }
        }

        void MoveToRandomTarget(Nav3DAgent _Agent)
        {
            Transform randomTarget = m_Targets[Random.Range(0, m_Targets.Count)];

            _Agent.MoveTo(
                randomTarget.position,
                () => EnqueueAgent(_Agent), //hide agent when target reached
                                            //check error type.
                                            //if error caused by timeout, then start moving to random point
                                            //else hide agent to queue
                _Error =>
                {
                    Debug.LogWarning($"{_Agent.name}: {_Error.Msg}");

                    if (_Error.Reason == PathfindingResultCode.TIMEOUT)
                        MoveToRandomTarget(_Agent);
                    else
                        EnqueueAgent(_Agent);
                }
            );
        }

        //deactivate agent and hide it to queue
        void EnqueueAgent(Nav3DAgent _Agent)
        {
            _Agent.gameObject.SetActive(false);
            m_AgentsPrefetched.Enqueue(_Agent);
            m_AgentsOperating.Remove(_Agent);
        }

        #endregion
    }
}