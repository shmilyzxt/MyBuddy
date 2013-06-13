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
        private void Log(string message)
        {
            string totalMessage = string.Format("[{0}] {1}", Name, message);
            Logging.Write(totalMessage);
        }
        private void Diagnostic(string message)
        {
            string totalMessage = string.Format("[{0}] {1}", Name, message);
            Logging.WriteDiagnostic(totalMessage);
        }
		public static int getRandNumber(int Low, int High)
		{
			Random rndNum = new Random(int.Parse(Guid.NewGuid().ToString().Substring(0, 8), System.Globalization.NumberStyles.HexNumber));
			int rnd = rndNum.Next(Low, High);
			return rnd;
		}
		public void WaitTimer(int waitTime = 250)
		{
			DateTime _Timer = DateTime.Now;
			while (DateTime.Now.Subtract(_Timer).TotalMilliseconds < waitTime)
			{
				_Timer = DateTime.Now;
			}
			return;
		}
        private void OnLevelUp(object sender, EventArgs e)
        {
            bNeedFullItemUpdate = true;
        }
	}
}