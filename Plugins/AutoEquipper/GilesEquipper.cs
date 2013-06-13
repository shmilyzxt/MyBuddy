using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using Zeta;
using Zeta.Common;
using Zeta.Common.Plugins;
using Zeta.CommonBot;
using Zeta.CommonBot.Profile;
using Zeta.CommonBot.Profile.Common;
using Zeta.XmlEngine;
using Zeta.TreeSharp;
using Zeta.Internals;
using Zeta.Internals.Actors;

namespace AutoEquipper
{
    public partial class AutoEquipper : IPlugin
    {
        public Version Version { get { return new Version(1, 8, 0, 4); } }
        public string Author { get { return "GilesSmith + Demonbuddy Community - Mod by WAR"; } }
        public string Description { get { return "AutoEquipper tenta equipar o seu personagem automaticamente com o melhor equipamento."; } }
        public string Name { get { return "[WAR] AutoEquipper " + Version; } }
        public bool Equals(IPlugin other) { return (other.Name == Name) && (other.Version == Version); }
		
        public void OnEnabled()
        {
            if (!Directory.Exists(pluginPath))
            {
                Log("Fatal Error - cannot enable plugin. Invalid path: " + pluginPath);
                Log("Please check you have installed the plugin to the correct location, and then restart DemonBuddy and re-enable the plugin.");
                Log(@"Plugin should be installed to \<DemonBuddyFolder>\Plugins\AutoEquipper\");
            }
            else
            {
				Zeta.CommonBot.GameEvents.OnLevelUp += OnLevelUp;
                LoadConfiguration();
                Log("*******************AUTOEQUIPPER*****************");
				Log("Ativado! O bot irá agora tentar auto-equipar itens melhores que pegar!");
				Log("*******************AUTOEQUIPPER*****************");
            }
        }
		
        public void OnDisabled()
        {
			Zeta.CommonBot.GameEvents.OnLevelUp -= OnLevelUp;
            Log("*******************AUTOEQUIPPER*****************");
			Log("Desativado!");
			Log("*******************AUTOEQUIPPER*****************");
        }
		
        public void OnPulse()
        {
            if (!LastLoop.IsRunning) LastLoop.Start();
            if (!LastFullEvaluation.IsRunning) LastFullEvaluation.Start();
			if (!ZetaDia.IsInGame || !ZetaDia.Me.IsValid || ZetaDia.Me.IsDead || ZetaDia.IsLoadingWorld) return;
			AnimationState aState = ZetaDia.Me.CommonData.AnimationState;
            if (aState == AnimationState.Attacking ||
                aState == AnimationState.Casting ||
                aState == AnimationState.Channeling ||
                aState == AnimationState.Dead ||
                aState == AnimationState.TakingDamage)
            {
                return;
            }

			// FUGLY HACK
			// DB .302+ runs through the stashing routine like Flash Gordon... Can't use a soft hook like the Potion method...
			// TODO: Look into overloading the TownRun routine and appending stash checks on TownRuns...
			if (Zeta.Internals.UIElements.StashWindow.IsVisible)
			{ // Stash Checks
				Diagnostic("UIElement Detected: StashWindow, Initiating Stash Checks...");
				if (bCheckStash)
				{
					// Spam Prevention (Can not run more than once per 3 minutes)
					double _tempLastStashCheck = DateTime.Now.Subtract(_lastStashCheck).TotalSeconds;
					Diagnostic("Last potion was purchased " + _tempLastStashCheck + " seconds ago");
					if (_tempLastStashCheck < 180) {
						Diagnostic("CheckPotions can not run again for another " + (180 - _tempLastStashCheck) + " seconds");
					}
					else
					{
						BotMain.PauseWhile(CheckStash, 0, new TimeSpan?(TimeSpan.FromSeconds(30.0)));
					}
				}
			}
			
			// Spam Prevention (Can not run more than once per 3 Seconds)
            if (LastLoop.ElapsedMilliseconds > 3000)
            {
				LastLoop.Restart();
				try
				{
					if (LastFullEvaluation.ElapsedMilliseconds > 300000)
					{
						LastFullEvaluation.Restart();
						bNeedFullItemUpdate = true;
					}
					
					if (currentLevel != ZetaDia.Me.Level)
					{
						LastFullEvaluation.Restart();
						bNeedFullItemUpdate = true;
						currentLevel = ZetaDia.Me.Level;
					}
					
					if (Zeta.Internals.UIElements.VendorWindow.IsVisible)
					{ // Vendor Checks
						Diagnostic("UIElement Detected: VendorWindow, Initiating Potion Checks...");
						if (bBuyPots)
						{
							// Spam Prevention (Can not run more than once per 1 minute)
							double _tempLastBuy = DateTime.Now.Subtract(_lastBuy).TotalSeconds;
							Diagnostic("Last potion was purchased " + _tempLastBuy + " seconds ago");
							if (_tempLastBuy < 60) {
								Diagnostic("CheckPotions can not run again for another " + (60 - _tempLastBuy) + " seconds");
							}
							else
							{
								BotMain.PauseWhile(CheckPotions, 0, new TimeSpan?(TimeSpan.FromSeconds(30.0)));
							}
						}
					}
					else if (Zeta.Internals.UIElements.StashWindow.IsVisible)
					{ // Stash Checks
						// Moved outside of the spam prevention loop... So that it will check on every pulse..
					}
					else
					{ // Backpack Checks
						CheckBackpack();
					}
				}
				catch (System.AccessViolationException ex)
				{
					// Maybe someday the DB core will stop throwing this error.... O_o
					throw;
				}
				catch (Exception ex)
				{
					Log("An exception occured: " + ex.ToString());
					//throw;
				}
            }
        }
		
        public void OnInitialize()
        {
			Log("*******************AUTOEQUIPPER*****************");
			Log("Iniciado - mod by WAR!.");
			Log("*******************AUTOEQUIPPER*****************");
						
            pluginPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + @"\Plugins\AutoEquipper\";
            sConfigFile = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Settings", ZetaDia.Service.CurrentHero.BattleTagName, "AutoEquipper.cfg");
			
			/*
			http://diablo.wikia.com/wiki/Healing_Potions#In_Diablo_III
			Name	                        Heals	Req'd Level
			Minor Health Potion	            250	    1
			Lesser Health Potion	        400	    6
			Health Potion	                550	    11
			Greater Health Potion	        1000	16
			Major Health Potion	            1600	21
			Super Health Potion	            2500	26
			Heroic Health Potion	        4500	37
			Resplendent Health Potion	    6500	47
			Runic Health Potion	            9000	53
			Mythic Health Potion	        12500	58 
			*/
			potionList = new List<Potion>();
			//                       SNO  DID         NAME                    HEAL   RLVL  COST
			potionList.Add(new Potion(-1, -1,	"Minor Health Potion", 			250, 	0, 	20));
			potionList.Add(new Potion(-1, -1, 	"Lesser Health Potion", 		400, 	6, 	110));
			potionList.Add(new Potion(-1, -1, 	"Health Potion", 				550, 	11, 160));
			potionList.Add(new Potion(-1, -1, 	"Greater Health Potion", 		1000, 	16, 210));
			potionList.Add(new Potion(-1, -1, 	"Major Health Potion", 			1600, 	21, 260));
			potionList.Add(new Potion(-1, -1, 	"Super Health Potion", 			2500, 	26, 310));
			potionList.Add(new Potion(-1, -1, 	"Heroic Health Potion", 		4500, 	37, 410));
			potionList.Add(new Potion(-1, -1, 	"Resplendent Health Potion", 	6500, 	47, 510));
			potionList.Add(new Potion(-1, -1, 	"Runic Health Potion", 			9000, 	53, 560));
			potionList.Add(new Potion(-1, -1,	"Mythic Health Potion", 		12500, 	58, 610));
        }
		
        public void OnShutdown()
        {
            if (configWindow != null) configWindow.Close();
            configWindow = null;
		}
    } // class

} // namespace
