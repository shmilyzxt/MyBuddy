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
        private void CheckBackpack()
        {	

            // Call a Zeta update
            ZetaDia.Actors.Update();

            // Exit if we have null data
            if (ZetaDia.Actors.Me == null)
                return;

            // Exit if our player level is already at 60 - don't want to mess with expert player backpacks and lose items!
            if (bDisable60 && (ZetaDia.Actors.Me.Level < 1 || ZetaDia.Actors.Me.Level > 59))
                return;

            // See if we need to refresh all our equipment points
            if (bNeedFullItemUpdate)
            {
                iCurrentDamage = GearCheck.instance.GetDamage();
                iCurrentEHP = GearCheck.instance.GetEHP();
                Diagnostic("[**PACK**] Current points = " + getValue(iCurrentDamage, iCurrentEHP).ToString());
                bNeedFullItemUpdate = false;
                _alreadyLookedAtBlacklist.Clear();
            } // Do a full equipped items update?

            // Loop through anything in the backpack that we haven't already checked
            foreach (ACDItem thisitem in ZetaDia.Actors.Me.Inventory.Backpack)
            {
                if (thisitem.BaseAddress == IntPtr.Zero)
                {
                    return;
                }
				
                // Check this item is of the necessary item level (if not don't blacklist it until it is!)
                if (thisitem.RequiredLevel <= currentLevel && !thisitem.IsUnidentified)
                {
                    // Make sure we haven't already analysed this item previously
                    if (!_alreadyLookedAtBlacklist.Contains(thisitem))
                    {					
                        // Prevent this item ever being looked at again
                        _alreadyLookedAtBlacklist.Add(thisitem);
						Diagnostic(String.Format("[**PACK**] Evaluating item '{0}'", thisitem.Name));
                        Dictionary<GearCheck.RatoshInventorySlot, ACDItem> currentEquipped = GearCheck.instance.getPlayerEquipped();
                        Dictionary<GearCheck.RatoshInventorySlot, double> replaceDamage = GearCheck.instance.EvaluateDamage(thisitem, currentEquipped);
                        Dictionary<GearCheck.RatoshInventorySlot, double> replaceEHP = GearCheck.instance.EvaluateEHP(thisitem, currentEquipped);
                        GearCheck.RatoshInventorySlot bestReplacement = GearCheck.RatoshInventorySlot.Unknown;
                        GearCheck.RatoshInventorySlot bestReplacement2 = GearCheck.RatoshInventorySlot.Unknown;
                        ACDItem bestReplacement2Equip = null;
                        double tempBestDamage = 0;
                        double tempBestEHP = 0;

                        foreach (GearCheck.RatoshInventorySlot key in replaceDamage.Keys)
                        {
                            double tempDamage;
                            double tempEHP;
                            if (replaceDamage.TryGetValue(key, out tempDamage) && replaceEHP.TryGetValue(key, out tempEHP))
                            {
                                if (getValue(tempBestDamage, tempBestEHP) < getValue(tempDamage, tempEHP))
                                {
                                    bestReplacement = key;
                                    tempBestDamage = tempDamage;
                                    tempBestEHP = tempEHP;
                                }
                            }
                        }
                        if (bestReplacement != GearCheck.RatoshInventorySlot.Unknown)
                        {
                            if (((bestReplacement == GearCheck.RatoshInventorySlot.PlayerRightHand || bestReplacement == GearCheck.RatoshInventorySlot.PlayerLeftHand) &&
                                currentEquipped.ContainsKey(GearCheck.RatoshInventorySlot.PlayerTwoHand)) ||
                                (bestReplacement == GearCheck.RatoshInventorySlot.PlayerTwoHand || bestReplacement == GearCheck.RatoshInventorySlot.PlayerRightHandSpecial))
                            {
                                Dictionary<GearCheck.RatoshInventorySlot, ACDItem> currentEquipped2 = GearCheck.instance.fakeEquip(currentEquipped, bestReplacement, thisitem);
                                foreach (ACDItem otherItem in _alreadyLookedAtBlacklist)
                                {
                                    Dictionary<GearCheck.RatoshInventorySlot, double> replaceDamage2 = GearCheck.instance.EvaluateDamage(otherItem, currentEquipped2);
                                    Dictionary<GearCheck.RatoshInventorySlot, double> replaceEHP2 = GearCheck.instance.EvaluateEHP(otherItem, currentEquipped2);

                                    foreach (GearCheck.RatoshInventorySlot key in replaceDamage.Keys)
                                    {
                                        double tempDamage2;
                                        double tempEHP2;
                                        if (bestReplacement == GearCheck.RatoshInventorySlot.PlayerRightHandSpecial)
                                        {
                                            if (key != GearCheck.RatoshInventorySlot.PlayerLeftHand || key != GearCheck.RatoshInventorySlot.PlayerTwoHand)
                                            {
                                                continue;
                                            }
                                        }
                                        if (bestReplacement == GearCheck.RatoshInventorySlot.PlayerTwoHand)
                                        {
                                            if (key != GearCheck.RatoshInventorySlot.PlayerRightHandSpecial)
                                            {
                                                continue;
                                            }
                                        }
                                        if (bestReplacement == GearCheck.RatoshInventorySlot.PlayerRightHand)
                                        {
                                            if (key != GearCheck.RatoshInventorySlot.PlayerLeftHand)
                                            {
                                                continue;
                                            }
                                        }
                                        if (bestReplacement == GearCheck.RatoshInventorySlot.PlayerLeftHand)
                                        {
                                            if (key != GearCheck.RatoshInventorySlot.PlayerRightHand || key != GearCheck.RatoshInventorySlot.PlayerRightHandSpecial)
                                            {
                                                continue;
                                            }
                                        }
                                        if (replaceDamage2.TryGetValue(key, out tempDamage2) && replaceEHP2.TryGetValue(key, out tempEHP2))
                                        {
                                            Diagnostic(String.Format("[**PACK**] Eval points {0}", getValue(tempDamage2, tempEHP2)));
                                            if (getValue(tempBestDamage, tempBestEHP) < getValue(tempDamage2, tempEHP2))
                                            {
                                                bestReplacement2Equip = otherItem;
                                                bestReplacement2 = key;
                                                tempBestDamage = tempDamage2;
                                                tempBestEHP = tempEHP2;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        Log(String.Format("[**PACK**] Evaluated item '{0}'[{1}] (points={2})", thisitem.Name, bestReplacement.ToString(), getValue(tempBestDamage, tempBestEHP).ToString()));
                        if (getValue(iCurrentDamage, iCurrentEHP) < getValue(tempBestDamage, tempBestEHP))
                        {
							if (InventorySlotProtected(bestReplacement.ToString()))
							{
								Log(String.Format("[**PACK**] InvSlot: '{0}' is Protected. Can not equip item '{1}'.", bestReplacement, thisitem.Name));
								return;
							}
							if (InventorySlotProtected(bestReplacement2.ToString()))
							{
								Log(String.Format("[**PACK**] InvSlot: '{0}' is Protected. Can not equip item '{1}'.", bestReplacement2, thisitem.Name));
								return;
							}
							if (!Zeta.Internals.UIElements.InventoryWindow.IsVisible)
							{
								Diagnostic("[**PACK**] Opening UIElement: Inventory");
								Zeta.Internals.UIElements.BackgroundScreenPCButtonInventory.Click();
							}
							Log(String.Format("[**PACK**] Equip item '{0}' (old points={1}, new points={2})", thisitem.Name, getValue(iCurrentDamage, iCurrentEHP).ToString(), getValue(tempBestDamage, tempBestEHP).ToString()));
                            iCurrentDamage = tempBestDamage;
                            iCurrentEHP = tempBestEHP;
                            GearCheck.instance.equip(bestReplacement, thisitem);
                            if (bestReplacement2 != GearCheck.RatoshInventorySlot.Unknown)
                            {
                                GearCheck.instance.equip(bestReplacement2, bestReplacement2Equip);
                            }
							if (Zeta.Internals.UIElements.InventoryWindow.IsVisible)
							{
								Diagnostic("[**PACK**] Closing UIElement: Inventory");
								Zeta.Internals.UIElements.BackgroundScreenPCButtonInventory.Click();
							}
                        }
                    }
                }
            }

			if (bIdentifyItems)
			{
				// Look for unidentified items if not in combat and not already identifying
				if (CombatTargeting.Instance.FirstNpc != null)
					return;
				if (ZetaDia.Me.LoopingAnimationEndTime != 0)
					return;
				if (GetUnidentifiedItems().Count() > 0)
				{
					Log("[**PACK**] Pausing bot while we identify items");
					BotMain.PauseWhile(identifyItems, 0, GetBotPauseTimeSpan());
				}
				else if (DateTime.Now.Subtract(lastIdentify).TotalMilliseconds < 5000 && Zeta.Internals.UIElements.InventoryWindow.IsVisible)
				{
					Zeta.Internals.UIElements.BackgroundScreenPCButtonInventory.Click();
					Log("[**PACK**] Closing Inventory Window");
				}
			}
			
			return;
        } // CheckBackpack
		
	}
}