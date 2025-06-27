using System.Collections.Generic;
using Nav3D.Common;
using UnityEngine;

namespace Nav3D.Obstacles
{
    public class ObstacleInfoTerrain : ObstacleInfoSingle
    {
        #region Attributes

        float[,] m_HeightMap;
        int m_HeightMapResolution;
        Vector3 m_RealSize;

        #endregion

        #region Constructors

        public ObstacleInfoTerrain(
            int _ObstacleControllerID,
            float[,] _HeightMap,
            int _HeightMapResolution,
            Vector3 _RealSize,
            Vector3 _Position) : base(_ObstacleControllerID)
        {
            m_HeightMap = _HeightMap;
            m_HeightMapResolution = _HeightMapResolution;
            m_RealSize = _RealSize;

            ObtainTriangles(_Position);
        }

        public ObstacleInfoTerrain(int _ObstacleControllerID, Bounds _Bounds, List<Triangle> _Triangles) : base(_ObstacleControllerID)
        {
            Bounds = _Bounds;
            Triangles = _Triangles;
        }

        #endregion

        #region Service methods

        //    Z
        //    ^
        //    |
        //  z —   p2 *————*p3
        //    |      |\   |  
        //    |      | \  |
        //    |      |  \ |
        //    | 	 |   \|
        // z-1—   p0 *————*p1 
        //    |
        //    O——————|————|————> X
        //		    x-1   x 
        protected override List<Triangle> GetTriangles(Vector3 _Position)
        {
            List<Triangle> result = new List<Triangle>();

            float xRealSize = m_RealSize.x;
            float yRealSize = m_RealSize.y;
            float zRealSize = m_RealSize.z;
            float inverseHeightMapResolution = 1f / m_HeightMapResolution;

            for (int x = 1; x < m_HeightMapResolution; x++)
            {
                int xPre = x - 1;
                int zPre = 0;

                Vector3 p0 = _Position + new Vector3(
                    xRealSize * xPre * inverseHeightMapResolution,
                    m_HeightMap[zPre, xPre] * yRealSize,
                    zRealSize * zPre * inverseHeightMapResolution
                );

                Vector3 p1 = _Position + new Vector3(
                    xRealSize * x * inverseHeightMapResolution,
                    m_HeightMap[zPre, x] * yRealSize,
                    zRealSize * zPre * inverseHeightMapResolution
                );

                for (int z = 0; z < m_HeightMapResolution; z++)
                {
                    Vector3 p2 = _Position + new Vector3(
                        xRealSize * xPre * inverseHeightMapResolution,
                        m_HeightMap[z, xPre] * yRealSize,
                        zRealSize * z * inverseHeightMapResolution
                    );

                    Vector3 p3 = _Position + new Vector3(
                        xRealSize * x * inverseHeightMapResolution,
                        m_HeightMap[z, x] * yRealSize,
                        zRealSize * z * inverseHeightMapResolution
                    );

                    result.Add(new Triangle(p0, p2, p1));
                    result.Add(new Triangle(p1, p2, p3));

                    zPre++;
                    p0 = p2;
                    p1 = p3;
                }
            }

            return result;
        }

        #endregion
    }
}