using UnityEngine;

namespace Nav3D.Demo
{
    public class CameraCircleMover : MonoBehaviour
    {
        #region Serializd fields

        [SerializeField] Transform m_LookPoint;
        [SerializeField] Transform m_RotationPivot;
        [SerializeField] float m_DegreesSpeed;

        #endregion

        #region Unity events

        void FixedUpdate()
        {
            transform.RotateAround(m_RotationPivot.position, m_RotationPivot.up, m_DegreesSpeed);
            transform.LookAt(m_LookPoint);
        }

        #endregion
    }
}
