using System.ComponentModel;
using System.Configuration;
using System.IO;
using Belphegor.GUI;
using Zeta.Common.Xml;
using Zeta.XmlEngine;

namespace Belphegor.Settings
{
    [XmlElement("MonkSettings")]
    public class MonkSettings : XmlSettings
    {
        public MonkSettings()
            : base(Path.Combine(Path.Combine(SettingsDirectory, "Belphegor"), "MonkSettings.xml"))
        {
        }

        [XmlElement("MaximumRange")]
        [DisplayName("Maximum Range")]
        [Category("Monk")]
        [DefaultValue(15f)]
        [Description("The maximum range for attacks.")]
        [Setting]
        [Limit(7, 50)]
        public float MaximumRange { get; set; }

        [XmlElement("WaitForSweepingWind")]
        [DisplayName("Wait for Sweeping Winds")]
        [Category("Mantra's")]
        [Description("Wont cast mantra's unless Sweeping Wind is active")]
        [DefaultValue(false)]
        [Setting]
        public bool WaitForSweepingWind { get; set; }

        [XmlElement("MantraSpirit")]
        [DisplayName("Mantra Spirit")]
        [Category("Mantra's")]
        [Description("Delays casting mantra's until you have this amount of spirit")]
        [DefaultValue(50)]
        [Limit(50, 100)]
        [Setting]
        public int MantraSpirit { get; set; }

        [XmlElement("LashingTailKickAoECount")]
        [DisplayName("Lashing Tail Kick AoE Count")]
        [Category("Secondary")]
        [Description("Will Auto cast on eliests")]
        [DefaultValue(3)]
        [Limit(1, 20)]
        [Setting]
        public int LashingTailKickAoECount { get; set; }

        [XmlElement("LashingTailKickSpiritTreshold")]
        [DisplayName("Lashing Tail Kick Spirit Treshold")]
        [Category("Secondary")]
        [Description("Will prevent Lashing Tail Kick from being casted when spirit is below this value.")]
        [DefaultValue(0f)]
        [Limit(0f, 150f)]
        [Setting]
        public float LashingTailKickSpiritTreshold { get; set; }

        [XmlElement("CycloneStrikeAoECount")]
        [DisplayName("Cyclone Strike AoE Count")]
        [Category("Focus")]
        [Description("Will Auto cast on eliests")]
        [DefaultValue(3)]
        [Limit(1, 20)]
        [Setting]
        public int CycloneStrikeAoECount { get; set; }

        [XmlElement("SevenSidedStrikeAoECount")]
        [DisplayName("Seven-Sided Strike AoE Count")]
        [Category("Focus")]
        [Description("Will Auto cast on eliests")]
        [DefaultValue(3)]
        [Limit(1, 20)]
        [Setting]
        public int SevenSidedStrikeAoECount { get; set; }


        [XmlElement("ExplodingPalmDelay")]
        [DisplayName("ExplodingPalmDelay")]
        [Category("Techniques")]
        [Description("Sets the delay on using Exploding Palm")]
        [DefaultValue(3)]
        [Limit(1, 15)]
        [Setting]
        public int ExplodingPalmDelay { get; set; }

        [XmlElement("SpamMantra")]
        [DisplayName("Spam Mantra's")]
        [Category("Mantra's")]
        [Description("Spam's Mantra's every 3 seconds")]
        [DefaultValue(false)]
        [Setting]
        public bool SpamMantra { get; set; }

        [XmlElement("SerenityHp")]
        [DisplayName("Serenity Hp")]
        [Category("Defensive")]
        [Description("Health % to use Serenity")]
        [DefaultValue(0.6)]
        [Setting]
        [Limit(0, 1)]
        public double SerenityHp { get; set; }

        [XmlElement("BreathOfHeavenHp")]
        [DisplayName("Breath Of Heaven Hp")]
        [Category("Defensive")]
        [Description("Health % to use Breath Of Heaven")]
        [DefaultValue(0.7)]
        [Setting]
        [Limit(0, 1)]
        public double BreathOfHeavenHp { get; set; }

        [XmlElement("BoHBlazingWrath")]
        [DisplayName("Breath Of Heaven (Blazing Wrath Rune)")]
        [Category("Defensive")]
        [Description("Use Breath Of Heaven to get the buff from Blazing Wrath Rune")]
        [DefaultValue(false)]
        [Setting]
        public bool BoHBlazingWrath { get; set; }

        [XmlElement("BoHBlazingWrathOutOfCombatSpiritTreshold")]
        [DisplayName("Blazing Wrath out of combat Spirit Treshold")]
        [Category("Defensive")]
        [Description(
            "Will prevent Breath Of Heaven from being casted when spirit is below this value and not in combat.")]
        [DefaultValue(0f)]
        [Limit(0f, 150f)]
        [Setting]
        public float BoHBlazingWrathOutOfCombatSpiritTreshold { get; set; }

        [XmlElement("UseTempestRushForMovement")]
        [DefaultValue(false)]
        [Setting]
        [Category("Movement")]
        [DisplayName("Use Tempest Rush for movement")]
        [Description("Uses Tempest Rush for movement outside of combat")]
        public bool UseTempestRushForMovement { get; set; }
    }
}