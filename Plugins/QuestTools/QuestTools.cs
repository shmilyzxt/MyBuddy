using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Zeta;
using Zeta.Common;
using Zeta.Common.Plugins;
using Zeta.CommonBot;
using Zeta.CommonBot.Dungeons;
using Zeta.CommonBot.Logic;
using Zeta.CommonBot.Profile;
using Zeta.Internals.Actors;
using UIElement = Zeta.Internals.UIElement;

namespace QuestTools
{

    public partial class QuestTools : IPlugin
    {
        public Version Version
        {
            get
            {
                return new Version(1, 3, 6);
            }
        }
        static int worldId = 0;
        static int levelAreaId = 0;
        static int questId = 0;
        static int questStepId = 0;
        // static int sceneId = 0;
        static Zeta.Act currentAct = Zeta.Act.Invalid;
        static string myName = "[QuestTools] ";
        static Stopwatch timer = new Stopwatch();
        private static Button btnDumpActors, btnOpenLogFile, btnResetGrid;

        private static bool reloadProfileOnDeath = true;
        public static bool ReloadProfileOnDeath { get { return reloadProfileOnDeath; } set { reloadProfileOnDeath = value; } }

        private static bool enableDebugLogging = false;
        public static bool EnableDebugLogging { get { return enableDebugLogging; } set { enableDebugLogging = value; } }

        static bool forceReloadProfile = false;
        static bool somethingChanged = false;

        public static DateTime lastProfileReload = DateTime.MinValue;

        private ProfileBehavior currentBehavior = null;
        private DateTime lastBehaviorChange = DateTime.Now;
        private DateTime LastBotStart = DateTime.MinValue;

        private static TimeSpan behaviorTimeout = new TimeSpan(0, 5, 0, 0);

        private static Dictionary<string, TimeSpan> behaviorTimeouts =
            new Dictionary<string, TimeSpan>
            {
                { "ExploreAreaTag", new TimeSpan(0, 15, 0, 0) },
                { "default", new TimeSpan(0,  3, 0, 0) },
            };

        private Stopwatch skipEventTimer = new Stopwatch();
        private int skipEventDuration = -1;
        private DateTime lastEvent = DateTime.Now;

        /// <summary>
        /// Starts the Random event timer
        /// </summary>
        /// <param name="min">Random time minimum</param>
        /// <param name="max">Random time maximum</param>
        /// <returns>If the timer was started</returns>
        private bool SetStartEventTimer(int min = 900, int max = 2200)
        {
            if (skipEventTimer.IsRunning)
                return false;

            skipEventDuration = new Random().Next(min, max);
            skipEventTimer.Start();
            return true;
        }

        /// <summary>
        /// Resets the Event Timer
        /// </summary>
        /// <returns>If timer was succesfully reset</returns>
        private bool StopEventTimer()
        {
            if (skipEventTimer.IsRunning)
            {
                skipEventTimer.Reset();
                skipEventDuration = -1;
                return true;
            }
            return false;
        }

        internal static HashSet<Vector3> PositionCache = new HashSet<Vector3>();
        internal static Vector3 PositionCacheLastPosition = Vector3.Zero;
        internal static DateTime PositionCacheLastRecorded = DateTime.MinValue;
        internal static bool ForceGeneratePath = false;

        internal static DateTime LastPluginPulse = DateTime.MinValue;

        internal static ulong LastGameId = 0;

        public static double GetMillisecondsSincePulse()
        {
            return DateTime.Now.Subtract(LastPluginPulse).TotalMilliseconds;
        }

        public void OnPulse()
        {
            if (!timer.IsRunning)
            {
                timer.Start();
                return;
            }

            if (timer.ElapsedMilliseconds < 100)
                return;

            using (ZetaDia.Memory.AcquireFrame())
            {
                if (GetMillisecondsSincePulse() > 500)
                {
                    ForceGeneratePath = true;
                }

                LastPluginPulse = DateTime.Now;

                if (ZetaDia.Me == null || !ZetaDia.Me.IsValid || !ZetaDia.Service.IsValid || !ZetaDia.IsInGame || ZetaDia.IsLoadingWorld || ZetaDia.Me.IsDead || !ZetaDia.WorldInfo.IsValid || !ZetaDia.ActInfo.IsValid)
                {
                    return;
                }

                timer.Reset();

                ProfileBehaviorTimeout();

                if (ZetaDia.Me.IsDead && ReloadProfileOnDeath)
                {
                    forceReloadProfile = true;
                }

                if (DateTime.Now.Subtract(PositionCacheLastRecorded).TotalMilliseconds > 100 && PositionCacheLastPosition.Distance2D(ZetaDia.Me.Position) > 5f)
                {
                    PositionCache.Add(ZetaDia.Me.Position);
                }

                if (Zeta.CommonBot.CombatTargeting.Instance.FirstNpc != null || Zeta.CommonBot.CombatTargeting.Instance.FirstObject != null)
                {
                    ForceGeneratePath = true;
                }

                ulong thisGameId = ZetaDia.Service.CurrentGameId.FactoryId;
                bool gameIdMatch = thisGameId == LastGameId;
                if (!gameIdMatch)
                {
                    LastGameId = thisGameId;
                }

                if (DateTime.Now.Subtract(LastBotStart).TotalSeconds > 30 && Zeta.CommonBot.GameStats.Instance.GamesPerHour > 20 && !gameIdMatch)
                {
                    Zeta.CommonBot.BotMain.Stop(false, String.Format("[QuestTools] Forcing bot stop - high rate of games/hour detected: {0} Games/hour", Zeta.CommonBot.GameStats.Instance.GamesPerHour));
                }

                if (forceReloadProfile)
                {
                    Zeta.CommonBot.ProfileManager.Load(Zeta.CommonBot.ProfileManager.CurrentProfile.Path);
                    Logging.WriteDiagnostic("[QuestTools] Reloading profile {0} - {1}", Zeta.CommonBot.ProfileManager.CurrentProfile.Name, Zeta.CommonBot.ProfileManager.CurrentProfile.Path);
                    forceReloadProfile = false;
                }

                if (ZetaDia.IsPlayingCutscene)
                {
                    if (!skipEventTimer.IsRunning)
                    {
                        SetStartEventTimer(900, 2200);
                        lastEvent = DateTime.Now;
                        Logging.WriteDiagnostic("[QuestTools] Waiting {0:0}ms to skip Cutscene", skipEventDuration);
                    }
                    else if (skipEventTimer.ElapsedMilliseconds > skipEventDuration)
                    {
                        Logging.WriteDiagnostic("[QuestTools] Skipping Cutscene");
                        ZetaDia.Me.SkipCutscene();
                        StopEventTimer();
                    }

                }
                if (ZetaDia.Me.IsInConversation)
                {
                    if (skipEventTimer.IsRunning)
                    {
                        SetStartEventTimer(500, 1100);
                        lastEvent = DateTime.Now;
                        Logging.WriteDiagnostic("[QuestTools] Waiting {0:0}ms before Advancing conversation");
                    }
                    else if (skipEventTimer.ElapsedMilliseconds > skipEventDuration)
                    {
                        Logging.WriteDiagnostic("[QuestTools] Advancing Conversation");
                        ZetaDia.Me.AdvanceConversation();
                        StopEventTimer();
                    }
                }

                GameUI.SafeClickUIButtons();

                //CheckForChanges();
            }
        }

        /// <summary>
        /// Checks to make sure the current profile behavior hasn't exceeded the allocated timeout
        /// </summary>
        private void ProfileBehaviorTimeout()
        {
            if (currentBehavior == null)
            {
                currentBehavior = ProfileManager.CurrentProfileBehavior;
                lastBehaviorChange = DateTime.Now;
            }
            else if (DateTime.Now.Subtract(lastBehaviorChange) > behaviorTimeout && currentBehavior != ProfileManager.CurrentProfileBehavior)
            {
                Logging.Write("[QuestTools] Behavior Timeout: {0} exceeded for Profile: {1} Behavior: {2}",
                    behaviorTimeout,
                    ProfileManager.CurrentProfile.Name,
                    currentBehavior
                    );

                currentBehavior = null;
                lastBehaviorChange = DateTime.Now;
                ProfileManager.Load(Zeta.CommonBot.ProfileManager.CurrentProfile.Path);
            }
            else
            {
                currentBehavior = Zeta.CommonBot.ProfileManager.CurrentProfileBehavior;
                lastBehaviorChange = DateTime.Now;
            }
        }

        private bool TimeoutExceededForCurrentBehavior()
        {
            if (ProfileManager.CurrentProfileBehavior != currentBehavior)
                return false;

            Type T = ProfileManager.CurrentProfileBehavior.GetType();

            switch (T.ToString())
            {
                case "ExploreAreaTag":
                    return DateTime.Now.Subtract(lastBehaviorChange) > behaviorTimeouts[T.ToString()];
                default:
                    return DateTime.Now.Subtract(lastBehaviorChange) > behaviorTimeouts["default"];
            }
        }

        private void CheckForChanges()
        {
            //if (ZetaDia.Me.SceneId != sceneId)
            //{
            //    Logging.WriteDiagnostic(String.Format("{0} Scene changed from {1} to {2}", myName, sceneId, ZetaDia.Me.SceneId));
            //    sceneId = ZetaDia.Me.SceneId;
            //    somethingChanged = true;
            //}

            if (!ZetaDia.IsInGame)
                return;

            if (!ZetaDia.Me.IsValid)
                return;

            if (ZetaDia.IsLoadingWorld)
                return;

            if (ZetaDia.ActInfo.IsValid && ZetaDia.CurrentAct != currentAct)
            {
                Logging.WriteVerbose(String.Format("{0} Act changed from {1} to {2} ({3}) SnoId={4}", myName, currentAct.ToString(), ZetaDia.CurrentAct.ToString(), (int)ZetaDia.CurrentAct, ZetaDia.CurrentActSNOId));
                currentAct = ZetaDia.CurrentAct;
                somethingChanged = true;
                PositionCache.Clear();
            }

            if (ZetaDia.WorldInfo.IsValid && ZetaDia.CurrentWorldId != worldId)
            {
                Logging.WriteVerbose(String.Format("{0} worldId changed from {1} to {2}", myName, worldId, ZetaDia.CurrentWorldId));
                worldId = ZetaDia.CurrentWorldId;
                somethingChanged = true;
                PositionCache.Clear();
            }

            if (ZetaDia.WorldInfo.IsValid && ZetaDia.CurrentLevelAreaId != levelAreaId)
            {
                Logging.WriteVerbose(String.Format("{0} levelAreaId changed from {1} to {2}", myName, levelAreaId, ZetaDia.CurrentLevelAreaId));
                levelAreaId = ZetaDia.CurrentLevelAreaId;
                somethingChanged = true;
            }

            if (ZetaDia.CurrentQuest.IsValid && ZetaDia.CurrentQuest.QuestSNO != questId)
            {
                Logging.WriteVerbose(String.Format("{0} questId changed from {1} to {2}", myName, questId, ZetaDia.CurrentQuest.QuestSNO));
                questId = ZetaDia.CurrentQuest.QuestSNO;
                somethingChanged = true;
            }

            if (ZetaDia.CurrentQuest.IsValid && ZetaDia.CurrentQuest.StepId != questStepId)
            {
                Logging.WriteVerbose(String.Format("{0} questStepId changed from {1} to {2}", myName, questStepId, ZetaDia.CurrentQuest.StepId));
                questStepId = ZetaDia.CurrentQuest.StepId;
                somethingChanged = true;
            }

            if (somethingChanged && ZetaDia.IsInGame && !ZetaDia.IsLoadingWorld && ZetaDia.Me.Position != Vector3.Zero)
            {
                Logging.WriteVerbose(String.Format("{0} Change(s) occured at Position {1} ", myName, GetProfileCoordinates(ZetaDia.Me.Position)));
                somethingChanged = false;
            }
        }
        public static string GetProfileCoordinates(Vector3 position)
        {
            return String.Format("x=\"{0:0}\" y=\"{1:0}\" z=\"{2:0}\"", position.X, position.Y, position.Z);
        }
        public void OnEnabled()
        {
            Logging.Write("*******************QUESTTOOLS*****************", Version);
			Logging.Write("[[WAR] QuestTools] Plugin v{0} Ativado - mod by WAR!", Version);
			Logging.Write("*******************QUESTTOOLS*****************", Version);

            Zeta.CommonBot.GameEvents.OnPlayerDied += new EventHandler<EventArgs>(GameEvents_OnPlayerDied);

            BotMain.OnStart += BotMain_OnStart;

            Application.Current.Dispatcher.Invoke(
                new System.Action(
                    () =>
                    {
                        Window mainWindow = Application.Current.MainWindow;
                        var tab = mainWindow.FindName("tabControlMain") as TabControl;
                        if (tab == null) return;
                        var infoDumpTab = tab.Items[1] as TabItem;
                        if (infoDumpTab == null) return;
                        var grid = infoDumpTab.Content as Grid;
                        if (grid == null) return;

                        btnDumpActors = new Button
                        {
                            Width = 120,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Top,
                            Margin = new Thickness(432, 100, 0, 0),
                            Content = "Dump Actor Attribs"
                        };

                        btnOpenLogFile = new Button
                        {
                            Width = 120,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Top,
                            Margin = new Thickness(432, 125, 0, 0),
                            Content = "Open Log File"
                        };

                        btnResetGrid = new Button
                        {
                            Width = 120,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Top,
                            Margin = new Thickness(552, 125, 0, 0),
                            Content = "Force Reset Grid"
                        };

                        btnDumpActors.Click += new RoutedEventHandler(btnDumpActors_Click);
                        grid.Children.Add(btnDumpActors);
                        btnOpenLogFile.Click += new RoutedEventHandler(btnOpenLogFile_Click);
                        grid.Children.Add(btnOpenLogFile);
                        btnResetGrid.Click += new RoutedEventHandler(btnResetGrid_Click);
                        grid.Children.Add(btnResetGrid);
                    }
                )
            );


        }

        void BotMain_OnStart(IBot bot)
        {
            LastBotStart = DateTime.Now;
            QuestTools.PositionCache.Clear();
            ReloadProfile._lastReloadLoopQuestStep = "";
            ReloadProfile._questStepReloadLoops = 0;
        }

        void btnResetGrid_Click(object sender, RoutedEventArgs e)
        {
            if (BotMain.IsRunning)
            {
                BotMain.Stop();
            }
            if (!ZetaDia.IsInGame || !ZetaDia.Me.IsValid)
                return;

            System.Threading.Thread.Sleep(500);

            GridSegmentation.Reset();
            GridSegmentation.Update();
            BrainBehavior.DungeonExplorer.Reset();
            BrainBehavior.DungeonExplorer.GetBestRoute();

        }

        void btnOpenLogFile_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(Logging.LogFilePath);
        }
        public void OnDisabled()
        {
            
			Logging.Write("*******************QUESTTOOLS*****************", Version);
			Logging.Write("[[WAR] QuestTools] Plugin v{0} Desativado - mod by WAR!", Version);
			Logging.Write("*******************QUESTTOOLS*****************", Version);
			
			currentAct = Act.Invalid;
            levelAreaId = 0;
            questId = 0;
            questStepId = 0;
            worldId = 0;
            somethingChanged = true;
            Zeta.CommonBot.GameEvents.OnPlayerDied -= GameEvents_OnPlayerDied;
            BotMain.OnStart -= BotMain_OnStart;

            Application.Current.Dispatcher.Invoke(
                new System.Action(
                    () =>
                    {
                        Window mainWindow = Application.Current.MainWindow;
                        var tab = mainWindow.FindName("tabControlMain") as TabControl;
                        if (tab == null) return;
                        var mainTab = tab.Items[1] as TabItem;
                        if (mainTab == null) return;
                        var grid = mainTab.Content as Grid;
                        if (grid == null) return;
                        grid.Children.Remove(btnDumpActors);
                        grid.Children.Remove(btnOpenLogFile);
                        grid.Children.Remove(btnResetGrid);
                    }
                )
            );
        }
        void btnDumpActors_Click(object sender, RoutedEventArgs e)
        {
            double iType = -1;

            ZetaDia.Actors.Update();
            var units = ZetaDia.Actors.GetActorsOfType<DiaUnit>(false, false)
                .Where(o => o.IsValid)
                .OrderBy(o => o.Distance);

            iType = DumpUnits(units, iType);


            //ZetaDia.Actors.Update();
            var objects = ZetaDia.Actors.GetActorsOfType<DiaObject>(false, false)
                .Where(o => o.IsValid)
                .OrderBy(o => o.Distance);

            iType = DumpObjects(objects, iType);

            //ZetaDia.Actors.Update();
            var gizmos = ZetaDia.Actors.GetActorsOfType<DiaGizmo>(true, false)
                .Where(o => o.IsValid)
                .OrderBy(o => o.Distance);

            iType = DumpGizmos(gizmos, iType);

            //ZetaDia.Actors.Update();
            //var players = ZetaDia.Actors.GetActorsOfType<DiaPlayer>(true, false)
            //    .Where(o => o.IsValid)
            //    .OrderBy(o => o.Distance);

            //iType = DumpPlayers(players, iType);

            //DumpPlayerProperties();

        }

        private static void DumpPlayerProperties()
        {
            Logging.Write("DiaActivePlayer properties:");

            Type type = typeof(DiaActivePlayer);
            foreach (PropertyInfo prop in type.GetProperties())
            {
                if (prop.PropertyType.IsValueType || prop.PropertyType == typeof(string))
                {
                    Logging.Write("Name: {0} Value: {1} Type: {2}", prop.Name, prop.GetValue(ZetaDia.Me, null), prop.PropertyType.Name);
                }
            }
        }

        private static double DumpUnits(IEnumerable<DiaUnit> units, double iType)
        {
            Logging.WriteDiagnostic("[QuestTools] Units found: {0}", units.Count());
            foreach (DiaUnit o in units)
            {
                if (!o.IsValid)
                    continue;

                string attributesFound = "";

                foreach (ActorAttributeType aType in Enum.GetValues(typeof(ActorAttributeType)))
                {
                    iType = GetAttribute(iType, o, aType);
                    if (iType > 0)
                    {
                        attributesFound += aType.ToString() + "=" + iType.ToString() + ", ";
                    }
                }
                try
                {
                    Logging.Write("[QuestTools] Unit ActorSNO: {0} Name: {1} Type: {2} Radius: {7:0.00} Position: {3} ({4}) Animation: {5} has Attributes: {6}\n",
                                        o.ActorSNO, o.Name, o.ActorInfo.GizmoType, getProfilePosition(o.Position), getSimplePosition(o.Position), o.CommonData.CurrentAnimation, attributesFound, o.CollisionSphere.Radius);
                }
                catch { }

            }
            return iType;
        }
        private static double DumpObjects(IEnumerable<DiaObject> objects, double iType)
        {
            Logging.Write("[QuestTools] Objects found: {0}", objects.Count());
            foreach (DiaObject o in objects)
            {
                if (!o.IsValid)
                    continue;

                string attributesFound = "";

                foreach (ActorAttributeType aType in Enum.GetValues(typeof(ActorAttributeType)))
                {
                    try
                    {
                        iType = GetAttribute(iType, o, aType);
                    }
                    catch { }

                    if (iType > 0)
                    {
                        attributesFound += aType.ToString() + "=" + iType.ToString() + ", ";
                    }
                }

                try
                {
                    Logging.Write("[QuestTools] Object ActorSNO: {0} Name: {1} Type: {2} Radius: {7:0.00} Position: {3} ({4}) Animation: {5} has Attributes: {6}\n",
                        o.ActorSNO, o.Name, o.ActorInfo.GizmoType, getProfilePosition(o.Position), getSimplePosition(o.Position), o.CommonData.CurrentAnimation, attributesFound, o.CollisionSphere.Radius);
                }
                catch { }
            }
            return iType;
        }
        private static double DumpGizmos(IEnumerable<DiaGizmo> gizmos, double iType)
        {
            Logging.Write("[QuestTools] Gizmos found: {0}", gizmos.Count());
            foreach (DiaGizmo o in gizmos)
            {
                if (!o.IsValid)
                    continue;

                string attributesFound = "";

                foreach (ActorAttributeType aType in Enum.GetValues(typeof(ActorAttributeType)))
                {
                    iType = GetAttribute(iType, o, aType);
                    if (iType > 0)
                    {
                        attributesFound += aType.ToString() + "=" + iType.ToString() + ", ";
                    }
                }

                if (o.ActorInfo.GizmoType == Zeta.Internals.SNO.GizmoType.ServerProp)
                    continue;

                if (o.ActorInfo.GizmoType == Zeta.Internals.SNO.GizmoType.StartLocations)
                    continue;

                if (o.ActorInfo.GizmoType == Zeta.Internals.SNO.GizmoType.Trigger)
                    continue;

                if (o.ActorInfo.GizmoType == Zeta.Internals.SNO.GizmoType.ProximityTriggered)
                    continue;

                if (o.ActorInfo.GizmoType == Zeta.Internals.SNO.GizmoType.CheckPoint)
                    continue;
                try
                {
                    Logging.Write("[QuestTools] Gizmo ActorSNO: {0} Name: {1} Type: {2} Radius: {8:0.00} Position: {3} ({4}) Distance: {5:0} Animation: {6} has Attributes: {7}\n",
                        o.ActorSNO, o.Name, o.ActorInfo.GizmoType, getProfilePosition(o.Position), getSimplePosition(o.Position), o.Distance, o.CommonData.CurrentAnimation, attributesFound, o.CollisionSphere.Radius);
                }
                catch { }
            }
            return iType;
        }
        private static double DumpPlayers(IEnumerable<DiaUnit> players, double iType)
        {
            Logging.Write("[QuestTools] Players found: {0}", players.Count());
            foreach (DiaPlayer o in players)
            {
                if (!o.IsValid)
                    continue;

                string attributesFound = "";

                foreach (ActorAttributeType aType in Enum.GetValues(typeof(ActorAttributeType)))
                {
                    double aat = GetAttribute(o, aType);
                    attributesFound += aType.ToString() + "=" + iType.ToString() + ", ";
                }
                try
                {
                    Logging.Write("[QuestTools] Player ActorSNO: {0} Name: {1} Type: {2} Radius: {7:0.00} Position: {3} ({4}) Animation: {5} has Attributes: {6}\n",
                                        o.ActorSNO, o.Name, o.ActorInfo.GizmoType, getProfilePosition(o.Position), getSimplePosition(o.Position), o.CommonData.CurrentAnimation, attributesFound, o.CollisionSphere.Radius);
                }
                catch { }

            }
            return iType;
        }

        private static string getProfilePosition(Vector3 pos)
        {
            return String.Format("x=\"{0:0}\" y=\"{1:0}\" z=\"{2:0}\" ", pos.X, pos.Y, pos.Z);
        }
        private static string getSimplePosition(Vector3 pos)
        {
            return String.Format("{0:0}, {1:0}, {2:0}", pos.X, pos.Y, pos.Z);
        }
        private static string SpacedConcat(params object[] args)
        {
            string output = "";

            foreach (object o in args)
            {
                output += o.ToString() + ", ";
            }

            return output;
        }
        private static double GetAttribute(double iType, DiaObject o, ActorAttributeType aType)
        {
            try
            {
                iType = (double)o.CommonData.GetAttribute<ActorAttributeType>(aType);
            }
            catch
            {
                iType = -1;
            }

            return iType;
        }
        private static double GetAttribute(DiaObject o, ActorAttributeType aType)
        {
            try
            {

                return o.CommonData.GetAttribute<double>(aType);
            }
            catch
            {
                return (double)(-1);
            }
        }
        void GameEvents_OnPlayerDied(object sender, EventArgs e)
        {
            if (ReloadProfileOnDeath)
                forceReloadProfile = true;

            Logging.Write("[QuestTools] Player died! Position={0} QuestId={1} StepId={2} WorldId={3}",
                ZetaDia.Me.Position, ZetaDia.CurrentQuest.QuestSNO, ZetaDia.CurrentQuest.StepId, ZetaDia.CurrentWorldId);
        }


        public void OnShutdown()
        {

        }

        public string Author
        {
            get { return "rrrix - Mod by WAR"; }
        }

        public string Description
        {
            get { return "Allows various XML tags useful in Questing profiles"; }
        }

        public System.Windows.Window DisplayWindow
        {
            get
            {
                return null;
                //return CreateWindow();
            }
        }

        public string Name
        {
            get { return "[WAR] QuestTools"; }
        }

        public void OnInitialize()
        {
            LastJoinedGame = DateTime.MinValue;
            Zeta.CommonBot.GameEvents.OnGameJoined += GameEvents_OnGameJoined;
        }

        internal static DateTime LastJoinedGame { get; private set; }
        void GameEvents_OnGameJoined(object sender, EventArgs e)
        {
            LastJoinedGame = DateTime.Now;
            Logging.WriteDiagnostic("[QuestTools] LastJoinedGame is {0}", LastJoinedGame);
        }

        public bool Equals(IPlugin other) { return (other.Name == Name) && (other.Version == Version); }
    }
}
