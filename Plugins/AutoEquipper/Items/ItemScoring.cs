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
        private void updateEHPFactor(object sender, RoutedEventArgs e)
        {
            EHPFactor = Convert.ToInt32(ehpFactor.Text);
        }
        private void updateDamageFactor(object sender, RoutedEventArgs e)
        {
            DamageFactor = Convert.ToInt32(damageFactor.Text);
        }
		private double getValue(double damage, double ehp)
        {
            return damage * DamageFactor + ehp * EHPFactor;
        }
		
		private bool InventorySlotProtected(string slot)
		{
			// Check to see if item is for a protected slot
			switch (slot)
			{
				case "PlayerBracers":
					if (bIgnoreWrists)
					{
						Diagnostic(String.Format("[**PACK**] Can't Equip. {0} is protected.", slot));
						return true;
					}
					break;
				case "PlayerFeet":
					if (bIgnoreFeet)
					{
						Diagnostic(String.Format("[**PACK**] Can't Equip. {0} is protected.", slot));
						return true;
					}
					break;
				case "PlayerHands":
					if (bIgnoreHands) 
					{
						Diagnostic(String.Format("[**PACK**] Can't Equip. {0} is protected.", slot));
						return true;
					}
					break;
				case "PlayerHead":
					if (bIgnoreHead) 
					{
						Diagnostic(String.Format("[**PACK**] Can't Equip. {0} is protected.", slot));
						return true;
					}
					break;
				case "PlayerLeftFinger":
					if (bIgnoreFingerL) 
					{
						Diagnostic(String.Format("[**PACK**] Can't Equip. {0} is protected.", slot));
						return true;
					}
					break;
				case "PlayerLeftHand":
					if (bIgnoreHand) 
					{
						Diagnostic(String.Format("[**PACK**] Can't Equip. {0} is protected.", slot));
						return true;
					}
					break;
				case "PlayerLegs":
					if (bIgnoreLegs) 
					{
						Diagnostic(String.Format("[**PACK**] Can't Equip. {0} is protected.", slot));
						return true;
					}
					break;
				case "PlayerNeck":
					if (bIgnoreNeck) 
					{
						Diagnostic(String.Format("[**PACK**] Can't Equip. {0} is protected.", slot));
						return true;
					}
					break;
				case "PlayerRightFinger":
					if (bIgnoreFingerR) 
					{
						Diagnostic(String.Format("[**PACK**] Can't Equip. {0} is protected.", slot));
						return true;
					}
					break;
				case "PlayerRightHand":
					if (bIgnoreOffhand) 
					{
						Diagnostic(String.Format("[**PACK**] Can't Equip. {0} is protected.", slot));
						return true;
					}
					break;
				case "PlayerShoulders":
					if (bIgnoreShoulders) 
					{
						Diagnostic(String.Format("[**PACK**] Can't Equip. {0} is protected.", slot));
						return true;
					}
					break;
				case "PlayerTorso":
					if (bIgnoreTorso) 
					{
						Diagnostic(String.Format("[**PACK**] Can't Equip. {0} is protected.", slot));
						return true;
					}
					break;
				case "PlayerWaist":
					if (bIgnoreWaist) 
					{
						Diagnostic(String.Format("[**PACK**] Can't Equip. {0} is protected.", slot));
						return true;
					}
					break;
				case "PlayerTwoHand":
					if (bIgnoreHand || bIgnoreOffhand) 
					{
						Diagnostic(String.Format("[**PACK**] Can't Equip. {0} is protected.", slot));
						return true;
					}
					break;
				case "Unknown":
					// Secondary Tests usually have a type Unknown....
					return false;
				default:
					Diagnostic("[**PACK**] Invalid Slot: " + slot);
					return false;
			}
			return false;
		}
	}
}