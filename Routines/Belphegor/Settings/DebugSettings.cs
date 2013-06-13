using System.ComponentModel;
using System.Configuration;
using System.IO;
using Zeta.Common.Xml;
using Zeta.XmlEngine;

namespace Belphegor.Settings
{
    [XmlElement("DebugSettings")]
    internal class DebugSettings : XmlSettings
    {
        public DebugSettings() :
            base(Path.Combine(Path.Combine(SettingsDirectory, "Belphegor"), "DebugSettings.xml"))
        {
        }

        [XmlElement("IsDebugTabActive")]
        [DisplayName("Is Debug Tab Active")]
        [Description("Adds a debug window to the GUI.")]
        [DefaultValue(false)]
        [Setting]
        [Category("Debug")]
        public bool IsDebugTabActive { get; set; }

        [XmlElement("IsDebugDrawingActive")]
        [DisplayName("Activate Debug drawing")]
        [Description("Enable only when you have ObscuR's MiniMap installed.")]
        [DefaultValue(false)]
        [Setting]
        [Category("Debug")]
        public bool IsDebugDrawingActive { get; set; }

        [XmlElement("IsDebugCastLoggingActive")]
        [DisplayName("Log UsePower Time")]
        [DefaultValue(false)]
        [Setting]
        [Category("Debug")]
        public bool IsDebugCastLoggingActive { get; set; }

        [XmlElement("IsDebugCanCastLogging")]
        [DisplayName("Log CanCast Time")]
        [DefaultValue(false)]
        [Setting]
        [Category("Debug")]
        public bool IsDebugCanCastLogging { get; set; }

        [XmlElement("IsDebugClusterLoggingActive")]
        [DisplayName("Log Cluster Counts")]
        [Description("Shows the return value for any cluster count performed")]
        [DefaultValue(false)]
        [Setting]
        [Category("Debug")]
        public bool IsDebugClusterLoggingActive { get; set; }

        [XmlElement("IsDebugTreeExecutionLoggingActive")]
        [DisplayName("Log Tree Execution Time")]
        [DefaultValue(false)]
        [Setting]
        [Category("Debug")]
        public bool IsDebugTreeExecutionLoggingActive { get; set; }

        [XmlElement("IsDebugSearchAreaProviderLoggingActive")]
        [DisplayName("Log Search Area Provider update.")]
        [DefaultValue(false)]
        [Setting]
        [Category("Debug")]
        public bool IsDebugSearchAreaProviderLoggingActive { get; set; }

        [XmlElement("IsDebugTargetProviderLoggingActive")]
        [DisplayName("Log Target Provider update.")]
        [DefaultValue(false)]
        [Setting]
        [Category("Debug")]
        public bool IsDebugTargetProviderLoggingActive { get; set; }

        [XmlElement("IsDebugHotbarCacheLog")]
        [DisplayName("Logs the hotbar cache update.")]
        [Description("Logs the current users powers when a refesh of them is called.")]
        [DefaultValue(false)]
        [Setting]
        [Category("Debug")]
        public bool IsDebugHotbarCacheLog { get; set; }

        [XmlElement("IsDebugAvoidanceLog")]
        [DisplayName("Logs avoidance.")]
        [Description("Logs the current avoidance action.")]
        [DefaultValue(false)]
        [Setting]
        [Category("Debug")]
        public bool IsDebugAvoidanceLog { get; set; }
    }
}