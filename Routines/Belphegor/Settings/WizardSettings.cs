using System.ComponentModel;
using System.Configuration;
using System.IO;
using Belphegor.GUI;
using Zeta.Common.Xml;
using Zeta.XmlEngine;

namespace Belphegor.Settings
{
    [XmlElement("WizardSettings")]
    public class WizardSettings : XmlSettings
    {
        public WizardSettings() :
            base(Path.Combine(Path.Combine(SettingsDirectory, "Belphegor"), "WizardSettings.xml"))
        {
        }

        [XmlElement("MaximumRange")]
        [DisplayName("Maximum Range")]
        [Category("Wizard")]
        [DefaultValue(30f)]
        [Description("The maximum range for attacks.")]
        [Setting]
        [Limit(7, 100)]
        public float MaximumRange { get; set; }

        [XmlElement("MinimumRange")]
        [DisplayName("Minimum Range")]
        [Category("Wizard")]
        [DefaultValue(15f)]
        [Description("The minimum range for attacks.")]
        [Setting]
        [Limit(7, 100)]
        public float MinimumRange { get; set; }

        [XmlElement("DiamondSkinHp")]
        [DisplayName("Diamond Skin Hp")]
        [Category("Defensive")]
        [Description("Hp percent to use Diamond Skin")]
        [DefaultValue(0.5)]
        [Setting]
        [Limit(0, 1)]
        public double DiamondSkinHp { get; set; }

        [XmlElement("SlowTimeHp")]
        [DisplayName("Slow Time Hp")]
        [Category("Defensive")]
        [Description("Hp percent to use Slow Time")]
        [DefaultValue(0.5)]
        [Setting]
        [Limit(0, 1)]
        public double SlowTimeHp { get; set; }

        [XmlElement("MirrorImageHp")]
        [DisplayName("Mirror Image Hp")]
        [Category("Mastery")]
        [Description("Hp percent to use Mirror Image")]
        [DefaultValue(0.5)]
        [Setting]
        [Limit(0, 1)]
        public double MirrorImageHp { get; set; }

        [XmlElement("ExplosiveBlastAoECount")]
        [DisplayName("Explosive Blast AoE Count")]
        [Category("Mastery")]
        [Description("Will always cast use on Elites")]
        [DefaultValue(3)]
        [Setting]
        [Limit(1, 20)]
        public int ExplosiveBlastAoECount { get; set; }

        [XmlElement("ArchonAoECount")]
        [DisplayName("Archon AoE Count")]
        [Category("Mastery")]
        [Description("Number of Mobs to use Archon on, will auto use on Elites")]
        [DefaultValue(5)]
        [Setting]
        [Limit(1, 20)]
        public int ArchonAoECount { get; set; }

        [XmlElement("FrostNovaAoECount")]
        [DisplayName("Frost Nova AoE Count")]
        [Category("Defensive")]
        [Description("Will auto use on Elites")]
        [DefaultValue(2)]
        [Setting]
        [Limit(1, 20)]
        public int FrostNovaAoECount { get; set; }


        [XmlElement("EnergyTwisterAoECount")]
        [DisplayName("Energy Twister AoE Count")]
        [Category("Force")]
        [Description("Will auto use on Elites")]
        [DefaultValue(2)]
        [Setting]
        [Limit(1, 20)]
        public int EnergyTwisterAoECount { get; set; }

        [XmlElement("HydraAoECount")]
        [DisplayName("Hydra AoE Count")]
        [Category("Force")]
        [Description("Will auto use on Elites")]
        [DefaultValue(4)]
        [Setting]
        [Limit(1, 20)]
        public int HydraAoECount { get; set; }

        [XmlElement("Teleport")]
        [DefaultValue(true)]
        [Setting]
        [Category("Movement")]
        [DisplayName("Use Teleport for movement")]
        [Description("Use Teleport for movement outside of combat")]
        public bool Teleport { get; set; }

        [XmlElement("TeleportDistance")]
        [DefaultValue(20f)]
        [Setting]
        [Limit(0, 100)]
        [Category("Movement")]
        [DisplayName("Teleport Distance")]
        [Description("The distance inbetween points before Teleporting")]
        public float TeleportDistance { get; set; }

        [XmlElement("UseArchonForMovement")]
        [DefaultValue(false)]
        [Setting]
        [Category("Movement")]
        [DisplayName("Use Archon for movement")]
        [Description("")]
        public bool UseArchonForMovement { get; set; }
    }
}