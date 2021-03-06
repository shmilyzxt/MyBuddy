﻿using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using GilesTrinity.DbProvider;
using GilesTrinity.Technicals;
using Zeta;
using Zeta.Common.Plugins;
using Zeta.CommonBot;
using Zeta.Navigation;

namespace GilesTrinity
{
    /// <summary>
    /// Trinity DemonBuddy Plugin 
    /// </summary>
    public partial class GilesTrinity : IPlugin
    {
        /// <summary>
        /// Gets the version of plugin.
        /// </summary>
        /// <remarks>
        /// This is used by DemonBuddy on plugin tab 
        /// </remarks>
        /// <value>
        /// The version of plugin.
        /// </value>
        public Version Version
        {
            get
            {
                return new Version(1, 7, 2, 13);
            }
        }

        /// <summary>
        /// Gets the author of plugin.
        /// </summary>
        /// <remarks>
        /// This is used by DemonBuddy on plugin tab 
        /// </remarks>
        /// <value>
        /// The author of plugin.
        /// </value>
        public string Author
        {
            get
            {
                return "GilesSmith + rrrix + Community Devs - Mod by WAR";
            }
        }

        /// <summary>
        /// Gets the description of plugin.
        /// </summary>
        /// <remarks>
        /// This is used by DemonBuddy on plugin tab 
        /// </remarks>
        /// <value>
        /// The description of plugin.
        /// </value>
        public string Description
        {
            get
            {
                return string.Format("Trinity v{0}", Version);
            }
        }

        /// <summary>
        /// Receive Pulse event from DemonBuddy.
        /// </summary>
        public void OnPulse()
        {
            try
            {
                if (ZetaDia.Me == null)
                    return;

                if (!ZetaDia.IsInGame || !ZetaDia.Me.IsValid || ZetaDia.IsLoadingWorld)
                    return;

                // hax for sending notifications after a town run
                if (!Zeta.CommonBot.Logic.BrainBehavior.IsVendoring && !PlayerStatus.IsInTown)
                {
                    TownRun.SendEmailNotification();
                    TownRun.SendMobileNotifications();
                }

                // See if we should update the stats file
                if (DateTime.Now.Subtract(ItemStatsLastPostedReport).TotalSeconds > 10)
                {
                    ItemStatsLastPostedReport = DateTime.Now;
                    OutputReport();
                }

                // Recording of all the XML's in use this run
                UsedProfileManager.RecordProfile();

                Monk_MaintainTempestRush();
            }
            catch (Exception ex)
            {
                DbHelper.Log(LogCategory.UserInformation, "Exception in Pulse: {0}", ex.ToString());
            }
        }

        /// <summary>
        /// Called when user Enable the plugin.
        /// </summary>
        public void OnEnabled()
        {
            BotMain.OnStart += TrinityBotStart;
            BotMain.OnStop += TrinityBotStop;

            // Set up the pause button

            // rrrix: removing for next DB beta... 
            //Application.Current.Dispatcher.Invoke(PaintMainWindowButtons());

            SetWindowTitle();

            if (!Directory.Exists(FileManager.PluginPath))
            {
                DbHelper.Log(TrinityLogLevel.Normal, LogCategory.UserInformation, "Fatal Error - cannot enable plugin. Invalid path: {0}", FileManager.PluginPath);
                DbHelper.Log(TrinityLogLevel.Normal, LogCategory.UserInformation, "Please check you have installed the plugin to the correct location, and then restart DemonBuddy and re-enable the plugin.");
                DbHelper.Log(TrinityLogLevel.Normal, LogCategory.UserInformation, @"Plugin should be installed to \<DemonBuddyFolder>\Plugins\Trinity\");
            }
            else
            {
                HasMappedPlayerAbilities = false;
                IsPluginEnabled = true;

                // Settings are available after this... 
                LoadConfiguration();

                Navigator.PlayerMover = new PlayerMover();
                SetUnstuckProvider();
                GameEvents.OnPlayerDied += TrinityOnDeath;
                GameEvents.OnGameJoined += TrinityOnJoinGame;
                GameEvents.OnGameLeft += TrinityOnLeaveGame;

                GameEvents.OnItemSold += TrinityOnItemSold;
                GameEvents.OnItemSalvaged += TrinityOnItemSalvaged;
                GameEvents.OnItemStashed += TrinityOnItemStashed;

                GameEvents.OnGameChanged += GameEvents_OnGameChanged;

                if (NavProvider == null)
                    NavProvider = new DefaultNavigationProvider();

                // enable or disable process exit events
                //ZetaDia.Memory.Process.EnableRaisingEvents = false;



                CombatTargeting.Instance.Provider = new BlankCombatProvider();
                LootTargeting.Instance.Provider = new BlankLootProvider();
                ObstacleTargeting.Instance.Provider = new BlankObstacleProvider();

                if (Settings.Loot.ItemFilterMode != global::GilesTrinity.Settings.Loot.ItemFilterMode.DemonBuddy)
                {
                    ItemManager.Current = new TrinityItemManager();
                }
                NavHelper.UpdateSearchGridProvider();

                // Safety check incase DB "OnStart" event didn't fire properly
                if (BotMain.IsRunning)
                {
                    TrinityBotStart(null);
                    if (ZetaDia.IsInGame)
                        TrinityOnJoinGame(null, null);
                }

                SetBotTPS();

                TrinityPowerManager.LoadLegacyDelays();

                DbHelper.Log(TrinityLogLevel.Normal, LogCategory.UserInformation, "*******************TRINITY*****************", Description); ;
				DbHelper.Log(TrinityLogLevel.Normal, LogCategory.UserInformation, "ATIVADO: {0} carregado - mod by WAR!", Description); ;
				DbHelper.Log(TrinityLogLevel.Normal, LogCategory.UserInformation, "*******************TRINITY*****************", Description); ;
            }

            if (StashRule != null)
            {
                // reseting stash rules
                StashRule.reset();
            }
        }

        void GameEvents_OnGameChanged(object sender, EventArgs e)
        {
            ClearCachesOnGameChange(sender, e);

            // reload the profile juuuuuuuuuuuust in case Demonbuddy missed it... which it is known to do on disconnects
            string currentProfilePath = ProfileManager.CurrentProfile.Path;
            ProfileManager.Load(currentProfilePath);
            ResetEverythingNewGame();
        }

        internal static void SetBotTPS()
        {
            // Carguy's ticks-per-second feature
            if (Settings.Advanced.TPSEnabled)
            {
                BotMain.TicksPerSecond = (int)Settings.Advanced.TPSLimit;
                DbHelper.Log(TrinityLogLevel.Verbose, LogCategory.UserInformation, "Bot TPS set to {0}", (int)Settings.Advanced.TPSLimit);
            }
            else
            {
                BotMain.TicksPerSecond = 10;
                DbHelper.Log(TrinityLogLevel.Verbose, LogCategory.UserInformation, "Reset bot TPS to default", (int)Settings.Advanced.TPSLimit);
            }
        }

        internal static void SetItemManagerProvider()
        {
            if (Settings.Loot.ItemFilterMode != global::GilesTrinity.Settings.Loot.ItemFilterMode.DemonBuddy)
            {
                ItemManager.Current = new TrinityItemManager();
            }
            else
            {
                ItemManager.Current = new LootRuleItemManager();
            }
        }

        internal static void SetUnstuckProvider()
        {
            if (Settings.Advanced.UnstuckerEnabled)
            {
                Navigator.StuckHandler = new StuckHandler();
                DbHelper.Log(TrinityLogLevel.Verbose, LogCategory.UserInformation, "Using Trinity Unstucker", true);
            }
            else
            {
                Navigator.StuckHandler = new Zeta.Navigation.DefaultStuckHandler();
                DbHelper.Log(TrinityLogLevel.Verbose, LogCategory.UserInformation, "Using Default Demonbuddy Unstucker", true);
            }
        }

        internal static void SetWindowTitle(string profileName = "")
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                string battleTagName = "";
                try
                {
                    battleTagName = ZetaDia.Service.CurrentHero.BattleTagName;
                }
                catch { }
                Window mainWindow = Application.Current.MainWindow;

                string windowTitle = "DB - " + battleTagName + " - PID:" + System.Diagnostics.Process.GetCurrentProcess().Id.ToString();

                if (profileName.Trim() != String.Empty)
                {
                    windowTitle += " - " + profileName;
                }

                mainWindow.Title = windowTitle;
            }));
        }

        /// <summary>
        /// Adds the Pause and Town Run buttons to Demonbuddy's main window. Sets Window Title.
        /// </summary>
        /// <param name="battleTagName"></param>
        /// <returns></returns>
        [Obsolete("This has been removed in the latest DemonbuddyBETA")]
        private static Action PaintMainWindowButtons()
        {
            return new System.Action(
                        () =>
                        {
                            Window mainWindow = Application.Current.MainWindow;
                            var tab = mainWindow.FindName("tabControlMain") as TabControl;
                            if (tab == null) return;
                            var infoDumpTab = tab.Items[0] as TabItem;
                            if (infoDumpTab == null) return;
                            btnPauseBot = new Button
                            {
                                Width = 100,
                                HorizontalAlignment = HorizontalAlignment.Left,
                                VerticalAlignment = VerticalAlignment.Top,
                                Margin = new Thickness(232, 6, 0, 0),
                                Content = "Pause Bot"
                            };
                            btnPauseBot.Click += buttonPause_Click;
                            btnTownRun = new Button
                            {
                                Width = 100,
                                HorizontalAlignment = HorizontalAlignment.Left,
                                VerticalAlignment = VerticalAlignment.Top,
                                Margin = new Thickness(232, 32, 0, 0),
                                Content = "Force Town Run"
                            };
                            btnTownRun.Click += buttonTownRun_Click;
                            var grid = infoDumpTab.Content as Grid;
                            if (grid == null) return;
                            grid.Children.Add(btnPauseBot);
                            grid.Children.Add(btnTownRun);
                        });
        }

        /// <summary>
        /// Called when user disable the plugin.
        /// </summary>
        public void OnDisabled()
        {
            IsPluginEnabled = false;
            Navigator.PlayerMover = new DefaultPlayerMover();
            Navigator.StuckHandler = new DefaultStuckHandler();
            CombatTargeting.Instance.Provider = new DefaultCombatTargetingProvider();
            LootTargeting.Instance.Provider = new DefaultLootTargetingProvider();
            ObstacleTargeting.Instance.Provider = new DefaultObstacleTargetingProvider();

            GameEvents.OnPlayerDied -= TrinityOnDeath;
            BotMain.OnStop -= TrinityBotStop;
            BotMain.OnStop -= PluginCheck;
            GameEvents.OnGameJoined -= TrinityOnJoinGame;
            GameEvents.OnGameLeft -= TrinityOnLeaveGame;
            DbHelper.Log(TrinityLogLevel.Normal, LogCategory.UserInformation, "*******************TRINITY*****************");
            DbHelper.Log(TrinityLogLevel.Normal, LogCategory.UserInformation, "DESATIVADO: Trinity foi desligado...");
            DbHelper.Log(TrinityLogLevel.Normal, LogCategory.UserInformation, "*******************TRINITY*****************");
            GenericCache.Shutdown();
            GenericBlacklist.Shutdown();
        }

        /// <summary>
        /// Called when DemonBuddy shut down.
        /// </summary>
        public void OnShutdown()
        {
            GenericCache.Shutdown();
            GenericBlacklist.Shutdown();
        }

        /// <summary>
        /// Called when DemonBuddy initialize the plugin.
        /// </summary>
        public void OnInitialize()
        {
            Zeta.CommonBot.BotMain.OnStart += PluginCheck;
        }

        void PluginCheck(IBot bot)
        {
            if (!IsPluginEnabled && bot != null)
            {
                DbHelper.Log(TrinityLogLevel.Error, LogCategory.UserInformation, "\tWARNING: Trinity Plugin is NOT YET ENABLED. Bot start detected");
                DbHelper.Log(TrinityLogLevel.Error, LogCategory.UserInformation, "\tIgnore this message if you are not currently using Trinity.");
                return;
            }

            if (BotMain.IsRunning && 
                (!Zeta.CommonBot.RoutineManager.Current.Name.ToLower().Contains("trinity") || !Zeta.CommonBot.RoutineManager.Current.Name.ToLower().Contains("gilesplugin")))
            {
                DbHelper.Log(TrinityLogLevel.Error, LogCategory.UserInformation, "Found Routine: {0}", Zeta.CommonBot.RoutineManager.Current.Name);
                DbHelper.Log(TrinityLogLevel.Error, LogCategory.UserInformation, "######################################");
                DbHelper.Log(TrinityLogLevel.Error, LogCategory.UserInformation, "ERROR: You are not using the Trinity Combat Routine!");
                DbHelper.Log(TrinityLogLevel.Error, LogCategory.UserInformation, "You MUST download and select the Trinity Combat Routine");
                DbHelper.Log(TrinityLogLevel.Error, LogCategory.UserInformation, "http://www.thebuddyforum.com/demonbuddy-forum/plugins/trinity/93720-trinity-download-here.html");
                DbHelper.Log(TrinityLogLevel.Error, LogCategory.UserInformation, "");
                DbHelper.Log(TrinityLogLevel.Error, LogCategory.UserInformation, "Trinity will NOT work with any other combat routine");
                DbHelper.Log(TrinityLogLevel.Error, LogCategory.UserInformation, "######################################");
                BotMain.Stop();
            }

        }



        /// <summary>
        /// Gets the displayed name of plugin.
        /// </summary>
        /// <remarks>
        /// This is used by DemonBuddy on plugin tab 
        /// </remarks>
        /// <value>The name.</value>
        public string Name
        {
            get
            {
                return "[WAR] Trinity";
            }
        }

        /// <summary>
        /// Check if this instance of plugin is equals to the specified other.
        /// </summary>
        /// <param name="other">The other plugin to compare.</param>
        /// <returns>
        /// <c>true</c> if this instance is equals to the specified other; otherwise <c>false</c>
        /// </returns>
        public bool Equals(IPlugin other)
        {
            return (other.Name == Name) && (other.Version == Version);
        }

        private static GilesTrinity _instance;
        public static GilesTrinity Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GilesTrinity();
                }
                return _instance;
            }
        }

        public GilesTrinity()
        {
            _instance = this;
        }
    }
}
