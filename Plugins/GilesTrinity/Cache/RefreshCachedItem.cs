﻿using GilesTrinity.DbProvider;
using GilesTrinity.Technicals;
using System;
using System.IO;
using System.Linq;
using Zeta;
using Zeta.Common;
using Zeta.Common.Plugins;
using Zeta.CommonBot;
using Zeta.Internals.Actors;
using Zeta.Internals.Actors.Gizmos;
using Zeta.Internals.SNO;
using System.Text;
using GilesTrinity.Cache;
using GilesTrinity.Settings.Combat;
using Zeta.Navigation;

namespace GilesTrinity
{
    public partial class GilesTrinity
    {
        private static bool RefreshGilesItem()
        {
            bool logNewItem = false;
            bool AddToCache = false;

            if (c_BalanceID == -1)
            {
                AddToCache = false;
                c_IgnoreSubStep = "InvalidBalanceID";
            }

            DiaItem item = c_diaObject as DiaItem;
            c_ItemQuality = item.CommonData.ItemQualityLevel;

            // Ignore it if it's not in range yet - allow legendary items to have 15 feet extra beyond our profile max loot radius
            float fExtraRange = 0f;


            if (iKeepLootRadiusExtendedFor > 0)
                fExtraRange = 90f;

            if (c_ItemQuality >= ItemQuality.Rare4)
                fExtraRange = CurrentBotLootRange;

            if (c_ItemQuality >= ItemQuality.Legendary)
            {
                // always pickup
                AddToCache = true;
            }

            if (c_CentreDistance > (CurrentBotLootRange + fExtraRange) && c_ItemQuality <= ItemQuality.Legendary)
            {
                c_IgnoreSubStep = "OutOfRange";
                AddToCache = false;
                // return here to save CPU on reading unncessary attributes for out of range items;
                if (!AddToCache)
                    return AddToCache;
            }

            c_ItemDisplayName = item.CommonData.Name;
            c_GameBalanceID = item.CommonData.GameBalanceId;
            c_ItemLevel = item.CommonData.Level;
            c_DBItemBaseType = item.CommonData.ItemBaseType;
            c_DBItemType = item.CommonData.ItemType;
            c_IsOneHandedItem = item.CommonData.IsOneHand;
            c_IsTwoHandedItem = item.CommonData.IsTwoHand;
            c_item_tFollowerType = item.CommonData.FollowerSpecialType;

            PickupItem pickupItem = new PickupItem()
            {
                Name = c_ItemDisplayName,
                InternalName = c_InternalName,
                Level = c_ItemLevel,
                Quality = c_ItemQuality,
                BalanceID = c_BalanceID,
                DBBaseType = c_DBItemBaseType,
                DBItemType = c_DBItemType,
                IsOneHand = c_IsOneHandedItem,
                IsTwoHand = c_IsTwoHandedItem,
                ItemFollowerType = c_item_tFollowerType,
                DynamicID = c_GameDynamicID,
                Position = c_Position,
                ActorSNO = c_ActorSNO
            };

            // Calculate custom Giles item type
            c_item_GItemType = DetermineItemType(c_InternalName, c_DBItemType, c_item_tFollowerType);

            // And temporarily store the base type
            GItemBaseType itemBaseType = DetermineBaseType(c_item_GItemType);

            // Treat all globes as a yes
            if (c_item_GItemType == GItemType.HealthGlobe)
            {
                c_ObjectType = GObjectType.Globe;
                // Create or alter this cached object type
                dictGilesObjectTypeCache[c_RActorGuid] = c_ObjectType;
                AddToCache = true;
            }

            // Item stats
            logNewItem = RefreshItemStats(itemBaseType);

            // Get whether or not we want this item, cached if possible
            if (!dictGilesPickupItem.TryGetValue(c_RActorGuid, out AddToCache))
            {
                if (Settings.Loot.ItemFilterMode == global::GilesTrinity.Settings.Loot.ItemFilterMode.DemonBuddy)
                {
                    AddToCache = ItemManager.Current.ShouldPickUpItem((ACDItem)c_CommonData);
                }
                else if (Settings.Loot.ItemFilterMode == global::GilesTrinity.Settings.Loot.ItemFilterMode.TrinityWithItemRules)
                {
                    AddToCache = ItemRulesPickupValidation(pickupItem);
                }
                else
                {
                    AddToCache = GilesPickupItemValidation(pickupItem);
                }

                dictGilesPickupItem.Add(c_RActorGuid, AddToCache);
            }

            // Using DB built-in item rules
            if (AddToCache && ForceVendorRunASAP)
                c_IgnoreSubStep = "ForcedVendoring";

            if (!AddToCache)
            {
                // Check if there's a monster intersecting the path-line to this item
                AddToCache = MosterObstacleInPathCacheObject(AddToCache);
            }

            // Didn't pass pickup rules, so ignore it
            if (!AddToCache && c_IgnoreSubStep == String.Empty)
                c_IgnoreSubStep = "NoMatchingRule";

            if (Settings.Advanced.LogDroppedItems && logNewItem && c_DBItemType != ItemType.Unknown)
                LogDroppedItem();

            return AddToCache;
        }
        private static void LogDroppedItem()
        {
            string droppedItemLogPath = Path.Combine(FileManager.TrinityLogsPath, String.Format("ItemsDropped.csv"));

            bool pickupItem = false;
            dictGilesPickupItem.TryGetValue(c_RActorGuid, out pickupItem);

            bool writeHeader = !File.Exists(droppedItemLogPath);
            using (StreamWriter LogWriter = new StreamWriter(droppedItemLogPath, true))
            {
                if (writeHeader)
                {
                    LogWriter.WriteLine("Timestamp,ActorSNO,RActorGUID,DyanmicID,GameBalanceID,ACDGuid,Name,InternalName,DBBaseType,TBaseType,DBItemType,TItemType,Quality,Level,IgnoreItemSubStep,Distance,Pickup,SHA1Hash");
                }
                LogWriter.Write(FormatCSVField(DateTime.Now));
                LogWriter.Write(FormatCSVField(c_ActorSNO));
                LogWriter.Write(FormatCSVField(c_RActorGuid));
                LogWriter.Write(FormatCSVField(c_GameDynamicID));
                // GameBalanceID
                LogWriter.Write(FormatCSVField(c_GameBalanceID));
                LogWriter.Write(FormatCSVField(c_ACDGUID));
                LogWriter.Write(FormatCSVField(c_ItemDisplayName));
                LogWriter.Write(FormatCSVField(c_InternalName));
                LogWriter.Write(FormatCSVField(c_DBItemBaseType.ToString()));
                LogWriter.Write(FormatCSVField(DetermineBaseType(c_item_GItemType).ToString()));
                LogWriter.Write(FormatCSVField(c_DBItemType.ToString()));
                LogWriter.Write(FormatCSVField(c_item_GItemType.ToString()));
                LogWriter.Write(FormatCSVField(c_ItemQuality.ToString()));
                LogWriter.Write(FormatCSVField(c_ItemLevel));
                LogWriter.Write(FormatCSVField(c_IgnoreSubStep));
                LogWriter.Write(FormatCSVField(c_CentreDistance));
                LogWriter.Write(FormatCSVField(pickupItem));
                LogWriter.Write(FormatCSVField(c_ItemMd5Hash));
                LogWriter.Write("\n");
            }

        }
        private static bool RefreshGilesGold(bool AddToCache)
        {
            //int rangedMinimumStackSize = 0;
            AddToCache = true;

            // Get the gold amount of this pile, cached if possible
            if (!dictGilesGoldAmountCache.TryGetValue(c_RActorGuid, out c_GoldStackSize))
            {
                try
                {
                    c_GoldStackSize = ((ACDItem)c_CommonData).Gold;
                }
                catch
                {
                    DbHelper.Log(TrinityLogLevel.Debug, LogCategory.CacheManagement, "Safely handled exception getting gold pile amount for item {0} [{1}]", c_InternalName, c_ActorSNO);
                    AddToCache = false;
                    c_IgnoreSubStep = "GetAttributeException";
                }
                dictGilesGoldAmountCache.Add(c_RActorGuid, c_GoldStackSize);
            }

            if (c_GoldStackSize < Settings.Loot.Pickup.MinimumGoldStack)
            {
                AddToCache = false;
                c_IgnoreSubStep = "NotEnoughGold";
                return AddToCache;
            }

            if (c_CentreDistance <= PlayerStatus.GoldPickupRadius)
            {
                AddToCache = false;
                c_IgnoreSubStep = "WithinPickupRadius";
                return AddToCache;
            }

            //if (!AddToCache)
            //    LogSkippedGold();

            //DbHelper.Log(TrinityLogLevel.Debug, LogCategory.CacheManagement, "Gold Stack {0} has iPercentage {1} with rangeMinimumStackSize: {2} Distance: {3} MininumGoldStack: {4} PickupRadius: {5} AddToCache: {6}",
            //    c_GoldStackSize, iPercentage, rangedMinimumStackSize, c_CentreDistance, Settings.Loot.Pickup.MinimumGoldStack, ZetaDia.Me.GoldPickUpRadius, AddToCache);

            return AddToCache;
        }

        private static void LogSkippedGold()
        {
            string skippedItemsPath = Path.Combine(FileManager.LoggingPath, String.Format("SkippedGoldStacks_{0}_{1}.csv", PlayerStatus.ActorClass, DateTime.Now.ToString("yyyy-MM-dd")));

            bool writeHeader = !File.Exists(skippedItemsPath);
            using (StreamWriter LogWriter = new StreamWriter(skippedItemsPath, true))
            {
                if (writeHeader)
                {
                    LogWriter.WriteLine("ActorSNO,RActorGUID,DyanmicID,ACDGuid,Name,GoldStackSize,IgnoreItemSubStep,Distance");
                }
                LogWriter.Write(FormatCSVField(c_ActorSNO));
                LogWriter.Write(FormatCSVField(c_RActorGuid));
                LogWriter.Write(FormatCSVField(c_GameDynamicID));
                LogWriter.Write(FormatCSVField(c_ACDGUID));
                LogWriter.Write(FormatCSVField(c_InternalName));
                LogWriter.Write(FormatCSVField(c_GoldStackSize));
                LogWriter.Write(FormatCSVField(c_IgnoreSubStep));
                LogWriter.Write(FormatCSVField(c_CentreDistance));
                LogWriter.Write("\n");
            }

        }

        private static string FormatCSVField(DateTime time)
        {
            return String.Format("\"{0:yyyy-MM-ddTHH:mm:ss.ffffzzz}\",", time);
        }

        private static string FormatCSVField(string text)
        {
            return String.Format("\"{0}\",", text);
        }
        private static string FormatCSVField(int number)
        {
            return String.Format("\"{0}\",", number);
        }
        private static string FormatCSVField(double number)
        {
            return String.Format("\"{0:0}\",", number);
        }
        private static string FormatCSVField(bool value)
        {
            return String.Format("\"{0}\",", value);
        }
    }
}
