using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using Nav3D.API;
using Nav3D.Obstacles;
using Nav3D.Common;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Nav3D.Pathfinding
{
    public static partial class Pathfinder
    {
        #region Constants

        const float SQRDISTANCE_EPSILON = 0.00001f;

        const string COMPOUND_FIND_PATH_FRAGMENT_RESULT    = "Fragment {0}/{1} stats: Start: {2}. Target: {3}, ResultCode: {4}, Duration(ms): {5}";
        const string COMPOUND_FIND_PATH_POSTPROCESS_RESULT = "Postprocess stats(ms): Optimizing duration: {0}, Smoothing duration: {1}";

        static readonly string FIND_PATH_INVOKE = $"{nameof(FindPath)}: for points {{0}} invoked";

        #endregion

        #region Public methods

        public static PathfindingResult FindPath(
                Vector3[]         _Points,
                bool              _Loop,
                bool              _SkipUnpassableTargets,
                bool              _TryRepositionStartToFreeLeaf,
                bool              _TryRepositionTargetToFreeLeaf,
                bool              _Smooth,
                int               _PerMinBucketSmoothSamples,
                CancellationToken _CancellationTokenExternal,
                CancellationToken _CancellationTokenTimeout,
                Action<string>    _StatusCallback,
                Log               _Log
            )
        {
            const string STATUS_PATHFINDING = "Pathfinding fragment {0}/{1}";
            const string STATUS_FINISHED    = "Finished";

            try
            {
                _Log?.WriteFormat(FIND_PATH_INVOKE, UtilsCommon.GetPointsString(_Points));

                List<Vector3> points;

                if (_Loop)
                {
                    if (_Points.First() != _Points.Last())
                    {
                        points = new List<Vector3>(_Points.Length + 1) { _Points.Last() };
                        points.AddRange(_Points);
                    }
                    else
                    {
                        points = new List<Vector3>(_Points);
                    }
                }
                else
                {
                    points = new List<Vector3>(_Points);
                }

                int                                    fragmentsCount                = points.Count - 1;
                List<List<Vector3>>                    pathFragments                 = new List<List<Vector3>>(fragmentsCount);
                List<(Vector3, PathfindingResultCode)> targetResultCodes             = new List<(Vector3, PathfindingResultCode)>(fragmentsCount);
                List<(Vector3, TimeSpan)>              fragmentsPathfindingDurations = new List<(Vector3, TimeSpan)>(fragmentsCount);

                TimeSpan pathfindingDuration = TimeSpan.Zero;

                for (int i = 1; i <= fragmentsCount; i++)
                {
                    Vector3 start  = points[i - 1];
                    Vector3 target = points[i];

                    bool tryRepositionStartToFreeLeaf  = i == 1              && !_Loop && _TryRepositionStartToFreeLeaf;
                    bool tryRepositionTargetToFreeLeaf = i == fragmentsCount && !_Loop && _TryRepositionTargetToFreeLeaf;

                    _StatusCallback(string.Format(STATUS_PATHFINDING, i, fragmentsCount));

                    List<Vector3> fragment = FindPath(
                            start,
                            target,
                            tryRepositionStartToFreeLeaf,
                            tryRepositionTargetToFreeLeaf,
                            _CancellationTokenExternal,
                            _CancellationTokenTimeout,
                            out PathfindingResultCode code,
                            out TimeSpan fragmentPathfindingDuration
                        );

                    _Log?.WriteFormat(
                            COMPOUND_FIND_PATH_FRAGMENT_RESULT,
                            i,
                            fragmentsCount,
                            start.ToStringExt(),
                            target.ToStringExt(),
                            code,
                            fragmentPathfindingDuration.TotalMilliseconds
                        );

                    pathfindingDuration = pathfindingDuration.Add(fragmentPathfindingDuration);
                    targetResultCodes.Add((target, code));
                    fragmentsPathfindingDurations.Add((target, fragmentPathfindingDuration));

                    if (code != PathfindingResultCode.SUCCEEDED)
                    {
                        if (i == 1 && code == PathfindingResultCode.START_POINT_INSIDE_OBSTACLE)
                        {
                            return new PathfindingResult(
                                    new List<(Vector3, PathfindingResultCode)> { (target, code) },
                                    new List<(Vector3, TimeSpan)> { (target, fragmentPathfindingDuration) },
                                    code
                                );
                        }

                        if (!_SkipUnpassableTargets)
                        {
                            return new PathfindingResult(
                                    targetResultCodes,
                                    fragmentsPathfindingDurations,
                                    code == PathfindingResultCode.START_POINT_INSIDE_OBSTACLE
                                        ? PathfindingResultCode.TARGET_POINT_INSIDE_OBSTACLE
                                        : code
                                );
                        }

                        continue;
                    }

                    pathFragments.Add(fragment);
                }

                _Log?.Write($"Pathfinding finished ({pathfindingDuration.TotalMilliseconds} ms.)");

                List<Vector3> entirePath = new List<Vector3>(pathFragments.Select(_Path => _Path.Count).Sum());

                foreach (List<Vector3> pathFragment in pathFragments)
                {
                    entirePath.AddRange(pathFragment);
                }

                if (_CancellationTokenExternal.IsCancellationRequested)
                {
                    return new PathfindingResult(targetResultCodes, fragmentsPathfindingDurations, PathfindingResultCode.CANCELLED);
                }

                if (_CancellationTokenTimeout.IsCancellationRequested)
                {
                    return new PathfindingResult(targetResultCodes, fragmentsPathfindingDurations, PathfindingResultCode.TIMEOUT);
                }

                PostprocessPath(
                        pathFragments,
                        points.ToArray(),
                        _Loop,
                        _Smooth,
                        _PerMinBucketSmoothSamples,
                        _StatusCallback,
                        _Log,
                        out Vector3[] pathOptimized,
                        out Vector3[] pathSmoothed,
                        out int[] targetIndices,
                        out TimeSpan optimizingDuration,
                        out TimeSpan smoothingDuration
                    );

                _Log?.WriteFormat(COMPOUND_FIND_PATH_POSTPROCESS_RESULT, optimizingDuration.TotalMilliseconds, smoothingDuration.TotalMilliseconds);
                _StatusCallback(STATUS_FINISHED);

                PathfindingResult result = new PathfindingResult(
                        entirePath.ToArray(),
                        pathOptimized,
                        pathSmoothed,
                        pathfindingDuration,
                        optimizingDuration,
                        smoothingDuration,
                        targetIndices,
                        targetResultCodes,
                        fragmentsPathfindingDurations
                    );

                return result;
            }
            catch (Exception _Exception)
            {
                Debug.LogException(_Exception);
                Debug.LogException(_Exception);

                return new PathfindingResult(
                        new List<(Vector3, PathfindingResultCode)>(),
                        new List<(Vector3, TimeSpan)>(),
                        PathfindingResultCode.UNKNOWN
                    );
            }
        }

        #endregion

        #region Service methods

        static List<Vector3> FindPath(
                Vector3                   _Start,
                Vector3                   _Target,
                bool                      _TryRepositionStartToFreeLeaf,
                bool                      _TryRepositionTargetToFreeLeaf,
                CancellationToken         _CancellationTokenExternal,
                CancellationToken         _CancellationTokenTimeout,
                out PathfindingResultCode _Code,
                out TimeSpan              _PathfindingDuration,
                bool                      _SkipStart = false
            )
        {
            DateTime start = DateTime.Now;
            TimeSpan pathfindingDuration;

            if ((_Start - _Target).sqrMagnitude < SQRDISTANCE_EPSILON)
            {
                _Code                = PathfindingResultCode.SUCCEEDED;
                _PathfindingDuration = DateTime.Now - start;
                return new List<Vector3> { _Start, _Target };
            }

            List<Obstacle> obstacles = ObstacleManager.Doomed ? new List<Obstacle>() : ObstacleManager.Instance.GetObstaclesCrossingTheLine(_Start, _Target);

            //sort obstacles by increasing distance from A
            if (obstacles.Count > 1)
                obstacles = obstacles.OrderBy(
                                          _Obstacle =>
                                          {
                                              return _Obstacle.Bounds.GetIntersection(new Segment3(_Start, _Target))
                                                              .Min(_Point => Vector3.SqrMagnitude(_Start - _Point));
                                          }
                                      )
                                     .ToList();

            List<Vector3> path = new List<Vector3>();

            if (!_SkipStart)
                path.Add(_Start);

            Segment3 segment = new Segment3(_Start, _Target);

            for (int i = 0; i < obstacles.Count; i++)
            {
                Obstacle obstacle = obstacles[i];

                bool tryRepositionStartToFreeLeaf  = i == 0                   && _TryRepositionStartToFreeLeaf;
                bool tryRepositionTargetToFreeLeaf = i == obstacles.Count - 1 && _TryRepositionTargetToFreeLeaf;

                if (_CancellationTokenExternal.IsCancellationRequested || _CancellationTokenTimeout.IsCancellationRequested)
                {
                    _Code = _CancellationTokenExternal.IsCancellationRequested
                        ? PathfindingResultCode.CANCELLED
                        : PathfindingResultCode.TIMEOUT;
                    _PathfindingDuration = DateTime.Now - start;
                    return new List<Vector3> { _Start, _Target };
                }

                //get intersection points and sort by distance from start
                List<Vector3> intersections = obstacle.Bounds
                                                      .GetIntersection(segment)
                                                      .OrderBy(_Point => Vector3.SqrMagnitude(_Start - _Point))
                                                      .ToList();

                OctreePathfindingResult pathfindingResult;

                /*
                 * Possible obstacle bounds intersection cases:
                 * 1) Both inside.
                 *  ___________
                 * |       B   |
                 * |      /    |
                 * |     /     |
                 * |    A      |
                 * |___________|
                 */
                if (intersections.Count == 0)
                {
                    pathfindingResult = obstacle.FindPath(
                            _Start,
                            _Target,
                            tryRepositionStartToFreeLeaf,
                            tryRepositionTargetToFreeLeaf,
                            _CancellationTokenExternal,
                            _CancellationTokenTimeout
                        );

                    if (pathfindingResult.Failed)
                    {
                        _Code                = pathfindingResult.ResultCode;
                        _PathfindingDuration = TimeSpan.Zero;
                        return null;
                    }

                    path = pathfindingResult.Path;
                }
                /*
                 * 2) Both outside.
                 *           B
                 *  ________/___
                 * |       /   |
                 * |      /    |
                 * |     /     |
                 * |    /      |
                 * |___/_______|
                 *    /
                 *   A
                 */
                else if (intersections.Count == 2)
                {
                    pathfindingResult = obstacle.FindPath(
                            intersections.First(),
                            intersections.Last(),
                            tryRepositionStartToFreeLeaf,
                            tryRepositionTargetToFreeLeaf,
                            _CancellationTokenExternal,
                            _CancellationTokenTimeout
                        );

                    if (pathfindingResult.Failed)
                    {
                        _Code                = pathfindingResult.ResultCode;
                        _PathfindingDuration = TimeSpan.Zero;
                        return null;
                    }

                    path.AddRange(pathfindingResult.Path);
                }
                /*
                 * 3) A inside, or B inside
                 *  ___________   ___________
                 * |       A   | |       B   |
                 * |      /    | |      /    |
                 * |     /     | |     /     |
                 * |    /      | |    /      |
                 * |___/_______| |___/_______|
                 *    /             /
                 *   B             A
                 */
                else if (obstacle.Bounds.Contains(_Start))
                {
                    pathfindingResult =
                        obstacle.FindPath(
                                _Start,
                                intersections.First(),
                                tryRepositionStartToFreeLeaf,
                                tryRepositionTargetToFreeLeaf,
                                _CancellationTokenExternal,
                                _CancellationTokenTimeout
                            );

                    if (pathfindingResult.Failed)
                    {
                        _Code                = pathfindingResult.ResultCode;
                        _PathfindingDuration = TimeSpan.Zero;
                        return null;
                    }

                    path.AddRange(pathfindingResult.Path);
                }
                else if (obstacle.Bounds.Contains(_Target))
                {
                    pathfindingResult =
                        obstacle.FindPath(
                                intersections.First(),
                                _Target,
                                tryRepositionStartToFreeLeaf,
                                tryRepositionTargetToFreeLeaf,
                                _CancellationTokenExternal,
                                _CancellationTokenTimeout
                            );

                    if (pathfindingResult.Failed)
                    {
                        _Code                = pathfindingResult.ResultCode;
                        _PathfindingDuration = TimeSpan.Zero;
                        return null;
                    }

                    path.AddRange(pathfindingResult.Path);
                }
                //oops, something goes wrong
                else
                {
                    Vector3 boundsMin = obstacle.Bounds.min;
                    Vector3 boundsMax = obstacle.Bounds.max;

                    string errorData =
                        $"Bounds: min:{{{boundsMin.x}, {boundsMin.y}, "                         +
                        $"{boundsMin.z}}}, max:{{{boundsMax.x}, {boundsMax.y}, {boundsMax.z}}}" +
                        $"Ray: A: {_Start.ToStringExt()}, {_Target.ToStringExt()}";

                    Debug.LogError(errorData);

                    _Code                = PathfindingResultCode.UNKNOWN;
                    _PathfindingDuration = TimeSpan.Zero;
                    return null;
                }
            }

            //record pathfinding duration
            pathfindingDuration = DateTime.Now - start;
            start               = DateTime.Now;

            path.Add(_Target);

            _Code                = PathfindingResultCode.SUCCEEDED;
            _PathfindingDuration = pathfindingDuration;
            return path;
        }

        static void PostprocessPath(
                List<List<Vector3>> _PathFragments,
                Vector3[]           _Targets,
                bool                _Loop,
                bool                _Smooth,
                int                 _PerMinBucketSmoothSamples,
                Action<string>      _StatusCallback,
                Log                 _Log,
                out Vector3[]       _PathOptimized,
                out Vector3[]       _PathSmoothed,
                out int[]           _TargetIndices,
                out TimeSpan        _OptimizingDuration,
                out TimeSpan        _SmoothingDuration
            )
        {
            const string STATUS_OPTIMIZING_DETAILING = "Postprocessing : Optimizing fragment {0}/{1} : Detailing";
            const string STATUS_OPTIMIZING_PRUNING   = "Postprocessing : Optimizing fragment {0}/{1} : Pruning";
            const string STATUS_SMOOTHING            = "Postprocessing : Smoothing";

            List<Vector3> pathOptimized = new List<Vector3>(_PathFragments.Sum(_Fragment => _Fragment.Count));
            Vector3[]     pathSmoothed;
            _TargetIndices = new int[_PathFragments.Count + 1];

            DateTime start = DateTime.Now;
            TimeSpan optimizingDuration;
            TimeSpan smoothingDuration;

            List<Obstacle> influencingObstacles =
                ObstacleManager.Instance.GetObstaclesCrossingTheBounds(
                        ExtensionBounds.PointBounds(_PathFragments.SelectMany(_Fragment => _Fragment).ToArray())
                    );

            _Log?.Write($"Postprocessing fragments:");
            for (int i = 0; i < _PathFragments.Count; i++)
            {
                _Log?.Write($"{i + 1}/{_PathFragments.Count}");
                List<Vector3> fragment = _PathFragments[i];

                if (fragment.Count > 2)
                {
                    //increase points density to provide efficient shorten procedure
                    _StatusCallback(string.Format(STATUS_OPTIMIZING_DETAILING, i + 1, _PathFragments.Count));
                    fragment = DetailPath(fragment);
                    //prune redundant path pieces
                    _StatusCallback(string.Format(STATUS_OPTIMIZING_PRUNING, i + 1, _PathFragments.Count));
                    fragment = ShortenPath(fragment, influencingObstacles);
                    fragment.Reverse();

                    //do the same in reverse
                    _StatusCallback(string.Format(STATUS_OPTIMIZING_DETAILING, i + 1, _PathFragments.Count));
                    fragment = DetailPath(fragment);
                    _StatusCallback(string.Format(STATUS_OPTIMIZING_PRUNING, i + 1, _PathFragments.Count));
                    fragment = ShortenPath(fragment, influencingObstacles);
                    fragment.Reverse();
                }

                _TargetIndices[i] = pathOptimized.Count;

                pathOptimized.AddRange(fragment);

                if (i < _PathFragments.Count - 1)
                    pathOptimized.RemoveAt(pathOptimized.Count - 1);
            }


            _TargetIndices[_PathFragments.Count] = pathOptimized.Count - 1;

            //record path optimizing duration
            optimizingDuration = DateTime.Now - start;

            _Log?.Write($"Postprocessing finished({optimizingDuration.TotalMilliseconds} ms.).");

            start = DateTime.Now;

            if (_Smooth)
            {
                _Log?.Write($"Path smoothing started.");
                _StatusCallback(STATUS_SMOOTHING);

                pathSmoothed = pathOptimized.Count > 2
                    ? Smooth(pathOptimized.ToArray(), _Targets, _Loop, _PerMinBucketSmoothSamples, out _TargetIndices)
                    : pathOptimized.ToArray();

                //record path smoothing duration
                smoothingDuration = DateTime.Now - start;

                _Log?.Write($"Path smoothing finished({smoothingDuration.TotalMilliseconds} ms.).");
            }
            else
            {
                pathSmoothed      = pathOptimized.ToArray();
                smoothingDuration = TimeSpan.Zero;
            }

            _PathOptimized      = pathOptimized.ToArray();
            _PathSmoothed       = pathSmoothed;
            _OptimizingDuration = optimizingDuration;
            _SmoothingDuration  = smoothingDuration;
        }

        static Vector3[] Smooth(Vector3[] _SourcePath, Vector3[] _Targets, bool _Loop, int _PerMinBucketSmoothSamples, out int[] _TargetIndices)
        {
            //Here we need to expand the area of obstacles that will be taken into account during the smoothing procedure.
            //This is due to the fact that the resulting spline will deviate in space from the original array of points.
            //And it may intersect any other obstacles that were not taken into account due pathfinding.

            //Firstly get the source points array extended with extreme points
            Vector3[] extendedPointsArray = AddExtremePointsToCatmullRomArray(_SourcePath);
            //Then get the bounds for array
            Bounds pointsBounds = ExtensionBounds.PointBounds(extendedPointsArray);
            //Obtain the list of all crossing obstacles for extended bounds
            List<Obstacle> touchedObstacles = ObstacleManager.Instance.GetObstaclesCrossingTheBounds(pointsBounds);

            Vector3[] trimmedSourcePath = UtilsCommon.TrimEqualPoints(_SourcePath);


            //Execute smoothing procedure for extended obstacles list
            return SmoothPath(trimmedSourcePath, _Targets, _Loop, touchedObstacles, _PerMinBucketSmoothSamples, out _TargetIndices);
        }

        //Add excessive points forming convex hull for each elementary spline.
        static Vector3[] AddExtremePointsToCatmullRomArray(Vector3[] _SourceArray)
        {
            if (_SourceArray.Length < 4)
                return _SourceArray;

            int sourceCount = _SourceArray.Length;

            List<Vector3> result = new List<Vector3>((sourceCount - 2) * 3 + 2);
            result.AddRange(_SourceArray);

            for (int i = 1; i < sourceCount - 2; i += 2)
            {
                Vector3 p1 = _SourceArray[i];
                Vector3 p2 = _SourceArray[i + 1];
                Vector3 p0 = _SourceArray[i - 1];
                Vector3 p3 = _SourceArray[i + 2];

                result.Add(p1 + (p2 - p0));
                result.Add(p1 + (p1 - p3));
                result.Add(p2 + (p2 - p0));
                result.Add(p2 + (p1 - p3));
            }

            return result.ToArray();
        }

        static List<Vector3> DetailPath(List<Vector3> _Path)
        {
            float minBucketSize    = ObstacleManager.Instance.MinBucketSize;
            float minBucketSizeSqr = ObstacleManager.Instance.MinBucketSizeSqr;

            int newArrayEstimatedLength = 1;

            for (int i = 0; i < _Path.Count - 1; i++)
            {
                float dist = Vector3.Distance(_Path[i], _Path[i + 1]);
                newArrayEstimatedLength += Mathf.CeilToInt(dist / minBucketSize);
            }

            List<Vector3> detailedPath = new List<Vector3>(newArrayEstimatedLength);

            Vector3 prevDirectionVectorNormalized = (_Path.Second() - _Path.First()).normalized;

            for (int i = 0; i < _Path.Count - 1; i++)
            {
                Vector3 curPoint  = _Path[i];
                Vector3 nextPoint = _Path[i + 1];

                detailedPath.Add(curPoint);

                Vector3 directionVector           = nextPoint - curPoint;
                Vector3 directionVectorNormalized = directionVector.normalized;

                //the prev and curr vectors enough co-directional (directions angle less than 5 deg.) so we have no need to detail straight path fragment
                if (Vector3.Dot(prevDirectionVectorNormalized, directionVectorNormalized) > 0.99619f)
                {
                    prevDirectionVectorNormalized = directionVectorNormalized;
                    continue;
                }

                Vector3 toNextVectorStep = directionVectorNormalized * minBucketSize;

                while ((nextPoint - curPoint).sqrMagnitude > minBucketSizeSqr)
                {
                    curPoint += toNextVectorStep;
                    detailedPath.Add(curPoint);
                }
            }

            detailedPath.Add(_Path.Last());

            return detailedPath;
        }

        static List<Vector3> ShortenPath(List<Vector3> _Path, List<Obstacle> _InfluencingObstacles)
        {
            List<Vector3> result = new List<Vector3>(_Path);

            for (int i = 0; i < result.Count; i++)
            {
                Vector3 curPoint = result[i];

                for (int j = 2; j < result.Count - i; j++)
                {
                    Vector3 checkPoint = result[i + j];

                    if (!RayIntersectOccupiedLeaf(curPoint, checkPoint, _InfluencingObstacles))
                    {
                        result.RemoveAt(i + j - 1);
                        j--;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return result;
        }

        static Vector3[] SmoothPath(
                Vector3[]      _Path,
                Vector3[]      _Targets,
                bool           _Loop,
                List<Obstacle> _InfluencingObstacles,
                int            _PerMinBucketSmoothSamples,
                out int[]      _TargetIndices
            )
        {
            try
            {
                if (_Path.Length == 2)
                {
                    _TargetIndices = new[] { 0, 1 };
                    return _Path.ToArray();
                }

                if (ObstacleManager.Doomed)
                {
                    _TargetIndices = null;
                    return null;
                }

                float minBucketSize = ObstacleManager.Instance.MinBucketSize;
                float sampleLength  = minBucketSize / _PerMinBucketSmoothSamples;

                List<Vector3> sourceTrajectory = new List<Vector3>(_Path);

                int newArrayMinEstimatedLength = 1;

                for (int i = 0; i < _Path.Length - 1; i++)
                {
                    float dist = Vector3.Distance(_Path[i], _Path[i + 1]);
                    newArrayMinEstimatedLength += Mathf.CeilToInt(dist / minBucketSize);
                }

                List<Vector3> smoothedTrajectory = new List<Vector3>(newArrayMinEstimatedLength) { sourceTrajectory[0] };

                //accessory extreme points adding
                if (_Loop)
                {
                    sourceTrajectory.Insert(0, sourceTrajectory[sourceTrajectory.Count - 2]);
                    sourceTrajectory.Add(sourceTrajectory[2]);
                }
                else
                {
                    sourceTrajectory.Insert(0, sourceTrajectory[0] + sourceTrajectory[0] - sourceTrajectory[1]);
                    sourceTrajectory.Add(sourceTrajectory.Last()                         + (sourceTrajectory.Last() - sourceTrajectory[sourceTrajectory.Count - 2]));
                }

                int lastTimePointsAdded = 0;

                for (int i = 1; i <= sourceTrajectory.Count - 3; i++)
                {
                    smoothedTrajectory.Add(sourceTrajectory[i]);

                    int smoothRatio = (int) (Vector3.Distance(sourceTrajectory[i], sourceTrajectory[i + 1]) / sampleLength);
                    if (smoothRatio == 0)
                        continue;

                    float paramStep = 1f / smoothRatio;
                    float t         = 0;

                    Vector3 lastPoint = sourceTrajectory[i];

                    int addedPointsCounter = 0;

                    for (int pass = 0; pass < smoothRatio - 1; pass++)
                    {
                        t += paramStep;

                        Vector3 newPoint = CatmullRomEq(sourceTrajectory[i - 1], sourceTrajectory[i], sourceTrajectory[i + 1], sourceTrajectory[i + 2], t);

                        //if segment from last point to new one intersects occupied leaf then insert accessory point
                        if (lastPoint != newPoint && RayIntersectOccupiedLeaf(lastPoint, newPoint, _InfluencingObstacles))
                        {
                            if (i != sourceTrajectory.Count - 3)
                            {
                                sourceTrajectory.Insert(i + 2, sourceTrajectory[i + 1] + (sourceTrajectory[i + 2] - sourceTrajectory[i + 1]) * 0.5f);
                            }

                            sourceTrajectory.Insert(i + 1, sourceTrajectory[i] + (sourceTrajectory[i + 1] - sourceTrajectory[i]) * 0.5f);

                            //remove all new points added for current sample
                            for (int passCounter = 0; passCounter < pass; passCounter++)
                                smoothedTrajectory.RemoveLast();

                            if (i != 1)
                            {
                                sourceTrajectory.Insert(i, sourceTrajectory[i - 1] + (sourceTrajectory[i] - sourceTrajectory[i - 1]) * 0.5f);
                                i--;

                                //remove all new points added for previous sample
                                for (int passCounter = 0; passCounter < lastTimePointsAdded; passCounter++)
                                    smoothedTrajectory.RemoveLast();
                            }

                            addedPointsCounter = 0;

                            break;
                        }

                        smoothedTrajectory.Add(newPoint);
                        addedPointsCounter++;

                        lastPoint = newPoint;
                    }

                    lastTimePointsAdded = addedPointsCounter;
                }

                smoothedTrajectory.Add(_Path.Last());

                _TargetIndices = GetTargetIndices(smoothedTrajectory.ToArray(), _Targets);
                return smoothedTrajectory.ToArray();
            }
            catch (Exception _Exception)
            {
                Debug.LogException(_Exception);
                _TargetIndices = null;
                return null;
            }
        }

        static int[] GetTargetIndices(Vector3[] _Path, Vector3[] _Targets)
        {
            int[] indices = new int[_Targets.Length];

            int     targetIndex = 0;
            Vector3 target      = _Targets[targetIndex];

            for (int i = 0; i < _Path.Length; i++)
            {
                if (_Path[i] != target)
                    continue;

                indices[targetIndex] = i;
                targetIndex++;

                if (targetIndex == _Targets.Length)
                    break;

                target = _Targets[targetIndex];
            }

            return indices;
        }

        static Vector3 CatmullRomEq(Vector3 _P0, Vector3 _P1, Vector3 _P2, Vector3 _P3, float _T)
        {
            return .5f * (-_T * (1 - _T) * (1 - _T) * _P0 + (2 - 5 * _T * _T + 3 * _T * _T * _T) * _P1 + _T * (1 + 4 * _T - 3 * _T * _T) * _P2 - _T * _T * (1 - _T) * _P3);
        }

        static bool RayIntersectOccupiedLeaf(Vector3 _Start, Vector3 _End, List<Obstacle> _Obstacles)
        {
            //perform lexicography sorting because due to inaccuracy during calculations the result can deffer for the same segment checked in different directions
            //so we have to check it in consistent direction independently to initial points order 
            Segment3 segment = _Start.LexCompare(_End) >= 0 ? new Segment3(_End, _Start) : new Segment3(_Start, _End);

            return _Obstacles.Any(_Obstacle => _Obstacle.SegmentIntersectOccupiedLeaf(segment));
        }

        #endregion
    }
}
