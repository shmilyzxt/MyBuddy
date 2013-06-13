using System.Linq;
using Belphegor.Settings;
using Belphegor.Utilities;
using Zeta;
using Zeta.Common;
using Zeta.Common.Helpers;
using Zeta.CommonBot;
using Zeta.Internals.Actors;
using Zeta.TreeSharp;

namespace Belphegor.Helpers
{
    internal class Kiting
    {
        private static readonly WaitTimer CheckTimer = WaitTimer.OneSecond;
        private static Vector3 _currentTargetPoint;
        private static PositionCache _targetsInsideMinRange;
        private static readonly float MinimumDistance = BelphegorSettings.Instance.Kiting.MinimumRange;
        private static readonly float MaximumDistance = BelphegorSettings.Instance.Kiting.MaximumRange;

        private static bool NeedsToGetGround
        {
            get { return _targetsInsideMinRange.CachedPositions.Any(); }
        }

        internal static Composite CreateKitingBehavior()
        {
            return
                new Decorator(
                    ctx => BelphegorSettings.Instance.Kiting.IsKitingActive,
                    new Sequence(
                        new Action(ctx => RegenerateCache(MinimumDistance, (CombatContext)ctx)),
                        new PrioritySelector(
                            new Decorator(
                                ctx => CheckTimer.IsFinished && _currentTargetPoint == Vector3.Zero && NeedsToGetGround,
                                new Action(
                                    ctx =>
                                    {
                                        Logger.Write("Kiting behavior started.");
                                        _currentTargetPoint =
                                            ((CombatContext)ctx).PlayerPosition.
                                                GeneratePossibleTargetPointsInHalfCircleFacingAwayFromTarget((CombatContext)ctx,
                                                    ((CombatContext)ctx).TargetPosition, MaximumDistance - MinimumDistance).
                                                FirstOrDefault(
                                                    p =>
                                                    ((CombatContext)ctx).UnitPositions.CachedPositions.Any(
                                                        cp =>
                                                        cp.Position.DistanceSqr(p) <
                                                        BelphegorSettings.Instance.Kiting.AggroRange *
                                                        BelphegorSettings.Instance.Kiting.AggroRange));
                                        if (_currentTargetPoint == Vector3.Zero)
                                        {
                                            Logger.Write("Kiting failed to find target spot.");
                                            CheckTimer.Reset();
                                            return RunStatus.Success;
                                        }
                                        Logger.Write("Kiting target position found.");
                                        return RunStatus.Failure;
                                    }
                                    )
                                ),
                            new Decorator(
                                ctx =>
                                (_currentTargetPoint != Vector3.Zero &&
                                 (_currentTargetPoint.DistanceSqr(((CombatContext)ctx).PlayerPosition) < 4f * 4f ||
                                  _currentTargetPoint.DistanceSqr(((CombatContext)ctx).PlayerPosition) >
                                  (MaximumDistance - MinimumDistance) * (MaximumDistance - MinimumDistance))) ||
                                ZetaDia.Me.Movement.StuckFlags.HasFlag(StuckFlags.WasStuck),
                                new Action(ret =>
                                               {
                                                   _currentTargetPoint = Vector3.Zero;
                                                   CheckTimer.Reset();
                                                   Logger.Write("Kiting behavior finished.");
                                               })
                                ),
                            new Decorator(
                                ctx => _currentTargetPoint != Vector3.Zero,
                                CommonBehaviors.MoveAndStop(ret => _currentTargetPoint, 4f, true, "Kiting Position")
                                )
                            )
                        )
                    );
        }

        private static void RegenerateCache(float minimumRange, CombatContext context)
        {
            _targetsInsideMinRange =
                new PositionCache(
                    context.UnitPositions.CachedPositions.Where(u =>
                        u.Position.DistanceSqr(context.PlayerPosition) < minimumRange * minimumRange));  
        }
    }
}