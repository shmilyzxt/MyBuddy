using System;
using System.Linq;
using Belphegor.Settings;
using Belphegor.Utilities;
using Zeta;
using Zeta.Common.Helpers;
using Zeta.CommonBot;
using Zeta.Internals.Actors;
using Zeta.TreeSharp;
using Action = Zeta.TreeSharp.Action;

namespace Belphegor.Helpers
{
    public static class Common
    {
        private static readonly WaitTimer PotionCooldownTimer = WaitTimer.ThirtySeconds;

        //public static bool IsInCombat
        //{
        //    get
        //    {
        //        return CombatTargeting.Instance.FirstNpc != null &&
        //               CombatTargeting.Instance.FirstNpc.Distance < CharacterSettings.Instance.KillRadius;
        //    }
        //}


        private static AnimationState CurrentAnimationState
        {
            get
            {
                if (ZetaDia.Me.CommonData.AnimationInfo != null)
                    return ZetaDia.Me.CommonData.AnimationInfo.State;

                return AnimationState.Invalid;
            }
        }

        /// <summary>
        /// Gets the best potion.
        /// </summary>
        /// <remarks>Created 2012-07-11</remarks>
        private static ACDItem BestPotion
        {
            get
            {
                return
                    ZetaDia.Me.Inventory.Backpack.Where(i => i.IsPotion && i.RequiredLevel <= ZetaDia.Me.Level).
                        OrderByDescending(p => p.HitpointsGranted).FirstOrDefault();
            }
        }

        private static DiaItem HealthGlobes
        {
            get
            {
                return
                    ZetaDia.Actors.GetActorsOfType<DiaItem>().Where(
                        i => i.ActorSNO == 4267 && i.Distance <= BelphegorSettings.Instance.HealthGlobeDistance).OrderBy
                        (i => i.Distance).FirstOrDefault();
            }
        }

        private static DiaObject HealthWell
        {
            get
            {
                return
                    ZetaDia.Actors.GetActorsOfType<DiaItem>().Where(
                        i =>
                        !Blacklist.Contains(i.ACDGuid) && i.ActorSNO == 4267 &&
                        i.Distance <= BelphegorSettings.Instance.HealthWellDistance).OrderBy(i => i.Distance).
                        FirstOrDefault();
            }
        }

        public static Composite CreateWaitWhileRunningAttackingChannelingOrCasting()
        {
            return
                new Decorator(
                    ret =>
                    CurrentAnimationState.HasFlag((AnimationState) 15),
                    new Action(ret => RunStatus.Success)
                    );
        }


        /// <summary>
        /// Creates  a behavior that makes sure the bot doesn't perform any actions in combat if the player is stunned
        /// </summary>
        /// <returns></returns>
        /// <remarks>Created 2012-07-29</remarks>
        public static Composite CreateWaitWhileFearedStunnedFrozenOrBlind()
        {
            return
                new Decorator(ret => ((CombatContext) ret).IsPlayerFearedStunnedFrozenOrBlind,
                              new Action(ret => RunStatus.Success)
                    );
        }

        /// <summary>
        /// Creates  a behavior that makes sure the bot doesn't perform any actions in combat if the player is incapacitated
        /// </summary>
        /// <returns></returns>
        /// <remarks>Created 2012-07-11</remarks>
        public static Composite CreateWaitWhileIncapacitated()
        {
            return
                new Decorator(ret => ((CombatContext) ret).IsPlayerIncapacited,
                              new Action(ret => RunStatus.Success)
                    );
        }

        /// <summary>
        /// Creates a behavior that makes sure the bot waits for the previous spell to finish casting.
        /// </summary>
        /// <returns></returns>
        /// <remarks>Created 2012-07-11</remarks>
        public static Composite CreateWaitForAttack()
        {
            return
                new Decorator(ret => CurrentAnimationState == AnimationState.Attacking,
                              new Action(ret => RunStatus.Success)
                    );
        }

        /// <summary>
        /// Creates a behavior that makes sure the bot waits for the previous spell to finish casting.
        /// </summary>
        /// <returns></returns>
        /// <remarks>Created 2012-07-11</remarks>
        public static Composite CreateWaitForCast()
        {
            return
                new Decorator(ret => CurrentAnimationState == AnimationState.Casting,
                              new Action(ret => RunStatus.Success)
                    );
        }

        /// <summary>
        /// Creates a behavior for using potions.
        /// </summary>
        /// <returns></returns>
        /// <remarks>Created 2012-07-11</remarks>
        public static Composite CreateUsePotion()
        {
            return
                new Decorator(
                    ret =>
                    PotionCooldownTimer.IsFinished &&
                    ((CombatContext) ret).CurrentHealthPercentage <= BelphegorSettings.Instance.HealthPotionPct,
                    new PrioritySelector(ctx => BestPotion,
                                         new Decorator(ctx => ctx != null,
                                                       new Sequence(
                                                           new Action(
                                                               ctx =>
                                                               ZetaDia.Me.Inventory.UseItem(((ACDItem) ctx).DynamicId)),
                                                           new Action(ctx => PotionCooldownTimer.Reset()),
                                                           new Action(
                                                               ctx =>
                                                               Logger.Write(
                                                                   "Potion set to use below {0}%, my health is currently {1}%, using {2}",
                                                                   100*BelphegorSettings.Instance.HealthPotionPct,
                                                                   Math.Round(100*ZetaDia.Me.HitpointsCurrentPct),
                                                                   ((ACDItem) ctx).Name))
                                                           )
                                             )
                        )
                    );
        }

        public static Composite CreateGetHealthGlobe()
        {
            return
                new Decorator(
                    ret =>
                    BelphegorSettings.Instance.GetHealthGlobe &&
                    ZetaDia.Me.HitpointsCurrentPct <= BelphegorSettings.Instance.HealthGlobeHP,
                    new PrioritySelector(ctx => HealthGlobes,
                                         new Decorator(ctx => ctx != null,
                                                       CommonBehaviors.MoveAndStop(ctx => ((DiaItem) ctx).Position, 1f,
                                                                                   false, "Health Globe")
                                             )));
        }

        public static Composite CreateUseHealthWell()
        {
            return
                new Decorator(
                    ret =>
                    BelphegorSettings.Instance.UseHealthWell &&
                    ZetaDia.Me.HitpointsCurrentPct <= BelphegorSettings.Instance.HealthWellHP,
                    new PrioritySelector(ctx => HealthWell,
                                         new Decorator(ctx => ctx != null,
                                                       new PrioritySelector(
                                                           new Decorator(ctx => ((DiaObject) ctx).Distance > 14f,
                                                                         CommonBehaviors.MoveAndStop(
                                                                             ctx => ((DiaObject) ctx).Position, 14f)),
                                                           new Decorator(ctx => ((DiaObject) ctx).Distance <= 14f,
                                                                         new Action(ctx =>
                                                                                        {
                                                                                            ((DiaObject) ctx).Interact();
                                                                                            Blacklist.Add(
                                                                                                ((DiaObject) ctx).
                                                                                                    ACDGuid,
                                                                                                BlacklistFlags.All,
                                                                                                TimeSpan.FromMinutes(10));
                                                                                        }
                                                                             )
                                                               )
                                                           )
                                             )
                        )
                    );
        }
    }
}