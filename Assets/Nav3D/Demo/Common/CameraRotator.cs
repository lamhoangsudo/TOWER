using UnityEngine;

namespace Nav3D.Demo
{
    public class CameraRotator : MonoBehaviour
    {
        #region Serialized fields

        [SerializeField] Transform m_Target;
        [SerializeField] int m_MouseRotateKeyCode = 0;
        [SerializeField] float m_Sensitivity = 3;
        [SerializeField] float m_YLimit = 80;
        [SerializeField] float m_Zoom = 0.25f;
        [SerializeField] float m_ZoomMaxDist = 20f;
        [SerializeField] float m_ZoomMinDist = 3;

        #endregion

        #region Attributes

        Transform m_Transform;
        Vector3 m_Offset;
        
        float m_X, m_Y;

        #endregion

        #region Properties

        KeyCode CameraRotationMouseKey
        {
            get
            {
                switch (m_MouseRotateKeyCode)
                {
                    case 0:
                        return KeyCode.Mouse0;
                    case 1:
                        return KeyCode.Mouse1;
                    case 2:
                        return KeyCode.Mouse2;
                    default:
                        return KeyCode.Mouse0;
                }
            }
        }

        #endregion

        #region Unity events

        void Start()
        {
            m_Transform = transform;

            m_YLimit = Mathf.Abs(m_YLimit);

            if (m_YLimit > 90) m_YLimit = 90;

            m_Offset             = new Vector3(m_Offset.x, m_Offset.y, -Mathf.Abs(m_ZoomMaxDist) / 2);
            m_Transform.position = m_Target != null ? m_Target.position : Vector3.zero + m_Offset;
        }

        void Update()
        {
            if (Input.GetAxis("Mouse ScrollWheel") > 0)
                m_Offset.z += m_Zoom;
            else if (Input.GetAxis("Mouse ScrollWheel") < 0)
                m_Offset.z -= m_Zoom;

            m_Offset.z = Mathf.Clamp(m_Offset.z, -Mathf.Abs(m_ZoomMaxDist), -Mathf.Abs(m_ZoomMinDist));

            if (Input.GetKey(CameraRotationMouseKey))
            {
                m_X = m_Transform.localEulerAngles.y + Input.GetAxis("Mouse X") * m_Sensitivity;
                m_Y += Input.GetAxis("Mouse Y") * m_Sensitivity;
                m_Y = Mathf.Clamp(m_Y, -m_YLimit, m_YLimit);
            }

            m_Transform.localEulerAngles = new Vector3(-m_Y, m_X, 0);
            m_Transform.position         = m_Transform.localRotation * m_Offset + (m_Target != null ? m_Target.position : Vector3.zero);
        }

        #endregion
    }
}