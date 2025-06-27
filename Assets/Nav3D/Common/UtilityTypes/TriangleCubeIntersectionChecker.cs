using System;
using UnityEngine;

namespace Nav3D.Common
{
    /// <summary>
    /// Implementation of the algorithm described in the original book Graphics Gems III (David Kirk), Triangle-Cube Intersection, pp. 236-239
    /// http://www.realtimerendering.com/resources/GraphicsGems/gemsiii/triangleCube.c
    /// </summary>
    public static class TriangleCubeIntersectionChecker
    {
        #region Constants

        const long INSIDE = 0;
        const long OUTSIDE = 1;

        #endregion

        #region Public methods

        public static bool Inside(Triangle _Triangle, Bounds _Cube)
        {
            Vector3 cubeCenter = _Cube.center;
            Vector3 cubeSize   = _Cube.size;

            DVector3 scaledV1 = new DVector3((_Triangle.V1.x - (double)cubeCenter.x) / cubeSize.x,
                                             (_Triangle.V1.y - (double)cubeCenter.y) / cubeSize.y,
                                             (_Triangle.V1.z - (double)cubeCenter.z) / cubeSize.z);

            DVector3 scaledV2 = new DVector3((_Triangle.V2.x - (double)cubeCenter.x) / cubeSize.x,
                                             (_Triangle.V2.y - (double)cubeCenter.y) / cubeSize.y,
                                             (_Triangle.V2.z - (double)cubeCenter.z) / cubeSize.z);

            DVector3 scaledV3 = new DVector3((_Triangle.V3.x - (double)cubeCenter.x) / cubeSize.x,
                                             (_Triangle.V3.y - (double)cubeCenter.y) / cubeSize.y,
                                             (_Triangle.V3.z - (double)cubeCenter.z) / cubeSize.z);

            //cut off last valuable digits after comma
            scaledV1 = new DVector3(Math.Round(scaledV1.x, 5),
                                    Math.Round(scaledV1.y, 5),
                                    Math.Round(scaledV1.z, 5));

            scaledV2 = new DVector3(Math.Round(scaledV2.x, 5),
                                    Math.Round(scaledV2.y, 5),
                                    Math.Round(scaledV2.z, 5));

            scaledV3 = new DVector3(Math.Round(scaledV3.x, 5),
                                    Math.Round(scaledV3.y, 5),
                                    Math.Round(scaledV3.z, 5));

            DVector3 V12 = scaledV2 - scaledV1;
            DVector3 V23 = scaledV3 - scaledV2;

            return InsideInternal(scaledV1, scaledV2, scaledV3, DVector3.Cross(V12, V23).normalized);
        }

        #endregion
        
        #region Service methods

        static bool InsideInternal(DVector3 _V1, DVector3 _V2, DVector3 _V3, DVector3 _Normal)
        {
            long     V1Test, V2Test, V3Test;
            double   d,      denom;
            DVector3 hitpp,  hitpn, hitnp, hitnn;

            /* First compare all three vertexes with all six face-planes */
            /* If any vertex is inside the cube, return immediately!     */

            if ((V1Test = FacePlane(_V1)) == INSIDE) return true;
            if ((V2Test = FacePlane(_V2)) == INSIDE) return true;
            if ((V3Test = FacePlane(_V3)) == INSIDE) return true;

            /* If all three vertexes were outside of one or more face-planes, */
            /* return immediately with a trivial rejection!                   */

            if ((V1Test & V2Test & V3Test) != 0) return false;

            /* Now do the same trivial rejection test for the 12 edge planes */

            V1Test |= Bevel2D(_V1) << 8;
            V2Test |= Bevel2D(_V2) << 8;
            V3Test |= Bevel2D(_V3) << 8;

            if ((V1Test & V2Test & V3Test) != 0) return false;

            /* Now do the same trivial rejection test for the 8 corner planes */

            V1Test |= Bevel3D(_V1) << 24;
            V2Test |= Bevel3D(_V2) << 24;
            V3Test |= Bevel3D(_V3) << 24;

            if ((V1Test & V2Test & V3Test) != 0) return false;

            /* If vertex 1 and 2, as a pair, cannot be trivially rejected */
            /* by the above tests, then see if the v1-->v2 triangle edge  */
            /* intersects the cube.  Do the same for v1-->v3 and v2-->v3. */
            /* Pass to the intersection algorithm the "OR" of the outcode */
            /* bits, so that only those cube faces which are spanned by   */
            /* each triangle edge need be tested.                         */

            if ((V1Test & V2Test) == 0)
                if (CheckLine(_V1, _V2, V1Test | V2Test) == INSIDE) return true;
            if ((V1Test & V3Test) == 0)
                if (CheckLine(_V1, _V3, V1Test | V3Test) == INSIDE) return true;
            if ((V2Test & V3Test) == 0)
                if (CheckLine(_V2, _V3, V2Test | V3Test) == INSIDE) return true;

            /* By now, we know that the triangle is not off to any side,     */
            /* and that its sides do not penetrate the cube.  We must now    */
            /* test for the cube intersecting the interior of the triangle.  */
            /* We do this by looking for intersections between the cube      */
            /* diagonals and the triangle...first finding the intersection   */
            /* of the four diagonals with the plane of the triangle, and     */
            /* then if that intersection is inside the cube, pursuing        */
            /* whether the intersection point is inside the triangle itself. */

            /* To find plane of the triangle, first perform crossproduct on  */
            /* two triangle side vectors to compute the normal vector.       */

            //(The calculations described are performed inside Triangle struct)

            //SUB(t.v1, t.v2, vect12);
            //SUB(t.v1, t.v3, vect13);
            //CROSS(vect12, vect13, norm)

            /* The normal vector "norm" X,Y,Z components are the coefficients */
            /* of the triangles AX + BY + CZ + D = 0 plane equation.  If we   */
            /* solve the plane equation for X=Y=Z (a diagonal), we get        */
            /* -D/(A+B+C) as a metric of the distance from cube center to the */
            /* diagonal/plane intersection.  If this is between -0.5 and 0.5, */
            /* the intersection is inside the cube.  If so, we continue by    */
            /* doing a point/triangle intersection.                           */
            /* Do this for all four diagonals.                                */

            d = _Normal.x * _V1.x + _Normal.y * _V1.y + _Normal.z * _V1.z;


            /* if one of the diagonals is parallel to the plane, the other will intersect the plane */
            if (Math.Abs(denom = (_Normal.x + _Normal.y + _Normal.z)) > Mathf.Epsilon)
            /* skip parallel diagonals to the plane; division by 0 can occur */
            {
                hitpp.x = hitpp.y = hitpp.z = d / denom;
                if (Math.Abs(hitpp.x) <= 0.5)
                    if (PointTriangleIntersection(hitpp, _V1, _V2, _V3) == INSIDE) return true;
            }
            if (Math.Abs(denom = (_Normal.x + _Normal.y - _Normal.z)) > Mathf.Epsilon)
            {
                hitpn.z = -(hitpn.x = hitpn.y = d / denom);
                if (Math.Abs(hitpn.x) <= 0.5)
                    if (PointTriangleIntersection(hitpn, _V1, _V2, _V3) == INSIDE) return true;
            }
            if (Math.Abs(denom = (_Normal.x - _Normal.y + _Normal.z)) > Mathf.Epsilon)
            {
                hitnp.y = -(hitnp.x = hitnp.z = d / denom);
                if (Math.Abs(hitnp.x) <= 0.5)
                    if (PointTriangleIntersection(hitnp, _V1, _V2, _V3) == INSIDE) return true;
            }
            if (Math.Abs(denom = (_Normal.x - _Normal.y - _Normal.z)) > Mathf.Epsilon)
            {
                hitnn.y = hitnn.z = -(hitnn.x = d / denom);
                if (Math.Abs(hitnn.x) <= 0.5)
                    if (PointTriangleIntersection(hitnn, _V1, _V2, _V3) == INSIDE) return true;
            }

            /* No edge touched the cube; no cube diagonal touched the triangle. */
            /* We're done...there was no intersection.                          */

            return false;
        }

        /* Which of the six face-plane(s) is point P outside of? */
        static long FacePlane(DVector3 _Point)
        {
            long outcode = 0;
            
            if (_Point.x > .5) outcode  |= 0x01;
            if (_Point.x < -.5) outcode |= 0x02;
            if (_Point.y > .5) outcode  |= 0x04;
            if (_Point.y < -.5) outcode |= 0x08;
            if (_Point.z > .5) outcode  |= 0x10;
            if (_Point.z < -.5) outcode |= 0x20;

            return outcode;
        }

        /* Which of the twelve edge plane(s) is point P outside of? */
        static long Bevel2D(DVector3 _Point)
        {
            long outcode = 0;

            if (_Point.x  + _Point.y > 1.0) outcode |= 0x001;
            if (_Point.x  - _Point.y > 1.0) outcode |= 0x002;
            if (-_Point.x + _Point.y > 1.0) outcode |= 0x004;
            if (-_Point.x - _Point.y > 1.0) outcode |= 0x008;
            if (_Point.x  + _Point.z > 1.0) outcode |= 0x010;
            if (_Point.x  - _Point.z > 1.0) outcode |= 0x020;
            if (-_Point.x + _Point.z > 1.0) outcode |= 0x040;
            if (-_Point.x - _Point.z > 1.0) outcode |= 0x080;
            if (_Point.y  + _Point.z > 1.0) outcode |= 0x100;
            if (_Point.y  - _Point.z > 1.0) outcode |= 0x200;
            if (-_Point.y + _Point.z > 1.0) outcode |= 0x400;
            if (-_Point.y - _Point.z > 1.0) outcode |= 0x800;

            return outcode;
        }

        /* Which of the eight corner plane(s) is point P outside of? */
        static long Bevel3D(DVector3 _Point)
        {
            long outcode = 0;

            if (( _Point.x + _Point.y + _Point.z) > 1.5) outcode |= 0x01;
            if (( _Point.x + _Point.y - _Point.z) > 1.5) outcode |= 0x02;
            if (( _Point.x - _Point.y + _Point.z) > 1.5) outcode |= 0x04;
            if (( _Point.x - _Point.y - _Point.z) > 1.5) outcode |= 0x08;
            if ((-_Point.x + _Point.y + _Point.z) > 1.5) outcode |= 0x10;
            if ((-_Point.x + _Point.y - _Point.z) > 1.5) outcode |= 0x20;
            if ((-_Point.x - _Point.y + _Point.z) > 1.5) outcode |= 0x40;
            if ((-_Point.x - _Point.y - _Point.z) > 1.5) outcode |= 0x80;

            return outcode;
        }

        /* Compute intersection of P1 --> P2 line segment with face planes */
        /* Then test intersection point to see if it is on cube face       */
        /* Consider only face planes in "outcode_diff"                     */
        /* Note: Zero bits in "outcode_diff" means face line is outside of */

        static long CheckLine(DVector3 _Point1, DVector3 _Point2, long _OutcodeDiff)
        {
            if ((0x01 & _OutcodeDiff) != 0)
                if (CheckPoint(_Point1, _Point2, (0.5f - _Point1.x) / (_Point2.x - _Point1.x), 0x3e) == INSIDE) return (INSIDE);
            if ((0x02 & _OutcodeDiff) != 0)
                if (CheckPoint(_Point1, _Point2, (-0.5f - _Point1.x) / (_Point2.x - _Point1.x), 0x3d) == INSIDE) return (INSIDE);
            if ((0x04 & _OutcodeDiff) != 0)
                if (CheckPoint(_Point1, _Point2, (0.5f - _Point1.y) / (_Point2.y - _Point1.y), 0x3b) == INSIDE) return (INSIDE);
            if ((0x08 & _OutcodeDiff) != 0)
                if (CheckPoint(_Point1, _Point2, (-0.5f - _Point1.y) / (_Point2.y - _Point1.y), 0x37) == INSIDE) return (INSIDE);
            if ((0x10 & _OutcodeDiff) != 0)
                if (CheckPoint(_Point1, _Point2, (0.5f - _Point1.z) / (_Point2.z - _Point1.z), 0x2f) == INSIDE) return (INSIDE);
            if ((0x20 & _OutcodeDiff) != 0)
                if (CheckPoint(_Point1, _Point2, (-0.5f - _Point1.z) / (_Point2.z - _Point1.z), 0x1f) == INSIDE) return (INSIDE);

            return OUTSIDE;
        }

        /* Test the point "alpha" of the way from P1 to P2 */
        /* See if it is on a face of the cube              */
        /* Consider only faces in "mask"                   */

        static long CheckPoint(DVector3 _Point1, DVector3 _Point2, double _Alpha, long _Mask)
        {
            DVector3 planePoint;

            planePoint.x = LERP(_Alpha, _Point1.x, _Point2.x);
            planePoint.y = LERP(_Alpha, _Point1.y, _Point2.y);
            planePoint.z = LERP(_Alpha, _Point1.z, _Point2.z);

            return FacePlane(planePoint) & _Mask;
        }

        /* Test if 3D point is inside 3D triangle */

        static double Max(double _A, double _B, double _C)
        {
            return _A > _B ? _A > _C ? _A : _C : _B > _C ? _B : _C;
        }
        
        static double Min(double _A, double _B, double _C)
        {
            return _A < _B ? _A < _C ? _A : _C : _B < _C ? _B : _C;
        }
        
        static long PointTriangleIntersection(DVector3 _Point, DVector3 _V1, DVector3 _V2, DVector3 _V3)
        {
            long     sign12, sign23, sign31;
            DVector3 vect1h, vect2h, vect3h;
            DVector3 cross12_1p, cross23_2p, cross31_3p;

            /* First, a quick bounding-box test:                               */
            /* If P is outside triangle bbox, there cannot be an intersection. */

            if (_Point.x > Max(_V1.x, _V2.x, _V3.x)) return OUTSIDE;
            if (_Point.y > Max(_V1.y, _V2.y, _V3.y)) return OUTSIDE;
            if (_Point.z > Max(_V1.z, _V2.z, _V3.z)) return OUTSIDE;
            if (_Point.x < Min(_V1.x, _V2.x, _V3.x)) return OUTSIDE;
            if (_Point.y < Min(_V1.y, _V2.y, _V3.y)) return OUTSIDE;
            if (_Point.z < Min(_V1.z, _V2.z, _V3.z)) return OUTSIDE;

            /* For each triangle side, make a vector out of it by subtracting vertexes; */
            /* make another vector from one vertex to point P.                          */
            /* The crossproduct of these two vectors is orthogonal to both and the      */
            /* signs of its X,Y,Z components indicate whether P was to the inside or    */
            /* to the outside of this triangle side.                                    */


            vect1h = _Point - _V1;
            cross12_1p = DVector3.Cross(_V2 - _V1, vect1h);
            sign12 = SIGN3(cross12_1p);      /* Extract X,Y,Z signs as 0..7 or 0...63 integer */


            vect2h = _Point - _V2;
            cross23_2p = DVector3.Cross(_V3 - _V2, vect2h);
            sign23 = SIGN3(cross23_2p);


            vect3h = _Point - _V3;
            cross31_3p = DVector3.Cross(_V1 - _V3, vect3h);
            sign31 = SIGN3(cross31_3p);

            /* If all three crossproduct vectors agree in their component signs,  */
            /* then the point must be inside all three.                           */
            /* P cannot be OUTSIDE all three sides simultaneously.                */

            /* this is the old test; with the revised SIGN3() macro, the test
             * needs to be revised. */

            return ((sign12 & sign23 & sign31) == 0) ? OUTSIDE : INSIDE;
        }

        static double LERP(double A, double B, double C) => B + A * (C - B);
        static long SIGN3(DVector3 A) =>
           (A.x < Mathf.Epsilon ? 4 : 0) | (A.x > -Mathf.Epsilon ? 32 : 0) |
           (A.y < Mathf.Epsilon ? 2 : 0) | (A.y > -Mathf.Epsilon ? 16 : 0) |
           (A.z < Mathf.Epsilon ? 1 : 0) | (A.z > -Mathf.Epsilon ? 8 : 0);

        #endregion
    }
}