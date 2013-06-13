using System.ComponentModel;
using System.Configuration;
using System.IO;
using Belphegor.GUI;
using Zeta.Common.Xml;
using Zeta.XmlEngine;

namespace Belphegor.Settings
{
    [XmlElement("BarbarianSettings")]
    public class BarbarianSettings : XmlSettings
    {
        public BarbarianSettings()
            : base(Path.Combine(Path.Combine(SettingsDirectory, "Belphegor"), "BarbarianSettings.xml"))
        {
        }

        [XmlElement("SpamWarCry")]
        [DisplayName("Spam War Cry")]
        [Category("Tactics")]
        [DefaultValue(false)]
        [Description("Spam War Cry on cool down rather than check if you have the buff, False Default")]
        [Setting]
        public bool SpamWarCry { get; set; }

        [XmlElement("FuriousChargeDreadnought")]
        [DisplayName("Furious Charge Dreadnought")]
        [Category("Might")]
        [DefaultValue(false)]
        [Description("Uses Dreadnought Rune to heal for 8% of your life, False Default")]
        [Setting]
        public bool FuriousChargeDreadnought { get; set; }

        [XmlElement("FuriousChargeDreadnoughtHP")]
        [DefaultValue(0.7)]
        [Setting]
        [Limit(0, 1)]
        [Category("Might")]
        [DisplayName("Furious Charge Drednought Health %")]
        public double FuriousChargeDreadnoughtHP { get; set; }

        [XmlElement("LeapOnCooldown")]
        [DefaultValue(false)]
        [Setting]
        [Category("Defensive")]
        [DisplayName("Leap on Cooldown")]
        [Description("Leaps on cooldown, great for use with Iron Impact")]
        public bool LeapOnCooldown { get; set; }

        [XmlElement("IgnorePainPct")]
        [DisplayName("Ignore Pain %")]
        [DefaultValue(0.6)]
        [Description("The health % on which to use ignore pain, 0.4 default")]
        [Setting]
        [Category("Defensive")]
        [Limit(0, 1)]
        public double IgnorePainPct { get; set; }

        [XmlElement("WotBAoeCount")]
        [DisplayName("WotB AoE Count")]
        [Description("Wrath Of The Berserker AoE Count")]
        [Category("Rage")]
        [DefaultValue(4)]
        [Setting]
        [Limit(1, 20)]
        public int WotBAoeCount { get; set; }

        [XmlElement("CotAAoeCount")]
        [DisplayName("CotA AoE Count")]
        [Description("Call Of The Ancients AoE Count")]
        [Category("Rage")]
        [DefaultValue(4)]
        [Setting]
        [Limit(1, 20)]
        public int CotAAoeCount { get; set; }

        [XmlElement("EarthquakeAoeCount")]
        [DisplayName("Earthquake AoE Count")]
        [Category("Rage")]
        [DefaultValue(4)]
        [Setting]
        [Limit(1, 20)]
        public int EarthquakeAoeCount { get; set; }

        [XmlElement("RendTime")]
        [DisplayName("Rend Timer")]
        [Category("Secondary")]
        [DefaultValue(4)]
        [Setting]
        [Limit(1, 10)]
        public int RendTimer { get; set; }

        [XmlElement("RendRange")]
        [DisplayName("Rend Range")]
        [Category("Secondary")]
        [DefaultValue(14)]
        [Setting]
        [Limit(1, 16)]
        public int RendRange { get; set; }

        [XmlElement("OverpowerAoeCount")]
        [DisplayName("Overpower AoE Count")]
        [Category("Might")]
        [DefaultValue(2)]
        [Description("Number of Mobs to use Overpower on, will auto use on Elites")]
        [Setting]
        [Limit(1, 20)]
        public int OverpowerAoeCount { get; set; }

        [XmlElement("WhirlwindAoeCount")]
        [DisplayName("Whirlwind AoE Count")]
        [Category("Barbarian")]
        [DefaultValue(2)]
        [Description("Number of Mobs to use Whirlwind on, will auto use on Elites")]
        [Setting]
        [Limit(1, 20)]
        public int WhirlwindAoeCount { get; set; }

        [XmlElement("WhirlwindClusterRange")]
        [DisplayName("Whirlwind Cluster Range")]
        [Category("Barbarian")]
        [DefaultValue(12f)]
        [Description("Range to calculate WW movement points, will auto use on Elites")]
        [Setting]
        [Limit(6f, 20f)]
        public float WhirlwindClusterRange { get; set; }

        [XmlElement("MaximumRange")]
        [DisplayName("Maximum Range")]
        [Category("Barbarian")]
        [DefaultValue(15f)]
        [Description("The maximum range for attacks.")]
        [Setting]
        [Limit(7, 70)]
        public float MaximumRange { get; set; }

        [XmlElement("IsThrowBarbEnabled")]
        [DisplayName("Throw Barb")]
        [Category("Barbarian")]
        [DefaultValue(false)]
        [Description("Enable when you're using a Throw Barb.")]
        [Setting]
        public bool IsThrowBarbEnabled { get; set; }

        [XmlElement("UseLeapForMovement")]
        [DefaultValue(false)]
        [Category("Movement")]
        [Setting]
        [DisplayName("Use Leap for Movement")]
        [Description("Uses Leap for movement outside of combat")]
        public bool UseLeapForMovement { get; set; }

        [XmlElement("LeapDistance")]
        [DefaultValue(35f)]
        [Category("Movement")]
        [Setting]
        [DisplayName("Leap Distance")]
        [Description("The distance inbetween points before Leaping")]
        [Limit(0, 100)]
        public float LeapDistance { get; set; }

        [XmlElement("UseFuriousChargeForMovement")]
        [DefaultValue(false)]
        [Category("Movement")]
        [Setting]
        [DisplayName("Use Furious Charge for movement")]
        [Description("Uses Furious Charge for movement outside of combat")]
        public bool UseFuriousChargeForMovement { get; set; }

        [XmlElement("FuriousChargeDistance")]
        [DefaultValue(30f)]
        [Category("Movement")]
        [Setting]
        [DisplayName("Furious Charge Distance")]
        [Description("The distance inbetween points before Charging")]
        [Limit(0, 100)]
        public float FuriousChargeDistance { get; set; }
    }
}