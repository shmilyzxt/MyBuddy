using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Net.Sockets;
using System.Net;
using Zeta.Common;
using System.Reflection;
using Zeta.Internals.Actors;
using Zeta;
using System.Threading;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using System.IO;
using System.Drawing.Imaging;
using Zeta.CommonBot.Settings;

namespace BuddyStatsD3Plugin
{
	public enum LoginResult
	{
		WrongApiKey = 0,
		Success = 1,
		WrongClientVersion = 2,
		ConnectionError = 3,
		Unknown = 4
	}
	class Connection
	{
		private TcpClient connection;
		private Thread listenThread;
		private object sendLock;
		private GugaPlugin plugin;
		private bool isConnected;
		private bool isLoggedIn;
		private object aliveLock;
		#region Assembly
		private static Assembly dll;
		private static Type typePacket;
		private static Type typePacketLogin;
		private static Type typePacketPlayerData;
		private static Type typePacketScreenShot;
		private static Type typePacketItemDrop;
		private static Type typePacketIsAlive;
		private static Type typePacketProfile;

		private static ConstructorInfo conPacketLogin;
		private static ConstructorInfo conPacketPlayerData;
		private static ConstructorInfo conPacketScreenShot;
		private static ConstructorInfo conPacketItemDrop;
		private static ConstructorInfo conPacketIsAlive;
		private static ConstructorInfo conPacketProfile;

		private static MethodInfo methodPacketToBytes;
		private static MethodInfo methodBytesToPacket;
		//Packet
		private static FieldInfo fieldSessionID;
		//PacketLogin
		private static FieldInfo fieldLoginSuccess;
		#endregion
		private string serverAddress = "buddystats.com";
		private int serverPort = 15999;
		private int sessionID = 0;

		public Connection(GugaPlugin plugin)
		{
			this.plugin = plugin;
			isConnected = false;
			isLoggedIn = false;
			connection = null;
			listenThread = null;
			sendLock = new object();
			aliveLock = new object();
		}
		public static bool LoadLibrary()
		{
			try
			{
				//string path = System.IO.Directory.GetCurrentDirectory() + "\\" + Zeta.CommonBot.Settings.GlobalSettings.Instance.PluginsPath + "\\BuddyStats\\WebStatsPackets.dll";
				string path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/" + GlobalSettings.Instance.PluginsPath + "/BuddyStats/WebStatsPackets.dll";
				dll = Assembly.LoadFile(path);
				typePacket = dll.GetType("WebStatsPackets.Packet");
				typePacketLogin = dll.GetType("WebStatsPackets.PacketLogin");
				typePacketPlayerData = dll.GetType("WebStatsPackets.PacketPlayerData");
				typePacketScreenShot = dll.GetType("WebStatsPackets.PacketScreenShot");
				typePacketItemDrop = dll.GetType("WebStatsPackets.PacketItemDrop");
				typePacketIsAlive = dll.GetType("WebStatsPackets.PacketIsAlive");
				typePacketProfile = dll.GetType("WebStatsPackets.PacketProfile");

				conPacketLogin = typePacketLogin.GetConstructor(new[] {typeof(int), typeof(string), typeof(int) });
				conPacketPlayerData = typePacketPlayerData.GetConstructor(new[] { typeof(int), typeof(byte), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(string), typeof(bool) });
				conPacketScreenShot = typePacketScreenShot.GetConstructor(new[] { typeof(int), typeof(byte[]) });
				conPacketItemDrop = typePacketItemDrop.GetConstructor(new[] {typeof(int), typeof(string), typeof(int), typeof(int), typeof(int), typeof(float[]) });
				conPacketIsAlive = typePacketIsAlive.GetConstructor(new[] { typeof(int) });
				conPacketProfile = typePacketProfile.GetConstructor(new[] { typeof(int), typeof(string[]) });

				methodPacketToBytes = typePacket.GetMethod("PacketToBytes");
				methodBytesToPacket = typePacket.GetMethod("BytesToPacket");

				fieldSessionID = typePacket.GetField("sessionID");
				fieldLoginSuccess = typePacketLogin.GetField("loginResult");
				return true;
			}
			catch (Exception e)
			{
				Logging.Write("DLL LOADING ERROR " + e.Message + "   " + e.StackTrace);
				return false;
			}
		}
		public void Connect()
		{
			try
			{
				connection = new TcpClient();
				connection.Connect(new IPEndPoint(Dns.GetHostAddresses(serverAddress)[0], serverPort));
				isConnected = true;
				connection.ReceiveTimeout = 2000;
				Login();
				connection.ReceiveTimeout = 0;
				listenThread = new Thread(ListenProc);
				listenThread.Start();
			}
			catch (Exception e)
			{	
				Logging.Write("CONNECTION ERROR: " + e.Message + "   " + e.StackTrace);
				isConnected = false;
			}
		}
		public void Disconnect()
		{
			if (listenThread != null)
			listenThread.Abort();
			connection.Close();
			isConnected = false;
			isLoggedIn = false;
		}
		public bool IsConnected()
		{
			return isConnected && isLoggedIn;
		}
		public bool SendPlayerData()
		{
			try
			{
				object playerDataPacket = conPacketPlayerData.Invoke(new object[] 
				{   
					sessionID, (byte)ZetaDia.Me.ActorClass, (int)Zeta.CommonBot.GameStats.Instance.GoldPerHour, 
					ZetaDia.Me.Inventory.Coinage, (int)Zeta.CommonBot.GameStats.Instance.ExpPerHour, 
					ZetaDia.Me.CommonData.GetAttribute<int>(ActorAttributeType.DeathCount), ZetaDia.Me.Level + ZetaDia.Me.ParagonLevel, 
					Zeta.CommonBot.ProfileManager.CurrentProfile.Name, plugin.IsPrivate 
				});
				byte[] buffer = (byte[])methodPacketToBytes.Invoke(null, new object[] { playerDataPacket });
				Send(buffer);
				return true;
			}
			catch (Exception e)
			{
				isConnected = false;
				return false;
			}
		}
		public bool SendScreenShot()
		{
			try
			{
				Process p = Process.GetProcessById(ZetaDia.Memory.Process.Id);
				MemoryStream stream = new MemoryStream();
				Bitmap bitmap = ScreenShotTaker.TakeScreenShot(p.MainWindowHandle);
				bitmap.Save(stream, ImageFormat.Jpeg);
				byte[] screenshot = stream.ToArray();
				stream.Close();
				bitmap.Dispose();
				object packetScreenShot = conPacketScreenShot.Invoke(new object[] { sessionID, screenshot });
				byte[] buffer = (byte[])methodPacketToBytes.Invoke(null, new object[] { packetScreenShot });
				Send(buffer);
				plugin.LogMessage("Sending screenshot... (" + (buffer.Length - 4) + " bytes)");
				return true;
			}
			catch (Exception e)
			{
				plugin.LogMessage(e.Message + "   " + e.StackTrace);
				isConnected = false;
				return false;
			}
		}
		public bool SendProfiles()
		{
			try
			{
				string[] profiles = plugin.GetProfileList();
				for (int i = 0; i < profiles.Length; i++)
				profiles[i] = Path.GetFileName(profiles[i]);

				object profilePacket = conPacketProfile.Invoke(new object[] { sessionID, profiles });
				byte[] buffer = (byte[])methodPacketToBytes.Invoke(null, new object[] { profilePacket });
				Send(buffer);
				return true;
			}
			catch (Exception e)
			{
				return false;
			}
		}
		public bool Login()
		{
			LoginResult result = LoginResult.Unknown;
			try
			{
				object loginPacket = conPacketLogin.Invoke(new object[] {sessionID, plugin.ApiKey, plugin.VersionRaw });
				byte[] buffer = (byte[])methodPacketToBytes.Invoke(null, new object[] { loginPacket });
				// connection.GetStream().Flush();
				Send(buffer);
				connection.Client.Receive(buffer);
				object recvPacket = methodBytesToPacket.Invoke(null, new object[] { buffer });
				sessionID = (int)fieldSessionID.GetValue(recvPacket);
				result = (LoginResult)fieldLoginSuccess.GetValue(recvPacket);
			}
			catch (Exception e)
			{
				isConnected = false;
				isLoggedIn = false;
				sessionID = 0;
				result = LoginResult.ConnectionError;
			}
			HandleLoginResult(result);
			return isLoggedIn;
		}
		public bool SendIsAlive()
		{
			lock (aliveLock)
			{
				try
				{
					object alivePacket = conPacketIsAlive.Invoke(new object[] { sessionID });
					byte[] buffer = (byte[])methodPacketToBytes.Invoke(null, new object[] { alivePacket });
					Send(buffer);
					return true;
				}
				catch (Exception e)
				{
					return false;
				}
			}
		}
		public bool SendItemDrop(ACDItem item)
		{
			try
			{
				Array enumVals = Enum.GetValues(typeof(Zeta.Internals.ItemStats.Stat));
				float[] stats = new float[enumVals.Length];
				int i = 0;
				foreach (Zeta.Internals.ItemStats.Stat stat in enumVals)
				{
					try
					{
						if (stat == Zeta.Internals.ItemStats.Stat.Sockets)
						{
							stats[i] = (float)item.Stats.GetStat<int>(stat);
						}
						else
						stats[i] = item.Stats.GetStat<float>(stat);
					}
					catch (Exception e)
					{
						stats[i] = 0;
						plugin.LogMessage("ERROR READING ITEM STATS: " + e.Message + "  " + e.StackTrace);
					}
					i++;
				}
				object itemPacket = conPacketItemDrop.Invoke(new object[] { sessionID, item.Name, item.ItemQualityLevel, item.Level, (int)item.ItemType, stats });
				byte[] buffer = (byte[])methodPacketToBytes.Invoke(null, new object[] { itemPacket });
				Send(buffer);
				return true;
			}
			catch (Exception e)
			{
				plugin.LogMessage("ERROR SENDING ITEM STATS TO SERVER: " + e.Message + "  " + e.StackTrace);
				isConnected = false;
				return false;
			}
		}
		public void Send(byte[] buffer)
		{
			lock (sendLock)
			{
				connection.GetStream().Write(buffer, 0, buffer.Length);
			}
		}
		public void HandleLoginResult(LoginResult result)
		{
			isLoggedIn = false;
			if (result == LoginResult.Success)
			{
				isLoggedIn = true;
			}
			else if (result == LoginResult.WrongClientVersion)
			{
				plugin.LogMessage("Wrong Plugin Version! Please update your plugin");
			}
			else if (result == LoginResult.WrongApiKey)
			{
				plugin.LogMessage("Wrong API Key");
			}
			else
			{
				plugin.LogMessage("Server not responding!");
			}
		}
		private void ListenProc()
		{
			while (!plugin.isMainWindowClosed())
			{
				string msg = "";
				Thread.Sleep(50);
				if (IsConnected())
				{
					try
					{
						byte[] buffer = new byte[2048];
						connection.Client.Receive(buffer);
						msg = new ASCIIEncoding().GetString(buffer);
						HandleWebRequest(msg);
					}
					catch (Exception e)
					{
						// plugin.LogMessage("Listener ERROR   " + e.Message + "    " + e.StackTrace);
						isConnected = false;
						isLoggedIn = false;
						return;
					}
				}
			}
		}
		private void HandleWebRequest(string request)
		{
			string[] split = request.Split(';');
			if (split.Length < 2)
			return;

			string cmd = split[0];
			int sessionID = Convert.ToInt32(split[1]);
			if (cmd == "COMMAND_START")
			{
				if (!Zeta.CommonBot.BotMain.IsRunning)
				{
					plugin.LogMessage("Start command from BuddyStats received! Starting the bot...");
					Zeta.CommonBot.BotMain.Start();
				}
			}
			else if (cmd == "COMMAND_STOP")
			{
				if (Zeta.CommonBot.BotMain.IsRunning)
				{
					plugin.LogMessage("Stop command from BuddyStats received! Stopping the bot...");
					Zeta.CommonBot.BotMain.Stop();
					ZetaDia.Service.Games.LeaveGame();
					SendIsAlive();
				}
			}
			else if (cmd == "COMMAND_SCREENSHOT")
			{
				plugin.LogMessage("Screenshot command from BuddyStats received! Sending screenshots...");
				SendScreenShot();
			}
			else if (cmd == "COMMAND_PROFILE_GET")
			{
				plugin.LogMessage("Profile request from BuddyStats received! Sending profile list...");
				SendProfiles();
			}
			else if (cmd == "COMMAND_PROFILE_LOAD" && split.Length == 3)
			{
				plugin.LogMessage("Profile load request from BuddyStats received! Loading new profile...");
				plugin.LoadProfileByIndex(Convert.ToInt32(split[2]));
			}
		}
	}
}
