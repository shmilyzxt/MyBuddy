using System;
using System.Linq;
using Belphegor.Composites;
using Belphegor.Dynamics;
using Belphegor.Helpers;
using Belphegor.Settings;
using Belphegor.Utilities;
using Zeta;
using Zeta.Common;
using Zeta.Common.Helpers;
using Zeta.CommonBot;
using Zeta.Internals.Actors;
using Zeta.TreeSharp;
using Action = Zeta.TreeSharp.Action;

namespace Belphegor.Routines
{
    public class Monk
    {
        private static readonly ContextChangeHandler CtxChanger = ctx => new CombatContext(context =>
        {
            return CombatTargeting.Instance.LastObjects.OfType<DiaUnit>().FirstOrDefault(u => u.IsValid && (!u.Position.IsCollidingWithAoe(context) || u.Position.DistanceSqr(ZetaDia.Me.Position) < BelphegorSettings.Instance.Monk.MaximumRange * BelphegorSettings.Instance.Monk.MaximumRange)) ?? CombatTargeting.Instance.LastObjects.OfType<DiaUnit>().FirstOrDefault(u => u.IsValid);
        });

        [Class(ActorClass.Monk)]
        [Behavior(BehaviorType.Buff)]
        public static Composite MonkBuff()
        {
            return
                new Decorator(ctx => !ZetaDia.Me.IsInTown,
                              new PrioritySelector(CtxChanger,
                                                   Common.CreateWaitForAttack(),
                                                   Common.CreateWaitForCast(),
                                                   new SelfCast(SNOPower.Monk_MysticAlly,
                                                                extra =>
                                                                AllyTimer.IsFinished && !Minion.HasPet(Pet.MysticAlly),
                                                                s => AllyTimer.Reset()),
                                                   new SelfCast(SNOPower.Monk_BreathOfHeaven,
                                                                ret =>
                                                                ZetaDia.Me.CurrentPrimaryResource >
                                                                BelphegorSettings.Instance.Monk.
                                                                    BoHBlazingWrathOutOfCombatSpiritTreshold &&
                                                                !ZetaDia.Me.HasBuff(SNOPower.Monk_BreathOfHeaven) &&
                                                                BelphegorSettings.Instance.Monk.BoHBlazingWrath),
                                                   Avoidance.CreateMoveForAvoidance(BelphegorSettings.Instance.Monk.MaximumRange)
                                  )
                    );
        }

        [Class(ActorClass.Monk)]
        [Behavior(BehaviorType.Combat)]
        public static Composite MonkCombat()
        {
            return
                new PrioritySelector(CtxChanger,
                                     new SelfCast(SNOPower.Monk_Serenity,
                                                  extra =>
                                                  ZetaDia.Me.HitpointsCurrentPct <=
                                                  BelphegorSettings.Instance.Monk.SerenityHp ||
                                                  ((CombatContext) extra).IsPlayerIncapacited),
                                     Common.CreateUsePotion(),
                                     Common.CreateWaitWhileFearedStunnedFrozenOrBlind(),
                                     Common.CreateGetHealthGlobe(),
                                     Common.CreateWaitForAttack(),
                                     //Heals
                                     new SelfCast(SNOPower.Monk_BreathOfHeaven,
                                                  extra =>
                                                  ((CombatContext) extra).CurrentHealthPercentage <=
                                                  BelphegorSettings.Instance.Monk.BreathOfHeavenHp
                                                  ||
                                                  (!ZetaDia.Me.HasBuff(SNOPower.Monk_BreathOfHeaven) &&
                                                   BelphegorSettings.Instance.Monk.BoHBlazingWrath)),
                                     new SelfCast(SNOPower.Monk_InnerSanctuary,
                                                  extra => ZetaDia.Me.HitpointsCurrentPct <= 0.4),
                                     new Decorator(
                                         ctx =>
                                         ctx is CombatContext &&
                                         ((CombatContext) ctx).CurrentTarget != null,
                                         new PrioritySelector(
                                             //Mantra
                                             new Decorator(
                                                 ret =>
                                                 MantraTimer.IsFinished &&
                                                 ZetaDia.Me.CurrentPrimaryResource >=
                                                 BelphegorSettings.Instance.Monk.MantraSpirit &&
                                                 (!BelphegorSettings.Instance.Monk.WaitForSweepingWind ||
                                                  ZetaDia.Me.HasBuff(SNOPower.Monk_SweepingWind)),
                                                 new PrioritySelector(
                                                     new SelfCast(SNOPower.Monk_MantraOfEvasion,
                                                                  extra =>
                                                                  !ZetaDia.Me.HasBuff(SNOPower.Monk_MantraOfEvasion) ||
                                                                  BelphegorSettings.Instance.Monk.SpamMantra,
                                                                  s => MantraTimer.Reset()),
                                                     new SelfCast(SNOPower.Monk_MantraOfConviction,
                                                                  extra =>
                                                                  !ZetaDia.Me.HasBuff(SNOPower.Monk_MantraOfConviction) ||
                                                                  BelphegorSettings.Instance.Monk.SpamMantra,
                                                                  s => MantraTimer.Reset()),
                                                     new SelfCast(SNOPower.Monk_MantraOfHealing,
                                                                  extra =>
                                                                  !ZetaDia.Me.HasBuff(SNOPower.Monk_MantraOfHealing) ||
                                                                  BelphegorSettings.Instance.Monk.SpamMantra,
                                                                  s => MantraTimer.Reset()),
                                                     new SelfCast(SNOPower.Monk_MantraOfRetribution,
                                                                  extra =>
                                                                  !ZetaDia.Me.HasBuff(SNOPower.Monk_MantraOfRetribution) ||
                                                                  BelphegorSettings.Instance.Monk.SpamMantra,
                                                                  s => MantraTimer.Reset())
                                                     )),
                                             // Pull phase.
                                             new Decorator(
                                                 ctx =>
                                                 ((CombatContext) ctx).TargetDistance >
                                                 BelphegorSettings.Instance.Monk.MaximumRange,
                                                 new PrioritySelector(
                                                     new CastOnUnit(SNOPower.Monk_DashingStrike,
                                                                    ctx => ((CombatContext) ctx).TargetGuid),
                                                     new CastOnUnit(SNOPower.Monk_FistsofThunder,
                                                                    ctx => ((CombatContext) ctx).TargetGuid)
                                                     //CommonBehaviors.MoveTo(ctx => ((DiaUnit)ctx).Position, "Moving towards unit")
                                                     )),
                                             //Buffs
                                             new SelfCast(SNOPower.Monk_SweepingWind,
                                                          extra =>
                                                          !ZetaDia.Me.HasBuff(SNOPower.Monk_SweepingWind) &&
                                                          (Unit.IsEliteInRange(16f) ||
                                                           Clusters.GetClusterCount(ZetaDia.Me, ((CombatContext) extra),
                                                                                    ClusterType.Radius, 16f) > 3)),
                                             //Focus Skills
                                             new SelfCast(SNOPower.Monk_CycloneStrike,
                                                          extra =>
                                                          Unit.IsEliteInRange(16f) ||
                                                          Clusters.GetClusterCount(ZetaDia.Me, ((CombatContext) extra),
                                                                                   ClusterType.Radius, 20f) >=
                                                          BelphegorSettings.Instance.Monk.CycloneStrikeAoECount),
                                             new SelfCast(SNOPower.Monk_SevenSidedStrike,
                                                          extra =>
                                                          Unit.IsEliteInRange(16f) ||
                                                          Clusters.GetClusterCount(ZetaDia.Me, ((CombatContext) extra),
                                                                                   ClusterType.Radius, 20f) >=
                                                          BelphegorSettings.Instance.Monk.SevenSidedStrikeAoECount),
                                             //Secondary
                                             new CastOnUnit(SNOPower.Monk_ExplodingPalm,
                                                            ctx => ((CombatContext) ctx).TargetGuid,
                                                            extra => ExplodingPalm.IsFinished,
                                                            s => ExplodingPalm.Reset()),
                                             new CastOnUnit(SNOPower.Monk_LashingTailKick,
                                                            ctx => ((CombatContext) ctx).TargetGuid,
                                                            ctx =>
                                                            (Clusters.GetClusterCount(ZetaDia.Me, ((CombatContext) ctx),
                                                                                      ClusterType.Radius, 16f) >=
                                                             BelphegorSettings.Instance.Monk.LashingTailKickAoECount ||
                                                             Unit.IsEliteInRange(16f)) &&
                                                            ZetaDia.Me.CurrentPrimaryResource >
                                                            BelphegorSettings.Instance.Monk.
                                                                LashingTailKickSpiritTreshold),
                                             new SelfCast(SNOPower.Monk_BlindingFlash,
                                                          ctx =>
                                                          Clusters.GetClusterCount(ZetaDia.Me, ((CombatContext) ctx),
                                                                                   ClusterType.Radius, 18f) >= 5 ||
                                                          Unit.IsEliteInRange(18f)),
                                             new CastOnUnit(SNOPower.Monk_WaveOfLight,
                                                            ctx => ((CombatContext) ctx).TargetGuid,
                                                            ctx => ((CombatContext)ctx).TargetDistance <= 16f),
                                             new CastOnUnit(SNOPower.Monk_TempestRush,
                                                            ctx => ((CombatContext) ctx).TargetGuid,
                                                            ctx => ZetaDia.Me.CurrentPrimaryResource > 15),
                                             // Primary Skills. 
                                             new CastOnUnit(SNOPower.Monk_DeadlyReach,
                                                            ctx => ((CombatContext) ctx).TargetGuid, null,
                                                            ctx => PrimarySpamTimer.Reset(),
                                                            keepSpamming => !PrimarySpamTimer.IsFinished, CtxChanger),
                                             new CastOnUnit(SNOPower.Monk_CripplingWave,
                                                            ctx => ((CombatContext) ctx).TargetGuid, null,
                                                            ctx => PrimarySpamTimer.Reset(),
                                                            keepSpamming => !PrimarySpamTimer.IsFinished, CtxChanger),
                                             new CastOnUnit(SNOPower.Monk_WayOfTheHundredFists,
                                                            ctx => ((CombatContext) ctx).TargetGuid, null,
                                                            ctx => PrimarySpamTimer.Reset(),
                                                            keepSpamming => !PrimarySpamTimer.IsFinished, CtxChanger),
                                             new CastOnUnit(SNOPower.Monk_FistsofThunder,
                                                            ctx => ((CombatContext) ctx).TargetGuid, null,
                                                            ctx => PrimarySpamTimer.Reset(),
                                                            keepSpamming => !PrimarySpamTimer.IsFinished, CtxChanger)
                                             )
                                         ),
                                     new Action(ret => RunStatus.Success)
                    );
        }

        [Class(ActorClass.Monk)]
        [Behavior(BehaviorType.Movement)]
        public static Composite MonkMovement()
        {
            return new PrioritySelector(
                new CastAtLocation(SNOPower.Monk_TempestRush, ctx => (Vector3) ctx,
                                   ctx => ZetaDia.Me.CurrentPrimaryResource > 15 &&
                                          BelphegorSettings.Instance.Monk.UseTempestRushForMovement),
                Common.CreateWaitForAttack(),
                Common.CreateWaitForCast(),
                new Action(ret =>
                               {
                                   ZetaDia.Me.Movement.MoveActor((Vector3) ret);
                                   return RunStatus.Success;
                               })
                );
        }

        public static void MonkOnLevelUp(object sender, EventArgs e)
        {
            if (ZetaDia.Me.ActorClass != ActorClass.Monk)
                return;

            int myLevel = ZetaDia.Me.Level;

            Logger.Write("Player leveled up, congrats! Your level is now: {0}", myLevel);

            // Set Lashing tail kick once we reach level 2
            if (myLevel == 2)
            {
                ZetaDia.Me.SetActiveSkill(SNOPower.Monk_LashingTailKick, -1, 1);
                Logger.Write("Setting Lash Tail Kick as Secondary");
            }

            // Set Dead reach it's better then Fists of thunder imo.
            if (myLevel == 3)
            {
                ZetaDia.Me.SetActiveSkill(SNOPower.Monk_DeadlyReach, -1, 0);
                Logger.Write("Setting Deadly Reach as Primary");
            }

            // Make sure we set binding flash, useful spell in crowded situations!
            if (myLevel == 4)
            {
                ZetaDia.Me.SetActiveSkill(SNOPower.Monk_BlindingFlash, -1, 2);
                Logger.Write("Setting Binding Flash as Defensive");
            }

            // Binding flash is nice but being alive is even better!
            if (myLevel == 8)
            {
                ZetaDia.Me.SetActiveSkill(SNOPower.Monk_BreathOfHeaven, -1, 2);
                Logger.Write("Setting Breath of Heaven as Defensive");
            }

            // Make sure we set Dashing strike, very cool and useful spell great opener.
            if (myLevel == 9)
            {
                ZetaDia.Me.SetActiveSkill(SNOPower.Monk_DashingStrike, -1, 3);
                Logger.Write("Setting Dashing Strike as Techniques");
            }

            SpellCast.UpdateHotbarPowerSNOs();
        }

        #region Timers

        private static readonly WaitTimer ExplodingPalm =
            new WaitTimer(TimeSpan.FromSeconds(BelphegorSettings.Instance.Monk.ExplodingPalmDelay));

        private static readonly WaitTimer MantraTimer = new WaitTimer(TimeSpan.FromSeconds(3));
        private static readonly WaitTimer AllyTimer = new WaitTimer(TimeSpan.FromSeconds(10));
        private static readonly WaitTimer PrimarySpamTimer = WaitTimer.OneSecond;

        static Monk()
        {
            GameEvents.OnGameLeft += OnGameLeft;
            GameEvents.OnPlayerDied += OnGameLeft;
        }

        private static void OnGameLeft(object sender, EventArgs e)
        {
            ExplodingPalm.Stop();
            MantraTimer.Stop();
            AllyTimer.Stop();
            PrimarySpamTimer.Stop();
        }

        #endregion
    }
}