using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Zeta.CommonBot.Profile;
using Zeta.XmlEngine;
using Zeta.TreeSharp;
using Zeta.Internals.Actors;
using Zeta;
using Zeta.Common;
using Zeta.CommonBot.Profile.Common;
using Zeta.Navigation;
using Zeta.Pathfinding;
using System.Diagnostics;
using Zeta.CommonBot.Profile.Composites;

namespace QuestTools
{
    [XmlElement("CustomCondition")]
    class CustomCondition : IfTag
    {
        private bool m_IsDone = false;
        /// <summary>
        /// Setting this to true will cause the Tree Walker to continue to the next profile tag
        /// </summary>
        public override bool IsDone
        {
            get { return m_IsDone; }
        }

        /// <summary>
        /// Initializes this instance of SafeMoveTo
        /// </summary>
        /// <returns></returns>
        private bool init()
        {
            Navigator.Clear();

            return true;
        }
        private bool initDone = false;


        /// <summary>
        /// Main SafeMoveTo behavior
        /// </summary>
        /// <returns></returns>
        protected override Composite CreateBehavior()
        {
            if (!initDone)
                initDone = init();
            
            try
            {
                return new Zeta.TreeSharp.Action(delegate
                    {

                    }
                );
            }
            catch (Exception ex)
            {
                Logging.WriteException(ex);
                return new Zeta.TreeSharp.Action(delegate
                    {
                        m_IsDone = true;
                    });
            }
        }
        
        /// <summary>
        /// Are we still in game and ready to go?
        /// </summary>
        /// <returns></returns>
        private bool isValid()
        {
            return ZetaDia.Me.IsValid &&
                ZetaDia.IsInGame &&
                !ZetaDia.IsLoadingWorld &&
                !ZetaDia.Me.IsDead;
        }

        public override void ResetCachedDone()
        {
            m_IsDone = false;
            base.ResetCachedDone();
        }
        public override void OnDone()
        {
            m_IsDone = true;
            base.OnDone();
        }
    }
}
