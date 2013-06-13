using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Belphegor.Settings;
using Belphegor.Utilities;
using Zeta;
using Zeta.Common;
using Zeta.Common.Helpers;
using Zeta.CommonBot;
using Zeta.Internals.Actors;
using Zeta.TreeSharp;
using Action = Zeta.TreeSharp.Action;

namespace Belphegor.Helpers
{
    internal static class Avoidance
    {
        private static readonly WaitTimer CheckTimer = WaitTimer.OneSecond;
        private static readonly Stopwatch StuckWatch = new Stopwatch();
        private static Vector3 _lastPosition;
        private static bool _wasStuck;
        private static float _lastVelosity;
        internal static bool IsAvoidanceCacheResetRequired { get; set; }

        public static Composite CreateMoveForAvoidance(float maximumDistance = 15f)
        {
            return
                new Decorator(
                    ctx => BelphegorSettings.Instance.Avoidance.IsAvoidanceActive && CheckTimer.IsFinished,
                    new PrioritySelector(
                        Common.CreateWaitWhileRunningAttackingChannelingOrCasting(),
                        new PrioritySelector(
                            new Decorator(
                                ctx => _currentTargetPoint != Vector3.Zero,
                                new Action(ctx =>
                                               {
                                                   if (IsStuck())
                                                   {
                                                       if (BelphegorSettings.Instance.Debug.IsDebugAvoidanceLog)
                                                           Logger.Write("Avoidance is stuck.");
                                                       _wasStuck = true;
                                                   }
                                                   return RunStatus.Failure;
                                               })
                                ),
                            new Decorator(
                                ctx =>
                                _currentTargetPoint.IsCollidingWithAoe((CombatContext)ctx) ||
                                (_currentTargetPoint == Vector3.Zero && ((CombatContext)ctx).IsCollidingWithAoe),
                                new Action(ctx =>
                                               {
                                                   if (BelphegorSettings.Instance.Debug.IsDebugAvoidanceLog)
                                                       Logger.Write("Avoidance started.");
                                                   _currentTargetPoint = GenerateTargetPoint((CombatContext)ctx);
                                                   if (_currentTargetPoint == Vector3.Zero)
                                                       CheckTimer.Reset();
                                                   else
                                                       _wasStuck = false;
                                               })
                                ),
                            new Decorator(
                                ctx => _currentTargetPoint != Vector3.Zero && ((CombatContext)ctx).TargetPosition != Vector3.Zero && !_wasStuck,
                                CommonBehaviors.MoveAndStop(ret => _currentTargetPoint, 2f, true, "Dodge Position")
                                ),
                            new Decorator(
                                ctx =>
                                !_wasStuck && !Unit.IsMeIncapacited && !((CombatContext)ctx).IsCollidingWithAoe &&
                                ((CombatContext)ctx).TargetDistance > maximumDistance &&
                                ((CombatContext)ctx).TargetPosition.IsCollidingWithAoe((CombatContext)ctx),
                                new Action(ctx =>
                                                {
                                                    if (BelphegorSettings.Instance.Debug.IsDebugAvoidanceLog)
                                                        Logger.Write("Avoidance waiting for target in range.");
                                                    return RunStatus.Success;
                                                })
                                ),
                            new Decorator(
                                ctx =>
                                _wasStuck ||
                                (_currentTargetPoint != Vector3.Zero && !((CombatContext)ctx).IsCollidingWithAoe),
                                new Action(ctx =>
                                               {
                                                   if (BelphegorSettings.Instance.Debug.IsDebugAvoidanceLog)
                                                       Logger.Write("Avoidance finished.");
                                                   ResetAvoidanceState();
                                               })
                                )
                            )
                        )
                    );
        }

        #region Avoidance

        private static Vector3 _currentTargetPoint;

        private static void ResetAvoidanceState()
        {
            _lastVelosity = 0f;
            StuckWatch.Stop();
            StuckWatch.Reset();
            _lastPosition = Vector3.Zero;
            _currentTargetPoint = Vector3.Zero;
            _wasStuck = false;
        }

        private static bool IsStuck()
        {
            bool isStuck = false;
            if (ZetaDia.Me.Movement.StuckFlags.HasFlag(StuckFlags.WasStuck))
            {
                isStuck = true;
            }
            else
            {
                StuckWatch.Stop();
                long msSinceLastTick = StuckWatch.ElapsedMilliseconds;
                Vector3 currentPosition = ZetaDia.Me.Position;
                if (msSinceLastTick > 0 && _lastPosition != Vector3.Zero)
                {
                    float traveledDistance = _lastPosition.Distance(currentPosition);
                    float velosity = traveledDistance / msSinceLastTick;
                    if (_lastVelosity != 0)
                    {
                        velosity += _lastVelosity;
                        velosity /= 2;
                        if (velosity < BelphegorSettings.Instance.Avoidance.StuckVelocity / 1000)
                        {
                            isStuck = true;
                        }
                    }
                    _lastVelosity = velosity;
                }
                _lastPosition = currentPosition;
                StuckWatch.Restart();
            }
            return isStuck;
        }

        internal static bool IsCollidingWithAoe(this Vector3 position, CombatContext context)
        {
            return context.AoePositions.Nodes.Any(p => p.Position.DistanceSqr(position) <= p.Radius * p.Radius);
        }

        internal static float GetCollisionRadius(this DiaObject obj)
        {
            switch (obj.ActorSNO)
            {
                case (int)GroundEffectSNOs.ArcaneCenterReverse:
                case (int)GroundEffectSNOs.ArcaneCenter:
                    return BelphegorSettings.Instance.Avoidance.ArcaneDodgeRadius;
                case (int)GroundEffectSNOs.IceClusters:
                    return BelphegorSettings.Instance.Avoidance.IceClustersDodgeRadius;
                case (int)GroundEffectSNOs.Molten:
                    return BelphegorSettings.Instance.Avoidance.MoltenDodgeRadius;
                case (int)GroundEffectSNOs.MoltenTrail:
                    return BelphegorSettings.Instance.Avoidance.MoltenTrailDodgeRadius;
                case (int)GroundEffectSNOs.Plagued:
                    return BelphegorSettings.Instance.Avoidance.PlaguedDodgeRadius;
                case (int)GroundEffectSNOs.Desecrator:
                    return BelphegorSettings.Instance.Avoidance.DesecratorDodgeRadius;
                case (int)GroundEffectSNOs.HighlandWalkerSpore:
                    return BelphegorSettings.Instance.Avoidance.SporeDodgeRadius;
                case (int)GroundEffectSNOs.Belial_GroundBomb_Pending:
                case (int)GroundEffectSNOs.Belial_GroundBomb_Impact:
                    return BelphegorSettings.Instance.Avoidance.BelialBombDodgeRadius;
                case (int)GroundEffectSNOs.Creep_Mob_Arm:
                    return BelphegorSettings.Instance.Avoidance.CreepMobArmDodgeRadius;
                case (int)GroundEffectSNOs.Azmodan_AOD_Demon:
                    return BelphegorSettings.Instance.Avoidance.AzmodanAODDemonDodgeRadius;
                case (int)GroundEffectSNOs.Ghom_Gas:
                    return BelphegorSettings.Instance.Avoidance.GhomGasDodgeRadius;
                case (int)GroundEffectSNOs.Diablo_Fire_Ring_A:
                case (int)GroundEffectSNOs.Diablo_Fire_Ring_B:
                    return BelphegorSettings.Instance.Avoidance.DiabloFireRingDodgeRadius;
                default:
                    return obj.CollisionSphere.Radius + 2;
            }
        }

        internal static bool IsAoe(this DiaObject obj)
        {
            switch (obj.ActorSNO)
            {
                case (int)GroundEffectSNOs.ArcaneCenterReverse:
                case (int)GroundEffectSNOs.ArcaneCenter:
                    return BelphegorSettings.Instance.Avoidance.IsArcaneDodgeActive;
                case (int)GroundEffectSNOs.IceClusters:
                    return BelphegorSettings.Instance.Avoidance.IsIceClustersDodgeActive;
                case (int)GroundEffectSNOs.Molten:
                    return BelphegorSettings.Instance.Avoidance.IsMoltenDodgeActive;
                case (int)GroundEffectSNOs.MoltenTrail:
                    return BelphegorSettings.Instance.Avoidance.IsMoltenTrailDodgeActive;
                case (int)GroundEffectSNOs.Plagued:
                    return BelphegorSettings.Instance.Avoidance.IsPlaguedDodgeActive;
                case (int)GroundEffectSNOs.Desecrator:
                    return BelphegorSettings.Instance.Avoidance.IsDesecratorDodgeActive;
                case (int)GroundEffectSNOs.HighlandWalkerSpore:
                    return BelphegorSettings.Instance.Avoidance.IsSporeDodgeActive;
                case (int)GroundEffectSNOs.Belial_GroundBomb_Pending:
                case (int)GroundEffectSNOs.Belial_GroundBomb_Impact:
                    return BelphegorSettings.Instance.Avoidance.IsBelialBombDodgeActive;
                case (int)GroundEffectSNOs.Creep_Mob_Arm:
                    return BelphegorSettings.Instance.Avoidance.IsCreepMobArmDodgeActive;
                case (int)GroundEffectSNOs.Azmodan_AOD_Demon:
                    return BelphegorSettings.Instance.Avoidance.IsAzmodanAODDemonDodgeActive;
                case(int)GroundEffectSNOs.Ghom_Gas:
                    return BelphegorSettings.Instance.Avoidance.IsGhomGasDodgeActive;
                case (int)GroundEffectSNOs.Diablo_Fire_Ring_A:
                case (int)GroundEffectSNOs.Diablo_Fire_Ring_B:
                    return BelphegorSettings.Instance.Avoidance.IsDiabloFireRingDodgeActive;
                default:
                    return false;
            }
        }

        internal static float GetTriggerHealthPct(this DiaObject obj)
        {
            switch (obj.ActorSNO)
            {
                case (int)GroundEffectSNOs.ArcaneCenterReverse:
                case (int)GroundEffectSNOs.ArcaneCenter:
                    return BelphegorSettings.Instance.Avoidance.ArcaneDodgeHealthPct;
                case (int)GroundEffectSNOs.IceClusters:
                    return BelphegorSettings.Instance.Avoidance.IceClustersDodgeHealthPct;
                case (int)GroundEffectSNOs.Molten:
                    return BelphegorSettings.Instance.Avoidance.MoltenDodgeHealthPct;
                case (int)GroundEffectSNOs.MoltenTrail:
                    return BelphegorSettings.Instance.Avoidance.MoltenTrailDodgeHealthPct;
                case (int)GroundEffectSNOs.Plagued:
                    return BelphegorSettings.Instance.Avoidance.PlaguedDodgeHealthPct;
                case (int)GroundEffectSNOs.Desecrator:
                    return BelphegorSettings.Instance.Avoidance.DesecratorDodgeHealthPct;
                case (int)GroundEffectSNOs.HighlandWalkerSpore:
                    return BelphegorSettings.Instance.Avoidance.SporeDodgeHealthPct;
                case (int)GroundEffectSNOs.Belial_GroundBomb_Pending:
                case (int)GroundEffectSNOs.Belial_GroundBomb_Impact:
                    return BelphegorSettings.Instance.Avoidance.BelialBombDodgeHealthPct;
                case (int)GroundEffectSNOs.Creep_Mob_Arm:
                    return BelphegorSettings.Instance.Avoidance.CreepMobArmDodgeHealthPct;
                case (int)GroundEffectSNOs.Azmodan_AOD_Demon:
                    return BelphegorSettings.Instance.Avoidance.AzmodanAODDemonDodgeHealthPct;
                case (int)GroundEffectSNOs.Ghom_Gas:
                    return BelphegorSettings.Instance.Avoidance.GhomGasDodgeHealthPct;
                case (int)GroundEffectSNOs.Diablo_Fire_Ring_A:
                case (int)GroundEffectSNOs.Diablo_Fire_Ring_B:
                    return BelphegorSettings.Instance.Avoidance.DiabloFireRingDodgeHealthPct;
                default:
                    return 0;
            }
        }

        private static Vector3 GenerateTargetPoint(CombatContext context)
        {
            IEnumerable<Vector3> points =
                context.AoePositions.Nodes.SelectMany(
                    p =>
                    p.Position.GeneratePossibleTargetPoints(context, p.Radius + 2,
                                                            BelphegorSettings.Instance.Avoidance.
                                                                IsLocalPathingForPointGenerationActive)).Union(
                                                                    context.PlayerPosition.GeneratePossibleTargetPoints(context,
                                                                        30f,
                                                                        BelphegorSettings.Instance.Avoidance.
                                                                            IsLocalPathingForPointGenerationActive)).
                    Where(p => !p.IsCollidingWithAoe(context));
            if (BelphegorSettings.Instance.Avoidance.IsNearestTargetPointEvaluationActive)
                return points.OrderBy(p => p.DistanceSqr(context.PlayerPosition)).FirstOrDefault();
            return points.FirstOrDefault();
        }

        #endregion

        #region Nested type: GroundEffectSNOs

        private enum GroundEffectSNOs
        {
            ArcaneCenterReverse = 221225,
            ArcaneCenter = 219702,
            Plagued = 108869,
            Desecrator = 84608,
            Molten = 4803,
            MoltenTrail = 95868,
            IceClusters = 223675,
            HighlandWalkerSpore = 5482,
            Belial_GroundBomb_Impact = 161833, // That may be the active ground effect for belial aoe.
            Belial_GroundBomb_Pending = 161822,
            Creep_Mob_Arm = 3865,
            Azmodan_AOD_Demon = 123124,
            Ghom_Gas = 93837,
            Diablo_Fire_Ring_A = 226350,
            Diablo_Fire_Ring_B = 226525
        }

        #endregion
    }
}