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

        private static IEnumerable<ACDItem> GetUnidentifiedItems()
        {
            return from i in ZetaDia.Actors.Me.Inventory.Backpack
                   where i.BaseAddress != IntPtr.Zero
                   && i.IsUnidentified
                   && !_alreadyLookedAtBlacklist.Contains(i)
                   select i;
        }

        DateTime lastIdentify = DateTime.MinValue;
        bool lastItemWasLegendary = false;
        private double healthOnLastIdentify = 100;
        /// <summary>
        /// Called from BotMain.PauseWhile() - returning True will keep the bot paused, False will un-pause the bot
        /// </summary>
        /// <returns></returns>
        private bool identifyItems()
        {
            // Check to make sure we're not being attacked
            if (GetMillisecondsSinceLastIdentify() < 3000 && Me.HitpointsCurrentPct < healthOnLastIdentify)
                return false;

            AnimationState animState = ZetaDia.Me.CommonData.AnimationState;

            // identify cast time for rares is 1 second
            if (DateTime.Now.Subtract(lastIdentify).TotalMilliseconds <= 1800 && !lastItemWasLegendary && IsCasting(animState))
                return true;

            // identify cast time for legendaries is 3 seconds
            if (DateTime.Now.Subtract(lastIdentify).TotalMilliseconds <= 3800 && lastItemWasLegendary && IsCasting(animState))
                return true;

            if (!Zeta.Internals.UIElements.InventoryWindow.IsVisible)
            {
                Zeta.Internals.UIElements.BackgroundScreenPCButtonInventory.Click();
                Log("Opening Inventory Window");
                return true;
            }

            var thisitem = GetUnidentifiedItems().FirstOrDefault();
            if (thisitem != null)
            {
                healthOnLastIdentify = ZetaDia.Me.HitpointsCurrentPct;

                ZetaDia.Me.Inventory.IdentifyItem(thisitem.DynamicId);
                Log("Identifying " + thisitem.Name + " Quality: " + thisitem.ItemQualityLevel + " Level: " + thisitem.Level);

                lastIdentify = DateTime.Now;
                lastItemWasLegendary = thisitem.ItemQualityLevel == ItemQuality.Legendary;

                return true;
            }

            return ((GetUnidentifiedItems().Count() > 0) ? true : false);
        }

        private double GetMillisecondsSinceLastIdentify()
        {
            return DateTime.Now.Subtract(lastIdentify).TotalMilliseconds;
        }

        private static bool IsCasting(AnimationState animState)
        {
            return animState == AnimationState.Casting || animState == AnimationState.Channeling;
        }

        private static TimeSpan GetBotPauseTimeSpan()
        {
            return new TimeSpan(0, 0, 3, 0);
        }

        private static bool PauseWhileIdentify()
        {
            return ZetaDia.Me.LoopingAnimationEndTime != 0 || Me.IsDead || ZetaDia.IsLoadingWorld || !ZetaDia.IsInGame;
        }
    }
}