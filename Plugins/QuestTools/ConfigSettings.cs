using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using Zeta;
using Zeta.Common;
using Zeta.Internals;
using Zeta.Common.Plugins;

namespace QuestTools
{
    [Serializable]
    public class QuestToolsSettings : IEquatable<QuestToolsSettings>
    {
        public bool ReloadProfileOnDeath { get; set; }
        public bool EnableDebugLogging { get; set; }

        public static QuestToolsSettings Instance { get; set; }

        private string battleTag = String.Empty;
        private string settingsPath = String.Empty;

        public QuestToolsSettings()
        {
            //battleTag = ZetaDia.Service.CurrentHero.BattleTagName;
            //settingsPath = Zeta.Common.Xml.XmlSettings.SettingsDirectory + "\\" + battleTag + "\\QuestTools.xml";
            //Instance = QuestToolsSettings.LoadConfiguration(settingsPath);
        }

        public static QuestToolsSettings LoadConfiguration(string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(QuestToolsSettings));
            var qts = new QuestToolsSettings();

            FileInfo settingsFile = new FileInfo(path);
            if (settingsFile.Exists)
            {
                try
                {
                    using (var reader = new StreamReader(path))
                    {
                        qts = (QuestToolsSettings)serializer.Deserialize(reader);
                    }
                }
                catch (Exception ex)
                {
                    Logging.WriteException(ex);
                }
            }

            return qts;
        }

        public static void SaveConfiguration(QuestToolsSettings _config)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(QuestToolsSettings));

            try
            {
                using (var writer = new StreamWriter(_config.settingsPath))
                {
                    serializer.Serialize(writer, _config);
                }
            }
            catch (Exception ex)
            {
                Logging.WriteException(ex);
            }

        }

        public bool Equals(QuestToolsSettings other)
        {
            return (other.ReloadProfileOnDeath == this.ReloadProfileOnDeath) && (other.EnableDebugLogging == this.EnableDebugLogging);
        }
    }
}
