using System;
using System.Diagnostics;
using System.Windows;
using Belphegor.Composites;
using Belphegor.Dynamics;
using Belphegor.GUI;
using Belphegor.Helpers;
using Belphegor.Routines;
using Belphegor.Settings;
using Belphegor.Utilities;
using Zeta;
using Zeta.Common.Helpers;
using Zeta.CommonBot;
using Zeta.Internals.Actors;
using Zeta.Navigation;
using Zeta.TreeSharp;
using Action = Zeta.TreeSharp.Action;

namespace Belphegor
{
    public class Belphegor : CombatRoutine
    {
        public Belphegor()
        {
            Instance = this;
        }

        public static Belphegor Instance { get; private set; }

        #region Overrides of CombatRoutine

        private readonly Stopwatch _stopwatch = new Stopwatch();
        private Composite _buff;
        private Composite _combat;
        private Window _configWindow;
        private ActorClass _lastClass = ActorClass.Invalid;
        private Composite _movement;

        /// <summary>
        /// Gets the name of this <see cref="CombatRoutine"/>.
        /// </summary>
        /// <remarks>Created 2012-04-03</remarks>
        public override string Name
        {
            get { return "Belphegor All-in-One"; }
        }

        /// <summary>
        /// Gets the configuration window.
        /// </summary>
        /// <remarks>Created 2012-04-03</remarks>
        public override Window ConfigWindow
        {
            get
            {
                if (_configWindow == null)
                {
                    _configWindow = new ConfigWindow("Configuration", Name,
                                                     "The All-in-One CC version " + Logger.Version, 350, 400,
                                                     BelphegorSettings.Instance);
                    _configWindow.Closed += ConfigWindowClosed;
                }
                return _configWindow;
            }
        }

        /// <summary>
        /// Gets the class.
        /// </summary>
        /// <remarks>Created 2012-04-03</remarks>
        public override ActorClass Class
        {
            get
            {
                if (!ZetaDia.IsInGame || ZetaDia.IsLoadingWorld)
                {
                    // Return none if we are oog to make sure we can start the bot anytime.
                    return ActorClass.Invalid;
                }

                return ZetaDia.Me.ActorClass;
            }
        }

        public override float DestroyObjectDistance
        {
            get { return BelphegorSettings.Instance.DestroyObjectDistance; }
        }

        private CachedValue<SNOPower> _destroyPowerCache;
        /// <summary> Gets the destroy object power. </summary>
        /// <value> The destroy object power. </value>
        public override SNOPower DestroyObjectPower
        {
            get
            {
                if (_destroyPowerCache == null)
                {
                    _destroyPowerCache = new CachedValue<SNOPower>(
                        () => ZetaDia.CPlayer.GetPowerForSlot(HotbarSlot.HotbarMouseLeft),
                        TimeSpan.FromSeconds(10)
                        );
                }
                
                return _destroyPowerCache.Value;
            }
        }

        /// <summary>
        /// Gets me.
        /// </summary>
        /// <remarks>Created 2012-05-08</remarks>
        public DiaActivePlayer Me
        {
            get { return ZetaDia.Me; }
        }

        /// <summary>
        /// Gets the combat behavior.
        /// </summary>
        /// <remarks>Created 2012-04-03</remarks>
        public override Composite Combat
        {
            get { return _combat; }
        }

        public override Composite Buff
        {
            get { return _buff; }
        }

        public Composite Movement
        {
            get { return _movement; }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public override void Dispose()
        {
            if(_configWindow != null)
                _configWindow.Close();
            Pulsator.OnPulse -= SetBehaviorPulse;
        }

        private void ConfigWindowClosed(object sender, EventArgs e)
        {
            BelphegorSettings.Instance.Save();
            _configWindow.Closed -= ConfigWindowClosed;
            _configWindow = null;
        }

        /// <summary>
        /// Initializes this <see cref="CombatRoutine"/>.
        /// </summary>
        /// <remarks>Created 2012-04-03</remarks>
        public override void Initialize()
        {
            GameEvents.OnLevelUp += Monk.MonkOnLevelUp;
            GameEvents.OnLevelUp += Wizard.WizardOnLevelUp;
            GameEvents.OnLevelUp += WitchDoctor.WitchDoctorOnLevelUp;
            GameEvents.OnLevelUp += DemonHunter.DemonHunterOnLevelUp;
            GameEvents.OnLevelUp += Barbarian.BarbarianOnLevelUp;
            GameEvents.OnWorldTransferStart += HandleWorldTransfer;

            if (!CreateBehaviors())
            {
                BotMain.Stop();
                return;
            }

            _lastClass = Class;
            Pulsator.OnPulse += SetBehaviorPulse;
            Navigator.PlayerMover = new BelphegorPlayerMover();

            //Navigator.SearchGridProvider = BelphegorCachedSearchAreaProvider.Instance;
            //CombatTargeting.Instance.Provider = BelphegorCombatTargetingProvider.Instance; No longer needed cause the internal one is doing almost the same stuff.

            Logger.Write("Behaviors created");
        }

        private static WaitTimer _worldTransferTimeoutTimer = WaitTimer.FiveSeconds;
        private static void HandleWorldTransfer(object sender, EventArgs e)
        {
            _worldTransferTimeoutTimer.Reset();
        }

        public void SetBehaviorPulse(object sender, EventArgs args)
        {
            if (!_worldTransferTimeoutTimer.IsFinished)
                return;

            if (ZetaDia.IsInGame && !ZetaDia.IsLoadingWorld && ZetaDia.Me != null && ZetaDia.Me.CommonData != null)
            {
                if (_combat == null || ZetaDia.Me.IsValid && Class != _lastClass)
                {
                    if (!CreateBehaviors())
                    {
                        BotMain.Stop();
                        return;
                    }

                    Logger.Write("Behaviors created");
                    _lastClass = Class;
                }
            }
            Avoidance.IsAvoidanceCacheResetRequired = true;
        }

        public bool CreateBehaviors()
        {
            int count;

            _combat = CompositeBuilder.GetComposite(Class, BehaviorType.Combat, out count);
            if (count == 0 || _combat == null)
            {
                Logger.Write("Combat support for {0} is not currently implemented.", Class);
                return false;
            }
            _combat = new Sequence(
                new Action(ctx =>
                               {
                                   if (BelphegorSettings.Instance.Debug.IsDebugTreeExecutionLoggingActive)
                                   {
                                       _stopwatch.Restart();
                                   }
                               }),
                _combat,
                new Action(ctx =>
                               {
                                   if (BelphegorSettings.Instance.Debug.IsDebugTreeExecutionLoggingActive)
                                   {
                                       _stopwatch.Stop();
                                       Logger.WriteVerbose("Tree execution took {0}ms.", _stopwatch.ElapsedMilliseconds);
                                   }
                               })
                );

            _buff = CompositeBuilder.GetComposite(Class, BehaviorType.Buff, out count);
            if (count == 0 || _buff == null)
            {
                Logger.Write("Buff support for {0} is not currently implemented.", Class);
            }

            _movement = CompositeBuilder.GetComposite(Class, BehaviorType.Movement, out count);
            if (count == 0 || _movement == null)
            {
                Logger.Write("Movement support for {0} is not currently implemented.", Class);
            }

            SpellCast.UpdateHotbarPowerSNOs();

            return true;
        }

        #endregion
    }
}