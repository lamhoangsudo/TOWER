using Nav3D.Common;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nav3D.Demo
{
    public class ContactPointsController : MonoBehaviour
    {
        #region Constants

        const int CONTACT_POINTS_COUNT = 15;
        const float CONTACT_RADIUS = 0.65f;

        #endregion

        #region Serialized fields

        [Space, Header("Values")]
        [SerializeField] int m_ContactPointsCount = CONTACT_POINTS_COUNT;
        [SerializeField] float m_ContactRadius = CONTACT_RADIUS;

        [Space, Header("Debug parameters")]
        [SerializeField] bool m_DrawContactPoints;

        #endregion

        #region Attributes

        List<Vector3> m_Points = new List<Vector3>(CONTACT_POINTS_COUNT);
        List<Vector3> m_FreePoints = new List<Vector3>(CONTACT_POINTS_COUNT);
        List<Vector3> m_OccupiedPoints = new List<Vector3>(CONTACT_POINTS_COUNT);

        #endregion

        #region Public methods

        public Vector3? GetClosestFreeTouchPoint(Vector3 _NearestPoint)
        {
            return m_FreePoints.Any() ? m_FreePoints.MinBy(_Point => Vector3.SqrMagnitude(_Point - _NearestPoint)) : (Vector3?)null;
        }

        public Vector3? GetRandomFreeTouchPoint()
        {
            return m_FreePoints.Any() ? m_FreePoints[Random.Range(0, m_FreePoints.Count)] : (Vector3?)null;
        }

        public Vector3 GetClosestTouchPoint(Vector3 _NearestPoint)
        {
            return m_Points.MinBy(_Point => Vector3.SqrMagnitude(_Point - _NearestPoint));
        }

        /// <summary>
        /// When someone reaches one of the touch points, he  report it to ContactPointsController.
        /// The latter remembers that the point is occupied.
        /// </summary>
        public void OccupyTouchPoint(Vector3 _Point)
        {
            if (m_FreePoints.Contains(_Point))
            {
                m_FreePoints.Remove(_Point);
                m_OccupiedPoints.Add(_Point);
            }
        }

        /// <summary>
        /// When someone leaves one of the touch points, he report it to ContactPointsController.
        /// The latter thinks that the point is free.
        /// </summary>
        public void ReleaseTouchPoint(Vector3 _Point)
        {
            if (m_OccupiedPoints.Contains(_Point))
            {
                m_OccupiedPoints.Remove(_Point);
                m_FreePoints.Add(_Point);
            }
        }

        #endregion

        #region Unity events

        void Start()
        {
            m_FreePoints = UtilsMath.GetSphereSurfacePoints(transform.position, m_ContactRadius, m_ContactPointsCount).ToList();

            m_Points.AddRange(m_FreePoints);
        }

#if UNITY_EDITOR
void OnDrawGizmos()
        {
            if (!Application.isPlaying || !enabled)
                return;

            if (m_DrawContactPoints)
            {
                using (Common.Debug.UtilsGizmos.ColorPermanence)
                {
                    Gizmos.color = Color.green;
                    m_FreePoints.ForEach(_Point => Gizmos.DrawWireSphere(_Point, 0.1f));

                    Gizmos.color = Color.red;
                    m_OccupiedPoints.ForEach(_Point => Gizmos.DrawWireSphere(_Point, 0.1f));
                }
            }
        }
#endif

        #endregion
    }
}