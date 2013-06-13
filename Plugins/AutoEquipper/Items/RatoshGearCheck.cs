using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.IO;
using Zeta;
using Zeta.Common;
using Zeta.Common.Plugins;
using Zeta.CommonBot;
using Zeta.CommonBot.Profile;
using Zeta.CommonBot.Profile.Common;
using Zeta.XmlEngine;
using Zeta.TreeSharp;
using Zeta.Internals.Actors;
using System.Windows.Controls;

namespace AutoEquipper
{
    public class GearCheck : IPlugin
    {
        public Version Version { get { return new Version(1, 0, 3); } }
        public string Author { get { return "Ratosh - Mod by WAR"; } }
        public string Description { get { return "Calcula EHP e DPS em sua gear."; } }
        public string Name { get { return "[WAR] GearCheck"; } }
        public bool Equals(IPlugin other) { return (other.Name == Name) && (other.Version == Version); }

        private static DateTime _lastPulse = DateTime.MinValue;

        // Damage factors
        private static int StatusCritPercent = 1;
        private static int StatusCritDamagePercent = 2;
        private static int StatusAttackSpeed = 3;
        private static int StatusMinDamage = 4;
        private static int StatusMaxDamage = 5;
        private static int StatusWeaponMaxDamage = 6;
        private static int StatusWeaponMinDamage = 7;
        private static int StatusWeaponAttacksPerSecond = 8;
        private static int StatusDamageMultiplier = 9;

        // EHP Factors
        private static int StatusArmor = 10;
        private static int StatusResist = 11;
        private static int StatusDextery = 12;
        private static int StatusInteligence = 13;
        private static int StatusStrength = 14;
        private static int StatusLifePercent = 15;
        private static int StatusVitality = 16;
        private static int StatusBlockValue = 17;
        private static int StatusBlockChance = 18;

        // Cache
        //private static Dictionary<String, double> ItemEvaluationCache = new Dictionary<String, double>();
        private static Dictionary<ACDItem, Dictionary<double, double>> ItemEvaluationCache = new Dictionary<ACDItem, Dictionary<double, double>>();

        private int currentLevel;
        private int paragonLevel;
        private ActorClass currentClass;
        public static Dictionary<RatoshInventorySlot, ACDItem> currentEquipped = new Dictionary<RatoshInventorySlot,ACDItem>();
        //public static IEnumerable<ACDItem> pet;

        public static GearCheck instance;

        // ********************************************
        // *********** CONFIG WINDOW REGION ***********
        // ********************************************
        private static string pluginPath = "";
        private static string deathLogFile = "";
        #region configWindow
        private Button closeButton;
        private TextBox damageValue, ehpValue;

        private Window configWindow = null;
        public Window DisplayWindow
        {
            get
            {
                if (!File.Exists(pluginPath + "UI\\RatoshGearCheck.xaml"))
                    Log("ERROR: Can't find \"" + pluginPath + "UI\\RatoshGearCheck.xaml\"");
                try
                {
                    if (configWindow == null)
                    {
                        configWindow = new Window();
                    }
                    StreamReader xamlStream = new StreamReader(pluginPath + "UI\\RatoshGearCheck.xaml");
                    DependencyObject xamlContent = System.Windows.Markup.XamlReader.Load(xamlStream.BaseStream) as DependencyObject;
                    configWindow.Content = xamlContent;

                    damageValue = LogicalTreeHelper.FindLogicalNode(xamlContent, "DamageValue") as TextBox;
                    ehpValue = LogicalTreeHelper.FindLogicalNode(xamlContent, "EHPValue") as TextBox;

                    closeButton = LogicalTreeHelper.FindLogicalNode(xamlContent, "buttonClose") as Button;
                    closeButton.Click += new RoutedEventHandler(buttonClose_Click);

                    UserControl mainControl = LogicalTreeHelper.FindLogicalNode(xamlContent, "GearCheckControl") as UserControl;
                    // Set height and width to main window
                    configWindow.Height = mainControl.Height + 30;
                    configWindow.Width = mainControl.Width;
                    configWindow.Title = "[WAR] Gear check";

                    // On load example
                    configWindow.Loaded += new RoutedEventHandler(configWindow_Loaded);
                    configWindow.Closed += configWindow_Closed;

                    // Add our content to our main window
                    configWindow.Content = xamlContent;
                }
                catch (System.Windows.Markup.XamlParseException ex)
                {
                    // You can get specific error information like LineNumber from the exception
                    Log(ex.ToString());
                }
                catch (Exception ex)
                {
                    // Some other error
                    Log(ex.ToString());
                }
                return configWindow;
            }
        }

        private void configWindow_Closed(object sender, EventArgs e)
        {
            configWindow = null;
        }
        private void configWindow_Loaded(object sender, RoutedEventArgs e)
        {
            damageValue.Text = Math.Round(GetRealDamage(), 2).ToString();
            ehpValue.Text = Math.Round(GetRealEHP(), 2).ToString();
        }
        private void buttonClose_Click(object sender, RoutedEventArgs e)
        {
            //SaveConfiguration();
            configWindow.Close();
        }
        #endregion
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
            Offhand,
            Armor,
            Jewelry,
            FollowerItem,
            Misc,
            Gem,
            HealthGlobe
        }

        public enum RatoshInventorySlot
        {
            Unknown,
            PlayerHead,
            PlayerTorso,
            PlayerRightHand,
            PlayerLeftHand,
            PlayerRightHandSpecial,
            PlayerTwoHand,
            PlayerHands,
            PlayerWaist,
            PlayerFeet,
            PlayerShoulders,
            PlayerLegs,
            PlayerBracers,
            PlayerRightFinger,
            PlayerLeftFinger,
            PlayerNeck,
            FollowerItem,
            FollowerNeck,
            FollowerRightFinger,
            FollowerLeftFinger,
            FollowerRightHand,
            FollowerLeftHand
        }

        public enum RatoshInventoryBase
        {
            Unknown,
            Player,
            Follower
        }

        // **********************************************************************************************
        // ***** DetermineItemType - Calculates what kind of item it is from D3 internalnames       *****
        // **********************************************************************************************
        public GilesItemType DetermineItemType(string sThisInternalName, ItemType DBItemType, bool DBOneHanded, DyeType DBDyeType)
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

        // **********************************************************************************************
        // *****      DetermineBaseType - Calculates a more generic, "basic" type of item           *****
        // **********************************************************************************************
        public GilesBaseItemType DetermineBaseType(GilesItemType thisGilesItemType)
        {
            GilesBaseItemType thisGilesBaseType = GilesBaseItemType.Unknown;
            if (thisGilesItemType == GilesItemType.Axe || thisGilesItemType == GilesItemType.CeremonialKnife || thisGilesItemType == GilesItemType.Dagger ||
                thisGilesItemType == GilesItemType.FistWeapon || thisGilesItemType == GilesItemType.Mace || thisGilesItemType == GilesItemType.MightyWeapon ||
                thisGilesItemType == GilesItemType.Spear || thisGilesItemType == GilesItemType.Sword || thisGilesItemType == GilesItemType.Wand || thisGilesItemType == GilesItemType.HandCrossbow)
            {
                thisGilesBaseType = GilesBaseItemType.WeaponOneHand;
            }
            else if (thisGilesItemType == GilesItemType.TwoHandDaibo || thisGilesItemType == GilesItemType.TwoHandMace ||
                thisGilesItemType == GilesItemType.TwoHandMighty || thisGilesItemType == GilesItemType.TwoHandPolearm || thisGilesItemType == GilesItemType.TwoHandStaff ||
                thisGilesItemType == GilesItemType.TwoHandSword || thisGilesItemType == GilesItemType.TwoHandAxe || thisGilesItemType == GilesItemType.TwoHandCrossbow || thisGilesItemType == GilesItemType.TwoHandBow)
            {
                thisGilesBaseType = GilesBaseItemType.WeaponTwoHand;
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
                thisGilesItemType == GilesItemType.SpecialItem || thisGilesItemType == GilesItemType.CraftingPlan || thisGilesItemType == GilesItemType.HealthPotion ||
                thisGilesItemType == GilesItemType.Dye || thisGilesItemType == GilesItemType.StaffOfHerding)
            {
                thisGilesBaseType = GilesBaseItemType.Misc;
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

        public RatoshInventorySlot FromInventorySlot(ACDItem item)
        {
            GilesItemType thisGilesType = DetermineItemType(item.InternalName, item.ItemType, item.IsOneHand, item.DyeType);
            GilesBaseItemType thisGilesBaseType = DetermineBaseType(thisGilesType);
            
            InventorySlot itemInventorySlot = item.InventorySlot;

            if (thisGilesBaseType == GilesBaseItemType.WeaponTwoHand && itemInventorySlot == InventorySlot.PlayerLeftHand)
            {
                return RatoshInventorySlot.PlayerTwoHand;
            }
            else if (thisGilesType == GilesItemType.Quiver)
            {
                return RatoshInventorySlot.PlayerRightHandSpecial;
            }
            else
            {
                switch (itemInventorySlot)
                {
                    case InventorySlot.PlayerHead:
                        return RatoshInventorySlot.PlayerHead;
                    case InventorySlot.PlayerTorso:
                        return RatoshInventorySlot.PlayerTorso;
                    case InventorySlot.PlayerRightHand:
                        return RatoshInventorySlot.PlayerRightHand;
                    case InventorySlot.PlayerLeftHand:
                        return RatoshInventorySlot.PlayerLeftHand;
                    case InventorySlot.PlayerHands:
                        return RatoshInventorySlot.PlayerHands;
                    case InventorySlot.PlayerWaist:
                        return RatoshInventorySlot.PlayerWaist;
                    case InventorySlot.PlayerFeet:
                        return RatoshInventorySlot.PlayerFeet;
                    case InventorySlot.PlayerShoulders:
                        return RatoshInventorySlot.PlayerShoulders;
                    case InventorySlot.PlayerLegs:
                        return RatoshInventorySlot.PlayerLegs;
                    case InventorySlot.PlayerBracers:
                        return RatoshInventorySlot.PlayerBracers;
                    case InventorySlot.PlayerRightFinger:
                        return RatoshInventorySlot.PlayerRightFinger;
                    case InventorySlot.PlayerLeftFinger:
                        return RatoshInventorySlot.PlayerLeftFinger;
                    case InventorySlot.PlayerNeck:
                        return RatoshInventorySlot.PlayerNeck;
                    case InventorySlot.PetRightHand:
                        return RatoshInventorySlot.FollowerRightHand;
                    case InventorySlot.PetLeftHand:
                        return RatoshInventorySlot.FollowerLeftHand;
                    case InventorySlot.PetSpecial:
                        return RatoshInventorySlot.FollowerItem;
                    case InventorySlot.PetNeck:
                        return RatoshInventorySlot.FollowerNeck;
                    case InventorySlot.PetRightFinger:
                        return RatoshInventorySlot.FollowerRightFinger;
                    case InventorySlot.PetLeftFinger:
                        return RatoshInventorySlot.FollowerLeftFinger;
                }
            }
            return RatoshInventorySlot.Unknown;
        }

        public InventorySlot ToInventorySlot(RatoshInventorySlot inventorySlot)
        {
            if (inventorySlot == RatoshInventorySlot.PlayerTwoHand)
            {
                return InventorySlot.PlayerLeftHand;
            }
            else
            {
                switch (inventorySlot)
                {
                    case RatoshInventorySlot.PlayerHead:
                        return InventorySlot.PlayerHead;
                    case RatoshInventorySlot.PlayerTorso:
                        return InventorySlot.PlayerTorso;
                    case RatoshInventorySlot.PlayerRightHand:
                        return InventorySlot.PlayerRightHand;
                    case RatoshInventorySlot.PlayerRightHandSpecial:
                        return InventorySlot.PlayerRightHand;
                    case RatoshInventorySlot.PlayerLeftHand:
                        return InventorySlot.PlayerLeftHand;
                    case RatoshInventorySlot.PlayerHands:
                        return InventorySlot.PlayerHands;
                    case RatoshInventorySlot.PlayerWaist:
                        return InventorySlot.PlayerWaist;
                    case RatoshInventorySlot.PlayerFeet:
                        return InventorySlot.PlayerFeet;
                    case RatoshInventorySlot.PlayerShoulders:
                        return InventorySlot.PlayerShoulders;
                    case RatoshInventorySlot.PlayerLegs:
                        return InventorySlot.PlayerLegs;
                    case RatoshInventorySlot.PlayerBracers:
                        return InventorySlot.PlayerBracers;
                    case RatoshInventorySlot.PlayerRightFinger:
                        return InventorySlot.PlayerRightFinger;
                    case RatoshInventorySlot.PlayerLeftFinger:
                        return InventorySlot.PlayerLeftFinger;
                    case RatoshInventorySlot.PlayerNeck:
                        return InventorySlot.PlayerNeck;
                    case RatoshInventorySlot.FollowerRightHand:
                        return InventorySlot.PetRightHand;
                    case RatoshInventorySlot.FollowerLeftHand:
                        return InventorySlot.PetLeftHand;
                    case RatoshInventorySlot.FollowerItem:
                        return InventorySlot.PetSpecial;
                    case RatoshInventorySlot.FollowerNeck:
                        return InventorySlot.PetNeck;
                    case RatoshInventorySlot.FollowerRightFinger:
                        return InventorySlot.PetRightFinger;
                    case RatoshInventorySlot.FollowerLeftFinger:
                        return InventorySlot.PetLeftFinger;
                }
            }
            return InventorySlot.None;
        }

        public List<RatoshInventorySlot> DeterminePossibleSlot(GilesItemType thisGilesItemType)
        {
            List<RatoshInventorySlot> thisInventorySlot = new List<RatoshInventorySlot>();
            if (thisGilesItemType == GilesItemType.Axe || thisGilesItemType == GilesItemType.CeremonialKnife || thisGilesItemType == GilesItemType.Dagger ||
                thisGilesItemType == GilesItemType.FistWeapon || thisGilesItemType == GilesItemType.Mace || thisGilesItemType == GilesItemType.MightyWeapon ||
                thisGilesItemType == GilesItemType.Spear || thisGilesItemType == GilesItemType.Sword || thisGilesItemType == GilesItemType.Wand || 
                thisGilesItemType == GilesItemType.HandCrossbow)
            {
                //thisInventorySlot.Add(RatoshInventorySlot.FollowerLeftHand);
                thisInventorySlot.Add(RatoshInventorySlot.PlayerLeftHand);
                if (currentClass == ActorClass.Barbarian || currentClass == ActorClass.Monk || currentClass == ActorClass.DemonHunter)
                {
                    thisInventorySlot.Add(RatoshInventorySlot.PlayerRightHand);
                }
            }
            else if (thisGilesItemType == GilesItemType.TwoHandDaibo || thisGilesItemType == GilesItemType.TwoHandMace ||
                thisGilesItemType == GilesItemType.TwoHandMighty || thisGilesItemType == GilesItemType.TwoHandPolearm || thisGilesItemType == GilesItemType.TwoHandStaff ||
                thisGilesItemType == GilesItemType.TwoHandSword || thisGilesItemType == GilesItemType.TwoHandAxe)
            {
                thisInventorySlot.Add(RatoshInventorySlot.PlayerTwoHand);
            }
            else if (thisGilesItemType == GilesItemType.TwoHandCrossbow || thisGilesItemType == GilesItemType.TwoHandBow)
            {
                thisInventorySlot.Add(RatoshInventorySlot.PlayerTwoHand);
            }
            else if (thisGilesItemType == GilesItemType.Shield)
            {
                //thisInventorySlot.Add(RatoshInventorySlot.FollowerRightHand);
                thisInventorySlot.Add(RatoshInventorySlot.PlayerRightHand);
            }
            else if (thisGilesItemType == GilesItemType.Mojo || thisGilesItemType == GilesItemType.Source)
            {
                thisInventorySlot.Add(RatoshInventorySlot.PlayerRightHand);
            }
            else if (thisGilesItemType == GilesItemType.Quiver)
            {
                thisInventorySlot.Add(RatoshInventorySlot.PlayerRightHandSpecial);
            }
            else if (thisGilesItemType == GilesItemType.Boots)
            {
                thisInventorySlot.Add(RatoshInventorySlot.PlayerFeet);
            }
            else if (thisGilesItemType == GilesItemType.Bracers)
            {
                thisInventorySlot.Add(RatoshInventorySlot.PlayerBracers);
            }
            else if (thisGilesItemType == GilesItemType.Chest || thisGilesItemType == GilesItemType.Cloak)
            {
                thisInventorySlot.Add(RatoshInventorySlot.PlayerTorso);
            }
            else if (thisGilesItemType == GilesItemType.Gloves)
            {
                thisInventorySlot.Add(RatoshInventorySlot.PlayerHands);
            }
            else if (thisGilesItemType == GilesItemType.Helm || thisGilesItemType == GilesItemType.SpiritStone || thisGilesItemType == GilesItemType.VoodooMask || thisGilesItemType == GilesItemType.WizardHat)
            {
                thisInventorySlot.Add(RatoshInventorySlot.PlayerHead);
            }
            else if (thisGilesItemType == GilesItemType.Pants)
            {
                thisInventorySlot.Add(RatoshInventorySlot.PlayerLegs);
            }
            else if (thisGilesItemType == GilesItemType.Shoulders)
            {
                thisInventorySlot.Add(RatoshInventorySlot.PlayerShoulders);
            }
            else if (thisGilesItemType == GilesItemType.Belt || thisGilesItemType == GilesItemType.MightyBelt)
            {
                thisInventorySlot.Add(RatoshInventorySlot.PlayerWaist);
            }
            else if (thisGilesItemType == GilesItemType.Amulet)
            {
                thisInventorySlot.Add(RatoshInventorySlot.PlayerNeck);
                //thisInventorySlot.Add(RatoshInventorySlot.FollowerNeck);
            }
            else if (thisGilesItemType == GilesItemType.Ring)
            {
                thisInventorySlot.Add(RatoshInventorySlot.PlayerRightFinger);
                thisInventorySlot.Add(RatoshInventorySlot.PlayerLeftFinger);
                //thisInventorySlot.Add(RatoshInventorySlot.FollowerRightFinger);
                //thisInventorySlot.Add(RatoshInventorySlot.FollowerLeftFinger);
            }
            else if (thisGilesItemType == GilesItemType.FollowerEnchantress || thisGilesItemType == GilesItemType.FollowerScoundrel ||
                thisGilesItemType == GilesItemType.FollowerTemplar)
            {
                //thisInventorySlot.Add(RatoshInventorySlot.FollowerItem);
            }
            return thisInventorySlot;
        }

        public RatoshInventoryBase GetInventoryBase(RatoshInventorySlot inventorySlot)
        {
            switch (inventorySlot)
            {
                case RatoshInventorySlot.PlayerHead:
                case RatoshInventorySlot.PlayerTorso:
                case RatoshInventorySlot.PlayerRightHand:
                case RatoshInventorySlot.PlayerLeftHand:
                case RatoshInventorySlot.PlayerRightHandSpecial:
                case RatoshInventorySlot.PlayerTwoHand:
                case RatoshInventorySlot.PlayerHands:
                case RatoshInventorySlot.PlayerWaist:
                case RatoshInventorySlot.PlayerFeet:
                case RatoshInventorySlot.PlayerShoulders:
                case RatoshInventorySlot.PlayerLegs:
                case RatoshInventorySlot.PlayerBracers:
                case RatoshInventorySlot.PlayerRightFinger:
                case RatoshInventorySlot.PlayerLeftFinger:
                case RatoshInventorySlot.PlayerNeck:
                    return RatoshInventoryBase.Player;
                case RatoshInventorySlot.FollowerItem:
                case RatoshInventorySlot.FollowerNeck:
                case RatoshInventorySlot.FollowerRightFinger:
                case RatoshInventorySlot.FollowerLeftFinger:
                case RatoshInventorySlot.FollowerRightHand:
                case RatoshInventorySlot.FollowerLeftHand:
                    return RatoshInventoryBase.Follower;
            }
            return RatoshInventoryBase.Unknown;
        }

        public Dictionary<RatoshInventorySlot, ACDItem> getPlayerEquipped()
        {
            Dictionary<RatoshInventorySlot, ACDItem> result = new Dictionary<RatoshInventorySlot, ACDItem>();
            foreach (KeyValuePair<RatoshInventorySlot, ACDItem> pair in currentEquipped)
            {
                if (GetInventoryBase(pair.Key) == RatoshInventoryBase.Player)
                {
                    result.Add(pair.Key, pair.Value);
                }
            }
            return result;
        }

        public double GetRealEHP()
        {
            return EvaluateEHP(getPlayerEquipped());
        }

        public double GetEHP()
        {
            return EvaluateEHP(currentEquipped);
        }

        public double EvaluateEHP(Dictionary<RatoshInventorySlot, ACDItem> equips)
        {
            if (equips == null)
            {
                return 0;
            }
            double TotalArmor = 0;
            double TotalResist = 0;
            double TotalDextery = 7 + ((currentClass == ActorClass.DemonHunter || currentClass == ActorClass.Monk ? 3 : 1) * (currentLevel + paragonLevel));
            double TotalInteligence = 7 + ((currentClass == ActorClass.Wizard || currentClass == ActorClass.WitchDoctor ? 3 : 1) * (currentLevel + paragonLevel));
            double TotalStrength = 7 + ((currentClass == ActorClass.Barbarian ? 3 : 1) * (currentLevel + paragonLevel));
            double TotalLifePercent = 0;
            double TotalVitality = 7 + (currentLevel + paragonLevel) * 2;
            double TotalBlockValue = 0;
            double TotalBlockChance = 0;

            double BaseReduction = (currentClass == ActorClass.Barbarian || currentClass == ActorClass.Monk ? .7 : 1);
            foreach (KeyValuePair<RatoshInventorySlot, ACDItem> pair in equips)
            {
                RatoshInventoryBase inventoryBase = GetInventoryBase(pair.Key);
                ACDItem item = pair.Value;
                double followerMultiplier = 1;
                if (inventoryBase == RatoshInventoryBase.Follower)
                {
                    followerMultiplier = 0.01;
                }
                TotalArmor += GetItemStatus(item, StatusArmor) * followerMultiplier;
                TotalResist += GetItemStatus(item, StatusResist) * followerMultiplier;
                TotalDextery += GetItemStatus(item, StatusDextery) * followerMultiplier;
                TotalInteligence += GetItemStatus(item, StatusInteligence) * followerMultiplier;
                TotalStrength += GetItemStatus(item, StatusStrength) * followerMultiplier;
                TotalLifePercent += GetItemStatus(item, StatusLifePercent) * followerMultiplier;
                TotalVitality += GetItemStatus(item, StatusVitality) * followerMultiplier;
                TotalBlockValue += GetItemStatus(item, StatusBlockValue) * followerMultiplier;
                TotalBlockChance += GetItemStatus(item, StatusBlockChance) * followerMultiplier;
            }

            TotalArmor += TotalStrength;
            TotalResist += TotalInteligence/10;

            Diagnostic("TotalArmor: " + TotalArmor);
            Diagnostic("TotalResistAll: " + TotalResist);
            Diagnostic("TotalDextery: " + TotalDextery);
            Diagnostic("TotalIntelect: " + TotalInteligence);
            Diagnostic("TotalStrength: " + TotalStrength);
            Diagnostic("TotalLifePercent: " + TotalLifePercent);
            Diagnostic("TotalVitality: " + TotalVitality);

            double TotalLife = 36 + (currentLevel) * 4 + Math.Max(10, (currentLevel - 25)) * TotalVitality * (1 + TotalLifePercent / 100);
            double TotalArmorDR = (1 - (TotalArmor / (50 * 63 + TotalArmor)));
            double TotalResistDR = (1 - (TotalResist / (5 * 63 + TotalResist)));
            double TotalDodgeDR = 1 -
                (Math.Min(TotalDextery, 100) * 0.1 + 
                Math.Max(0, Math.Min(500, TotalDextery) - 100) * 0.025 +
                Math.Max(0, Math.Min(1000, TotalDextery) - 500) * 0.020 +
                Math.Max(0, TotalDextery-1000) * 0.01)/100;
            Diagnostic("TotalDodgeDR" + TotalDodgeDR);
            Diagnostic("TotalLife: " + TotalLife);
            Diagnostic("TotalArmorDR: " + (1 - TotalArmorDR));
            Diagnostic("TotalResistDR: " + (1 - TotalResistDR));
            Diagnostic("TotalDodgeDR: " + (1 - TotalDodgeDR));
            Diagnostic("Total EHP NO DODGE: " + (TotalLife + TotalBlockValue * TotalBlockChance) / (TotalArmorDR * TotalResistDR *  BaseReduction));
            Diagnostic("Total EHP DODGE: " + (TotalLife + TotalBlockValue * TotalBlockChance) / (TotalArmorDR * TotalResistDR * BaseReduction * TotalDodgeDR));

            return (TotalLife + TotalBlockValue * TotalBlockChance) / (TotalArmorDR * TotalResistDR * TotalDodgeDR * BaseReduction);
        }

        public Dictionary<RatoshInventorySlot, double> EvaluateEHP(ACDItem item, Dictionary<RatoshInventorySlot, ACDItem> equips)
        {
            Dictionary<RatoshInventorySlot, double> result = new Dictionary<RatoshInventorySlot, double>();

            if (item.BaseAddress != IntPtr.Zero)
            {
                GilesItemType itemGilesItemType = DetermineItemType(item.InternalName, item.ItemType, item.IsOneHand, item.DyeType);
                List<RatoshInventorySlot> possibleSlots = DeterminePossibleSlot(itemGilesItemType);

                foreach (RatoshInventorySlot possibleSlot in possibleSlots)
                {
                    if (IsItemCompatible(item, possibleSlot))
                    {
                        Dictionary<RatoshInventorySlot, ACDItem> tempEquips = fakeEquip(equips, possibleSlot, item);
                        result.Add(possibleSlot, EvaluateEHP(tempEquips));
                    }
                }
            }
            return result;
        }

        public double GetRealDamage()
        {
            return EvaluateDamage(getPlayerEquipped());
        }

        public double GetDamage()
        {
            return EvaluateDamage(currentEquipped);
        }

        public double EvaluateDamage(Dictionary<RatoshInventorySlot, ACDItem> equips)
        {
            if (equips == null)
            {
                return 0;
            }
            double TotalPrimary = 7 + (currentLevel + paragonLevel) * 3;
            double TotalCritPercent = 5;
            double TotalCritDamagePercent = 50;
            double TotalAttackSpeed = 0;
            double TotalMinDamage = 0;
            double TotalMaxDamage = 0;
            double TotalWeaponMinDamage = 0;
            double TotalWeaponMaxDamage = 0;
            double TotalWeaponAttacksPerSecond = 0;
            double TotalDamageMultiplier = 0;

            double weaponsEquipped = 0;

            foreach (KeyValuePair<RatoshInventorySlot, ACDItem> pair in equips)
            {
                RatoshInventoryBase inventoryBase = GetInventoryBase(pair.Key);
                ACDItem item = pair.Value;
                double followerMultiplier = 1;
                if (inventoryBase == RatoshInventoryBase.Follower)
                {
                    followerMultiplier = 0.01;
                }
                // Work out the primary stat based on your class
                switch (currentClass)
                {
                    case ActorClass.Barbarian:
                        TotalPrimary += GetItemStatus(item, StatusStrength) * followerMultiplier;
                        break;
                    case ActorClass.Monk:
                    case ActorClass.DemonHunter:
                        TotalPrimary += GetItemStatus(item, StatusDextery) * followerMultiplier;
                        break;
                    case ActorClass.Wizard:
                    case ActorClass.WitchDoctor:
                        TotalPrimary += GetItemStatus(item, StatusInteligence) * followerMultiplier;
                        break;
                } // Switch on your actorclass
                TotalCritPercent += GetItemStatus(item, StatusCritPercent) * followerMultiplier;
                TotalCritDamagePercent += GetItemStatus(item, StatusCritDamagePercent) * followerMultiplier;
                TotalAttackSpeed += GetItemStatus(item, StatusAttackSpeed) * followerMultiplier;
                TotalMinDamage += GetItemStatus(item, StatusMinDamage) * followerMultiplier;
                TotalMaxDamage += GetItemStatus(item, StatusMaxDamage) * followerMultiplier;
                TotalWeaponMinDamage += GetItemStatus(item, StatusWeaponMinDamage) * followerMultiplier;
                TotalWeaponMaxDamage += GetItemStatus(item, StatusWeaponMaxDamage) * followerMultiplier;
                TotalWeaponAttacksPerSecond += GetItemStatus(item, StatusWeaponAttacksPerSecond) * followerMultiplier;
                TotalDamageMultiplier += GetItemStatus(item, StatusDamageMultiplier) * followerMultiplier;
                weaponsEquipped += (GetItemStatus(item, StatusWeaponAttacksPerSecond) > 0 ? 1 : 0);
            }

            double TotalBaseDPS = 0;

            if (weaponsEquipped > 1)
            {
                TotalWeaponAttacksPerSecond = (TotalWeaponAttacksPerSecond / weaponsEquipped) * (1.15 + TotalAttackSpeed / 100);
                TotalBaseDPS = ((TotalWeaponMinDamage + TotalWeaponMaxDamage + TotalMinDamage + TotalMaxDamage) / (2 * weaponsEquipped)) * TotalWeaponAttacksPerSecond;
            }
            else
            {
                TotalWeaponAttacksPerSecond = (TotalWeaponAttacksPerSecond / weaponsEquipped) * (1 + TotalAttackSpeed / 100);
                TotalBaseDPS = ((TotalWeaponMinDamage + TotalWeaponMaxDamage + TotalMinDamage + TotalMaxDamage) / 2) * TotalWeaponAttacksPerSecond;
            }

            Diagnostic("Primary: " + TotalPrimary);
            Diagnostic("Crit Percent: " + TotalCritPercent);
            Diagnostic("Crit Damage Percent: " + TotalCritDamagePercent);
            Diagnostic("Attack speed: " + TotalAttackSpeed);
            Diagnostic("Min Damage: " + TotalMinDamage);
            Diagnostic("Max Damage: " + TotalMaxDamage);
            Diagnostic("Weapon Damage: " + (TotalWeaponMinDamage + TotalWeaponMaxDamage)/2);
            Diagnostic("Weapon AttacksPerSecond: " + TotalWeaponAttacksPerSecond);
            Diagnostic("Base DPS: " + TotalBaseDPS);
            //Weapon DPS * (1 + (Strength%/100)) * (1 + (CritChance%/100 * CritBonus%/100)) * (1 + AttackSpeed%/100) * (1 + DamageMultiplier%/100)
            return (TotalBaseDPS) * (1 + (TotalPrimary / 100)) * (1 + (TotalCritPercent / 100 * TotalCritDamagePercent / 100)) * (1 + TotalDamageMultiplier / 100);
        }

        public Dictionary<RatoshInventorySlot, double> EvaluateDamage(ACDItem item, Dictionary<RatoshInventorySlot, ACDItem> equips)
        {
            Dictionary<RatoshInventorySlot, double> result = new Dictionary<RatoshInventorySlot, double>();

            if (item.BaseAddress != IntPtr.Zero)
            {
                GilesItemType itemGilesItemType = DetermineItemType(item.InternalName, item.ItemType, item.IsOneHand, item.DyeType);
                List<RatoshInventorySlot> possibleSlots = DeterminePossibleSlot(itemGilesItemType);

                foreach (RatoshInventorySlot possibleSlot in possibleSlots)
                {
                    if (IsItemCompatible(item, possibleSlot))
                    {
                        Dictionary<RatoshInventorySlot, ACDItem> tempEquips = fakeEquip(equips, possibleSlot, item);
                        result.Add(possibleSlot, EvaluateDamage(tempEquips));
                    }
                }
            }
            return result;
        }

        private int BoolToInt(bool value)
        {
            return value ? 1 : 0;
        }

        // Evaluate equipped items only
        private double GetItemStatus(ACDItem item, int status)
        {
            Dictionary<double, double> statusResult;
            //ItemCacheKey itemCacheKey = new ItemCacheKey { itemDynamicId = thisitem.DynamicId, itemStatus = status };
            if (ItemEvaluationCache.TryGetValue(item, out statusResult))
            {
                return statusResult[status];
            }
            statusResult = new Dictionary<double, double>();

            double PointsCritPercent = 0;
            double PointsCritDamagePercent = 0;
            double PointsAttackSpeed = 0;
            double PointsMinDamage = 0;
            double PointsMaxDamage = 0;
            double PointsWeaponMinDamage = 0;
            double PointsWeaponMaxDamage = 0;
            double PointsWeaponAttacksPerSecond = 0;
            double PointsDamageMultiplier = 0;

            double PointsArmor = 0;
            double PointsResist = 0;
            double PointsDextery = 0;
            double PointsInteligence = 0;
            double PointsStrength = 0;
            double PointsLifePercent = 0;
            double PointsVitality = 0;
            double PointsBlockValue = 0;
            double PointsBlockChance = 0;

            // Deal with armor and jewelry together
            GilesItemType thisGilesType = DetermineItemType(item.InternalName, item.ItemType, item.IsOneHand, item.DyeType);
            GilesBaseItemType thisGilesBaseType = DetermineBaseType(thisGilesType);
            if (thisGilesBaseType == GilesBaseItemType.Armor || thisGilesBaseType == GilesBaseItemType.Jewelry || thisGilesBaseType == GilesBaseItemType.Offhand)
            {
                // Give 5 points free - so it values something without any supported stats over an empty inventory slot
                //iTempPoints = 5;
            }
            // Now deal with weapons
            else if (thisGilesBaseType == GilesBaseItemType.WeaponOneHand || thisGilesBaseType == GilesBaseItemType.WeaponTwoHand)
            {
                //iTempPoints += (thisitem.Stats.damage * int(status == StatusDamageMultiplier)); // Damage multiplier (not supported)
            } // Is a base item type of weapon
            
            PointsCritPercent = item.Stats.CritPercent; // Crit chance %
            PointsCritDamagePercent = item.Stats.CritDamagePercent; // crit damage bonus %
            PointsAttackSpeed = item.Stats.AttackSpeedPercent; // Attack speed
            PointsMinDamage = item.Stats.MinDamage; // Min damage bonus (currently broken in DB)
            PointsMaxDamage = item.Stats.MaxDamage; // Max damage bonus (currently broken in DB)
            PointsWeaponMinDamage = item.Stats.WeaponMinDamage; // DPS
            PointsWeaponMaxDamage = item.Stats.WeaponMaxDamage; // DPS
            PointsWeaponAttacksPerSecond = item.Stats.WeaponAttacksPerSecond; // DPS
            //PointsDamageMultiplier = item.Stats.; // DPS

            PointsArmor = item.Stats.Armor + item.Stats.ArmorBonus; // Armor 
            PointsResist = item.Stats.ResistAll; // Resist all 
            PointsDextery = item.Stats.Dexterity; // Armor 
            PointsInteligence = item.Stats.Intelligence; // Armor 
            PointsStrength = item.Stats.Strength; // Armor 
            PointsLifePercent = item.Stats.LifePercent; // Life Percent 
            PointsVitality = item.Stats.Vitality; // vitality 
            //PointsBlockValue = thisitem.Stats.block; // Block amount
            PointsBlockChance = item.Stats.BlockChance; // Block chance 

            statusResult.Add(StatusCritPercent, PointsCritPercent);
            statusResult.Add(StatusCritDamagePercent, PointsCritDamagePercent);
            statusResult.Add(StatusAttackSpeed, PointsAttackSpeed);
            statusResult.Add(StatusMinDamage, PointsMinDamage);
            statusResult.Add(StatusMaxDamage, PointsMaxDamage);
            statusResult.Add(StatusWeaponMinDamage, PointsWeaponMinDamage);
            statusResult.Add(StatusWeaponMaxDamage, PointsWeaponMaxDamage);
            statusResult.Add(StatusWeaponAttacksPerSecond, PointsWeaponAttacksPerSecond);
            statusResult.Add(StatusDamageMultiplier, PointsDamageMultiplier);

            statusResult.Add(StatusArmor, PointsArmor);
            statusResult.Add(StatusResist, PointsResist);
            statusResult.Add(StatusDextery, PointsDextery);
            statusResult.Add(StatusInteligence, PointsInteligence);
            statusResult.Add(StatusStrength, PointsStrength);
            statusResult.Add(StatusLifePercent, PointsLifePercent);
            statusResult.Add(StatusVitality, PointsVitality);
            statusResult.Add(StatusBlockValue, PointsBlockValue);
            statusResult.Add(StatusBlockChance, PointsBlockChance);

            ItemEvaluationCache.Add(item, statusResult);

            return statusResult[status];
        } // ValueThisItem() function

        public void OnPulse()
        {
            if (ZetaDia.Me.BaseAddress == IntPtr.Zero || ZetaDia.Me == null || !ZetaDia.Me.IsValid || ZetaDia.IsLoadingWorld)
            {
                return;
            }

            if (DateTime.Now.Subtract(_lastPulse).TotalSeconds < 10)
            {
                return;
            }
			
            reloadEquipped();
            currentLevel = ZetaDia.Me.Level;
            paragonLevel = ZetaDia.Me.ParagonLevel;
            currentClass = ZetaDia.Me.ActorClass;
            _lastPulse = DateTime.Now;
            //ItemEvaluationCache.Clear();
        }

        public void reloadEquipped()
        {
            // Call a Zeta update
            //currentEquipped.Clear();
            Dictionary<RatoshInventorySlot, bool> tempEquips = new Dictionary<RatoshInventorySlot, bool>();
            foreach (RatoshInventorySlot slot in currentEquipped.Keys)
            {
                tempEquips.Add(slot, true);
            }
            foreach (ACDItem item in ZetaDia.Me.Inventory.Equipped)
            {
                if (item.BaseAddress == IntPtr.Zero)
                {
                    continue;
                }
                RatoshInventorySlot slot = FromInventorySlot(item);
                equip(slot, item);
                tempEquips.Remove(slot);
            }
            foreach (ACDItem item in ZetaDia.Me.Inventory.Pet)
            {
                if (item.BaseAddress == IntPtr.Zero)
                {
                    continue;
                }
                RatoshInventorySlot slot = FromInventorySlot(item);
                equip(slot, item);
                tempEquips.Remove(slot);
            }
            foreach (RatoshInventorySlot slot in tempEquips.Keys)
            {
                Log(String.Format("Missing item at {0}! Removed or bug?", slot));
                currentEquipped.Remove(slot);
            }
        }

        public Dictionary<RatoshInventorySlot, ACDItem> fakeEquip(Dictionary<RatoshInventorySlot, ACDItem> equips, RatoshInventorySlot inventorySlot, ACDItem equip)
        {
            Dictionary<RatoshInventorySlot, ACDItem> tempEquips = new Dictionary<RatoshInventorySlot, ACDItem>();
            foreach (RatoshInventorySlot slot in equips.Keys)
            {
                ACDItem tempItem;
                if (equips.TryGetValue(slot, out tempItem))
                {
                    tempEquips.Add(slot, tempItem);
                }
            }

            ACDItem oldItem;
            if (tempEquips.TryGetValue(inventorySlot, out oldItem))
            {
                if (oldItem == equip)
                {
                    return tempEquips;
                }
                Diagnostic(String.Format("Removing {0} at {1}", oldItem.Name, inventorySlot.ToString()));
                tempEquips.Remove(inventorySlot);
            }
            // Check for Righthands and Offhands if its a TwoHanded
            if (inventorySlot == RatoshInventorySlot.PlayerTwoHand)
            {
                if (tempEquips.TryGetValue(RatoshInventorySlot.PlayerRightHand, out oldItem))
                {
                    Diagnostic(String.Format("Removing {0} at {1}", oldItem.Name, RatoshInventorySlot.PlayerRightHand.ToString()));
                    tempEquips.Remove(RatoshInventorySlot.PlayerRightHand);
                }
                if (tempEquips.TryGetValue(RatoshInventorySlot.PlayerLeftHand, out oldItem))
                {
                    Diagnostic(String.Format("Removing {0} at {1}", oldItem.Name, RatoshInventorySlot.PlayerLeftHand.ToString()));
                    tempEquips.Remove(RatoshInventorySlot.PlayerLeftHand);
                }
            }
            // Check for TwoHanded if its a Offhand
            if (inventorySlot == RatoshInventorySlot.PlayerLeftHand || inventorySlot == RatoshInventorySlot.PlayerRightHand)
            {
                if (tempEquips.TryGetValue(RatoshInventorySlot.PlayerTwoHand, out oldItem))
                {
                    Diagnostic(String.Format("Removing {0} at {1}", oldItem.Name, RatoshInventorySlot.PlayerTwoHand.ToString()));
                    tempEquips.Remove(RatoshInventorySlot.PlayerTwoHand);
                }
                if (inventorySlot == RatoshInventorySlot.PlayerRightHand)
                {
                    if (tempEquips.TryGetValue(RatoshInventorySlot.PlayerRightHandSpecial, out oldItem))
                    {
                        Diagnostic(String.Format("Removing {0} at {1}", oldItem.Name, RatoshInventorySlot.PlayerRightHandSpecial.ToString()));
                        tempEquips.Remove(RatoshInventorySlot.PlayerRightHandSpecial);
                    }
                }
            }
            if (inventorySlot == RatoshInventorySlot.PlayerRightHandSpecial)
            {
                if (tempEquips.TryGetValue(RatoshInventorySlot.PlayerRightHand, out oldItem))
                {
                    Diagnostic(String.Format("Removing {0} at {1}", oldItem.Name, RatoshInventorySlot.PlayerRightHand.ToString()));
                    tempEquips.Remove(RatoshInventorySlot.PlayerRightHand);
                }
            }

            tempEquips.Add(inventorySlot, equip);
            Diagnostic(String.Format("Equipping {0} at {1}", equip.Name, inventorySlot.ToString()));
            return tempEquips;
        }

        public void equip(RatoshInventorySlot inventorySlot, ACDItem equip)
        {
            currentEquipped = fakeEquip(currentEquipped, inventorySlot, equip);
            if (!ZetaDia.Me.Inventory.Equipped.Contains(equip))
            {
                ZetaDia.Me.Inventory.EquipItem(equip.DynamicId, ToInventorySlot(inventorySlot));
            }
        }

        private bool IsItemCompatible(ACDItem item, RatoshInventorySlot inventorySlot)
        {
            GilesItemType itemType = DetermineItemType(item.InternalName, item.ItemType, item.IsOneHand, item.DyeType);

            //bool usingTwoHanded = false;
            GilesBaseItemType itemBaseType = DetermineBaseType(DetermineItemType(item.InternalName, item.ItemType, item.IsOneHand, item.DyeType));
            RatoshInventoryBase inventoryBase = GetInventoryBase(inventorySlot);

            if (inventoryBase == RatoshInventoryBase.Follower)
            {
                //Log(String.Format("Follower slot type {0}", itemType));

                bool result = itemType == GilesItemType.Amulet || itemType == GilesItemType.Ring || itemType == GilesItemType.Axe ||
                    itemType == GilesItemType.Dagger || itemType == GilesItemType.Mace || itemType == GilesItemType.Spear || itemType == GilesItemType.Sword;
                if (item.RequiredLevel >= currentLevel - 1)
                {
                    return false;
                }
                return result;
            }
            if (inventoryBase == RatoshInventoryBase.Player)
            {
                bool result = (itemType == GilesItemType.Belt || itemType == GilesItemType.Helm || itemType == GilesItemType.Shield || itemType == GilesItemType.Chest ||
                    itemType == GilesItemType.Boots || itemType == GilesItemType.Bracers || itemType == GilesItemType.Gloves || itemType == GilesItemType.Pants ||
                    itemType == GilesItemType.Shoulders || itemType == GilesItemType.Amulet || itemType == GilesItemType.Ring);
                switch (currentClass)
                {
                    case ActorClass.Barbarian:
                        if (inventorySlot == RatoshInventorySlot.PlayerLeftHand)
                        {
                            if (itemType == GilesItemType.Axe || itemType == GilesItemType.Dagger || itemType == GilesItemType.Mace ||
                                itemType == GilesItemType.Spear || itemType == GilesItemType.Sword || itemType == GilesItemType.MightyWeapon)
                            {
                                return true;
                            }
                        }
                        if (itemType == GilesItemType.TwoHandAxe || itemType == GilesItemType.TwoHandMace || itemType == GilesItemType.TwoHandPolearm || 
                            itemType == GilesItemType.TwoHandSword || itemType == GilesItemType.TwoHandMighty)
                        {
                            result = true;
                        }
                        if (itemType == GilesItemType.MightyBelt)
                        {
                            result = true;
                        }
                        break;
                    case ActorClass.Monk:
                        if (inventorySlot == RatoshInventorySlot.PlayerLeftHand)
                        {
                            if (itemType == GilesItemType.Axe || itemType == GilesItemType.Dagger || itemType == GilesItemType.Mace || 
                                itemType == GilesItemType.Spear || itemType == GilesItemType.Sword || itemType == GilesItemType.FistWeapon)
                            {
                                result = true;
                            }
                        }
                        if (itemType == GilesItemType.TwoHandAxe || itemType == GilesItemType.Mace || itemType == GilesItemType.TwoHandPolearm || 
                            itemType == GilesItemType.TwoHandSword || itemType == GilesItemType.TwoHandDaibo)
                        {
                            result = true;
                        }
                        if (itemType == GilesItemType.SpiritStone)
                        {
                            result = true;
                        }
                        break;
                    case ActorClass.DemonHunter:
                        if (inventorySlot == RatoshInventorySlot.PlayerLeftHand || inventorySlot == RatoshInventorySlot.PlayerTwoHand)
                        {
                            if (itemType == GilesItemType.TwoHandCrossbow || itemType == GilesItemType.TwoHandBow || itemType == GilesItemType.HandCrossbow)
                            {
                                result = true;
                            }
                        }
                        if (itemType == GilesItemType.Quiver)
                        {
                            result = true;
                        }
                        if (itemType == GilesItemType.Cloak)
                        {
                            result = true;
                        }
                        break;
                    case ActorClass.Wizard:
                        if (inventorySlot == RatoshInventorySlot.PlayerLeftHand)
                        {
                            if (itemType == GilesItemType.Axe || itemType == GilesItemType.Dagger || itemType == GilesItemType.Mace ||
                                itemType == GilesItemType.Spear || itemType == GilesItemType.Sword || itemType == GilesItemType.Wand)
                            {
                                result = true;
                            }
                        }
                        if (itemType == GilesItemType.TwoHandAxe || itemType == GilesItemType.TwoHandMace || itemType == GilesItemType.TwoHandCrossbow ||
                            itemType == GilesItemType.TwoHandSword || itemType == GilesItemType.TwoHandStaff || itemType == GilesItemType.TwoHandBow)
                        {
                            result = true;
                        }
                        if (itemType == GilesItemType.Source)
                        {
                            result = true;
                        }
                        if (itemType == GilesItemType.WizardHat)
                        {
                            result = true;
                        }
                        break;
                    case ActorClass.WitchDoctor:
                        if (itemType == GilesItemType.Axe || itemType == GilesItemType.Dagger || itemType == GilesItemType.Mace ||
                            itemType == GilesItemType.Spear || itemType == GilesItemType.Sword || itemType == GilesItemType.CeremonialKnife)
                        {
                            result = true;
                        }
                        if (itemType == GilesItemType.TwoHandAxe || itemType == GilesItemType.TwoHandMace || itemType == GilesItemType.TwoHandSword ||
                            itemType == GilesItemType.TwoHandStaff || itemType == GilesItemType.TwoHandSword || itemType == GilesItemType.TwoHandCrossbow ||
                            itemType == GilesItemType.TwoHandBow || itemType == GilesItemType.TwoHandPolearm)
                        {
                            result = true;
                        }
                        if (itemType == GilesItemType.Mojo)
                        {
                            result = true;
                        }
                        if (itemType == GilesItemType.VoodooMask)
                        {
                            result = true;
                        }
                        break;

                } // Character class check
                return result;
            }
            
            return false;
        }

        void OnPlayerDied(object sender, EventArgs e)
        {
            Diagnostic(String.Format("Player died! Position={0} QuestId={1} StepId={2} WorldId={3} EHP={4}, DMG={5}",
                ZetaDia.Me.Position, ZetaDia.CurrentQuest.QuestSNO, ZetaDia.CurrentQuest.StepId, ZetaDia.CurrentWorldId, Math.Round(GetRealEHP(), 2), Math.Round(GetRealDamage(), 2)));
            FileStream configStream = File.Open(deathLogFile, FileMode.Create, FileAccess.Write, FileShare.Read);
            using (StreamWriter configWriter = new StreamWriter(configStream))
            {
                configWriter.WriteLine(String.Format("Player died! Position={0} QuestId={1} StepId={2} WorldId={3} EHP={4}, DMG={5}",
                ZetaDia.Me.Position, ZetaDia.CurrentQuest.QuestSNO, ZetaDia.CurrentQuest.StepId, ZetaDia.CurrentWorldId, Math.Round(GetRealEHP(), 2), Math.Round(GetRealDamage(), 2)));
            }
            configStream.Close();
        }

        public void OnEnabled()
        {
            if (!Directory.Exists(pluginPath))
            {
                Log("Fatal Error - cannot enable plugin. Invalid path: " + pluginPath);
                Log("Please check you have installed the plugin to the correct location, and then restart DemonBuddy and re-enable the plugin.");
                Log(@"Plugin should be installed to \<DemonBuddyFolder>\Plugins\AutoEquipper\");
            }
            else
            {
                GameEvents.OnPlayerDied += new EventHandler<EventArgs>(OnPlayerDied);
                Log("*******************GEARCHECK*****************");
				Log("Ativado! Agora, o bot é capaz de avaliar seu DPS e EHP!");
				Log("*******************GEARCHECK*****************");
            }
        }
        public void OnDisabled()
        {
            GameEvents.OnPlayerDied -= new EventHandler<EventArgs>(OnPlayerDied);
            Log("*******************GEARCHECK*****************");
			Log("Desativado! O bot não irá mais avaliar o seu DPS e EHP!");
			Log("*******************GEARCHECK*****************");
        }

        public void OnShutdown()
        {

        }

        public void OnInitialize()
        {
            pluginPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + @"\Plugins\AutoEquipper\";
            deathLogFile = pluginPath + "DeathLog.txt";
            instance = this;
        }
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
    }
}
