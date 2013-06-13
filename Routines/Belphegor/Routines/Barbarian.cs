using System;
using System.Diagnostics;
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
    public class Barbarian
    {
        private static readonly ContextChangeHandler CtxChanger = ctx => new CombatContext(context =>
            {
                DiaUnit first = null;
                foreach (DiaObject lastObject in CombatTargeting.Instance.LastObjects)
                {
                    DiaUnit u = lastObject as DiaUnit;
                    if (u != null)
                    {
                        if (u.IsValid && (!u.Position.IsCollidingWithAoe(context) || 
                            u.Position.DistanceSqr(ZetaDia.Me.Position) < BelphegorSettings.Instance.Barbarian.MaximumRange*BelphegorSettings.Instance.Barbarian.MaximumRange))
                        {
                            first = u;
                            break;
                        }
                    }
                }

                return first ?? CombatTargeting.Instance.LastObjects.FirstOrDefault(u => u is DiaUnit && u.IsValid) as DiaUnit;
            });

        [Class(ActorClass.Barbarian)]
        [Behavior(BehaviorType.Buff)]
        public static Composite BarbarianBuffs()
        {
            return
                new PrioritySelector(CtxChanger,
                    Common.CreateWaitForAttack(),
                    Common.CreateWaitForCast(),
                    new SelfCast(SNOPower.Barbarian_WarCry,
                        extra => (!ZetaDia.Me.HasBuff(SNOPower.Barbarian_WarCry) 
                            || BelphegorSettings.Instance.Barbarian.SpamWarCry) 
                            && !ZetaDia.Me.IsInTown),
                            Avoidance.CreateMoveForAvoidance(BelphegorSettings.Instance.Barbarian.MaximumRange)
                    );
        }

        [Class(ActorClass.Barbarian)]
        [Behavior(BehaviorType.Combat)]
        public static Composite BarbarianCombat()
        {
            return
                new PrioritySelector(CtxChanger,
                    new Decorator(ctx => ctx != null && ((CombatContext)ctx).CurrentTarget != null,
                        new PrioritySelector(
                            
                            // Buff attack rate or get free!
                            new SelfCast(SNOPower.Barbarian_WrathOfTheBerserker, extra => ((CombatContext)extra).IsPlayerIncapacited
                            ),
                            
                            Common.CreateUsePotion(),
                            Common.CreateGetHealthGlobe(),
                            Common.CreateWaitWhileFearedStunnedFrozenOrBlind(),
                            Common.CreateWaitForAttack(),
                
                            // Defence low hp or many attackers.
                            new SelfCast(SNOPower.Barbarian_IgnorePain, require => 
                                ((CombatContext)require).CurrentHealthPercentage <= BelphegorSettings.Instance.Barbarian.IgnorePainPct
                                || Clusters.GetClusterCount(ZetaDia.Me, (CombatContext)require, ClusterType.Radius, 12f) >= 6
                            ),
                                             
                            ThrowBarbBehavior(),
                            // Pull phase.
                            new Decorator(ctx => 
                                ((CombatContext)ctx).TargetDistance > BelphegorSettings.Instance.Barbarian.MaximumRange  && !((CombatContext)ctx).IsPlayerIncapacited,
                                new PrioritySelector(
                                    new CastAtLocation(SNOPower.Barbarian_Leap, ctx => ((CombatContext)ctx).TargetPosition),
                                    new CastOnUnit(SNOPower.Barbarian_FuriousCharge, ctx => ((CombatContext)ctx).TargetGuid),
                                    new CastOnUnit(SNOPower.Barbarian_AncientSpear, ctx => ((CombatContext)ctx).TargetGuid)
                                )
                            ),

                            //Leap on cooldown, usefull for the increased armour buff
                            new CastAtLocation(SNOPower.Barbarian_Leap, ctx => ((CombatContext)ctx).TargetPosition,
                                extra => BelphegorSettings.Instance.Barbarian.LeapOnCooldown 
                                    && !((CombatContext)extra).IsPlayerIncapacited
                            ),
                                    
                            new SelfCast(SNOPower.Barbarian_Sprint, 
                                extra => SprintTimer.IsFinished 
                                    && !ZetaDia.Me.HasBuff(SNOPower.Barbarian_Sprint),
                                    s => SprintTimer.Reset()
                            ),

                            new SelfCast(SNOPower.Barbarian_BattleRage, extra => !ZetaDia.Me.HasBuff(SNOPower.Barbarian_BattleRage)),
                            new SelfCast(SNOPower.Barbarian_Rend, ctx => RendTimer.Elapsed >= 
                                TimeSpan.FromSeconds(BelphegorSettings.Instance.Barbarian.RendTimer) &&
                                ((CombatContext)ctx).TargetDistance <= BelphegorSettings.Instance.Barbarian.RendRange,
                                s => RendTimer.Restart()
                            ),
                            
                            new CastAtLocation(SNOPower.Barbarian_Revenge,
                                ctx => ((CombatContext)ctx).TargetPosition,
                                ctx => ((CombatContext)ctx).TargetDistance < 16f
                            ),

                            //Uses Dreadnought rune to heal
                            new CastOnUnit(SNOPower.Barbarian_FuriousCharge, ctx => ((CombatContext)ctx).TargetGuid, 
                                extra => BelphegorSettings.Instance.Barbarian.FuriousChargeDreadnought 
                                    && ((CombatContext)extra).CurrentHealthPercentage <= BelphegorSettings.Instance.Barbarian.FuriousChargeDreadnoughtHP
                            ),
                
                                    
                            //Rage
                            new SelfCast(SNOPower.Barbarian_WrathOfTheBerserker, 
                                ctx => Unit.IsEliteInRange(16f) || 
                                    Clusters.GetClusterCount(ZetaDia.Me, (CombatContext)ctx, ClusterType.Radius, 16f) >= 
                                    BelphegorSettings.Instance.Barbarian.WotBAoeCount
                               ),

                            new SelfCast(SNOPower.Barbarian_CallOfTheAncients,
                                ctx => Unit.IsEliteInRange(16f) ||
                                    Clusters.GetClusterCount(ZetaDia.Me, (CombatContext)ctx, ClusterType.Radius, 16f) >=
                                    BelphegorSettings.Instance.Barbarian.CotAAoeCount
                            ),

                            new SelfCast(SNOPower.Barbarian_Earthquake,
                                ctx => Unit.IsEliteInRange(16f) ||
                                    Clusters.GetClusterCount(ZetaDia.Me, (CombatContext)ctx, ClusterType.Radius, 16f) >=
                                    BelphegorSettings.Instance.Barbarian.EarthquakeAoeCount
                            ),

                            new SelfCast(SNOPower.Barbarian_GroundStomp,
                                ctx => Clusters.GetClusterCount(ZetaDia.Me, (CombatContext)ctx, ClusterType.Radius, 12f) >= 2 
                                    || Unit.IsEliteInRange(18f)
                            ),

                            new SelfCast(SNOPower.Barbarian_Overpower, 
                                ctx => Clusters.GetClusterCount(ZetaDia.Me, (CombatContext)ctx, ClusterType.Radius, 16f) >=
                                    BelphegorSettings.Instance.Barbarian.OverpowerAoeCount 
                                    || Unit.IsEliteInRange(16f)
                            ),

                            // Threatning shout.
                            new SelfCast(SNOPower.Barbarian_ThreateningShout,
                                ctx => Clusters.GetClusterCount(ZetaDia.Me, (CombatContext)ctx, ClusterType.Radius, 16f) >= 2 
                                    || Unit.IsEliteInRange(16f)
                            ),

                            // Fury spenders.
                            new CastOnUnit(SNOPower.Barbarian_HammerOfTheAncients,
                                ctx => ((CombatContext)ctx).TargetGuid
                            ),

                            new CastAtLocation(SNOPower.Barbarian_SeismicSlam,
                                ctx => ((CombatContext)ctx).TargetPosition
                            ),

                            new CastOnUnit(SNOPower.Barbarian_WeaponThrow,
                                ctx => ((CombatContext)ctx).TargetGuid
                            ),

                            new CastAtLocation(SNOPower.Barbarian_Whirlwind,
                                ctx => ((CombatContext)ctx).WhirlWindTargetPosition,
                                ctx => Clusters.GetClusterCount(ZetaDia.Me, (CombatContext)ctx, ClusterType.Radius, BelphegorSettings.Instance.Barbarian.WhirlwindClusterRange) >=
                                    BelphegorSettings.Instance.Barbarian.WhirlwindAoeCount
                                    || ((CombatContext)ctx).CurrentTarget.IsElite()
                            ),
                
                            // Fury Generators
                            new CastOnUnit(SNOPower.Barbarian_Cleave,
                                ctx => ((CombatContext)ctx).TargetGuid, null, 
                                ctx => PrimarySpamTimer.Reset(),
                                keepSpamming => !PrimarySpamTimer.IsFinished, CtxChanger
                            ),

                            new CastOnUnit(SNOPower.Barbarian_Bash,
                                ctx => ((CombatContext)ctx).TargetGuid, null,
                                ctx => PrimarySpamTimer.Reset(),
                                keepSpamming => !PrimarySpamTimer.IsFinished, CtxChanger
                            ),

                            new CastOnUnit(SNOPower.Barbarian_Frenzy,
                                ctx => ((CombatContext)ctx).TargetGuid, null,
                                ctx => PrimarySpamTimer.Reset(),
                                keepSpamming => !PrimarySpamTimer.IsFinished, CtxChanger
                            )

                            //new Action(ctx => ZetaDia.Me.UsePower(SNOPower.Weapon_Melee_Instant, ((DiaUnit)ctx).Position, ZetaDia.CurrentWorldDynamicId))
                        )
                    ),

                    new Action(ret => RunStatus.Success)
                );
        }

        [Class(ActorClass.Barbarian)]
        [Behavior(BehaviorType.Movement)]
        public static Composite BarbarianMovement()
        {
            return
                new PrioritySelector(
                    Common.CreateWaitForAttack(),
                    Common.CreateWaitForCast(),
                    new SelfCast(SNOPower.Barbarian_Sprint,
                                 extra => SprintTimer.IsFinished && !ZetaDia.Me.HasBuff(SNOPower.Barbarian_Sprint),
                                 s => SprintTimer.Reset()),
                    new CastAtLocation(SNOPower.Barbarian_Leap, ctx => (Vector3)ctx,
                                       ctx =>
                                       BelphegorSettings.Instance.Barbarian.UseLeapForMovement &&
                                       ZetaDia.Me.Position.Distance((Vector3)ctx) >
                                       BelphegorSettings.Instance.Barbarian.LeapDistance),
                    new CastAtLocation(SNOPower.Barbarian_FuriousCharge, ctx => (Vector3)ctx,
                                       ctx =>
                                       BelphegorSettings.Instance.Barbarian.UseFuriousChargeForMovement &&
                                       ZetaDia.Me.Position.Distance((Vector3)ctx) >
                                       BelphegorSettings.Instance.Barbarian.FuriousChargeDistance),
                    new Action(ret =>
                                   {
                                       ZetaDia.Me.Movement.MoveActor((Vector3)ret);
                                       return RunStatus.Success;
                                   })
                    );
        }

        public static Composite ThrowBarbBehavior()
        {
            return new Decorator(
                ctx => BelphegorSettings.Instance.Barbarian.IsThrowBarbEnabled,
                new PrioritySelector(
                    new SelfCast(SNOPower.Barbarian_BattleRage,
                                 extra => !ZetaDia.Me.HasBuff(SNOPower.Barbarian_BattleRage)),
                    new SelfCast(SNOPower.Barbarian_WarCry, extra => !ZetaDia.Me.HasBuff(SNOPower.Barbarian_WarCry)),
                    new SelfCast(SNOPower.Barbarian_Overpower),
                    new CastOnUnit(SNOPower.Barbarian_AncientSpear, ctx => ((CombatContext)ctx).TargetGuid, null,
                                   ctx => PrimarySpamTimer.Reset(), keepSpamming => !PrimarySpamTimer.IsFinished,
                                   CtxChanger),
                    new CastOnUnit(SNOPower.Barbarian_WeaponThrow, ctx => ((CombatContext)ctx).TargetGuid),
                    new SelfCast(SNOPower.Barbarian_WarCry)
                    )
                );
        }

        public static void BarbarianOnLevelUp(object sender, EventArgs e)
        {
            if (ZetaDia.Me.ActorClass != ActorClass.Barbarian)
                return;

            int myLevel = ZetaDia.Me.Level;

            Logger.Write("Player leveled up, congrats! Your level is now: {0}",
                         myLevel
                );

            #region Primarey Slot

            if (myLevel == 18)
            {
                Logger.Write("Add [R] Ravage to Cleave");
                ZetaDia.Me.SetActiveSkill(SNOPower.Barbarian_Cleave, 2, 0);
            }
            else if (myLevel == 9)
            {
                Logger.Write("Add [R] Rupture to Cleave");
                ZetaDia.Me.SetActiveSkill(SNOPower.Barbarian_Cleave, 1, 0);
            }
            else if (myLevel == 3)
            {
                Logger.Write("Equip Cleave");
                ZetaDia.Me.SetActiveSkill(SNOPower.Barbarian_Cleave, -1, 0);
            }

            #endregion

            #region Secondary Slot

            if (myLevel == 19)
            {
                Logger.Write("Add [R] Blood Lust to Rend. ");
                ZetaDia.Me.SetActiveSkill(SNOPower.Barbarian_Rend, 2, 1);
            }
            else if (myLevel == 11)
            {
                Logger.Write("Equip Rend with [R] Ravage in the place of Hammer of the Ancients.");
                ZetaDia.Me.SetActiveSkill(SNOPower.Barbarian_Rend, 1, 1);
            }
            else if (myLevel == 7)
            {
                Logger.Write("Add [R] Rolling Thunder to Hammer of the Ancients.");
                ZetaDia.Me.SetActiveSkill(SNOPower.Barbarian_HammerOfTheAncients, 1, 1);
            }
            else if (myLevel == 2)
            {
                Logger.Write("Setting Hammer of the Ancients");
                ZetaDia.Me.SetActiveSkill(SNOPower.Barbarian_HammerOfTheAncients, -1, 1);
            }

            #endregion

            #region Defencive Slot

            if (myLevel == 27)
            {
                Logger.Write("Add [R] Battering Ram to Furious Charge. ");
                ZetaDia.Me.SetActiveSkill(SNOPower.Barbarian_FuriousCharge, 1, 2);
            }
            else if (myLevel == 21)
            {
                Logger.Write("Equip Furious Charge in the place of Leap.");
                ZetaDia.Me.SetActiveSkill(SNOPower.Barbarian_FuriousCharge, -1, 2);
            }
            else if (myLevel == 14)
            {
                Logger.Write("Add [R] Iron Impact to Leap.");
                ZetaDia.Me.SetActiveSkill(SNOPower.Barbarian_Leap, -1, 2);
            }
            else if (myLevel == 8)
            {
                Logger.Write("Equip Leap in the place of Ground Stomp.");
                ZetaDia.Me.SetActiveSkill(SNOPower.Barbarian_Leap, -1, 2);
            }
            else if (myLevel == 4)
            {
                Logger.Write("Setting Ground Stomp");
                ZetaDia.Me.SetActiveSkill(SNOPower.Barbarian_GroundStomp, -1, 2);
            }

            #endregion

            #region Might Slot

            if (myLevel == 26)
            {
                Logger.Write("Add [R] Marauder's Rage to Battle Rage. ");
                ZetaDia.Me.SetActiveSkill(SNOPower.Barbarian_BattleRage, 1, 3);
            }
            else if (myLevel == 22)
            {
                Logger.Write("Equip Battle Rage in the place of Threatening Shout.");
                ZetaDia.Me.SetActiveSkill(SNOPower.Barbarian_BattleRage, -1, 3);
            }
            else if (myLevel == 17)
            {
                Logger.Write("Equip Threatening Shout in the place of Ground Stomp.");
                ZetaDia.Me.SetActiveSkill(SNOPower.Barbarian_ThreateningShout, -1, 3);
            }
            else if (myLevel == 12)
            {
                Logger.Write("Add [R] Deafening Crash to Ground Stomp.");
                ZetaDia.Me.SetActiveSkill(SNOPower.Barbarian_GroundStomp, 1, 3);
            }
            else if (myLevel == 9)
            {
                Logger.Write("Equip Ground Stomp.");
                ZetaDia.Me.SetActiveSkill(SNOPower.Barbarian_GroundStomp, -1, 3);
            }

            #endregion

            #region Tactics Slot

            if (myLevel == 19)
            {
                Logger.Write("Add [R] Vengeance Is Mine to Revenge. ");
                ZetaDia.Me.SetActiveSkill(SNOPower.Barbarian_Revenge, 1, 4);
            }
            if (myLevel == 14)
            {
                Logger.Write("Equip Revenge. ");
                ZetaDia.Me.SetActiveSkill(SNOPower.Barbarian_Revenge, -1, 4);
            }

            #endregion

            #region Rage Slot

            if (myLevel == 29)
            {
                Logger.Write("Add [R] Storm of Steel to Overpower.");
                ZetaDia.Me.SetActiveSkill(SNOPower.Barbarian_Overpower, 1, 5);
            }
            else if (myLevel == 26)
            {
                Logger.Write("Equip Overpower in the place of Earthquake.");
                ZetaDia.Me.SetActiveSkill(SNOPower.Barbarian_Overpower, -1, 5);
            }
            else if (myLevel == 24)
            {
                Logger.Write("Add [R] Giant's Stride to Earthquake.");
                ZetaDia.Me.SetActiveSkill(SNOPower.Barbarian_Earthquake, 1, 5);
            }
            else if (myLevel == 19)
            {
                Logger.Write("Equip Earthquake.");
                ZetaDia.Me.SetActiveSkill(SNOPower.Barbarian_Earthquake, -1, 5);
            }

            #endregion

            #region Passive Skills

            if (myLevel == 30)
            {
                Logger.Write("Equip [P] Bloodthirst.");
                ZetaDia.Me.SetTraits(SNOPower.Barbarian_Passive_WeaponsMaster,
                                     SNOPower.Barbarian_Passive_InspiringPresence,
                                     SNOPower.Barbarian_Passive_Bloodthirst);
            }
            if (myLevel == 20)
            {
                Logger.Write("Equip [P] Inspiring Presence.");
                ZetaDia.Me.SetTraits(SNOPower.Barbarian_Passive_WeaponsMaster,
                                     SNOPower.Barbarian_Passive_InspiringPresence);
            }
            else if (myLevel == 16)
            {
                Logger.Write("Equip [P] Weapons Master in the place of [P] Ruthless.");
                ZetaDia.Me.SetTraits(SNOPower.Barbarian_Passive_WeaponsMaster);
            }
            else if (myLevel == 10)
            {
                Logger.Write("Equip [P] Ruthless.");
                ZetaDia.Me.SetTraits(SNOPower.Barbarian_Passive_Ruthless);
            }

            #endregion

            SpellCast.UpdateHotbarPowerSNOs();
        }

        #region timmers

        private static readonly WaitTimer SprintTimer = new WaitTimer(TimeSpan.FromSeconds(3));
        private static readonly Stopwatch RendTimer = new Stopwatch();
        private static readonly WaitTimer PrimarySpamTimer = WaitTimer.OneSecond;

        static Barbarian()
        {
            GameEvents.OnGameLeft += OnGameLeft;
            GameEvents.OnPlayerDied += OnGameLeft;
            GameEvents.OnGameJoined += OnGameJoin;
            GameEvents.OnWorldChanged += OnGameJoin;
        }

        private static void OnGameJoin(object sender, EventArgs e)
        {
            RendTimer.Start();
        }

        private static void OnGameLeft(object sender, EventArgs e)
        {
            SprintTimer.Stop();
            RendTimer.Stop();
            PrimarySpamTimer.Stop();
        }

        #endregion
    }
}