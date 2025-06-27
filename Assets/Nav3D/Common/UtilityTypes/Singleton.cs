using UnityEngine;

namespace Nav3D.Common
{
    public static class Singleton<T> where T : MonoBehaviour
    {
        #region Attributes

        static T m_Instance;
        // ReSharper disable once StaticMemberInGenericType
        static readonly object m_Lock = new object();

        #endregion

        #region Properties

        public static T Instance => GetInstance();

        #endregion

        #region Public methods

        public static T GetInstance(string _Name = null)
        {
            if (m_Instance == null)
            {
                lock (m_Lock)
                {
                    if (m_Instance == null)
                    {
                        // try find already created singleton in scene
                        m_Instance = Object.FindObjectOfType<T>();

                        // try find singleton by name in edit mode because all this have flag DontSave
                        if (m_Instance == null && !Application.isPlaying)
                        {
                            GameObject go = GameObject.Find(_Name ?? typeof(T).FullName);
                            if (go != null)
                                m_Instance = go.GetComponent<T>();
                        }

                        // create new instance of singleton
                        if (m_Instance == null)
                        {
                            GameObject go = new GameObject(_Name ?? typeof(T).FullName);

                            // do not destroy this singletons
                            if (Application.isPlaying)
                                Object.DontDestroyOnLoad(go);
                            else // set this flag only for singletons in edit mode
                                go.hideFlags |= HideFlags.DontSave;

                            // add component and return it
                            m_Instance = go.AddComponent<T>();
                        }
                    }

                    return m_Instance;
                }
            }

            return m_Instance;
        }

        #endregion
    }
}