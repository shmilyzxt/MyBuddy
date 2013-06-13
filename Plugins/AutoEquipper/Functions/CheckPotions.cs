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
	    private sealed class Potion
        {
            public int ActorSNO { get; set; }
            public int DynamicId { get; set; }
            public string Name { get; set; }
            public int HitpointsGranted { get; set; }
            public int Level { get; set; }
            public int RequiredLevel { get; set; }
            public int Gold { get; set; }

            public Potion() { }

            public Potion(int actorSno, int dynamicId, string name, int hitpointsGranted, int requiredLevel, int gold)
            {
                ActorSNO = actorSno;
				DynamicId = dynamicId;
                Name = name;
                HitpointsGranted = hitpointsGranted;
				Level = requiredLevel;
                RequiredLevel = requiredLevel;
                Gold = gold;
            }
        }
	
		private bool CheckPotions()
		{
			Diagnostic("[**POTION**] CheckPotions method has been fired");
			
			// Verify VendorWindow is available.. Incase user manually runs away or the pause isn't working for some reason...
			if (!Zeta.Internals.UIElements.VendorWindow.IsVisible) {
				Diagnostic("[**POTION**] UIElement VendorWindow can not be found. Exiting CheckPotion Method...");
				return false;
			}
			
			int _iPotionsBuy = 0;
			int _iPotionsNeeded = 0;
			
			// Merchant Potions
			Potion merchantitem;
			var merchantItems = ZetaDia.Actors.Me.Inventory.MerchantItems.Where(i => i.IsPotion);
			if (merchantItems == null)
			{
				Log("[**POTION**] Memory read error or Vendor has no potions.");
				_lastBuy = DateTime.Now;
				return false;
			}
			else
			{
				merchantList = new List<Potion>();
				foreach (ACDItem item in merchantItems)
				{
					Diagnostic(String.Format("[**POTION**] Merchant potion found... Name: {0}, ActorSNO: {1}, GUID: {2}, DynamicId: {3}, IsPotion: {4}, maxStackCount: {5}, RequiredLevel: {6}",
						item.Name, item.ActorSNO, item.ACDGuid, item.DynamicId, item.IsPotion, item.MaxStackCount, item.RequiredLevel));
					Potion P = potionList.Where(p => p.Name == item.Name).FirstOrDefault();
					merchantList.Add(new Potion(item.ActorSNO, item.DynamicId, item.Name, P.HitpointsGranted, P.RequiredLevel, P.Gold));
				}
				merchantitem = merchantList.Where(i => i.RequiredLevel <= ZetaDia.Me.Level).OrderByDescending(p => p.HitpointsGranted).FirstOrDefault();
			}
			
			
			Diagnostic("[**POTION**] Strongest Potion that can be purchased from merchant is '" + merchantitem.Name + "'");
			
			// Pack Potions
			ACDItem thisPackPotion;
			try { thisPackPotion = ZetaDia.Me.Inventory.Backpack.Where(i => (i.IsPotion) && (i.RequiredLevel <= ZetaDia.Me.Level)).OrderByDescending(p => p.HitpointsGranted).ThenByDescending(p => p.ItemStackQuantity).FirstOrDefault(); }
			catch (Exception e) { Diagnostic("[**POTION**] Exception occured trying to read potions from pack. Exception: " + e.ToString()); thisPackPotion = null; }
			if (thisPackPotion == null) {
				Log("[**POTION**] Memory read error or you do not have any potions in your backpack. Assuming the 2nd...");
				Log("[**POTION**] Buying a single potion from merchant to initialize BuyPotion logic.");
				ZetaDia.Actors.Me.Inventory.BuyItem(merchantitem.DynamicId);
				DateTime _lastCheck = DateTime.Now;
				
				int _numChecks = 0;
				while (true) {
					if (_numChecks >= 10) {
						Log("[**POTION**] Its been ~10 seconds since we tried purchasing a potion and it still is not reading from our backpack.");
						break;
					}
					if (DateTime.Now.Subtract(_lastCheck).TotalSeconds > 1) {
						try {
							ACDItem tempACDItem = ZetaDia.Me.Inventory.Backpack.Where(i => (i.IsPotion) && (i.RequiredLevel <= ZetaDia.Me.Level)).OrderByDescending(p => p.ItemStackQuantity).FirstOrDefault();
							if (tempACDItem == null) {
								Diagnostic("[**POTION**] Memory read error or Still can't see the potion in our backpack... **Sigh**");
							} else {
								Diagnostic("[**POTION**] Backpack finally updated. We can now move forward with buying potions.");
								thisPackPotion = tempACDItem;
								break;
							}
						}
						catch (Exception e) {
							Diagnostic("[**POTION**] An exception occured trying to initialize BuyPotion logic. Exception: " + e.ToString());
						}
						ZetaDia.Actors.Update();
						_lastCheck = DateTime.Now;
						_numChecks++;
					}
				}
				if (_numChecks >= 10) {
					Log(String.Format("[**POTION**] An error occured trying to purchase potion '{0}'", merchantitem.Name));
					return false;
				}
			}
			int totalPackPotions = ZetaDia.Me.Inventory.Backpack.Where(i => (i.IsPotion) && (i.HitpointsGranted == thisPackPotion.HitpointsGranted)).Sum(p => p.ItemStackQuantity);
			Diagnostic(String.Format("[**POTION**] Backpack potion found... Name: {0}, ActorSNO: {1}, GUID: {2}, DynamicId: {3}, IsPotion: {4}, maxStackCount: {5}, RequiredLevel: {6}",
				thisPackPotion.Name, thisPackPotion.ActorSNO, thisPackPotion.ACDGuid, thisPackPotion.DynamicId, thisPackPotion.IsPotion, thisPackPotion.MaxStackCount, thisPackPotion.RequiredLevel));
			Diagnostic(String.Format("[**POTION**] MerchantPotion: '{0}', BackPackPotion: {1} ({2})", merchantitem.Name, thisPackPotion.Name, totalPackPotions));

			switch (merchantitem.Level) {
				case 1:  // 1-5, Minor Health Potion
					_iPotionsBuy = (int)iQtyPotion1; break;
				case 6: // 6-10, Lesser Health Potion
					_iPotionsBuy = (int)iQtyPotion2; break;
				case 11: // 11-15, Health Potion
					_iPotionsBuy = (int)iQtyPotion3; break;
				case 16: // 16-20, Greater Health Potion
					_iPotionsBuy = (int)iQtyPotion4; break;
				case 21: // 21-25, Major Health Potion
					_iPotionsBuy = (int)iQtyPotion5; break;
				case 26: // 26-36, Super Health Potion
					_iPotionsBuy = (int)iQtyPotion6; break;
				case 37: // 37-46, Heroic Health Potion
					_iPotionsBuy = (int)iQtyPotion7; break;
				case 47: // 47-52, Resplendent Health Potion
					_iPotionsBuy = (int)iQtyPotion8; break;
				case 53: // 53-57, Runic Health Potion
					_iPotionsBuy = (int)iQtyPotion9; break;
				case 58: // 58-60, Mythic Health Potion
					_iPotionsBuy = (int)iQtyPotion10; break;
				default: // O_o Memory Read Error, Item became Invalid, or Blizz added new potions and I need to add support for them....
					Diagnostic(String.Format("Unknown Item Encountered: Name: {0}, ActorSNO: {1}, DynamicId: {2}, RequiredLevel: {3}",
						merchantitem.Name, merchantitem.ActorSNO, merchantitem.DynamicId, merchantitem.RequiredLevel));
					break;
			}
			Diagnostic(String.Format("[**POTION**] Vendor Price for {0} is {1}g", merchantitem.Name, merchantitem.Gold));

			// TODO: Need to verify all conditions under which we may want or need potions and ensure each condition is covered.
			if (_iPotionsBuy > 0 && (potionList.Where(p => p.Name == merchantitem.Name).FirstOrDefault()).Level == (potionList.Where(p => p.Name == thisPackPotion.Name).FirstOrDefault()).Level) {
				_iPotionsNeeded = (_iPotionsBuy - totalPackPotions);
				
				// No Potions are needed
				if (_iPotionsNeeded == 0) {
					Diagnostic(String.Format("[**POTION**] No more potions are needed. BackPackPotion: {0} ({1}), MerchantPotion: {2} ({3})", thisPackPotion.Name, totalPackPotions, merchantitem.Name, _iPotionsBuy));
					_lastBuy = DateTime.Now;
					return false;
				}
			} else if ((potionList.Where(p => p.Name == merchantitem.Name).FirstOrDefault()).Level > (potionList.Where(p => p.Name == thisPackPotion.Name).FirstOrDefault()).Level || merchantitem.HitpointsGranted >= (ZetaDia.Actors.Me.HitpointsMax * .25)) {
				Diagnostic("[**POTION**] Either a new and improved potion is now available or something is better than nothing");
				Diagnostic(String.Format("[**POTION**] MerchantPotion:  Name: {0}, Level: {1}, HitpointsGranted: {2}", merchantitem.Name, merchantitem.RequiredLevel, merchantitem.HitpointsGranted));
				_iPotionsNeeded = _iPotionsBuy;
			} else {
				Diagnostic("[**POTION**] O_o What condition actually lead the method this far along.... ");
				Diagnostic(String.Format("[**POTION**] MerchantPotion=  Name: {0}, RequiredLevel: {1}, Needed: {2} / BackPackPotion= Name: {3}, RequiredLevel: {4}, Qty: {5}", merchantitem.Name, merchantitem.RequiredLevel, _iPotionsNeeded, thisPackPotion.Name, thisPackPotion.RequiredLevel, totalPackPotions));
				_lastBuy = DateTime.Now;
				return false;
			}
			
			for(int count = 1; count <= _iPotionsNeeded; count++) {
				// If we run away while purchasing potions this can cause DB/D3 to crash and burn....
				if (!Zeta.Internals.UIElements.VendorWindow.IsVisible) {
					Log("[**POTION**] UIElement VendorWindow is not open, breaking BuyPotion routine...");
					break;
				}
				// Check Coinage
				if ( ZetaDia.Me.Inventory.Coinage <= merchantitem.Gold) {
					Log(String.Format("[**POTION**] {0}'s cost {1}, Your current coinage reads {2}. You Know Your Broke When... You have to save up to be poor.", merchantitem.Name, merchantitem.Gold, ZetaDia.Me.Inventory.Coinage));
					break;
				}
				if (DateTime.Now.Subtract(_lastBuy).TotalMilliseconds > getRandNumber(200, 400)) {
					Log(String.Format("[**POTION**] Buying '{0}' ({1}/{2})", merchantitem.Name, count.ToString(), _iPotionsNeeded.ToString()));
					ZetaDia.Actors.Me.Inventory.BuyItem(merchantitem.DynamicId);
					_lastBuy = DateTime.Now;
				} else {
					// We dont want to count this loop
					count--;
				}
			}
			return false;
		}
	}
}