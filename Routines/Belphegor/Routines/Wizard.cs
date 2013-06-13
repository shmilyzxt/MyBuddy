using System;
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
    public class Wizard
    {
        private static readonly ContextChangeHandler CtxChanger = ctx => new CombatContext(context => CombatTargeting.Instance.FirstNpc);

        [Class(ActorClass.Wizard)]
        [Behavior(BehaviorType.Buff)]
        public static Composite WizardBuffs()
        {
            return
                new PrioritySelector(CtxChanger,
                                     Common.CreateWaitForAttack(),
                                     Common.CreateWaitForCast(),
                                     new SelfCast(SNOPower.Wizard_MagicWeapon,
                                                  extra => !ZetaDia.Me.HasBuff(SNOPower.Wizard_MagicWeapon)),
                                     new SelfCast(SNOPower.Wizard_Familiar, extra => FamiliarTimer.IsFinished,
                                                  s => FamiliarTimer.Reset()),
                                     new SelfCast(SNOPower.Wizard_EnergyArmor,
                                                  extra => !ZetaDia.Me.HasBuff(SNOPower.Wizard_EnergyArmor)),
                                     new SelfCast(SNOPower.Wizard_StormArmor,
                                                  extra => !ZetaDia.Me.HasBuff(SNOPower.Wizard_StormArmor)),
                                     new SelfCast(SNOPower.Wizard_IceArmor,
                                                  extra => !ZetaDia.Me.HasBuff(SNOPower.Wizard_IceArmor)),
                                     Avoidance.CreateMoveForAvoidance(
                                        BelphegorSettings.Instance.Wizard.MaximumRange)
                    );
        }


        [Class(ActorClass.Wizard)]
        [Behavior(BehaviorType.Combat)]
        public static Composite WizardCombat()
        {
            return
                new PrioritySelector(CtxChanger,
                                     new Decorator(
                                         ctx =>
                                         ctx is CombatContext &&
                                         ((CombatContext) ctx).CurrentTarget != null,
                                         new PrioritySelector(
                                             new SelfCast(SNOPower.Wizard_DiamondSkin,
                                                          extra =>
                                                          ((CombatContext) extra).CurrentHealthPercentage <=
                                                          BelphegorSettings.Instance.Wizard.DiamondSkinHp ||
                                                          ((CombatContext) extra).IsPlayerIncapacited),
                                             Common.CreateUsePotion(),
                                             Common.CreateWaitWhileFearedStunnedFrozenOrBlind(),
                                             Common.CreateGetHealthGlobe(),
                                             Common.CreateWaitForAttack(),
                                             Kiting.CreateKitingBehavior(),
                                             //Movement.MoveTo(ctx => ((DiaUnit)ctx).Position, BelphegorSettings.Instance.Wizard.MaximumRange),

                                             new Decorator(ret => ZetaDia.Me.HasBuff(SNOPower.Wizard_Archon),
                                                           new PrioritySelector(
                                                               new SelfCast(SNOPower.Wizard_Archon_SlowTime,
                                                                            extra =>
                                                                            ((CombatContext) extra).
                                                                                CurrentHealthPercentage <= 0.4),
                                                               new SelfCast(SNOPower.Wizard_Archon_ArcaneBlast,
                                                                            ctx =>
                                                                            ((CombatContext) ctx).CurrentTarget.IsElite(
                                                                                16f)
                                                                            ||
                                                                            Clusters.GetClusterCount(ZetaDia.Me,
                                                                                                     ((CombatContext)
                                                                                                      ctx),
                                                                                                     ClusterType.Radius,
                                                                                                     16f) >= 2),
                                                               new CastOnUnit(
                                                                   SNOPower.Wizard_Archon_DisintegrationWave,
                                                                   ctx => ((CombatContext) ctx).TargetGuid),
                                                               new CastOnUnit(SNOPower.Wizard_Archon_ArcaneStrike,
                                                                              ctx => ((CombatContext) ctx).TargetGuid)
                                                               )),
                                             // Low health stuff
                                             new SelfCast(SNOPower.Wizard_MirrorImage,
                                                          extra =>
                                                          ((CombatContext) extra).CurrentHealthPercentage <=
                                                          BelphegorSettings.Instance.Wizard.MirrorImageHp),
                                             new SelfCast(SNOPower.Wizard_SlowTime,
                                                          extra =>
                                                          ((CombatContext) extra).CurrentHealthPercentage <=
                                                          BelphegorSettings.Instance.Wizard.SlowTimeHp),
                                             // AoE spells.
                                             new SelfCast(SNOPower.Wizard_WaveOfForce,
                                                          ctx =>
                                                          Clusters.GetClusterCount(ZetaDia.Me, ((CombatContext) ctx),
                                                                                   ClusterType.Radius, 12f) >= 2 ||
                                                          (((CombatContext) ctx).CurrentTarget.IsElite(16f))),
                                             new SelfCast(SNOPower.Wizard_FrostNova,
                                                          ctx =>
                                                          Clusters.GetClusterCount(ZetaDia.Me, ((CombatContext) ctx),
                                                                                   ClusterType.Radius, 16f) >=
                                                          BelphegorSettings.Instance.Wizard.FrostNovaAoECount ||
                                                          (((CombatContext) ctx).CurrentTarget.IsElite(16f))),
                                             //Hydra
                                             new CastAtLocation(SNOPower.Wizard_Hydra,
                                                                ctx => ((CombatContext) ctx).TargetPosition,
                                                                ctx =>
                                                                HydraTimer.IsFinished && !Minion.HasPet(Pet.Hydra)
                                                                &&
                                                                (((CombatContext) ctx).CurrentTarget.IsElite() ||
                                                                 Clusters.GetClusterCount(
                                                                     ((CombatContext) ctx).CurrentTarget,
                                                                     ((CombatContext) ctx), ClusterType.Radius, 12f) >=
                                                                 BelphegorSettings.Instance.Wizard.HydraAoECount),
                                                                s => HydraTimer.Reset()),
                                             new SelfCast(SNOPower.Wizard_Archon,
                                                          ctx => ((CombatContext) ctx).CurrentTarget.IsElite() ||
                                                                 Clusters.GetClusterCount(ZetaDia.Me,
                                                                                          ((CombatContext) ctx),
                                                                                          ClusterType.Radius, 60f) >=
                                                                 BelphegorSettings.Instance.Wizard.ArchonAoECount),
                                             new CastOnUnit(SNOPower.Wizard_EnergyTwister,
                                                            ctx => ((CombatContext) ctx).TargetGuid,
                                                            ctx =>
                                                            Clusters.GetClusterCount(
                                                                ((CombatContext) ctx).CurrentTarget,
                                                                ((CombatContext) ctx), ClusterType.Radius, 20f) >=
                                                            BelphegorSettings.Instance.Wizard.EnergyTwisterAoECount),
                                             new CastAtLocation(SNOPower.Wizard_Meteor,
                                                                ctx => ((CombatContext) ctx).TargetPosition,
                                                                ctx =>
                                                                MeteorTimer.IsFinished &&
                                                                (((CombatContext) ctx).CurrentTarget.IsElite() ||
                                                                 Clusters.GetClusterCount(
                                                                     ((CombatContext) ctx).CurrentTarget,
                                                                     ((CombatContext) ctx), ClusterType.Radius, 16f) >=
                                                                 4),
                                                                s => MeteorTimer.Reset()),
                                             new CastAtLocation(SNOPower.Wizard_Blizzard,
                                                                ctx => ((CombatContext) ctx).TargetPosition,
                                                                ctx =>
                                                                BlizzardTimer.IsFinished &&
                                                                Clusters.GetClusterCount(
                                                                    ((CombatContext) ctx).CurrentTarget,
                                                                    ((CombatContext) ctx), ClusterType.Radius, 16f) >= 4,
                                                                s => BlizzardTimer.Reset()),
                                             new SelfCast(SNOPower.Wizard_ExplosiveBlast,
                                                          ctx =>
                                                          ExplosiveBlast.IsFinished &&
                                                          (((CombatContext) ctx).CurrentTarget.IsElite(16f) ||
                                                           Clusters.GetClusterCount(ZetaDia.Me, ((CombatContext) ctx),
                                                                                    ClusterType.Radius, 16f) >=
                                                           BelphegorSettings.Instance.Wizard.ExplosiveBlastAoECount),
                                                          s => ExplosiveBlast.Reset()),
                                             // Arcane power spenders.
                                             new CastOnUnit(SNOPower.Wizard_ArcaneOrb,
                                                            ctx => ((CombatContext) ctx).TargetGuid),
                                             new CastOnUnit(SNOPower.Wizard_RayOfFrost,
                                                            ctx => ((CombatContext) ctx).TargetGuid),
                                             new CastOnUnit(SNOPower.Wizard_ArcaneTorrent,
                                                            ctx => ((CombatContext) ctx).TargetGuid),
                                             new CastOnUnit(SNOPower.Wizard_Disintegrate,
                                                            ctx => ((CombatContext) ctx).TargetGuid),
                                             // Signature spells.
                                             new CastOnUnit(SNOPower.Wizard_SpectralBlade,
                                                            ctx => ((CombatContext) ctx).TargetGuid),
                                             new CastOnUnit(SNOPower.Wizard_Electrocute,
                                                            ctx => ((CombatContext) ctx).TargetGuid),
                                             new CastOnUnit(SNOPower.Wizard_ShockPulse,
                                                            ctx => ((CombatContext) ctx).TargetGuid),
                                             new CastOnUnit(SNOPower.Wizard_MagicMissile,
                                                            ctx => ((CombatContext) ctx).TargetGuid)
                                             )),
                                     new Action(ret => RunStatus.Success)
                    );
        }

        [Class(ActorClass.Wizard)]
        [Behavior(BehaviorType.Movement)]
        public static Composite WizardMovement()
        {
            return new PrioritySelector(
                Common.CreateWaitForAttack(),
                Common.CreateWaitForCast(),
                new CastAtLocation(SNOPower.Wizard_Teleport, ctx => (Vector3) ctx,
                                   ctx =>
                                   BelphegorSettings.Instance.Wizard.Teleport &&
                                   ZetaDia.Me.Position.Distance((Vector3) ctx) >
                                   BelphegorSettings.Instance.Wizard.TeleportDistance),
                new SelfCast(SNOPower.Wizard_Archon,
                             ctx =>
                             BelphegorSettings.Instance.Wizard.UseArchonForMovement &&
                             ZetaDia.Me.Position.Distance((Vector3) ctx) >
                             BelphegorSettings.Instance.Wizard.TeleportDistance),
                new CastAtLocation(SNOPower.Wizard_Archon_Teleport, ctx => (Vector3) ctx,
                                   ctx =>
                                   BelphegorSettings.Instance.Wizard.UseArchonForMovement &&
                                   ZetaDia.Me.Position.Distance((Vector3) ctx) >
                                   BelphegorSettings.Instance.Wizard.TeleportDistance),
                new Action(ret =>
                               {
                                   ZetaDia.Me.Movement.MoveActor((Vector3) ret);
                                   return RunStatus.Success;
                               })
                );
        }

        public static void WizardOnLevelUp(object sender, EventArgs e)
        {
            if (ZetaDia.Me.ActorClass != ActorClass.Wizard)
                return;

            int myLevel = ZetaDia.Me.Level;

            Logger.Write("Player leveled up, congrats! Your level is now: {0}",
                         myLevel
                );


            // ********** PRIMARY SLOT CHANGES **********
            // Set Shock Pulse as primary.
            if (myLevel == 3)
            {
                ZetaDia.Me.SetActiveSkill(SNOPower.Wizard_ShockPulse, -1, 0);
                Logger.Write("Setting Shock Pulse as Primary");
            }
            // Set Shock Pulse-Explosive bolts as primary.
            if (myLevel == 9)
            {
                ZetaDia.Me.SetActiveSkill(SNOPower.Wizard_ShockPulse, 1, 0);
                Logger.Write("Changing rune for Shock Pulse: \"Explosive Bolts\"");
            }
            // Set Electrocute as primary.
            if (myLevel == 15)
            {
                ZetaDia.Me.SetActiveSkill(SNOPower.Wizard_Electrocute, -1, 0);
                Logger.Write("Setting Electrocute as Primary");
            }
            // Set Electrocute-Chain lightning as primary.
            if (myLevel == 22)
            {
                ZetaDia.Me.SetActiveSkill(SNOPower.Wizard_Electrocute, 1, 0);
                Logger.Write("Changing rune for Electrocute: \"Chain Lightning\"");
            }

            // ********** SECONDARY SLOT CHANGES **********
            // Set Ray of Frost as secondary spell.
            if (myLevel == 2)
            {
                ZetaDia.Me.SetActiveSkill(SNOPower.Wizard_RayOfFrost, -1, 1);
                Logger.Write("Setting Ray of Frost as Secondary");
            }
            // Set arcane orb as secondary
            if (myLevel == 5)
            {
                ZetaDia.Me.SetActiveSkill(SNOPower.Wizard_ArcaneOrb, -1, 1);
                Logger.Write("Setting Arcane Orb as Secondary");
            }
            // Set arcane orb rune to "obliteration"
            if (myLevel == 11)
            {
                ZetaDia.Me.SetActiveSkill(SNOPower.Wizard_ArcaneOrb, 1, 1);
                Logger.Write("Changing rune for Arcane Orb: \"Obliteration\"");
            }
            // ********** SKILL SLOTS 1-4 **********
            // Set Frost Nova as slot 1
            if (myLevel == 4)
            {
                ZetaDia.Me.SetActiveSkill(SNOPower.Wizard_FrostNova, -1, 2);
                Logger.Write("Setting Frost Nova as slot 1");
            }
            // Set Diamond Skin as slot 1
            if (myLevel == 8)
            {
                ZetaDia.Me.SetActiveSkill(SNOPower.Wizard_DiamondSkin, -1, 2);
                Logger.Write("Setting Diamond Skin as slot 1");
            }
            // Level 9, slot 2 unlocked!
            // Set Wave of Force as slot 2
            if (myLevel == 9)
            {
                ZetaDia.Me.SetActiveSkill(SNOPower.Wizard_WaveOfForce, -1, 3);
                Logger.Write("Setting Wave of Force as slot 2");
            }
            // Level 14, slot 3 unlocked!
            // Set Diamond Skin-Crystal Shell as slot 1, Ice Armor as slot 3
            if (myLevel == 14)
            {
                ZetaDia.Me.SetActiveSkill(SNOPower.Wizard_DiamondSkin, 1, 2);
                Logger.Write("Changing rune for Diamond Skin: \"Crystal Shell\"");
                ZetaDia.Me.SetActiveSkill(SNOPower.Wizard_IceArmor, -1, 4);
                Logger.Write("Setting Ice Armor as slot 3");
            }
            // Set Wave of Force-Impactful Wave as slot 2
            if (myLevel == 15)
            {
                ZetaDia.Me.SetActiveSkill(SNOPower.Wizard_WaveOfForce, 1, 3);
                Logger.Write("Changing rune for Wave of Force: \"Impactful Wave\"");
            }
            // Level 19, slot 4 unlocked!
            // Set Explosive Blast as slot 4
            if (myLevel == 19)
            {
                ZetaDia.Me.SetActiveSkill(SNOPower.Wizard_ExplosiveBlast, -1, 5);
                Logger.Write("Setting Explosive Blast as slot 4");
            }
            // Set Ice Armor-Chilling Aura as slot 3, Hydra as slot 4
            if (myLevel == 21)
            {
                ZetaDia.Me.SetActiveSkill(SNOPower.Wizard_IceArmor, 1, 4);
                Logger.Write("Changing rune for Ice Armor: \"Chilling Aura\"");
                ZetaDia.Me.SetActiveSkill(SNOPower.Wizard_Hydra, -1, 5);
                Logger.Write("Setting Hydra as slot 4");
            }
            // Set Hydra-Arcane Hydra as slot 4
            if (myLevel == 26)
            {
                ZetaDia.Me.SetActiveSkill(SNOPower.Wizard_Hydra, 1, 5);
                Logger.Write("Changing rune for Hydra: \"Arcane Hydra\"");
            }
            // Set Energy Armor as slot 3
            if (myLevel == 28)
            {
                ZetaDia.Me.SetActiveSkill(SNOPower.Wizard_EnergyArmor, -1, 4);
                Logger.Write("Setting Energy Armor as slot 3");
            }
            // Set Energy Armor-Absorption as slot 3
            if (myLevel == 32)
            {
                ZetaDia.Me.SetActiveSkill(SNOPower.Wizard_EnergyArmor, 1, 4);
                Logger.Write("Changing rune for Energy Armor: \"Absorption\"");
            }
            // Set Hydra-Venom Hydra as slot 4
            if (myLevel == 38)
            {
                ZetaDia.Me.SetActiveSkill(SNOPower.Wizard_Hydra, 3, 5);
                Logger.Write("Changing rune for Hydra: \"Venom Hydra\"");
            }
            // Set Energy Armor-Pinpoint Barrier as slot 3
            if (myLevel == 41)
            {
                ZetaDia.Me.SetActiveSkill(SNOPower.Wizard_EnergyArmor, 2, 4);
                Logger.Write("Changing rune for Energy Armor: \"Pinpoint Barrier\"");
            }

            // ********** PASSIVE SKILLS **********
            if (myLevel == 10)
            {
                // Blur - Decreases melee damage taken by 20%.
                ZetaDia.Me.SetTraits(SNOPower.Wizard_Passive_Blur);
            }
            if (myLevel == 20)
            {
                // Blur - Decreases melee damage taken by 20%.
                // Prodigy - 4 arcane power from signature casts
                ZetaDia.Me.SetTraits(SNOPower.Wizard_Passive_Blur, SNOPower.Wizard_Passive_Prodigy);
            }
            if (myLevel == 30)
            {
                // Blur - Decreases melee damage taken by 20%.
                // Prodigy - 4 arcane power from signature casts
                // Astral Presence - +20 arcane power, +2 arcane regen
                ZetaDia.Me.SetTraits(SNOPower.Wizard_Passive_Blur, SNOPower.Wizard_Passive_Prodigy,
                                     SNOPower.Wizard_Passive_AstralPresence);
            }

            //Refresh Spells
            SpellCast.UpdateHotbarPowerSNOs();
        }

        #region Timers

        private static readonly WaitTimer ExplosiveBlast = new WaitTimer(TimeSpan.FromSeconds(2));
        private static readonly WaitTimer BlizzardTimer = new WaitTimer(TimeSpan.FromSeconds(5));
        private static readonly WaitTimer MeteorTimer = new WaitTimer(TimeSpan.FromSeconds(5));
        private static readonly WaitTimer FamiliarTimer = new WaitTimer(TimeSpan.FromMinutes(5));
        private static readonly WaitTimer HydraTimer = new WaitTimer(TimeSpan.FromSeconds(10));

        static Wizard()
        {
            GameEvents.OnGameLeft += ResetTimers;
            GameEvents.OnPlayerDied += ResetTimers;
        }

        private static void ResetTimers(object sender, EventArgs e)
        {
            FamiliarTimer.Stop();
            BlizzardTimer.Stop();
            HydraTimer.Stop();
            MeteorTimer.Stop();
            ExplosiveBlast.Stop();
        }

        #endregion
    }
}