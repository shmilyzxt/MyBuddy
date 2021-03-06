﻿using System;
using GilesTrinity.Technicals;
using Zeta;
using Zeta.CommonBot.Profile;
using Zeta.TreeSharp;
using Zeta.XmlEngine;

namespace GilesTrinity.XmlTags
{
    // * TrinityUseReset - Resets a UseOnce tag as if it has never been used
    [XmlElement("TrinityCastSweepingWinds")]
    public class TrinityCastSweepingWinds : ProfileBehavior
    {
        private bool m_IsDone = false;

        public override bool IsDone
        {
            get { return m_IsDone; }
        }

        protected override Composite CreateBehavior()
        {
            return new Zeta.TreeSharp.Action(ret =>
            {
                if (ZetaDia.Me.ActorClass == Zeta.Internals.Actors.ActorClass.Monk && GilesTrinity.Hotbar.Contains(Zeta.Internals.Actors.SNOPower.Monk_SweepingWind)
                    && GilesTrinity.Settings.Combat.Monk.HasInnaSet && GilesTrinity.PlayerStatus.PrimaryResource > 10)
                {
                    if (DateTime.Now.Subtract(GilesTrinity.SweepWindSpam).TotalMilliseconds >= 1500)
                    {
                        if (GilesTrinity.GetHasBuff(Zeta.Internals.Actors.SNOPower.Monk_SweepingWind))
                        {
                            ZetaDia.Me.UsePower(Zeta.Internals.Actors.SNOPower.Monk_SweepingWind, GilesTrinity.PlayerStatus.CurrentPosition, GilesTrinity.CurrentWorldDynamicId, -1);
                            GilesTrinity.SweepWindSpam = DateTime.Now;
                            DbHelper.Log(TrinityLogLevel.Normal, LogCategory.ProfileTag, "Cast Sweeping Winds.");
                        }
                        else
                        {
                            DbHelper.Log(TrinityLogLevel.Normal, LogCategory.ProfileTag, "Sweeping winds buff is down - not casting.");
                        }
                    }
                    else
                    {
                        DbHelper.Log(TrinityLogLevel.Normal, LogCategory.ProfileTag, " Too soon to cast SW again, avoiding spam.");
                    }
                }
                m_IsDone = true;
            });
        }

        public override void ResetCachedDone()
        {
            m_IsDone = false;
            base.ResetCachedDone();
        }
    }
}