using System.ComponentModel;
using System.Configuration;
using System.IO;
using Belphegor.GUI;
using Zeta.Common.Xml;
using Zeta.XmlEngine;

namespace Belphegor.Settings
{
    [XmlElement("KitingSettings")]
    public class KitingSettings : XmlSettings
    {
        public KitingSettings()
            : base(Path.Combine(Path.Combine(SettingsDirectory, "Belphegor"), "KitingSettings.xml"))
        {
        }

        [XmlElement("IsKitingActive")]
        [DisplayName("Activate Kiting")]
        [Description("Allows Kiting of monsters.")]
        [DefaultValue(false)]
        [Setting]
        public bool IsKitingActive { get; set; }

        [XmlElement("AggroRange")]
        [DisplayName("Aggro range")]
        [Description("Range to keep from monsters to not pull aggro.")]
        [DefaultValue(35f)]
        [Setting]
        [Limit(0, 40)]
        public float AggroRange { get; set; }

        [XmlElement("MaximumRange")]
        [DisplayName("Maximum Range")]
        [DefaultValue(30f)]
        [Description("The maximum range for kiting.")]
        [Setting]
        [Limit(7, 100)]
        public float MaximumRange { get; set; }

        [XmlElement("MinimumRange")]
        [DisplayName("Minimum Range")]
        [DefaultValue(15f)]
        [Description("The minimum range for kiting.")]
        [Setting]
        [Limit(7, 100)]
        public float MinimumRange { get; set; }
    }
}