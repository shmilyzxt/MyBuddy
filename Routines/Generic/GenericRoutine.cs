using System;
using System.Linq;
using System.Windows;
using Zeta;
using Zeta.Common;
using Zeta.Common.Helpers;
using Zeta.CommonBot;
using Zeta.Internals.Actors;
using Zeta.TreeSharp;

using Action = Zeta.TreeSharp.Action;

namespace Demonbuddy.Routines.Generic
{
    public class GenericRoutine : CombatRoutine
    {
        private GenericRoutineSettings _settings;

        private GenericRoutineSettings Settings
        {
            get
            {
                if (_settings == null && ZetaDia.Service.CurrentHero.IsValid)
                    _settings = new GenericRoutineSettings();

                return _settings;
            }
        }

        #region Overrides of CombatRoutine

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public override void Dispose()
        {
            Pulsator.OnPulse -= PulsatorOnPulse;
        }

        /// <summary>
        /// Gets the name of this <see cref="CombatRoutine"/>.
        /// </summary>
        /// <remarks>Created 2012-04-03</remarks>
        public override string Name { get { return "Generic"; } }

        /// <summary>
        /// Gets the configuration window.
        /// </summary>
        /// <remarks>Created 2012-04-03</remarks>
        public override Window ConfigWindow { get { return new GenericRoutineConfigWindow(Settings); } }

        /// <summary> Gets the buff behavior. </summary>
        /// <value> The buffer. </value>
        public override Composite Buff { get { return _buff; } }

        /// <summary>
        /// Gets the class.
        /// </summary>
        /// <remarks>Created 2012-04-03</remarks>
        public override ActorClass Class { get { return ZetaDia.Me.ActorClass; } }

        public override SNOPower DestroyObjectPower
        {
            get { return ZetaDia.CPlayer.GetPowerForSlot((HotbarSlot)Settings.DestroyObjectAction); }
        }

        public override float DestroyObjectDistance { get { return Settings.DestroyObjectsDistance; } }

        /// <summary>
        /// Initializes this <see cref="CombatRoutine"/>.
        /// </summary>
        /// <remarks>Created 2012-04-03</remarks>
        public override void Initialize()
        {
            Pulsator.OnPulse += PulsatorOnPulse;
        }

        private bool _isInitialized;
        private void PulsatorOnPulse(object sender, EventArgs e)
        {
            if (!_isInitialized && ZetaDia.Service.CurrentHero.IsValid)
            {
                //_settings = new GenericRoutineSettings();
                // Reset LastUsed every time we leave/join a game.
                GameEvents.OnGameJoined += (ea, se) => Settings.Skills.ForEach(s => s.LastUsed = DateTime.MinValue);
                GameEvents.OnGameLeft += (ea, se) => Settings.Skills.ForEach(s => s.LastUsed = DateTime.MinValue);

                _combat = new PrioritySelector(
                    CreateUsePotion(),
                    CreateWaitWhileIncapacitated(),
                    CreateWaitForAttack(),

                    CreateUseSkill(ret => Settings.Skills.FirstOrDefault(s => s.Slot == ActionSlot.Slot4)),
                    CreateUseSkill(ret => Settings.Skills.FirstOrDefault(s => s.Slot == ActionSlot.Slot3)),
                    CreateUseSkill(ret => Settings.Skills.FirstOrDefault(s => s.Slot == ActionSlot.Slot2)),
                    CreateUseSkill(ret => Settings.Skills.FirstOrDefault(s => s.Slot == ActionSlot.Slot1)),
                    CreateUseSkill(ret => Settings.Skills.FirstOrDefault(s => s.Slot == ActionSlot.Secondary)),
                    CreateUseSkill(ret => Settings.Skills.FirstOrDefault(s => s.Slot == ActionSlot.Primary))
                    );

                _buff = new PrioritySelector(
                    CreateWaitWhileIncapacitated(),
                    CreateWaitForAttack(),

                    CreateUseBuff(ret => Settings.Skills.FirstOrDefault(s => s.Slot == ActionSlot.Slot4)),
                    CreateUseBuff(ret => Settings.Skills.FirstOrDefault(s => s.Slot == ActionSlot.Slot3)),
                    CreateUseBuff(ret => Settings.Skills.FirstOrDefault(s => s.Slot == ActionSlot.Slot2)),
                    CreateUseBuff(ret => Settings.Skills.FirstOrDefault(s => s.Slot == ActionSlot.Slot1)),
                    CreateUseBuff(ret => Settings.Skills.FirstOrDefault(s => s.Slot == ActionSlot.Secondary)),
                    CreateUseBuff(ret => Settings.Skills.FirstOrDefault(s => s.Slot == ActionSlot.Primary))
                    );

                _isInitialized = true;
            }
        }

        private Composite CreateUseSkill(ValueRetriever<GenericRoutineSettings.SkillSetting> setting)
        {
            return new Decorator(ret => setting != null && setting(ret) != null,
                new Decorator(ret => setting(ret).ShouldUse,
                    new Action(ret => setting(ret).Use(CombatTargeting.Instance.FirstNpc.Position, CombatTargeting.Instance.FirstNpc.ACDGuid))));
        }

        private Composite CreateUseBuff(ValueRetriever<GenericRoutineSettings.SkillSetting> setting)
        {
            return new Decorator(ret => setting != null && setting(ret) != null && setting(ret).Type == UseType.EveryNSeconds,
                new Decorator(ret => setting(ret).ShouldUse,
                    new Action(ret => setting(ret).Use(Vector3.Zero, -1))));
        }

        private Composite _combat;
        private Composite _buff;

        /// <summary>
        /// Gets the combat behavior.
        /// </summary>
        /// <remarks>Created 2012-04-03</remarks>
        public override Composite Combat { get { return _combat; } }

        #endregion

        #region Stuffs
        public  Composite CreateWaitWhileIncapacitated()
        {
            return
                new Decorator(ret =>
                    ZetaDia.Me.IsFeared ||
                    ZetaDia.Me.IsStunned ||
                    ZetaDia.Me.IsFrozen ||
                    ZetaDia.Me.IsBlind ||
                    ZetaDia.Me.IsRooted,

                    new Action(ret => RunStatus.Success)
                );
        }

        public  Composite CreateWaitForAttack()
        {
            return
                new Decorator(ret => ZetaDia.Me.CommonData.AnimationInfo != null && ZetaDia.Me.CommonData.AnimationInfo.State == AnimationState.Attacking,
                    new Action(ret => RunStatus.Success)
                );

        }

        private readonly WaitTimer _potionCooldownTimer = WaitTimer.ThirtySeconds;
        public  Composite CreateUsePotion()
        {
            return
                new Decorator(ret => ZetaDia.Me.HitpointsCurrentPct*100f <= Settings.PotionHealth && _potionCooldownTimer.IsFinished,
                    new PrioritySelector(ctx => ZetaDia.Me.Inventory.Backpack.FirstOrDefault(i => i.IsPotion),

                        new Decorator(ctx => ctx != null,
                            new Sequence(
                                new Action(ctx => ZetaDia.Me.Inventory.UseItem(((ACDItem)ctx).DynamicId)),
                                new Action(ctx => _potionCooldownTimer.Reset())
                                )
                            )
                        )
                    );
        }
        #endregion
    }
}
