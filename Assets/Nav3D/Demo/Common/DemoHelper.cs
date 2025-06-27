using UnityEngine;

namespace Nav3D.Demo
{
    public static class DemoHelper
    {
        #region Constants

        const string PREFAB_CONE_PATH = "AgentBodyCone";
        const float RADIUS_TO_SCALE_FACTOR = 20f;

        #endregion

        #region Public methods

        public static GameObject InstantiateAgentConeBody(string _Name, float _Radius, out Material _Material, Material _SourceMaterial = null, Color? _BodyColor = null)
        {
            //load cone prefab
            GameObject bodyPrefab = Resources.Load<GameObject>(PREFAB_CONE_PATH);

            //instantiate agent GameObject
            GameObject agentGO = new GameObject(_Name);
            GameObject agentBodyGO = Object.Instantiate(bodyPrefab, Vector3.zero, Quaternion.identity);

            agentBodyGO.transform.SetParent(agentGO.transform);

            if (_SourceMaterial != null)
                _Material = _SourceMaterial;
            else
            {
                //creates material with default shader
                _Material = new Material(Shader.Find("Legacy Shaders/Diffuse"));
                _Material.color = _BodyColor ?? Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
            }

            //attach material to the agent body
            agentBodyGO.transform.Find("agentBodyMesh").GetComponent<Renderer>().material = _Material;

            //scale the cone body by agent radius factor  
            float localScale = _Radius * RADIUS_TO_SCALE_FACTOR;
            agentBodyGO.transform.localScale = new Vector3(localScale, localScale, localScale);

            return agentGO;
        }

        public static void ScaleAgentConeBody(GameObject _ConeAgent, float _Radius)
        {
            float localScale = _Radius * RADIUS_TO_SCALE_FACTOR;
            //find body instance and scale by agent radius factor
            (_ConeAgent.transform.Find(PREFAB_CONE_PATH) ?? _ConeAgent.transform.Find($"{PREFAB_CONE_PATH}(Clone)")).localScale = new Vector3(localScale, localScale, localScale);
        }

        #endregion
    }
}
