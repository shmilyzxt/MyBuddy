using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Reflection;

namespace BuddyStatsD3Plugin
{
	public class Updater
	{
		private string updateUrl;
		private string pluginsDir;
		private int version;
		private WebClient client;

		public Updater(string updateUrl, string pluginsDir, int version)
		{
			this.updateUrl = updateUrl;
			this.pluginsDir = pluginsDir;
			this.version = version;
			client = new WebClient();
		}
		public bool IsUpToDate()
		{
			bool isUpToDate = false;
			int latestVersion = 0;

			StreamReader reader = new StreamReader(client.OpenRead(updateUrl + "/latest"));
			latestVersion = Convert.ToInt32(reader.ReadLine());
			isUpToDate = version >= latestVersion;
			reader.Close();
			return isUpToDate;
		}
		public void DownloadFiles()
		{
			StreamReader reader = new StreamReader(client.OpenRead(updateUrl + "/dllist"));
			while (!reader.EndOfStream)
			{
				string file = reader.ReadLine();
				client.DownloadFile(updateUrl + "/files/" + file, Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/" + pluginsDir + "/BuddyStats/" + file);
			}
			reader.Close();
		}
	}
}
