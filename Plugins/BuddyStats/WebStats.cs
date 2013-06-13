using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Zeta.Common.Plugins;
using System.Windows;
using Zeta;
using Zeta.Common;
using System.Threading;
using Zeta.Internals;
using Zeta.Internals.Actors;
using Zeta.Internals.SNO;
using Zeta.CommonBot;
using System.Net.Sockets;
using System.Net;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Drawing;
using Zeta.CommonBot.Settings;
namespace BuddyStatsD3Plugin
{
	class GugaPlugin : IPlugin
	{
		private int version = 0983;
		private List<ACDItem> lastInventoryState;
		private string lastItemPickedName;
		private DateTime lastSent;
		private Connection connection;
		private bool isRunning;
		private Thread mainThread;
		private Updater updater;

		public bool IsPrivate { get; set; }
		public string AutoUpdate { get; set; }
		public string ApiKey { get; set; }
		public string Author { get { return "Guga & Creedo - Modded by WAR"; } }
		public string Description { get { return "Este plugin irá enviar suas estatísticas para um servidor, onde é armazenada enquanto bot está ativo e você pode acompanhar como bot está fazendo através do seu smartphone ou pelo site do buddystats.com!"; } }
		public string Name { get { return "[WAR] BuddyStats"; } }
		public Version Version { get { return new Version(version / 1000, version % 1000 / 10); } }
		public int VersionRaw { get { return version; } }
		public bool Equals(IPlugin other) { return other.Name == Name && other.Version == Version; }
		public Window DisplayWindow { get { return null; } }

		public void OnEnabled()
		{
			IsPrivate = false;
			AutoUpdate = "True";
			lastInventoryState = null;
			lastItemPickedName = "";
			ApiKey = "VuIP7SKZi4Y6dKV";
			lastSent = DateTime.Now;
			connection = new Connection(this);
			LoadConfig();
			
			updater = new Updater("http://146.255.27.116/get/db", GlobalSettings.Instance.PluginsPath, version);
			if (CheckForUpdates())
			{
				return;
			}

			if (!Connection.LoadLibrary())
			{
				LogMessage("Loading of DLL failed! Please make sure, that file WebStatsPackets.dll is located in Plugins/BuddyStats");
			}
			
			if (ApiKey == "")
			{
				LogMessage("Please register on our website and fill in your ApiKey in the Settings.cfg located in plugin directory");
				return;
			}
			LogMessage("Conectando no servidor e logando...");
			connection.Connect();
			if (!connection.IsConnected())
			{
				connection.Disconnect();
				return;
			}
			LogMessage("Conectado no servidor");
			// GameEvents.OnItemLooted += OnItemLooted;
			GameEvents.OnGameJoined += OnGameJoined;
			mainThread = new Thread(MainThreadProc);
			mainThread.Start();
			
			Demonbuddy.App.Current.Dispatcher.ShutdownStarted += onExit;

			LogMessage("Website:  www.buddystats.com  ");
			LogMessage("[ON]");
			LogMessage("*******************BUDDYSTATS*****************");

		}
		public void OnDisabled()
		{
			GameEvents.OnGameJoined -= OnGameJoined;
			Demonbuddy.App.Current.Dispatcher.ShutdownStarted -= onExit;

			connection.Disconnect();
			if (mainThread != null && mainThread.IsAlive)
			mainThread.Abort();
			LogMessage("*******************BUDDYSTATS*****************");
			LogMessage("[OFF]");
			LogMessage("*******************BUDDYSTATS*****************");
		}
		public void onExit(object o, EventArgs e)
		{
			connection.Disconnect();
			if (mainThread != null && mainThread.IsAlive)
			mainThread.Abort();
		}
		public void OnInitialize()
		{
			
			
		}
		
		

		
		public void OnShutdown()
		{
			if (connection != null)
			{
				connection.Disconnect();
				if (mainThread != null && mainThread.IsAlive)
				mainThread.Abort();
			}
			LogMessage("[OFF]");
		}
		public void OnPulse()
		{
			if (!ZetaDia.IsInGame || !ZetaDia.Me.IsValid || !connection.IsConnected())
			return;

			CheckItemsDropped();

			if (DateTime.Now.Subtract(lastSent).TotalSeconds > 10)
			{
				bool success = false;
				if (connection.IsConnected())
				{
					success = connection.SendPlayerData();
				}
				if (!success)
				connection.Disconnect();
				lastSent = DateTime.Now;
			}
		}
		public void OnGameJoined(object sender, EventArgs args)
		{
			lastInventoryState = new List<ACDItem>(ZetaDia.Me.Inventory.Backpack);
		}
		public void OnItemLooted(object sender, ItemLootedEventArgs args)
		{
			if (connection.IsConnected())
			{
				if (args.Item.ItemQualityLevel >= ItemQuality.Magic1 && args.Item.Name != lastItemPickedName)
				{
					if (!connection.SendItemDrop(args.Item))
					LogMessage("Sending item drop failed!");
					lastItemPickedName = args.Item.Name;
				}
			}
		}
		public void OnItemLootedEx(ACDItem item)
		{
			//LogMessage("NEW ITEM PICKED UP! " + item.Name);
			if (connection.IsConnected())
			{
				if (item.ItemQualityLevel >= ItemQuality.Magic1 && item.Name != lastItemPickedName)
				{
					if (!connection.SendItemDrop(item))
					LogMessage("Sending item drop failed!");
					lastItemPickedName = item.Name;
				}
			}
		}
		public void CheckItemsDropped()
		{
			if (ZetaDia.Me.IsInTown)
			return;

			if (lastInventoryState == null)
			{
				lastInventoryState = new List<ACDItem>(ZetaDia.Me.Inventory.Backpack);
				return;
			}

			int countDifference = ZetaDia.Me.Inventory.Backpack.Count() - lastInventoryState.Count;
			if (countDifference == 1)
			{
				/*foreach (ACDItem item in ZetaDia.Me.Inventory.Backpack)
				{
					if (!lastInventoryState.Contains(item))
					{
						OnItemLootedEx(ZetaDia.Me.Inventory.Backpack.Last<ACDItem>());
					}
				}*/
				foreach (ACDItem item in ZetaDia.Me.Inventory.Backpack)
				{
					bool found = false;
					foreach (ACDItem itemLastState in lastInventoryState)
					{
						if (item.Name == itemLastState.Name && item.InventoryRow == itemLastState.InventoryRow && item.InventoryColumn == itemLastState.InventoryColumn)
						{
							found = true;
							break;
						}
					}
					if (!found)
					{
						OnItemLootedEx(item);
						break;
					}
				}
			}
			if (countDifference != 0)
			{
				lastInventoryState = new List<ACDItem>(ZetaDia.Me.Inventory.Backpack);
			}
		}
		public string[] GetProfileList()
		{
			try
			{
				//string path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/Profiles/";
				string path = Path.GetDirectoryName(GlobalSettings.Instance.LastProfile);
				string[] profileList = Directory.GetFiles(path, "*.xml");
				string temp = profileList[0];
				profileList[0] = GlobalSettings.Instance.LastProfile;
				for (int i = 1; i < profileList.Length; i++)
				{
					if (profileList[i] == GlobalSettings.Instance.LastProfile)
					profileList[i] = temp;
				}
				return profileList;
			}
			catch (Exception e)
			{
				LogMessage("Error while loading profile: " + e.Message);
				return null;
			}
		}
		public void LoadProfileByIndex(int index)
		{
			string profilePath = GetProfileList()[index];
			Zeta.CommonBot.BotMain.Stop();
			ZetaDia.Service.Games.LeaveGame();
			Zeta.CommonBot.ProfileManager.Load(profilePath);

			LogMessage("Profile [" + profilePath + "] has been loaded!");
			Thread.Sleep(10000);
			Zeta.CommonBot.BotMain.Start();
		}

		// Thanks to sinterlkaas
		public bool isMainWindowClosed()
		{
			Process db = Process.GetCurrentProcess();
			IntPtr h = db.MainWindowHandle;
			return (h == IntPtr.Zero || h == null);
		}
		public void MainThreadProc()
		{
			DateTime lastSentAlive = DateTime.Now;
			DateTime lastConnect = DateTime.Now;
			int connTriesCount = 0;
			while (!isMainWindowClosed())
			{
				if (!connection.IsConnected())
				{
					if (DateTime.Now.Subtract(lastConnect).TotalSeconds > 10)
					{
						if (connTriesCount > 10)
						{
							if (!updater.IsUpToDate())
							{
								RestartDB();
								return;
							}
							connTriesCount = 0;
						}
						LogMessage("Trying to reconnect...");
						connTriesCount++;
						connection.Disconnect();
						connection.Connect();
						if (connection.IsConnected())
						LogMessage("Reconnecting has been succesful");
						lastConnect = DateTime.Now;
					}
				}
				else if ((!Zeta.CommonBot.BotMain.IsRunning) && DateTime.Now.Subtract(lastSentAlive).TotalSeconds > 30)
				{
					try
					{
						if (!connection.SendIsAlive())
						connection.Disconnect();
						lastSentAlive = DateTime.Now;
					}
					catch (Exception e)
					{
						LogMessage("ERROR IN MAIN THREAD:" + e.StackTrace);
					}
				}
				Thread.Sleep(1000);
			}
		}
		public bool CheckForUpdates()
		{
			
			if (AutoUpdate == "True")
			{
				LogMessage("Opted in to auto updates! To change this setting, adjust settings.cfg file");
				
				try
				{	
					
					if (!updater.IsUpToDate())
					{
						LogMessage("New version of BuddyStats found, downloading...");
						updater.DownloadFiles();
						LogMessage("BuddyStats succesfuly updated, restarting plugin...");
						Thread t = new Thread(ReloadPluginsProc);
						t.Start();
						return true;
					}
					else
					{
						LogMessage("Your plugin is up to date!");
					}
				}	
				
				
				catch (Exception e)
				{
					LogMessage("Checking for updates failed! ERROR:" + e.Message + "  " + e.StackTrace);
				}
			}
			else
			{
				LogMessage("Optado por NÃO ter atualizações automáticas! Para mudar essa configuração, altere o arquivo settings.cfg");
			}
			
			return false;
			
			
			
		}
		public void ReloadPluginsProc()
		{
			string[] plugins = Zeta.Common.Plugins.PluginManager.GetEnabledPlugins().ToArray();
			Zeta.Common.Plugins.PluginManager.ReloadAllPlugins(GlobalSettings.Instance.PluginsPath);
			Zeta.Common.Plugins.PluginManager.SetEnabledPlugins(plugins);
		}
		public void RestartDB()
		{
			if (!CharacterSettings.Instance.EnabledPlugins.Contains("BuddyStats"))
			{
				CharacterSettings.Instance.EnabledPlugins.Add("BuddyStats");
				CharacterSettings.Instance.Save();
			}
			ZetaDia.Service.Games.LeaveGame();
			Process.Start(Assembly.GetEntryAssembly().Location, "-autostart -routine=\"" + RoutineManager.Current.Name + "\"");
			SafeCloseProcess();
		}
		//<3 Nesox
		public void SafeCloseProcess()
		{
			if (Thread.CurrentThread != Application.Current.Dispatcher.Thread)
			{
				Application.Current.Dispatcher.Invoke(new Action(SafeCloseProcess));
				return;
			}

			Application.Current.Shutdown();
		}
		public void LoadConfig()
		{
			if (!File.Exists("Plugins/BuddyStats/Settings.cfg"))
			return;
			StreamReader reader = new StreamReader(GlobalSettings.Instance.PluginsPath + "/BuddyStats/Settings.cfg");
			while (!reader.EndOfStream)
			{
				string[] split = reader.ReadLine().Split('=');
				if (split != null)
				{
					if (split[0] == "IsPrivate")
					{
						IsPrivate = Convert.ToBoolean(split[1]);
					}
					else if (split[0] == "ApiKey")
					{
						ApiKey = split[1];
					}

					else if (split[0] == "AutoUpdate")
					{
						AutoUpdate = split[1];
					}
				}
			}
			reader.Close();
			LogMessage("*******************BUDDYSTATS*****************");
			LogMessage("Carregando configurações");			
		}
		public void SaveConfig()
		{
			StreamWriter writer = new StreamWriter(GlobalSettings.Instance.PluginsPath + "/BuddyStats/Settings.cfg", false);
			writer.WriteLine("IsPrivate=" + IsPrivate.ToString());
			writer.WriteLine("AutoUpdate=" + AutoUpdate);
			writer.Close();
			LogMessage("Settings Saved");
		}
		public void LogMessage(string msg)
		{
			Logging.Write("[" + Name + "]:  " + msg);
		}
	}
}
