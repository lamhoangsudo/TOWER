using Nav3D.API;
using System;
using System.Collections;
using UnityEngine;

namespace Nav3D.Demo
{
    [RequireComponent(typeof(Nav3DAgent))]
    public class MinerController : MonoBehaviour
    {
        #region Attributes

        Nav3DAgent m_Agent;
        
        ContactPointsController m_Mine;
        ContactPointsController m_Planet;

        Vector3 m_MineContactPoint;
        Vector3 m_PlanetContactPoint;

        #endregion

        #region Public methods

        //attach planet and mine transforms
        //the start game logic loop
        public void Init(ContactPointsController _Planet, ContactPointsController _Mine)
        {
            m_Planet = _Planet;
            m_Mine = _Mine;

            //set miner position equals to planet one, then move to mine position
            TeleportToPlanetAndGo(true);
        }

        #endregion

        #region Service methods

        //looped game logic that makes the agent go to it's mine, and then back to planet and so on.
        void TeleportToPlanetAndGo(bool _ForceTpToPlanet = false)
        {
            if (_ForceTpToPlanet)
            {
                transform.position = m_Planet.GetRandomFreeTouchPoint() ?? m_Planet.transform.position;
            }

            SelectMinePointAndGo();
        }

        void SelectMinePointAndGo()
        {
            Vector3? touchPoint = m_Mine.GetClosestFreeTouchPoint(transform.position);

            if (touchPoint.HasValue)
            {
                m_Planet.ReleaseTouchPoint(m_PlanetContactPoint);

                m_MineContactPoint = touchPoint.Value;

                m_Mine.OccupyTouchPoint(m_MineContactPoint);

                m_Agent.MoveTo(m_MineContactPoint, OnMineReached, ErrorHandler);
            }
            else
            {
                StartCoroutine(DoAfterDelay(SelectMinePointAndGo, 3));
            }
        }

        //just do something after delay
        IEnumerator DoAfterDelay(Action _Do, float _Delay)
        {
            yield return new WaitForSeconds(_Delay);

            _Do?.Invoke();
        }

        #endregion

        #region Callbacks

        //mine reaching handler
        void OnMineReached()
        {
            //after delay start moving to planet
            StartCoroutine(
                DoAfterDelay(
                    () =>
                    {
                        Vector3? touchPoint = m_Planet.GetClosestFreeTouchPoint(transform.position);

                        if (touchPoint.HasValue)
                        {
                            m_Mine.ReleaseTouchPoint(m_MineContactPoint);

                            m_PlanetContactPoint = touchPoint.Value;

                            m_Planet.OccupyTouchPoint(m_PlanetContactPoint);

                            m_Agent.MoveTo(m_PlanetContactPoint, SelectMinePointAndGo, ErrorHandler);
                        }
                        else
                        {
                            StartCoroutine(DoAfterDelay(OnMineReached, 3));
                        }
                    },
                    1f)
            );
        }

        void ErrorHandler(PathfindingError _Error)
        {
            Debug.LogWarning($"{name}: {_Error.Msg}");

            m_Mine.ReleaseTouchPoint(m_MineContactPoint);
            m_Planet.ReleaseTouchPoint(m_PlanetContactPoint);

            TeleportToPlanetAndGo(true);
        }

        #endregion

        #region Unity events

        void Awake()
        {
            m_Agent = GetComponent<Nav3DAgent>();
        }

        #endregion
    }
}