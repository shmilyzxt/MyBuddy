using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using Belphegor.Helpers;
using Belphegor.Settings;
using Zeta.Common;
using Zeta.Navigation;

namespace Belphegor.Utilities
{
    internal static class PointGenerator
    {
        internal static IEnumerable<Vector3> GeneratePossibleTargetPoints(this Vector3 center, CombatContext context, float radius,
                                                                          bool useLocalNavigation = true,
                                                                          int numberOfPoints = 32)
        {
            if (BelphegorSettings.Instance.Debug.IsDebugDrawingActive)
            {
                PointGenerationDebugDrawer.Instance.ClearDebugOutput();
            }

            PointGeneratorCache pointGenerator = new PointGeneratorCache(context);

            IEnumerable<Vector3> returnEnum = useLocalNavigation ? pointGenerator.GetNavigateablePointsInRandomOrder(center, radius, numberOfPoints) : pointGenerator.GetRaycastReachablePointsInRanomOrder(center, radius, numberOfPoints);
            for (var i = radius + 5; i <= radius + 20; i += 5)
            {
                returnEnum = returnEnum.Union(useLocalNavigation ? pointGenerator.GetNavigateablePointsInRandomOrder(center, i, numberOfPoints) : pointGenerator.GetRaycastReachablePointsInRanomOrder(center, i, numberOfPoints));
            }

            return returnEnum;
        }

        internal static Vector3 GeneratePointAtPosition(this Vector3 center, CombatContext context, float radius, int pointNumber = 0, bool useLocalNavigation = true, int numberOfPoints = 32)
        {
            if (BelphegorSettings.Instance.Debug.IsDebugDrawingActive)
            {
                PointGenerationDebugDrawer.Instance.ClearDebugOutput();
            }

            PointGeneratorCache pointGenerator = new PointGeneratorCache(context);
            IEnumerable<Vector3> points = pointGenerator.GetNavigateablePoints(center, radius, numberOfPoints);
            if (pointNumber < points.Count())
            {
                return points.ElementAt(pointNumber);
            }
            if (points.Count() > 0)
            {
                return points.First();
            }
            return center;
        }

        internal static IEnumerable<Vector3> GeneratePossibleTargetPointsInHalfCircleFacingAwayFromTarget(
            this Vector3 center, CombatContext context, Vector3 target, float radius, int numberOfPoints = 32)
        {
            if (BelphegorSettings.Instance.Debug.IsDebugDrawingActive)
            {
                PointGenerationDebugDrawer.Instance.ClearDebugOutput();
            }

            PointGeneratorCache pointGenerator = new PointGeneratorCache(context);

            float deltaY = target.Y - center.Y;
            float deltaX = target.X - center.X;
            float alpha = Convert.ToSingle(Math.Atan2(deltaY, deltaX) + Math.PI / 2);
            return pointGenerator.GetNavigateablePointsInHalfCircleInRandomOrder(center, radius, numberOfPoints, alpha);
        }

        #region Nested type: PointGeneratorCache

        internal class PointGeneratorCache
        {
            private const double FullCircle = 2 * Math.PI;
            private readonly Random _rng = new Random();
            private readonly CombatContext _context;


            internal PointGeneratorCache(CombatContext context)
            {
                _context = context;
            }

            #region Half Circle

            internal Vector3 GetPossiblePointInHalfCircle(Vector3 center, float radius, int pointNumber,
                                                          int numberOfPoints, float firstAngle)
            {
                float alpha = Convert.ToSingle((FullCircle / (numberOfPoints * 2)) * pointNumber) + firstAngle;
                return MathEx.GetPointAt(center, radius, alpha);
            }

            internal IEnumerable<Vector3> GetPointsInHalfCircleInRandomOrder(Vector3 center, float radius,
                                                                             int numberOfPoints = 32,
                                                                             float firstAngle = 0)
            {
                var points = new List<int>();
                for (int i = 0; i < numberOfPoints; i++)
                {
                    points.Add(i);
                }
                while (points.Count > 0)
                {
                    int cIndex = _rng.Next(0, points.Count);
                    int cPoint = points[cIndex];
                    points.RemoveAt(cIndex);
                    yield return GetPossiblePointInHalfCircle(center, radius, cPoint, numberOfPoints, firstAngle);
                }
            }

            internal IEnumerable<Vector3> GetPointsInHalfCircle(Vector3 center, float radius, int numberOfPoints = 32,
                                                                float firstAngle = 0)
            {
                for (int i = 0; i < numberOfPoints; i++)
                {
                    yield return GetPossiblePointInHalfCircle(center, radius, i, numberOfPoints, firstAngle);
                }
            }


            internal IEnumerable<Vector3> GetCheckedPointsInHalfCircle(Vector3 center, float radius,
                                                                       Func<Vector3, bool> pointCheck,
                                                                       int numberOfPoints = 32, float firstAngle = 0)
            {
                return GetPointsInHalfCircle(center, radius, numberOfPoints, firstAngle).Where(pointCheck);
            }

            internal IEnumerable<Vector3> GetCheckedPointsInHalfCircleInRandomOrder(Vector3 center, float radius,
                                                                                    Func<Vector3, bool> pointCheck,
                                                                                    int numberOfPoints = 32,
                                                                                    float firstAngle = 0)
            {
                return GetPointsInHalfCircleInRandomOrder(center, radius, numberOfPoints, firstAngle).Where(pointCheck);
            }

            internal IEnumerable<Vector3> GetNavigateablePointsInHalfCircle(Vector3 center, float radius,
                                                                            int numberOfPoints = 32,
                                                                            float firstAngle = 0)
            {
                return GetCheckedPointsInHalfCircle(center, radius, p =>
                                                                        {
                                                                            if (p != Vector3.Zero)
                                                                            {
                                                                                if (
                                                                                    Navigator.NavigationProvider is
                                                                                    DefaultNavigationProvider)
                                                                                {
                                                                                    if (
                                                                                        ((Navigator.NavigationProvider)
                                                                                         as DefaultNavigationProvider).
                                                                                            CanFullyClientPathTo(p))
                                                                                    {
                                                                                        return true;
                                                                                    }
                                                                                }
                                                                            }
                                                                            return false;
                                                                        }, numberOfPoints, firstAngle);
            }

            internal IEnumerable<Vector3> GetNavigateablePointsInHalfCircleInRandomOrder(Vector3 center, float radius,
                                                                                         int numberOfPoints = 32,
                                                                                         float firstAngle = 0)
            {
                return GetCheckedPointsInHalfCircleInRandomOrder(center, radius, p =>
                                                                                     {
                                                                                         if (p != Vector3.Zero)
                                                                                         {
                                                                                             if (
                                                                                                 Navigator.
                                                                                                     NavigationProvider
                                                                                                 is
                                                                                                 DefaultNavigationProvider)
                                                                                             {
                                                                                                 if (
                                                                                                     ((Navigator.
                                                                                                          NavigationProvider)
                                                                                                      as
                                                                                                      DefaultNavigationProvider)
                                                                                                         .
                                                                                                         CanFullyClientPathTo
                                                                                                         (p))
                                                                                                 {
                                                                                                     if (
                                                                                                         BelphegorSettings
                                                                                                             .Instance.
                                                                                                             Debug.
                                                                                                             IsDebugDrawingActive)
                                                                                                         PointGenerationDebugDrawer
                                                                                                             .Instance.
                                                                                                             AddDebugPoint
                                                                                                             (p,
                                                                                                              Brushes.
                                                                                                                  Azure);
                                                                                                     return true;
                                                                                                 }
                                                                                             }
                                                                                         }
                                                                                         return false;
                                                                                     }, numberOfPoints, firstAngle);
            }

            internal IEnumerable<Vector3> GetRaycastReachablePointsInHalfCircle(Vector3 center, float radius,
                                                                                int numberOfPoints = 32,
                                                                                float firstAngle = 0)
            {
                return GetCheckedPointsInHalfCircle(center, radius, p =>
                                                                        {
                                                                            Vector2 outPoint;
                                                                            if (
                                                                                !Navigator.SearchGridProvider.Raycast(
                                                                                    _context.PlayerPosition.ToVector2(),
                                                                                    p.ToVector2(), out outPoint))
                                                                            {
                                                                                return
                                                                                    !_context.AvoidanceCollisionPositions.CachedPositions.Any(
                                                                                        cp =>
                                                                                        MathEx.IntersectsPath(
                                                                                            cp.Position, cp.Radius,
                                                                                            _context.PlayerPosition, p));
                                                                            }
                                                                            return false;
                                                                        }, numberOfPoints, firstAngle);
            }

            internal IEnumerable<Vector3> GetRaycastReachablePointsInHalfCircleInRanomOrder(Vector3 center, float radius,
                                                                                            int numberOfPoints = 32,
                                                                                            float firstAngle = 0)
            {
                return GetCheckedPointsInHalfCircleInRandomOrder(center, radius, p =>
                                                                                     {
                                                                                         Vector2 outPoint;
                                                                                         if (
                                                                                             !Navigator.
                                                                                                  SearchGridProvider.
                                                                                                  Raycast(
                                                                                                      _context.PlayerPosition.
                                                                                                          ToVector2(),
                                                                                                      p.ToVector2(),
                                                                                                      out outPoint))
                                                                                         {
                                                                                             return
                                                                                                 !_context.AvoidanceCollisionPositions.
                                                                                                      CachedPositions.
                                                                                                      Any(
                                                                                                          cp =>
                                                                                                          MathEx.
                                                                                                              IntersectsPath
                                                                                                              (cp.
                                                                                                                   Position,
                                                                                                               cp.Radius,
                                                                                                               _context.PlayerPosition,
                                                                                                               p));
                                                                                         }
                                                                                         return false;
                                                                                     }, numberOfPoints, firstAngle);
            }

            #endregion

            #region Full Circle

            internal Vector3 GetPossiblePointInCircle(Vector3 center, float radius, int pointNumber, int numberOfPoints)
            {
                float alpha = Convert.ToSingle((FullCircle / numberOfPoints) * pointNumber);
                return MathEx.GetPointAt(center, radius, alpha);
            }

            internal IEnumerable<Vector3> GetPointsInCircleInRandomOrder(Vector3 center, float radius,
                                                                         int numberOfPoints = 32)
            {
                var points = new List<int>();
                for (int i = 0; i < numberOfPoints; i++)
                {
                    points.Add(i);
                }
                while (points.Count > 0)
                {
                    int cIndex = _rng.Next(0, points.Count);
                    int cPoint = points[cIndex];
                    points.RemoveAt(cIndex);
                    yield return GetPossiblePointInCircle(center, radius, cPoint, numberOfPoints);
                }
            }

            internal IEnumerable<Vector3> GetPointsInCircle(Vector3 center, float radius, int numberOfPoints = 32)
            {
                for (int i = 0; i < numberOfPoints; i++)
                {
                    yield return GetPossiblePointInCircle(center, radius, i, numberOfPoints);
                }
            }

            internal IEnumerable<Vector3> GetCheckedPointsInCircle(Vector3 center, float radius,
                                                                   Func<Vector3, bool> pointCheck,
                                                                   int numberOfPoints = 32)
            {
                return GetPointsInCircle(center, radius, numberOfPoints).Where(pointCheck);
            }

            internal IEnumerable<Vector3> GetCheckedPointsInCircleInRandomOrder(Vector3 center, float radius,
                                                                                Func<Vector3, bool> pointCheck,
                                                                                int numberOfPoints = 32)
            {
                return GetPointsInCircleInRandomOrder(center, radius, numberOfPoints).Where(pointCheck);
            }

            internal IEnumerable<Vector3> GetNavigateablePoints(Vector3 center, float radius, int numberOfPoints = 32)
            {
                return GetCheckedPointsInCircle(center, radius, p =>
                                                                    {
                                                                        if (p != Vector3.Zero)
                                                                        {
                                                                            if (
                                                                                Navigator.NavigationProvider is
                                                                                DefaultNavigationProvider)
                                                                            {
                                                                                if (
                                                                                    ((Navigator.NavigationProvider) as
                                                                                     DefaultNavigationProvider).
                                                                                        CanFullyClientPathTo(p))
                                                                                {
                                                                                    return true;
                                                                                }
                                                                            }
                                                                        }
                                                                        return false;
                                                                    }, numberOfPoints);
            }

            internal IEnumerable<Vector3> GetNavigateablePointsInRandomOrder(Vector3 center, float radius,
                                                                             int numberOfPoints = 32)
            {
                return GetCheckedPointsInCircleInRandomOrder(center, radius, p =>
                                                                                 {
                                                                                     if (p != Vector3.Zero)
                                                                                     {
                                                                                         if (
                                                                                             Navigator.
                                                                                                 NavigationProvider is
                                                                                             DefaultNavigationProvider)
                                                                                         {
                                                                                             if (
                                                                                                 ((Navigator.
                                                                                                      NavigationProvider)
                                                                                                  as
                                                                                                  DefaultNavigationProvider)
                                                                                                     .
                                                                                                     CanFullyClientPathTo
                                                                                                     (p))
                                                                                             {
                                                                                                 return true;
                                                                                             }
                                                                                         }
                                                                                     }
                                                                                     return false;
                                                                                 }, numberOfPoints);
            }

            internal IEnumerable<Vector3> GetRaycastReachablePoints(Vector3 center, float radius,
                                                                    int numberOfPoints = 32)
            {
                return GetCheckedPointsInCircle(center, radius, p =>
                                                                    {
                                                                        Vector2 outPoint;
                                                                        if (
                                                                            !Navigator.SearchGridProvider.Raycast(
                                                                                _context.PlayerPosition.ToVector2(),
                                                                                p.ToVector2(), out outPoint))
                                                                        {
                                                                            return
                                                                                !_context.AvoidanceCollisionPositions.CachedPositions.Any(
                                                                                    cp =>
                                                                                    MathEx.IntersectsPath(cp.Position,
                                                                                                          cp.Radius,
                                                                                                          _context.PlayerPosition,
                                                                                                          p));
                                                                        }
                                                                        return false;
                                                                    }, numberOfPoints);
            }

            internal IEnumerable<Vector3> GetRaycastReachablePointsInRanomOrder(Vector3 center, float radius,
                                                                                int numberOfPoints = 32)
            {
                return GetCheckedPointsInCircleInRandomOrder(center, radius, p =>
                                                                                 {
                                                                                     Vector2 outPoint;
                                                                                     if (
                                                                                         !Navigator.SearchGridProvider.
                                                                                              Raycast(
                                                                                                  _context.PlayerPosition.
                                                                                                      ToVector2(),
                                                                                                  p.ToVector2(),
                                                                                                  out outPoint))
                                                                                     {
                                                                                         return
                                                                                             !_context.AvoidanceCollisionPositions.
                                                                                                  CachedPositions.Any(
                                                                                                      cp =>
                                                                                                      MathEx.
                                                                                                          IntersectsPath
                                                                                                          (cp.Position,
                                                                                                           cp.Radius,
                                                                                                           _context.PlayerPosition,
                                                                                                           p));
                                                                                     }
                                                                                     return false;
                                                                                 }, numberOfPoints);
            }

            #endregion
        }

        #endregion
    }
}