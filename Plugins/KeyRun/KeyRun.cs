using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;
using System.Threading;
using Zeta;
using Zeta.Common;
using Zeta.Common.Plugins;
using Zeta.CommonBot;
using Zeta.CommonBot.Profile;
using Zeta.CommonBot.Profile.Composites;
using Zeta.Internals;
using Zeta.Internals.Actors;
using Zeta.Internals.Actors.Gizmos;
using Zeta.Internals.SNO;
using Zeta.Navigation;
using Zeta.TreeSharp;
using Zeta.XmlEngine;
using GilesTrinity;

namespace KeyRun
{
    public class KeyRun : IPlugin
    {
        static readonly string NAME = "[WAR] KeyRun";
        static readonly string AUTHOR = "skeetermcdiggles & Magi - Mod by WAR";
        static readonly Version VERSION = new Version(1, 8);
        static readonly string DESCRIPTION = "KeyRun escolhe o ato e caça o chaveiro baseado nas chaves que você possui.";
		
		public static bool ChooseActProfileExitGame = false;
		public static string ChooseActProfile = "";
		public static string KeyWardenProfileCurrentlyRunning = "";
		
		private int[] _keywardenSNOs = { 255996, 256000, 256015 };
		private string[] _keyNames = { "demonkey_", "demontrebuchetkey" };
        
		public bool _keydropped = false;
		public bool _keywardenFound = false;
		public bool _keywardenDead = false;
		public bool _keywardenFindCorpse = false;
		public bool _keywardenReadyForWarp = false;
		
		public Window DisplayWindow { get { return null; } }
		
		private float _keywardenCurrentHitpoints = 0f;
		// number of hitpoints left on warden to assume death 
		// because Pulse may not fire quick enough to record 0HP
		public static float _keywardenAssumedDeathHitpoints = 350000f;	
		
		// current location of keywarden
		public static Vector3 _blankVector = new Vector3(0, 0, 0);
		public static Vector3 _keywardenPosition = _blankVector;
		
		// distance from Keywarden
		public static float _distanceFromKeywarden = 0f;
		
		
		// Plugin Auth Info
        public string Author
        {
            get
            {
                return AUTHOR;
            }
        }
        public string Description
        {
            get
            {
                return DESCRIPTION;
            }
        }
        public string Name
        {
            get
            {
                return NAME;
            }
        }
        public Version Version
        {
            get
            {
                return VERSION;
            }
        }
		
		
		///////////////
		// DB EVENTS //
        public void OnDisabled()
        {
			GameEvents.OnPlayerDied -= KeyRunOnDeath;
			GameEvents.OnGameChanged -= KeyRunOnGameChange;

			Log("*******************KEYRUN*****************");
            Log("DESATIVADO: [WAR] KeyRun " + Version + " foi desativado - mod by WAR!");
            Log("*******************KEYRUN*****************");
			
        }
        public void OnEnabled()
        {
			GameEvents.OnPlayerDied += KeyRunOnDeath;
			GameEvents.OnGameChanged += KeyRunOnGameChange;
			
			Log("*******************KEYRUN*****************");
            Log("ATIVADO: [WAR] KeyRun " + Version + " foi ativado - mod by WAR!");
            Log("*******************KEYRUN*****************");
        }
        public void OnInitialize()
        {
        }
        public void OnPulse()
        {
            if (GilesTrinity.GilesTrinity.PlayerStatus.CurrentHealthPct > 0)
				KeyWardenCheck();
				
			if (GilesTrinity.GilesTrinity.PlayerStatus.CurrentHealthPct > 0 && _keywardenDead)
				InfernalKeyCheck();
        }
        public void OnShutdown()
        {
        }
		
		
		////////////////////
		// CORE FUNCTIONS //
		// KeyRunOnGameChange
		private void KeyRunOnGameChange(object src, EventArgs mea)
        {
			Log("New Game Started, Reset Variables");
			_keydropped = false;
			_keywardenFound = false;
			_keywardenDead = false;
			_keywardenFindCorpse = false;
			_keywardenReadyForWarp = false;
			_keywardenCurrentHitpoints = 0f;
			_distanceFromKeywarden = 0f;
			_keywardenPosition = _blankVector;
			KeyWardenProfileCurrentlyRunning = "";
			ChooseActProfileExitGame = false;
			ChooseActProfile = "";
        }
		
		// KeyRunOnDeath
		private void KeyRunOnDeath(object src, EventArgs mea)
        {
			if (_keywardenFound)
			{
				// reset warden if found so we don't skip him
				//Log("========== DIED BEFORE KILLING KEYWARDEN - RETURN TO WARDEN LOCATION ============");
				//_keywardenFound = false;
			} 
			else if (KeyWardenProfileCurrentlyRunning != "" && !_keywardenReadyForWarp)
			{
				// keywarden is dead, but so are you. Probably should go back
				Log("========== KILLED KEYWARDEN BUT DIED - LET'S GO BACK TO BE SAFE ============");
				// set find corpse boolean
				_keywardenFindCorpse = true;
			}
			
        }
		
		// Get Nephalem Valor
		private int GetNephalemValor()
		{
			return(ZetaDia.Me.GetAllBuffs().Where(b => b.SNOId == 230745).First().StackCount);
		}
		
		// InfernalKeyCheck
		private void InfernalKeyCheck() {
			// set key object
			DiaObject keyObject = ZetaDia.Actors.RActorList.OfType<DiaObject>().FirstOrDefault(r => _keyNames.Any(k => r.Name.ToLower().StartsWith(k)));
			
			// key check
			if (keyObject != null) {
				if (!_keydropped) { 
					Log("========== INFERNAL KEY DROPPED ==========");
					
					// key dropped
					_keydropped = true;
				}
				
				// Get Key if we aren't try to vendor or do something
				if (!GilesTrinity.GilesTrinity.bDontMoveMeIAmDoingShit && !GilesTrinity.GilesTrinity.IsReadyToTownRun && !GilesTrinity.GilesTrinity.ForceVendorRunASAP && !Zeta.CommonBot.Logic.BrainBehavior.IsVendoring) {
					
					// Go Get Key if Trinity doesn't *spammed to ensure nothing prevents us from getting that key!*
					Log("========== RETRIEVING INFERNAL KEY ==========");
					ZetaDia.Me.UsePower(SNOPower.Axe_Operate_Gizmo, Vector3.Zero, 0, keyObject.ACDGuid);
				}
					
			} else if (keyObject == null && _keydropped && !ZetaDia.Me.IsInTown && !GilesTrinity.GilesTrinity.IsReadyToTownRun && !GilesTrinity.GilesTrinity.ForceVendorRunASAP && !Zeta.CommonBot.Logic.BrainBehavior.IsVendoring) {
				Log("=+=+=+=+=+= INFERNAL KEY ACQUIRED!!! =+=+=+=+=+=");
				
				// key dropped
				_keydropped = false;
			}
		}
		
		// KeyWardenCheck
        private void KeyWardenCheck()
        {
            // set keywarden object
			DiaObject wardenObject = ZetaDia.Actors.RActorList.OfType<DiaObject>().FirstOrDefault(r => _keywardenSNOs.Any(k => k == r.ActorSNO));
			
			// set distance from keywarden
			if (_distanceFromKeywarden >= 0f && !_keywardenDead) _distanceFromKeywarden = ZetaDia.Me.Position.Distance(_keywardenPosition);
			
			// perform check
			if (wardenObject != null)
            {
				KeyWardenUpdateStats(wardenObject);		// update keywarden stats
				
                if (_keywardenCurrentHitpoints > 0f && !_keywardenFound)
                {
					KeyWardenFound();					// found
                }
                else if (_keywardenCurrentHitpoints <= 0f && !_keywardenDead) 
                {
					KeyWardenDead();					// defintely dead
                }            
            }
			else if (_keywardenFound && !_keywardenDead)
			{
				KeyWardenOutOfRange();					// out of range or possibly dead
			}
			else if (_keywardenDead && !_keywardenReadyForWarp && !_keywardenFindCorpse)
			{
				KeyWardenGoToCorpse();					// confirmed dead, go to corpse
			}
			else if (_keywardenFindCorpse && !_keywardenReadyForWarp)
			{
				KeyWardenLookForCorpse();				// Keywarden dead, but so are you, find his corpse
			}
			else if (_keywardenReadyForWarp && !_keydropped)
			{
				KeyWardenWarpOut();						// Keywarden dead, found corpse, looted, lets restart!
			}
        }
		
		// KeyWardenUpdateStats
		private void KeyWardenUpdateStats(DiaObject wardenObject)
		{
			// set keywarden hitpoints
			_keywardenCurrentHitpoints = wardenObject.CommonData.GetAttribute<float>(ActorAttributeType.HitpointsCur);
			// set keywarden location
			_keywardenPosition = wardenObject.Position;
		}
		
		// KeyWardenFound
		private void KeyWardenFound()
		{
			_keywardenFound = true;
			_keywardenDead = false;
			Log("========== KEYWARDEN FOUND ============");
			
			//KeyWardenGoToPosition();
		}
		
		// KeyWardenOutOfRange
		private void KeyWardenOutOfRange()
		{
			Log("WARDEN HP: " + _keywardenCurrentHitpoints);
				
			if (_keywardenCurrentHitpoints > _keywardenAssumedDeathHitpoints)
			{
				// assume keywarden is out of range
				Log("========== KEYWARDEN OUT OF RANGE ============");
				
				//KeyWardenGoToPosition();
				//Thread.Sleep(2000);
				_keywardenFound = false;
			}
			else
			{
				// assume keywarden is dead
				KeyWardenDead();
			}
		}
		
		// KeyWardenDead
		private void KeyWardenDead()
		{	
			_keywardenFound = false;
			_keywardenDead = true;
			
			// Set Current Profile in case we die after killing Keywarden
			KeyWardenProfileCurrentlyRunning = Zeta.CommonBot.Settings.GlobalSettings.Instance.LastProfile;
			
			Log("=+=+=+=+=+= KEYWARDEN VANQUISHED!!! =+=+=+=+=+=");
		}
		
		// KeyWardenLookForCorpse
		private void KeyWardenLookForCorpse()
		{
			// set distance from keywardem
			float distanceFromKeywarden = ZetaDia.Me.Position.Distance(_keywardenPosition);
			//Log("Distance from Warden = " + distanceFromKeywarden);
			
			// corpse within range
			if (distanceFromKeywarden < 60f && GilesTrinity.GilesTrinity.PlayerStatus.CurrentHealthPct > 0)
			{
				// go to corpse (currently cycles as there are likely enemies lurking)
				KeyWardenGoToCorpse();
			}
		}
		
		// KeyWardenGoToPosition
		public static void KeyWardenGoToPosition()
		{
			Vector3 NavTarget = _keywardenPosition;
            Vector3 MyPos = GilesTrinity.GilesTrinity.PlayerStatus.CurrentPosition;
            if (Vector3.Distance(MyPos, NavTarget) > 250) {
                NavTarget = MathEx.CalculatePointFrom(MyPos, NavTarget, Vector3.Distance(MyPos, NavTarget) - 250);
            }
			
			// Move to Warden Location
			Navigator.MoveTo(NavTarget);
		}
		
		// KeyWardenGoToCorpse
		private void KeyWardenGoToCorpse()
		{
			// Don't do anything until done doing shit
			if (GilesTrinity.GilesTrinity.bDontMoveMeIAmDoingShit || GilesTrinity.GilesTrinity.IsReadyToTownRun || GilesTrinity.GilesTrinity.ForceVendorRunASAP || Zeta.CommonBot.Logic.BrainBehavior.IsVendoring)
			{
				return ;
			}
			
			// Get distance to keywarden corpse
			float distanceFromKeywarden = ZetaDia.Me.Position.Distance(_keywardenPosition);
			
			// Move to Keywarden's last known position to pick up items
			if (distanceFromKeywarden > 6f)
			{
				// Don't do anything
				if (GilesTrinity.GilesTrinity.bDontMoveMeIAmDoingShit || GilesTrinity.GilesTrinity.IsReadyToTownRun || GilesTrinity.GilesTrinity.ForceVendorRunASAP || Zeta.CommonBot.Logic.BrainBehavior.IsVendoring)
				{
					return ;
				}
				
				// Moving to Keywarden Corpse
				Log("Moving to last known Keywarden Location to ensure item pickup...");
				//Navigator.PlayerMover.MoveTowards(_keywardenPosition);
				ZetaDia.Me.UsePower(SNOPower.Walk, _keywardenPosition, ZetaDia.Me.WorldDynamicId);
				//Navigator.MoveTo(_keywardenPosition, "Moving to Keywarden", true);
				
				// Sleep
				Thread.Sleep(1000);
			}
			else
			{
				// Found corpse, reset trinity loot parameters for final pickup
				Log("Keywarden Corpse Found...");
				
				// Don't do anything
				if (GilesTrinity.GilesTrinity.bDontMoveMeIAmDoingShit || GilesTrinity.GilesTrinity.IsReadyToTownRun || GilesTrinity.GilesTrinity.ForceVendorRunASAP || Zeta.CommonBot.Logic.BrainBehavior.IsVendoring)
				{
					return ;
				}
				
				// Ready for Warp
				_keywardenReadyForWarp = true;
			}
		}
		
		// KeyWardenWarpOut
		private void KeyWardenWarpOut()
		{
			// Reset variables for warp
			_keywardenReadyForWarp = false;
			_keywardenDead = false;
			_keywardenFindCorpse = false;
			
			// Get ready for new Act
			Log("Ready to choose new Act.");
			
			// Load next profile
			ProfileManager.Load(ChooseActProfile);
			// A quick nap-time helps prevent some funny issues
			Thread.Sleep(3000);
			// See if we need to exit the game
			if (ChooseActProfileExitGame)
			{
				Log("Exiting game to continue with next profile.");
				// Attempt to teleport to town first for a quicker exit
				int iSafetyLoops = 0;
				while (!ZetaDia.Me.IsInTown)
				{
					iSafetyLoops++;
					ZetaDia.Me.UsePower(SNOPower.UseStoneOfRecall, ZetaDia.Me.Position, ZetaDia.Me.WorldDynamicId, -1);
					Thread.Sleep(1000);
					if (iSafetyLoops > 5)
						break;
				}
				Thread.Sleep(1000);
				ZetaDia.Service.Games.LeaveGame();
				GilesTrinity.GilesTrinity.ResetEverythingNewGame();
				// Wait for 10 second log out timer if not in town, else wait for 3 seconds instead
				Thread.Sleep(!ZetaDia.Me.IsInTown ? 10000 : 3000);
			}
		}
		
		// Log / Ancillary
        private void Log(string message, LogLevel logLevel = LogLevel.Normal)
        {
            Logging.Write(logLevel, string.Format("[[WAR] KeyRun] {0}", message));
        }
		
		public static bool isPlayerDoingAnything()
		{
			if (GilesTrinity.GilesTrinity.bDontMoveMeIAmDoingShit || GilesTrinity.GilesTrinity.IsReadyToTownRun || GilesTrinity.GilesTrinity.ForceVendorRunASAP || Zeta.CommonBot.Logic.BrainBehavior.IsVendoring)
			{
				return false;
			}
			else
			{
				return true;
			}
		}

        public bool Equals(IPlugin other)
        {
            return Name.Equals(other.Name) && Author.Equals(other.Author) && Version.Equals(other.Version);
        }
    }
	
	//////////////
	// XML TAGS //
	// Force Town Warp
	[XmlElement("KeyRunForceTownWarp")]
    public class KeyRunForceTownWarp : ProfileBehavior
    {
        private bool m_IsDone = false;
        public override bool IsDone
        {
            get
            {
                return m_IsDone;
            }
        }

        protected override Composite CreateBehavior()
        {
            return new Zeta.TreeSharp.Action(ret =>
            {
				// Attempt to teleport to town first for a quicker exit
                int iSafetyLoops = 0;
                while (iSafetyLoops <= 5)
                {
                    iSafetyLoops++;
                    ZetaDia.Me.UsePower(SNOPower.UseStoneOfRecall, ZetaDia.Me.Position, ZetaDia.Me.WorldDynamicId, -1);
                    //Thread.Sleep(1000);
                }

                m_IsDone = true;
            });
        }

        public override void ResetCachedDone()
        {
            m_IsDone = false;
            base.ResetCachedDone();
        }

        private void Log(string message, LogLevel logLevel = LogLevel.Normal)
        {
            Logging.Write(logLevel, string.Format("[[WAR] KeyRun] {0}", message));
        }
    }
	
	// IfReadyToWarp
	[XmlElement("IfReadyToWarp")]
    public class IfReadyToWarp : ComplexNodeTag
    {
        private bool? bComplexDoneCheck;
        private bool? bAlreadyCompleted;
        private Func<bool> funcConditionalProcess;
        private static Func<ProfileBehavior, bool> funcBehaviorProcess;
        
        protected override Composite CreateBehavior()
        {
            PrioritySelector decorated = new PrioritySelector(new Composite[0]);
            foreach (ProfileBehavior behavior in base.GetNodes())
            {
                decorated.AddChild(behavior.Behavior);
            }
            return new Zeta.TreeSharp.Decorator(new CanRunDecoratorDelegate(CheckNotAlreadyDone), decorated);
        }

        public bool GetConditionExec()
        {
            // If trinity is doing shit, we're not ready
			bool flag = KeyRun.isPlayerDoingAnything();
			
			// check for reverse
			if (Type != null && Type == "reverse")
			{
				return !flag;
			}
			else
			{
				return flag;
			}
        }

        private bool CheckNotAlreadyDone(object object_0)
        {
            return !IsDone;
        }

        public override void ResetCachedDone()
        {
            foreach (ProfileBehavior behavior in Body)
            {
                behavior.ResetCachedDone();
            }
            bComplexDoneCheck = null;
        }

        private static bool CheckBehaviorIsDone(ProfileBehavior profileBehavior)
        {
            return profileBehavior.IsDone;
        }
		
		[XmlAttribute("type")]
        public string Type { get; set; }
		
        public Func<bool> Conditional
        {
            get
            {
                return funcConditionalProcess;
            }
            set
            {
                funcConditionalProcess = value;
            }
        }
		

        public override bool IsDone
        {
            get
            {
                // Make sure we've not already completed this tag
                if (bAlreadyCompleted.HasValue && bAlreadyCompleted == true)
                {
                    return true;
                }
                if (!bComplexDoneCheck.HasValue)
                {
                    bComplexDoneCheck = new bool?(GetConditionExec());
                }
                if (bComplexDoneCheck == false)
                {
                    return true;
                }
                if (funcBehaviorProcess == null)
                {
                    funcBehaviorProcess = new Func<ProfileBehavior, bool>(CheckBehaviorIsDone);
                }
                bool bAllChildrenDone = Body.All<ProfileBehavior>(funcBehaviorProcess);
                if (bAllChildrenDone)
                {
                    bAlreadyCompleted = true;
                }
                return bAllChildrenDone;
            }
        }
    }
	
	// GoToKeyWarden
	[XmlElement("GoToKeyWarden")]
    public class GoToKeyWarden : ProfileBehavior
    {
        private bool m_IsDone = false;
		private float fPathPrecision;
		private Vector3 kVector = KeyRun._keywardenPosition;
		private Vector3 blankVector = KeyRun._blankVector;
		
		protected override Composite CreateBehavior()
        {
            Composite[] children = new Composite[2];
            Composite[] compositeArray = new Composite[2];
            compositeArray[0] = new Zeta.TreeSharp.Action(new ActionSucceedDelegate(FlagTagAsCompleted));
            children[0] = new Zeta.TreeSharp.Decorator(new CanRunDecoratorDelegate(CheckDistance), new Sequence(compositeArray));
            ActionDelegate actionDelegateMove = new ActionDelegate(MoveToKeywarden);
            Sequence sequenceblank = new Sequence(
                new Zeta.TreeSharp.Action(actionDelegateMove)
                );
            children[1] = sequenceblank;
            return new PrioritySelector(children);
        }

        private RunStatus MoveToKeywarden(object ret)
        {
			if (kVector != blankVector) {
				KeyRun.KeyWardenGoToPosition();
			}
			
            return RunStatus.Success;
        }

        private bool CheckDistance(object object_0)
        {
            if (kVector != blankVector) {
				return (ZetaDia.Me.Position.Distance(kVector) <= Math.Max(PathPrecision, Navigator.PathPrecision));
			}
			return true;
        }

        private void FlagTagAsCompleted(object object_0)
        {
            m_IsDone = true;
        }

        public override void ResetCachedDone()
        {
            m_IsDone = false;
            base.ResetCachedDone();
        }

        public override bool IsDone
        {
            get
            {
                if (IsActiveQuestStep)
                {
                    return m_IsDone;
                }
                return true;
            }
        }
		
		[XmlAttribute("pathPrecision")]
        public float PathPrecision
        {
            get
            {
                return fPathPrecision;
            }
            set
            {
                fPathPrecision = value;
            }
        }
    }
	
	// GetAwayFromKeyWarden
	[XmlElement("GetAwayFromKeyWarden")]
    public class GetAwayFromKeyWarden : ProfileBehavior
    {
        private bool m_IsDone = false;
		private float fPosX;
        private float fPosY;
        private float fPosZ;
		private float fPathPrecision;
		private Vector3? vMainVector;
		
		protected override Composite CreateBehavior()
        {
            Composite[] children = new Composite[2];
            Composite[] compositeArray = new Composite[2];
            compositeArray[0] = new Zeta.TreeSharp.Action(new ActionSucceedDelegate(FlagTagAsCompleted));
            children[0] = new Zeta.TreeSharp.Decorator(new CanRunDecoratorDelegate(CheckDistance), new Sequence(compositeArray));
            ActionDelegate actionDelegateMove = new ActionDelegate(MoveAwayFromKeywarden);
            Sequence sequenceblank = new Sequence(
                new Zeta.TreeSharp.Action(actionDelegateMove)
                );
            children[1] = sequenceblank;
            return new PrioritySelector(children);
        }

        private RunStatus MoveAwayFromKeywarden(object ret)
        {
			Vector3 NavTarget = Position;
			Vector3 MyPos = GilesTrinity.GilesTrinity.PlayerStatus.CurrentPosition;
			if (Vector3.Distance(MyPos, NavTarget) > 250) {
				NavTarget = MathEx.CalculatePointFrom(MyPos, NavTarget, Vector3.Distance(MyPos, NavTarget) - 250);
			}
			
			// Move Away from KeyWarden
			Navigator.MoveTo(NavTarget);
			
            return RunStatus.Success;
        }

        private bool CheckDistance(object object_0)
        {
			if (KeyRun._distanceFromKeywarden >= fSafeDistance) {
				return true;
			}
			
			// return distance from point
			return (ZetaDia.Me.Position.Distance(Position) <= Math.Max(PathPrecision, Navigator.PathPrecision));
        }

        private void FlagTagAsCompleted(object object_0)
        {
            m_IsDone = true;
        }

        public override void ResetCachedDone()
        {
            m_IsDone = false;
            base.ResetCachedDone();
        }

        public override bool IsDone
        {
            get
            {
                if (IsActiveQuestStep)
                {
                    return m_IsDone;
                }
                return true;
            }
        }
		
		public Vector3 Position
        {
            get
            {
                vMainVector = new Vector3(X, Y, Z);
                return vMainVector.Value;
            }
        }
		
		[XmlAttribute("pathPrecision")]
        public float PathPrecision
        {
            get
            {
                return fPathPrecision;
            }
            set
            {
                fPathPrecision = value;
            }
        }
		
		
		[XmlAttribute("x")]
        public float X
        {
            get
            {
                return fPosX;
            }
            set
            {
                fPosX = value;
            }
        }

        [XmlAttribute("y")]
        public float Y
        {
            get
            {
                return fPosY;
            }
            set
            {
                fPosY = value;
            }
        }

        [XmlAttribute("z")]
        public float Z
        {
            get
            {
                return fPosZ;
            }
            set
            {
                fPosZ = value;
            }
        }
		
		[XmlAttribute("safeDistance")]
        public float fSafeDistance { get; set; }
    }
	
	// KeyRunProfile
    [XmlElement("KeyRunProfile")]
    public class KeyRunProfile : ProfileBehavior
    {
        private bool m_IsDone = false;
        public override bool IsDone
        {
            get
            {
                return m_IsDone;
            }
        }

        protected override Composite CreateBehavior()
        {
            return new Zeta.TreeSharp.Action(ret =>
            {
				int[] keySNO = { 255809, 255807, 255808 };
                int[] keys = { 0, 0, 0 };
                ZetaDia.Actors.Update();

                foreach (ACDItem item in ZetaDia.Me.Inventory.StashItems)
                {
					if (keySNO.Any(k => k == item.ActorSNO))
                    {
                        keys[keySNO.IndexOf(item.ActorSNO)] += item.ItemStackQuantity;
                    }
                    //Log(string.Format("{0} ({1})", item.Name, item.ActorSNO));
                }
                foreach (ACDItem item in ZetaDia.Me.Inventory.Backpack)
                {
					if (keySNO.Any(k => k == item.ActorSNO))
                    {
                        keys[keySNO.IndexOf(item.ActorSNO)] += item.ItemStackQuantity;
                    }
                    //Log(string.Format("{0} ({1})", item.Name, item.ActorSNO));
                }

                for (int i = 0; i < keys.Length; i++)
                {
                    Log(string.Format("Act {0}: {1}", i + 1, keys[i]));
                }
				
				// Choose Act with least amount of Keys
                int act = keys.IndexOf(keys.Min()) + 1;
				
				// Choose Random Act if all key amounts are the same
				if ( act == keys.IndexOf(keys.Max()) + 1 )
				{
					Random rndAct = new Random(int.Parse(Guid.NewGuid().ToString().Substring(0, 8), NumberStyles.HexNumber));
                	act = (rndAct.Next(3)) + 1;
				}

                string sThisProfileString = string.Empty;
                Log(string.Format("Loading act {0}", act));
                switch (act)
                {
                    case 1:
                        sThisProfileString = Act1Profile;
                        break;
                    case 2:
                        sThisProfileString = Act2Profile;
                        break;
                    case 3:
                        sThisProfileString = Act3Profile;
                        break;
                    default:
                        break;
                }

                // See if there are multiple profile choices, if so split them up and pick a random one
                if (sThisProfileString.Contains("!"))
                {
                    string[] sProfileChoices;
                    sProfileChoices = sThisProfileString.Split(new string[] { "!" }, StringSplitOptions.None);
                    Random rndNum = new Random(Guid.NewGuid().GetHashCode());
                    int iChooseProfile = (rndNum.Next(sProfileChoices.Count())) - 1;
                    sThisProfileString = sProfileChoices[iChooseProfile];
                }
                // Now calculate our current path by checking the currently loaded profile
                string sCurrentProfilePath = Path.GetDirectoryName(Zeta.CommonBot.Settings.GlobalSettings.Instance.LastProfile);
                // And prepare a full string of the path, and the new .xml file name
                string sNextProfile = sCurrentProfilePath + @"\" + sThisProfileString;
                Log("Loading new profile.");
                Log(sNextProfile);
                ProfileManager.Load(sNextProfile);
                // A quick nap-time helps prevent some funny issues
                Thread.Sleep(3000);
                
				// Leaves Game
                Log("Exiting game to continue with next profile.");
                // Attempt to teleport to town first for a quicker exit
                int iSafetyLoops = 0;
                while (!ZetaDia.Me.IsInTown)
                {
                    iSafetyLoops++;
                    ZetaDia.Me.UsePower(SNOPower.UseStoneOfRecall, ZetaDia.Me.Position, ZetaDia.Me.WorldDynamicId, -1);
                    Thread.Sleep(1000);
                    if (iSafetyLoops > 5)
                        break;
                }
                Thread.Sleep(1000);
                ZetaDia.Service.Games.LeaveGame();
                GilesTrinity.GilesTrinity.ResetEverythingNewGame();

                // Wait for 10 second log out timer if not in town, else wait for 3 seconds instead
                Thread.Sleep(!ZetaDia.Me.IsInTown ? 10000 : 3000);

                m_IsDone = true;
            });
        }

        [XmlAttribute("act1profile")]
        public string Act1Profile
        {
            get;
            set;
        }
        [XmlAttribute("act2profile")]
        public string Act2Profile
        {
            get;
            set;
        }
        [XmlAttribute("act3profile")]
        public string Act3Profile
        {
            get;
            set;
        }

        public override void ResetCachedDone()
        {
            m_IsDone = false;
            base.ResetCachedDone();
        }

        private void Log(string message, LogLevel logLevel = LogLevel.Normal)
        {
            Logging.Write(logLevel, string.Format("[[WAR] KeyRun] {0}", message));
        }
    }
	
	// KeyRunChooseActProfile
	[XmlElement("KeyRunChooseActProfile")]
    public class KeyRunChooseActProfile : ProfileBehavior
    {
        private bool m_IsDone = false;
        public override bool IsDone
        {
            get { return m_IsDone; }
        }

        [XmlAttribute("profile")]
        public string ProfileName { get; set; }
		
		[XmlAttribute("exit")]
        public string Exit { get; set; }

        protected override Composite CreateBehavior()
        {
            return new Zeta.TreeSharp.Action(ret =>
                                                 {
                                                     // Set Exit Game
													 KeyRun.ChooseActProfileExitGame = Exit != null && Exit.ToLower() == "true";
													 // Now calculate our current path by checking the currently loaded profile
													 string sCurrentProfilePath = Path.GetDirectoryName(Zeta.CommonBot.Settings.GlobalSettings.Instance.LastProfile);
													 // And prepare a full string of the path, and the new .xml file name
													 string sNextProfile = sCurrentProfilePath + @"\" + ProfileName;
													 KeyRun.ChooseActProfile = sNextProfile;
														
                                                     m_IsDone = true;
                                                 });
        }

        public override void ResetCachedDone()
        {
            m_IsDone = false;
            base.ResetCachedDone();
        }
		
		private void Log(string message, LogLevel logLevel = LogLevel.Normal)
        {
            Logging.Write(logLevel, string.Format("[[WAR] KeyRun] {0}", message));
        }
    }
	
	// KeyRunSetWardenDeathHP
	[XmlElement("KeyRunSetWardenDeathHP")]
    public class KeyRunSetWardenDeathHP : ProfileBehavior
    {
        private bool m_IsDone = false;
        public override bool IsDone
        {
            get { return m_IsDone; }
        }

		[XmlAttribute("hitpoints")]
        public float KeyWardenHP { get; set; }

        protected override Composite CreateBehavior()
        {
            return new Zeta.TreeSharp.Action(ret =>
                                                 {
                                                     
													 
													 if (KeyWardenHP != null)
													 {
														 KeyRun._keywardenAssumedDeathHitpoints = KeyWardenHP;
														 Log("KeyWarden Assumed Death HP set to " + KeyWardenHP);
													 }
                                                     m_IsDone = true;
                                                 });
        }

        public override void ResetCachedDone()
        {
            m_IsDone = false;
            base.ResetCachedDone();
        }
		
		private void Log(string message, LogLevel logLevel = LogLevel.Normal)
        {
            Logging.Write(logLevel, string.Format("[[WAR] KeyRun] {0}", message));
        }
    }
}
