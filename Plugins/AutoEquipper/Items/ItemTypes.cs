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
        // *****    GilesItemType and GilesBaseItemType - for reliable methods of handling items    *****
        public enum GilesItemType
        {
            Unknown,
            Axe,
            CeremonialKnife,
            HandCrossbow,
            Dagger,
            FistWeapon,
            Mace,
            MightyWeapon,
            Spear,
            Sword,
            Wand,
            TwoHandAxe,
            TwoHandBow,
            TwoHandDaibo,
            TwoHandCrossbow,
            TwoHandMace,
            TwoHandMighty,
            TwoHandPolearm,
            TwoHandStaff,
            TwoHandSword,
            StaffOfHerding,
            Mojo,
            Source,
            Quiver,
            Shield,
            Amulet,
            Ring,
            Belt,
            Boots,
            Bracers,
            Chest,
            Cloak,
            Gloves,
            Helm,
            Pants,
            MightyBelt,
            Shoulders,
            SpiritStone,
            VoodooMask,
            WizardHat,
            FollowerEnchantress,
            FollowerScoundrel,
            FollowerTemplar,
            CraftingMaterial,
            LoreBook,
            Ruby,
            Emerald,
            Topaz,
            Amethyst,
            SpecialItem,
            CraftingPlan,
            HealthPotion,
            Dye,
            HealthGlobe
        }
        public enum GilesBaseItemType
        {
            Unknown,
            WeaponOneHand,
            WeaponTwoHand,
            WeaponRange,
            Offhand,
            Armor,
            Jewelry,
            FollowerItem,
            Misc,
            Gem,
			Potion,
            HealthGlobe
        }
        
        // ***** DetermineItemType - Calculates what kind of item it is from D3 internalnames       *****
        private GilesItemType DetermineItemType(string sThisInternalName, ItemType DBItemType, bool DBOneHanded, DyeType DBDyeType)
        {
            sThisInternalName = sThisInternalName.ToLower();
            if (sThisInternalName.StartsWith("axe_")) return GilesItemType.Axe;
            if (sThisInternalName.StartsWith("ceremonialdagger_")) return GilesItemType.CeremonialKnife;
            if (sThisInternalName.StartsWith("handxbow_")) return GilesItemType.HandCrossbow;
            if (sThisInternalName.StartsWith("dagger_")) return GilesItemType.Dagger;
            if (sThisInternalName.StartsWith("fistweapon_")) return GilesItemType.FistWeapon;
            if (sThisInternalName.StartsWith("mace_")) return GilesItemType.Mace;
            if (sThisInternalName.StartsWith("mightyweapon_1h_")) return GilesItemType.MightyWeapon;
            if (sThisInternalName.StartsWith("spear_")) return GilesItemType.Spear;
            if (sThisInternalName.StartsWith("sword_")) return GilesItemType.Sword;
            if (sThisInternalName.StartsWith("wand_")) return GilesItemType.Wand;
            if (sThisInternalName.StartsWith("twohandedaxe_")) return GilesItemType.TwoHandAxe;
            if (sThisInternalName.StartsWith("bow_")) return GilesItemType.TwoHandBow;
            if (sThisInternalName.StartsWith("combatstaff_")) return GilesItemType.TwoHandDaibo;
            if (sThisInternalName.StartsWith("xbow_")) return GilesItemType.TwoHandCrossbow;
            if (sThisInternalName.StartsWith("twohandedmace_")) return GilesItemType.TwoHandMace;
            if (sThisInternalName.StartsWith("mightyweapon_2h_")) return GilesItemType.TwoHandMighty;
            if (sThisInternalName.StartsWith("polearm_")) return GilesItemType.TwoHandPolearm;
            if (sThisInternalName.StartsWith("staff_")) return GilesItemType.TwoHandStaff;
            if (sThisInternalName.StartsWith("twohandedsword_")) return GilesItemType.TwoHandSword;
            if (sThisInternalName.StartsWith("staffofcow")) return GilesItemType.StaffOfHerding;
            if (sThisInternalName.StartsWith("mojo_")) return GilesItemType.Mojo;
            if (sThisInternalName.StartsWith("orb_")) return GilesItemType.Source;
            if (sThisInternalName.StartsWith("quiver_")) return GilesItemType.Quiver;
            if (sThisInternalName.StartsWith("shield_")) return GilesItemType.Shield;
            if (sThisInternalName.StartsWith("amulet_")) return GilesItemType.Amulet;
            if (sThisInternalName.StartsWith("ring_")) return GilesItemType.Ring;
            if (sThisInternalName.StartsWith("boots_")) return GilesItemType.Boots;
            if (sThisInternalName.StartsWith("bracers_")) return GilesItemType.Bracers;
            if (sThisInternalName.StartsWith("cloak_")) return GilesItemType.Cloak;
            if (sThisInternalName.StartsWith("gloves_")) return GilesItemType.Gloves;
            if (sThisInternalName.StartsWith("pants_")) return GilesItemType.Pants;
            if (sThisInternalName.StartsWith("barbbelt_")) return GilesItemType.MightyBelt;
            if (sThisInternalName.StartsWith("shoulderpads_")) return GilesItemType.Shoulders;
            if (sThisInternalName.StartsWith("spiritstone_")) return GilesItemType.SpiritStone;
            if (sThisInternalName.StartsWith("voodoomask_")) return GilesItemType.VoodooMask;
            if (sThisInternalName.StartsWith("wizardhat_")) return GilesItemType.WizardHat;
            if (sThisInternalName.StartsWith("lore_book_")) return GilesItemType.LoreBook;
            if (sThisInternalName.StartsWith("ruby_")) return GilesItemType.Ruby;
            if (sThisInternalName.StartsWith("emerald_")) return GilesItemType.Emerald;
            if (sThisInternalName.StartsWith("topaz_")) return GilesItemType.Topaz;
            if (sThisInternalName.StartsWith("amethyst")) return GilesItemType.Amethyst;
            if (sThisInternalName.StartsWith("healthpotion_")) return GilesItemType.HealthPotion;
            if (sThisInternalName.StartsWith("followeritem_enchantress_")) return GilesItemType.FollowerEnchantress;
            if (sThisInternalName.StartsWith("followeritem_scoundrel_")) return GilesItemType.FollowerScoundrel;
            if (sThisInternalName.StartsWith("followeritem_templar_")) return GilesItemType.FollowerTemplar;
            if (sThisInternalName.StartsWith("craftingplan_")) return GilesItemType.CraftingPlan;
            if (sThisInternalName.StartsWith("dye_")) return GilesItemType.Dye;
            if (sThisInternalName.StartsWith("a1_")) return GilesItemType.SpecialItem;
            if (sThisInternalName.StartsWith("healthglobe")) return GilesItemType.HealthGlobe;
            // DB doesn't yet have support for all three follower type recognition, treating everything as enchantress for now
            if (sThisInternalName.StartsWith("jewelbox_") && DBItemType == ItemType.FollowerSpecial) return GilesItemType.FollowerEnchantress;

            // Fall back on some DB item type checking 
            if (sThisInternalName.StartsWith("crafting_"))
            {
                if (DBItemType == ItemType.CraftingPage) return GilesItemType.LoreBook;
                return GilesItemType.CraftingMaterial;
            }

            if (sThisInternalName.StartsWith("chestarmor_"))
            {
                if (DBItemType == ItemType.Cloak) return GilesItemType.Cloak;
                return GilesItemType.Chest;
            }
            if (sThisInternalName.StartsWith("helm_"))
            {
                if (DBItemType == ItemType.SpiritStone) return GilesItemType.SpiritStone;
                if (DBItemType == ItemType.VoodooMask) return GilesItemType.VoodooMask;
                if (DBItemType == ItemType.WizardHat) return GilesItemType.WizardHat;
                return GilesItemType.Helm;
            }
            if (sThisInternalName.StartsWith("helmcloth_"))
            {
                if (DBItemType == ItemType.SpiritStone) return GilesItemType.SpiritStone;
                if (DBItemType == ItemType.VoodooMask) return GilesItemType.VoodooMask;
                if (DBItemType == ItemType.WizardHat) return GilesItemType.WizardHat;
                return GilesItemType.Helm;
            }
            if (sThisInternalName.StartsWith("belt_"))
            {
                if (DBItemType == ItemType.MightyBelt) return GilesItemType.MightyBelt;
                return GilesItemType.Belt;
            }

            return GilesItemType.Unknown;
        }

        // *****      DetermineBaseType - Calculates a more generic, "basic" type of item           *****
        private GilesBaseItemType DetermineBaseType(GilesItemType thisGilesItemType)
        {
            GilesBaseItemType thisGilesBaseType = GilesBaseItemType.Unknown;
            if (thisGilesItemType == GilesItemType.Axe || thisGilesItemType == GilesItemType.CeremonialKnife || thisGilesItemType == GilesItemType.Dagger ||
                thisGilesItemType == GilesItemType.FistWeapon || thisGilesItemType == GilesItemType.Mace || thisGilesItemType == GilesItemType.MightyWeapon ||
                thisGilesItemType == GilesItemType.Spear || thisGilesItemType == GilesItemType.Sword || thisGilesItemType == GilesItemType.Wand)
            {
                thisGilesBaseType = GilesBaseItemType.WeaponOneHand;
            }
            else if (thisGilesItemType == GilesItemType.TwoHandDaibo || thisGilesItemType == GilesItemType.TwoHandMace ||
                thisGilesItemType == GilesItemType.TwoHandMighty || thisGilesItemType == GilesItemType.TwoHandPolearm || thisGilesItemType == GilesItemType.TwoHandStaff ||
                thisGilesItemType == GilesItemType.TwoHandSword || thisGilesItemType == GilesItemType.TwoHandAxe)
            {
                thisGilesBaseType = GilesBaseItemType.WeaponTwoHand;
            }
            else if (thisGilesItemType == GilesItemType.TwoHandCrossbow || thisGilesItemType == GilesItemType.HandCrossbow || thisGilesItemType == GilesItemType.TwoHandBow)
            {
                thisGilesBaseType = GilesBaseItemType.WeaponRange;
            }
            else if (thisGilesItemType == GilesItemType.Mojo || thisGilesItemType == GilesItemType.Source ||
                thisGilesItemType == GilesItemType.Quiver || thisGilesItemType == GilesItemType.Shield)
            {
                thisGilesBaseType = GilesBaseItemType.Offhand;
            }
            else if (thisGilesItemType == GilesItemType.Boots || thisGilesItemType == GilesItemType.Bracers || thisGilesItemType == GilesItemType.Chest ||
                thisGilesItemType == GilesItemType.Cloak || thisGilesItemType == GilesItemType.Gloves || thisGilesItemType == GilesItemType.Helm ||
                thisGilesItemType == GilesItemType.Pants || thisGilesItemType == GilesItemType.Shoulders || thisGilesItemType == GilesItemType.SpiritStone ||
                thisGilesItemType == GilesItemType.VoodooMask || thisGilesItemType == GilesItemType.WizardHat || thisGilesItemType == GilesItemType.Belt ||
                thisGilesItemType == GilesItemType.MightyBelt)
            {
                thisGilesBaseType = GilesBaseItemType.Armor;
            }
            else if (thisGilesItemType == GilesItemType.Amulet || thisGilesItemType == GilesItemType.Ring)
            {
                thisGilesBaseType = GilesBaseItemType.Jewelry;
            }
            else if (thisGilesItemType == GilesItemType.FollowerEnchantress || thisGilesItemType == GilesItemType.FollowerScoundrel ||
                thisGilesItemType == GilesItemType.FollowerTemplar)
            {
                thisGilesBaseType = GilesBaseItemType.FollowerItem;
            }
            else if (thisGilesItemType == GilesItemType.CraftingMaterial || thisGilesItemType == GilesItemType.LoreBook ||
                thisGilesItemType == GilesItemType.SpecialItem || thisGilesItemType == GilesItemType.CraftingPlan ||
                thisGilesItemType == GilesItemType.Dye || thisGilesItemType == GilesItemType.StaffOfHerding)
            {
                thisGilesBaseType = GilesBaseItemType.Misc;
            }
			else if (thisGilesItemType == GilesItemType.HealthPotion)
			{
				thisGilesBaseType = GilesBaseItemType.Potion;
			}
            else if (thisGilesItemType == GilesItemType.Ruby || thisGilesItemType == GilesItemType.Emerald || thisGilesItemType == GilesItemType.Topaz ||
                thisGilesItemType == GilesItemType.Amethyst)
            {
                thisGilesBaseType = GilesBaseItemType.Gem;
            }
            else if (thisGilesItemType == GilesItemType.HealthGlobe)
            {
                thisGilesBaseType = GilesBaseItemType.HealthGlobe;
            }
            return thisGilesBaseType;
        }
        
	}
}