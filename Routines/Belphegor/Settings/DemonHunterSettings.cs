using System.ComponentModel;
using System.Configuration;
using System.IO;
using Belphegor.GUI;
using Zeta.Common.Xml;
using Zeta.XmlEngine;

namespace Belphegor.Settings
{
    [XmlElement("DemonHunterSettings")]
    public class DemonHunterSettings : XmlSettings
    {
        public DemonHunterSettings() :
            base(Path.Combine(Path.Combine(SettingsDirectory, "Belphegor"), "DemonHunterSettings.xml"))
        {
        }

        [XmlElement("MaximumRange")]
        [DisplayName("Maximum Range")]
        [Category("Demon Hunter")]
        [DefaultValue(30f)]
        [Description("The maximum range for attacks.")]
        [Setting]
        [Limit(7, 100)]
        public float MaximumRange { get; set; }

        [XmlElement("MinimumRange")]
        [DisplayName("Minimum Range")]
        [Category("Demon Hunter")]
        [DefaultValue(15f)]
        [Description("The minimum range for attacks.")]
        [Setting]
        [Limit(7, 100)]
        public float MinimumRange { get; set; }

        [XmlElement("StandInCaltrops")]
        [DefaultValue(false)]
        [Setting]
        [Category("Defensive")]
        [DisplayName("Caltrops - Bait the Trap Rune")]
        [Description("When stood in the trap with this rune gives you a 10% crit buff.")]
        public bool StandInCaltrops { get; set; }

		[XmlElement("ShurikenCloud")]
		[DefaultValue(false)]
		[Setting]
		[Category("Secondary")]
		[DisplayName("Chakram - Shuriken Cloud")]
		[Description("Will only cast Chakram every 120 seconds to keep up Shuriken Cloud buff.")]
		public bool ShurikenCloud { get; set; }

        [XmlElement("CaltropsAoECount")]
        [DefaultValue(3)]
        [Setting]
        [Limit(0, 20)]
        [Category("Defensive")]
        [DisplayName("Caltrop AoE Count")]
        [Description("The number of normal mobs in an area before you use Caltrops")]
        public int CaltropsAoECount { get; set; }

        [XmlElement("SpikeTrapAoECount")]
        [DefaultValue(3)]
        [Setting]
        [Limit(0, 20)]
        [Category("Devices")]
        [DisplayName("Spike Trap AoE Count")]
        [Description("The number of normal mobs in an area before you use Spike Trap's")]
        public int SpikeTrapAoECount { get; set; }

        [XmlElement("SpamSmokeScreen")]
        [DefaultValue(false)]
        [Setting]
        [Category("Defensive")]
        [DisplayName("Spam Smoke Screen")]
        [Description("Spams Smoke Screen every 10 seconds")]
        public bool SpamSmokeScreen { get; set; }

        [XmlElement("SmokeScreenHP")]
        [DefaultValue(0.50)]
        [Setting]
        [Category("Defensive")]
        [DisplayName("Smoke Screen Health %")]
        [Description("Your health % to use Smoke Screen")]
        [Limit(0, 1)]
        public double SmokeScreenHP { get; set; }

        [XmlElement("ShadowPowerHp")]
        [DefaultValue(0.50)]
        [Setting]
        [Category("Defensive")]
        [DisplayName("Shadow Power Health %")]
        [Description("Your health % to use Shadow Power")]
        [Limit(0, 1)]
        public double ShadowPowerHp { get; set; }

        [XmlElement("OnlyEvasiveFireWhenClose")]
        [DisplayName("Only use Evasive Fire when close to a mob")]
        [Setting]
        [Category("Devices")]
        [DefaultValue(false)]
        public bool OnlyEvasiveFireWhenClose { get; set; }

        [XmlElement("FanOfKnivesAoECount")]
        [DefaultValue(3)]
        [Setting]
        [Category("Devices")]
        [DisplayName("Fan of Knives AoE Count")]
        [Description("The number of monsters that need to be near you to active Fan of Knives.")]
        [Limit(0, 20)]
        public int FanOfKnivesAoECount { get; set; }

        [XmlElement("PrperationDiscipline")]
        [DefaultValue(10)]
        [Setting]
        [Category("Hunting")]
        [DisplayName("Preparation Discipline")]
        [Description("Your Discipline level before useing Preparation")]
        [Limit(1, 30)]
        public int PrperationDiscipline { get; set; }

        [XmlElement("VaultDistance")]
        [DefaultValue(0)]
        [Setting]
        [Category("Movement")]
        [DisplayName("Vault Distance")]
        [Description("The distance inbetween points before Vaulting")]
        [Limit(35, 100)]
        public float VaultDistance { get; set; }

        [XmlElement("VaultDiscipline")]
        [DefaultValue(35)]
        [Setting]
        [Category("Movement")]
        [DisplayName("Vault discipline")]
        [Description("The discipline threshold for Vaulting")]
        [Limit(0, 100)]
        public int VaultDiscipline { get; set; }

        [XmlElement("UseShadowPowerForMovement")]
        [DisplayName("Use ShadowPower for movement")]
        [Setting]
        [Category("Movement")]
        [DefaultValue(false)]
        public bool UseShadowPowerForMovement { get; set; }

        [XmlElement("UsePreparationForMovement")]
        [DisplayName("Use Preparation for movement")]
        [Setting]
        [Category("Movement")]
        [DefaultValue(false)]
        public bool UsePreparationForMovement { get; set; }

        [XmlElement("Vault")]
        [DefaultValue(true)]
        [Setting]
        [Category("Movement")]
        [DisplayName("Use Vault for movement")]
        [Description("Uses Vault for movement outside of combat")]
        public bool UseVaultForMovement { get; set; }
    }
}