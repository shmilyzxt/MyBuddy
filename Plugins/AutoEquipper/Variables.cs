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
        private static string pluginPath = "";
        private static string sConfigFile = "";
        private static bool bSavingConfig = false;
		private static List<ACDItem> _alreadyLookedAtBlacklist = new List<ACDItem>();
		private Stopwatch LastLoop = new Stopwatch();
        private Stopwatch LastFullEvaluation = new Stopwatch();
		
        public static Boolean bNeedFullItemUpdate = true;
		
		private int currentLevel;
			
		private static DiaActivePlayer Me { get { return ZetaDia.Me; } }
		
		// ConfigWindow Defaults
		public static bool bIgnoreHead = false; 
		public static bool bIgnoreShoulders = false; 
		public static bool bIgnoreTorso = false; 
		public static bool bIgnoreHands = false; 
		public static bool bIgnoreWrists = false; 
		public static bool bIgnoreWaist = false; 
		public static bool bIgnoreLegs = false; 
		public static bool bIgnoreFeet = false; 
		public static bool bIgnoreNeck = false; 
		public static bool bIgnoreFingerL = false; 
		public static bool bIgnoreFingerR = false; 
		public static bool bIgnoreHand = false; 
		public static bool bIgnoreOffhand = false; 
        public static bool bIdentifyItems = true;
		public static bool bBuyPots = true;
		public static bool bDisable60 = true;
		public static bool bCheckStash = false;
		public static double iQtyPotion1 = 10;
		public static double iQtyPotion2 = 15;
		public static double iQtyPotion3 = 35;
		public static double iQtyPotion4 = 35;
		public static double iQtyPotion5 = 35;
		public static double iQtyPotion6 = 35;
		public static double iQtyPotion7 = 50;
		public static double iQtyPotion8 = 50;
		public static double iQtyPotion9 = 50;
		public static double iQtyPotion10 = 99;
        public static double EHPFactor = 1;
        public static double DamageFactor = 30;
        public static double iCurrentEHP = 0;
        public static double iCurrentDamage = 0;
		
		// CheckPotion Vars
		private List<Potion> merchantList = new List<Potion>();
		private List<Potion> potionList = new List<Potion>();
        private static DateTime _lastBuy = DateTime.MinValue;
		
		// CheckStash Vars
        private static DateTime _lastStashCheck = DateTime.MinValue;
	}
}