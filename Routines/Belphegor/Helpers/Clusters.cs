using System;
using System.Collections.Generic;
using System.Linq;
using Belphegor.Settings;
using Belphegor.Utilities;
using Zeta.Common;
using Zeta.Internals.Actors;

namespace Belphegor.Helpers
{
    internal static class Clusters
    {
        internal static int EvaluateClusterSize(Vector3 inputPoint, PositionCache positions, ClusterType clusterType,
                                                float clusterRange)
        {
            switch (clusterType)
            {
                case ClusterType.Radius:
                    return
                        positions.CachedPositions.Count(
                            v => v.Position.DistanceSqr(inputPoint) < clusterRange*clusterRange);
                default:
                    throw new NotImplementedException("Operation currently not implemented.");
            }
        }

        public static int GetClusterCount(DiaUnit target, CombatContext context, ClusterType type, float clusterRange)
        {
            using (new PerformanceLogger(BelphegorSettings.Instance.Debug.IsDebugClusterLoggingActive, "GetClusterCount"))
            {
                int count;
                if (BelphegorSettings.Instance.EnableClusterCounts)
                    count = EvaluateClusterSize(target.Position, context.UnitPositions, type, clusterRange);

                else count = -1;
                return count;
            }
        }

        public static Vector3 GetBestPositionForClusters(CombatContext context, ClusterType type, float clusterRange)
        {
            return context.UnitPositions.CachedPositions.Select(p => new { Position = p.Position, Count = EvaluateClusterSize(p.Position, context.UnitPositions, type, clusterRange) }).OrderByDescending(e => e.Count).Select(e => e.Position).FirstOrDefault();
        }

        public static DiaObject GetBestUnitForCluster(IEnumerable<DiaObject> units, CombatContext context,
                                                      ClusterType type, float clusterRange)
        {
            var firstOrDefault = units.Where(u => u.IsValid).Select(u => new {Unit = u, Count = EvaluateClusterSize(u.Position, context.UnitPositions, type, clusterRange)}).OrderByDescending(e => e.Count).FirstOrDefault();
            if (firstOrDefault != null)
                return
                    firstOrDefault.Unit;
            return null;
        }
    }
}