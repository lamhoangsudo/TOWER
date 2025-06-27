using Nav3D.API;
using Nav3D.Common;
using System.Collections;
using UnityEngine;

namespace Nav3D.Demo
{
    public class EnemySpawner : MonoBehaviour
    {
        #region Constants

        const float PERIOD_MIN = 6f;
        const float PERIOD_MAX = 8f;

        #endregion

        #region Serialized fields

        [SerializeField] ContactPointsController m_TargetController;
        [SerializeField] Nav3DAgentConfig        m_EnemyConfig;

        #endregion

        #region Attributes

        int m_Counter = 0;
        Transform m_EnemiesRoot;
        float m_Period;

        #endregion

        #region Service methods

        void DoStart()
        {
            //get random spawn period
            m_Period = Random.Range(PERIOD_MIN, PERIOD_MAX);

            //create the root gameobject for all spawned enemies 
            m_EnemiesRoot = new GameObject($"{name}_EnemiesRoot").transform;

            //use agents only after Nav3D initialization 
            Nav3DManager.OnNav3DInit += InitSpawner;
        }

        void InitSpawner()
        {
            //start spawning agents
            StartCoroutine(SpawnRoutine());
        }

        void Spawn()
        {
            //create enemy config variant
            Nav3DAgentConfig config = m_EnemyConfig.Copy();

            //instantiate cone body for it
            GameObject agentGO = DemoHelper.InstantiateAgentConeBody($"{name}_Enemy_{m_Counter++}", config.Radius, out _, _BodyColor: BlueTone());

            //set random position relative to the spawner
            agentGO.transform.position = transform.position + UtilsMath.RandomNormal;
            agentGO.transform.SetParent(m_EnemiesRoot);

            Nav3DAgent agent = agentGO.AddComponent<Nav3DAgent>();
            
            //add controller to enemy gameObject
            EnemyController enemyController = agentGO.AddComponent<EnemyController>();
            
            //apply config to agent
            agent.SetConfig(config);

            //start move to target
            enemyController.Init(m_TargetController.GetClosestTouchPoint(agentGO.transform.position));
        }

        //generate random blue tone
        Color BlueTone()
        {
            return Random.ColorHSV(0.6f, 0.7f, 1f, 1f, 1, 1f);
        }

        //periodically spawn the enemy
        IEnumerator SpawnRoutine()
        {
            while (true)
            {
                Spawn();

                yield return new WaitForSeconds(m_Period);
            }
            // ReSharper disable once IteratorNeverReturns
        }

        #endregion

        #region Unity events

        void Start()
        {
            DoStart();
        }

        #endregion
    }
}