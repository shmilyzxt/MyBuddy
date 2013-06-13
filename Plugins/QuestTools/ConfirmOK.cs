using System;
using Zeta;
using Zeta.Common;
using Zeta.CommonBot.Profile;
using Zeta.Internals.Actors;
using Zeta.TreeSharp;
using Zeta.XmlEngine;

namespace QuestTools
{
    [XmlElement("ConfirmOK")]
    class ConfirmOK : ProfileBehavior
    {
        private bool m_IsDone = false;
        public override bool IsDone
        {
            get { return m_IsDone; }
        }

        protected override Composite CreateBehavior()
        {
            try
            {
                Zeta.Internals.UIElement okBtn = Zeta.Internals.UIElements.ConfirmationDialogOkButton;
                healthCheck();
                return new PrioritySelector(
                    new Decorator(ret => isBusy(),
                        new Zeta.TreeSharp.Action(delegate 
                            {
                            if (isBusy())
                                return RunStatus.Running;
                            else
                                return RunStatus.Success;
                            }
                        )
                    ),
                    new Decorator(ret => okBtn != null && okBtn.IsValid && okBtn.IsVisible,
                        new Zeta.TreeSharp.Action(delegate
                            {
                                Logging.WriteDiagnostic("[ConfirmOK] Clicking OK");
                                okBtn.Click();
                                return RunStatus.Success;
                            }
                        )
                    ),
                    new Zeta.TreeSharp.Action(delegate 
                        {
                            Logging.WriteDiagnostic("[ConfirmOK] Finished");
                            m_IsDone = true;
                        }
                    )
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
        private bool healthCheck()
        {
            if (!(QuestId == ZetaDia.CurrentQuest.QuestSNO && StepId == ZetaDia.CurrentQuest.StepId))
            {
                m_IsDone = true;
                return false;
            }
            else
            {
                return true;
            }
        }

        private static bool isBusy()
        {
            return (ZetaDia.Me != null && ZetaDia.Me.CommonData != null) && (
                                                ZetaDia.Me.CommonData.AnimationState == AnimationState.Casting ||
                                                ZetaDia.Me.CommonData.AnimationState == AnimationState.Channeling)
                                                && !ZetaDia.IsLoadingWorld;
        }
        public override void ResetCachedDone()
        {
            m_IsDone = false;
            base.ResetCachedDone();
        }

    }
}
