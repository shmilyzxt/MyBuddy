using System.ComponentModel;
using System.Configuration;
using System.IO;
using Belphegor.GUI;
using Zeta.Common.Xml;
using Zeta.XmlEngine;

namespace Belphegor.Settings
{
    [XmlElement("WitchDoctorSettings")]
    public class WitchDoctorSettings : XmlSettings
    {
        public WitchDoctorSettings()
            : base(Path.Combine(Path.Combine(SettingsDirectory, "Belphegor"), "WitchDoctorSettings.xml"))
        {
        }

        [XmlElement("MaximumRange")]
        [DisplayName("Maximum Range")]
        [Category("Witch Doctor")]
        [DefaultValue(30f)]
        [Description("The maximum range for attacks.")]
        [Setting]
        [Limit(7, 100)]
        public float MaximumRange { get; set; }

        [XmlElement("MinimumRange")]
        [DisplayName("Minimum Range")]
        [Category("Witch Doctor")]
        [DefaultValue(15f)]
        [Description("The minimum range for attacks.")]
        [Setting]
        [Limit(7, 100)]
        public float MinimumRange { get; set; }

        [XmlElement("SoulHarvestAoECount")]
        [DisplayName("Soul Harvest AoE Count")]
        [Category("Terror")]
        [DefaultValue(4)]
        [Setting]
        [Limit(1, 20)]
        public int SoulHarvestAoECount { get; set; }

        [XmlElement("BigBadVoodooAoECount")]
        [DisplayName("Big Bad Voodoo AoE Count")]
        [Category("Voodoo")]
        [DefaultValue(4)]
        [Setting]
        [Limit(1, 20)]
        public int BigBadVoodooAoECount { get; set; }

        [XmlElement("FetishArmyAoECount")]
        [DisplayName("Fetish Army AoE Count")]
        [Category("Voodoo")]
        [DefaultValue(4)]
        [Setting]
        [Limit(1, 20)]
        public int FetishArmyAoECount { get; set; }

        [XmlElement("HorrifyAoECount")]
        [DisplayName("Horrify AoE Count")]
        [Category("Defensive")]
        [DefaultValue(4)]
        [Setting]
        [Limit(1, 20)]
        public int HorrifyAoECount { get; set; }

        [XmlElement("MassConfusionAoECount")]
        [DisplayName("Mass Confusion AoE Count")]
        [Category("Terror")]
        [DefaultValue(4)]
        [Setting]
        [Limit(1, 20)]
        public int MassConfusionAoECount { get; set; }

        [XmlElement("AcidCloudAoECount")]
        [DisplayName("Acid Cloud AoE Count")]
        [Category("Decay")]
        [DefaultValue(4)]
        [Setting]
        [Limit(1, 20)]
        public int AcidCloudAoECount { get; set; }

        [XmlElement("FirebatsAoECount")]
        [DisplayName("Firebat's AoE Count")]
        [Category("Secondary")]
        [DefaultValue(3)]
        [Setting]
        [Limit(1, 20)]
        public int FirebatsAoECount { get; set; }

        [XmlElement("WallOfZombiesAoECount")]
        [DisplayName("Wall Of Zombies AoE Count")]
        [Category("Decay")]
        [DefaultValue(3)]
        [Setting]
        [Limit(1, 20)]
        public int WallOfZombiesAoECount { get; set; }

        [XmlElement("LocustSwarmAoECount")]
        [DisplayName("Locust Swarm AoE Count")]
        [Category("Secondary")]
        [DefaultValue(3)]
        [Setting]
        [Limit(1, 20)]
        public int LocustSwarmAoECount { get; set; }

        [XmlElement("SacrificeHp")]
        [DisplayName("Sacrifice Hp")]
        [Category("Terror")]
        [Description("Health % to use Sacrifice")]
        [DefaultValue(0.5)]
        [Setting]
        [Limit(0, 1)]
        public double SacrificeHp { get; set; }

		[XmlElement("SpiritWalkHp")]
		[DisplayName("Spirit Walk Hp")]
		[Category("Defensive")]
		[Description("Health % to use Spirit Walk")]
		[DefaultValue(0.4)]
		[Setting]
		[Limit(0, 1)]
		public double SpiritWalkHp { get; set; }

        [XmlElement("UseSpiritWalkForMovment")]
        [DefaultValue(false)]
        [Setting]
        [Category("Movement")]
        [DisplayName("Use Spirit Walk for movement")]
        [Description("Use Spirit Walk for movement outside of combat")]
        public bool UseSpiritWalkForMovement { get; set; }
    }
}