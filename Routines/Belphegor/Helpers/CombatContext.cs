using System;
using System.Linq;
using Belphegor.Utilities;
using Zeta;
using Zeta.Common;
using Zeta.CommonBot;
using Zeta.Internals.Actors;
using Belphegor.Settings;

namespace Belphegor.Helpers
{
    internal class CombatContext
    {
        private static int LastPointInCircle
        {
            get;
            set;
        }
        private static Vector3 LastWhirlWindPosition
        {
            get;
            set;
        }

        internal CombatContext(Func<CombatContext, DiaUnit> targetReceiver)
        {
            // Blargh
            _targetReceiver = targetReceiver;
        }

        private readonly Func<CombatContext, DiaUnit> _targetReceiver;
        private Nullable<Vector3> _playerPosition;
        private DiaUnit _currentTarget;
        private DiaObject _currentLootTarget;
        private PointCollisionTree _aoePositions;
        //private PositionCache _aoePositions;
        private PositionCache _unitPositions;
        private PositionCache _avoidanceCollisionPositions;
        private double? _currentHealthPercentage;
        private bool? _isCollidingWithAoe;
        private Nullable<Vector3> _targetPosition;
        private int? _targetGuid;
        private float? _targetDistance;
        private bool? _isPlayerIncapacited;
        private bool? _isPlayerFearedStunnedForzenOrBlind;
        private Vector3? _whirlWindTargetPosition;

        internal DiaUnit CurrentTarget
        {
            get
            {
                return _currentTarget ?? (_currentTarget = _targetReceiver(this));
            }
        }

        internal DiaObject CurrentLootTarget
        {
            get
            {
                return _currentLootTarget ?? (_currentLootTarget = LootTargeting.Instance.FirstObject);
            }
        }

        internal Vector3 PlayerPosition
        {
            get
            {
                return (_playerPosition ?? (_playerPosition = new Nullable<Vector3>(ZetaDia.Me.Position))).Value;
            }
        }

        internal PointCollisionTree AoePositions
        {
            get
            {
                if (_aoePositions == null)
                {
                    _aoePositions = new PointCollisionTree();
                    var nodes = ZetaDia.Actors.GetActorsOfType<DiaObject>(true).Where(u => u.IsValid && u.IsAoe() && u.GetTriggerHealthPct() >= CurrentHealthPercentage).Select(u => new PointCollisionTree.PointNode(u.Position, u.GetCollisionRadius()));
                    nodes.ForEach(p => _aoePositions.AddPoint(p));
                }
                return _aoePositions;
            }
        }

        internal PositionCache UnitPositions
        {
            get
            {
                return _unitPositions ?? (_unitPositions =
                    new PositionCache(
                        ZetaDia.Actors.GetActorsOfType<DiaUnit>().Where(u => u.IsValid && !u.ActorSNO.IsSNOContainedInFollowerBlacklist() && u.IsACDBased && !u.IsDead).Select(
                            u => new PositionCache.CachedPosition { Position = u.Position, Radius = u.CollisionSphere.Radius })));
            }
        }

        internal PositionCache AvoidanceCollisionPositions
        {
            get
            {
                return _avoidanceCollisionPositions ?? (_avoidanceCollisionPositions =
                        new PositionCache(UnitPositions.CachedPositions.Where(p => p.Position.DistanceSqr(PlayerPosition) > 2.5 * 2.5)));
            }
        }

        internal double CurrentHealthPercentage
        {
            get
            {
                return _currentHealthPercentage ?? (_currentHealthPercentage = new double?(ZetaDia.Me.HitpointsCurrentPct)).Value;
            }
        }

        internal bool IsCollidingWithAoe
        {
            get
            {
                return (_isCollidingWithAoe ?? (_isCollidingWithAoe = new bool?(PlayerPosition.IsCollidingWithAoe(this)))).Value;
            }
        }

        internal Vector3 TargetPosition
        {
            get
            {
                return (_targetPosition ?? (_targetPosition = CurrentTarget != null ? new Nullable<Vector3>(CurrentTarget.Position) : CurrentLootTarget != null ? new Nullable<Vector3>(CurrentLootTarget.Position) : new Nullable<Vector3>(Vector3.Zero))).Value;
            }
        }

        internal int TargetGuid
        {
            get
            {
                return (_targetGuid ?? (_targetGuid = CurrentTarget != null ? new int?(CurrentTarget.ACDGuid) : new int?(-1))).Value;
            }
        }

        internal float TargetDistance
        {
            get
            {
                return (_targetDistance ?? (_targetDistance = CurrentTarget != null ? new float?(CurrentTarget.Distance - CurrentTarget.GetMonsterDistanceModifier()) : CurrentLootTarget != null ? new float?(CurrentLootTarget.Distance) : new float?(float.MaxValue))).Value;
            }
        }

        internal bool IsPlayerIncapacited
        {
            get
            {
                return (_isPlayerIncapacited ?? (_isPlayerIncapacited = new bool?(Unit.IsMeIncapacited))).Value;
            }
        }

        internal bool IsPlayerFearedStunnedFrozenOrBlind
        {
            get
            {
                return (_isPlayerFearedStunnedForzenOrBlind ?? (_isPlayerFearedStunnedForzenOrBlind = new bool?(Unit.IsMeFearedStunnedFrozenOrBlind))).Value;
            }
        }

        private CachedValue<Vector3> _whirlwindCache;
        /// <summary> Gets the whirlwind target position. </summary>
        /// <value> The whirl wind target position. </value>
        internal Vector3 WhirlWindTargetPosition
        {
            get
            {
                var cachedValue = _whirlwindCache ?? (_whirlwindCache = new CachedValue<Vector3>(GetWhirlwindPosition, TimeSpan.FromMilliseconds(100)));
                return cachedValue.Value;
            }
        }

        private Vector3 GetWhirlwindPosition()
        {
            var pos = Clusters.GetBestPositionForClusters(this, ClusterType.Radius, BelphegorSettings.Instance.Barbarian.WhirlwindClusterRange);
            if (pos == Vector3.Zero)
                pos = TargetPosition;

            float dir = 0.0f;
            switch (new Random().Next(1, 5))
            {
                case 1:
                    dir = MathEx.ToRadians(0f);
                    break;
                case 2:
                    dir = MathEx.ToRadians(90f);
                    break;
                case 3:
                    dir = MathEx.ToRadians(180f);
                    break;
                case 4:
                    dir = MathEx.ToRadians(270f);
                    break;
            }

            Vector3 wwPoint = MathEx.GetPointAt(pos, 2f, dir);
            return wwPoint;
        }
    }
}