using Nav3D.API;
using Nav3D.Common;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nav3D.Demo
{
    public class EvadersScenario : MonoBehaviour
    {
        #region Constants

        const int   EVADERS_COUNT_LIMIT = 200;
        const float SPAWN_DELAY         = 0.3f;

        #endregion

        #region Serialized fields

        [Header("Game entities")]
        [SerializeField] Nav3DObstacle m_Obstacle;
        
        [Space, Header("Agent configs")]
        [SerializeField] Nav3DAgentConfig m_AgentConfig;

        [Space, Header("Prefabs")]
        [SerializeField] GameObject m_EvaderPrefab;

        [Space, Header("Spawn X boundaries")]
        [SerializeField] Transform m_LeftSpawnBorder;
        [SerializeField] Transform m_RightSpawnBorder;

        [Space, Header("Spawn Y and Z constraints")]
        [SerializeField] float m_YMin;
        [SerializeField] float m_YMax;
        [SerializeField] float m_ZMin;
        [SerializeField] float m_ZMax;

        [Space, Header("Debug draw")]
        [SerializeField] bool m_VisualizeAgents;

        #endregion

        #region Attributes

        readonly List<Nav3DAgent> m_Agents = new List<Nav3DAgent>();

        int m_EvadersCountCurrent;

        Transform m_EvadersRoot;

        #endregion

        #region Unity events

        void Start()
        {
            Nav3DManager.OnNav3DInit += () =>
            {
                m_EvadersRoot = new GameObject("EvadersRoot").transform;

                //start spawn agents coroutine
                StartCoroutine(SpawnAgents(SPAWN_DELAY));

                //start evaders respawn routine
                StartCoroutine(CheckEvadersCount());
            };

            m_Obstacle.OnObstacleAdded += () =>
            {
                UnityEngine.Debug.Log(m_Obstacle.AdditionProgress.GetResultStats());
            };
        }

        #if UNITY_EDITOR

        void OnDrawGizmos()
        {
            if (!Application.isPlaying || !enabled)
                return;

            if (m_VisualizeAgents)
            {
                foreach (Nav3DAgent agent in m_Agents)
                    agent.Draw(true, true, true);
            }
        }

        #endif

        #endregion

        #region Service methods

        void SpawnEvader()
        {
            GameObject evaderGO = Instantiate(m_EvaderPrefab, GetEvadersPosition(), Quaternion.identity, m_EvadersRoot);

            //enable evader destroying if it goes out of boundaries
            Nav3DEvader evader = evaderGO.GetComponent<Nav3DEvader>();
            evader.OnPositionChanged += _Position =>
            {
                if (_Position.x > m_RightSpawnBorder.position.x || _Position.x < m_LeftSpawnBorder.position.x ||
                    _Position.y > m_YMax                        || _Position.y < m_YMin                       ||
                    _Position.z > m_ZMax                        || _Position.z < m_ZMin)
                {
                    m_EvadersCountCurrent--;
                    Destroy(evaderGO);
                }
            };

            float radius   = UtilsMath.GetRandomUniformValue(0.1f, 0.5f);
            float diameter = radius * 2f;
            
            evader.Radius                 = radius;
            evaderGO.transform.localScale = new Vector3(diameter, diameter, diameter);

            m_EvadersCountCurrent++;
        }

        Vector3 GetEvadersPosition()
        {
            Vector3 position;

            do
            {
                position = new Vector3(
                        Random.Range(m_LeftSpawnBorder.position.x, m_RightSpawnBorder.position.x),
                        Random.Range(m_YMin, m_YMax),
                        Random.Range(m_ZMin, m_ZMax)
                    );
            } while (Nav3DManager.IsPointInsideOccupiedVolume(position));

            return position;
        }

        IEnumerator CheckEvadersCount()
        {
            while (true)
            {
                while (m_EvadersCountCurrent < EVADERS_COUNT_LIMIT)
                {
                    SpawnEvader();
                }

                yield return new WaitForSeconds(2);
            }
        }

        IEnumerator SpawnAgents(float _Delay)
        {
            GameObject agentsRoot = new GameObject("AgentsRoot");

            int i = 0;

            while (true)
            {
                //get agent config variant
                Nav3DAgentConfig configVariant = m_AgentConfig.Copy();

                configVariant.Radius = UtilsMath.GetRandomNormalValue(0.15f, 0.35f);
                
                //create agent's GameObject
                GameObject agentGO = DemoHelper.InstantiateAgentConeBody($"agent_{i}", configVariant.Radius, out _);

                agentGO.transform.SetParent(agentsRoot.transform);

                Nav3DAgent agent = agentGO.AddComponent<Nav3DAgent>();

                m_Agents.Add(agent);

                //apply config variant
                agent.SetConfig(configVariant);

                Vector3 initialPosition = GetAgentPosition(out bool left);
                agent.transform.position = initialPosition;

                agent.MoveTo(
                        new Vector3(
                                left ? m_RightSpawnBorder.position.x : m_LeftSpawnBorder.position.x,
                                initialPosition.y,
                                initialPosition.z
                            ),
                        () => { Destroy(agentGO); },
                        _ => Destroy(agentGO)
                    );

                i++;
                yield return new WaitForSeconds(_Delay);
            }
        }

        Vector3 GetAgentPosition(out bool _Left)
        {
            _Left = Random.Range(0, 2) == 0;

            return new Vector3(
                    _Left ? m_LeftSpawnBorder.position.x : m_RightSpawnBorder.position.x,
                    Random.Range(m_YMin, m_YMax),
                    Random.Range(m_ZMin, m_ZMax)
                );
        }

        #endregion
    }
}