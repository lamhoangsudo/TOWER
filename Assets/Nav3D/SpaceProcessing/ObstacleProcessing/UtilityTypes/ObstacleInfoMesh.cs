using UnityEngine;
using System.Collections.Generic;
using Nav3D.Common;

namespace Nav3D.Obstacles
{
    public class ObstacleInfoMesh : ObstacleInfoSingle
    {
        #region Attributes

        Vector3[]  m_Vertices;
        int[]      m_Triangles;
        Vector3    m_Scale;
        Quaternion m_Rotation;

        #endregion

        #region Constructors

        public ObstacleInfoMesh(
                int        _ObstacleControllerID,
                Vector3[]  _Vertices,
                int[]      _Triangles,
                Vector3    _Position,
                Vector3    _Scale,
                Quaternion _Rotation
            ) : base(_ObstacleControllerID)
        {
            m_Vertices  = _Vertices;
            m_Triangles = _Triangles;
            m_Scale     = _Scale;
            m_Rotation  = _Rotation;

            ObtainTriangles(_Position);
        }

        public ObstacleInfoMesh(int _ObstacleControllerID, Bounds _Bounds, List<Triangle> _Triangles) : base(_ObstacleControllerID)
        {
            Bounds    = _Bounds;
            Triangles = _Triangles;
        }

        #endregion

        #region Service methods

        protected override List<Triangle> GetTriangles(Vector3 _Position)
        {
            int            trianglesCount = m_Triangles.Length / 3;
            List<Triangle> triangles      = new List<Triangle>(trianglesCount);

            for (int i = 0; i < trianglesCount; i++)
            {
                int index = i * 3;

                Vector3 p0 = m_Vertices[m_Triangles[index]];
                Vector3 p1 = m_Vertices[m_Triangles[index + 1]];
                Vector3 p2 = m_Vertices[m_Triangles[index + 2]];

                //first - scale
                //second - rotate
                //third - translate
                p0.Scale(m_Scale);
                p0 = m_Rotation * p0 + _Position;
                p1.Scale(m_Scale);
                p1 = m_Rotation * p1 + _Position;
                p2.Scale(m_Scale);
                p2 = m_Rotation * p2 + _Position;

                //triangle in global space
                Triangle triangle = new Triangle(p0, p1, p2);
                triangle.InitSolidHunkTriangles();

                triangles.Add(triangle);
            }

            return triangles;
        }

        #endregion
    }
}