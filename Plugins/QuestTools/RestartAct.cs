using System;
using System.IO;
using System.Text.RegularExpressions;
using Zeta;
using Zeta.Common;
using Zeta.CommonBot;
using Zeta.CommonBot.Profile;
using Zeta.TreeSharp;
using Zeta.XmlEngine;
using Action = Zeta.TreeSharp.Action;

namespace QuestTools
{
    [XmlElement("RestartAct")]
    public class RestartAct : ProfileBehavior
    {
        private bool _isDone = false;
        public override bool IsDone { get { return _isDone; } }

        public override void OnStart()
        {
            Logging.Write("[QuestTools] RestartAct initialized");
        }

        protected override Composite CreateBehavior()
        {
            return
            new Action(ret => ForceRestartAct());
        }

        private RunStatus ForceRestartAct()
        {
            Regex questingProfileName = new Regex(@"Act \d by rrrix");

            string act = "";
            string difficulty = "";

            switch (ZetaDia.CurrentAct)
            {
                case Act.A1: act = "Act1";
                    break;
                case Act.A2: act = "Act2";
                    break;
                case Act.A3: act = "Act3";
                    break;
                case Act.A4: act = "Act4";
                    break;
                default:
                    break;
            }
            switch (ZetaDia.Service.CurrentHero.CurrentDifficulty)
            {
                case GameDifficulty.Normal: difficulty = "Normal";
                    break;
                case GameDifficulty.Nightmare: difficulty = "Nightmare";
                    break;
                case GameDifficulty.Hell: difficulty = "Hell";
                    break;
                case GameDifficulty.Inferno: difficulty = "Inferno";
                    break;
            }

            string restartActProfile = String.Format("{0}_StartNew{1}.xml", act, difficulty);
            Logging.Write("[QuestTools] Restarting Act - loading {0}", restartActProfile);

            string profilePath = Path.Combine(Path.GetDirectoryName(ProfileManager.CurrentProfile.Path), restartActProfile);
            ProfileManager.Load(profilePath);

            return RunStatus.Success;
        }
        public override void ResetCachedDone()
        {
            _isDone = false;
            base.ResetCachedDone();
        }
    }
}
