using System;
using Belphegor.Utilities;
using Zeta;
using Zeta.Common;
using Zeta.Common.Helpers;
using Zeta.CommonBot;
using Zeta.Navigation;
using Zeta.TreeSharp;

namespace Belphegor
{
    public class BelphegorPlayerMover : IPlayerMover
    {
        #region Implementation of IPlayerMover

        public void MoveTowards(Vector3 to)
        {
            var belphegor = RoutineManager.Current as Belphegor;
            if (belphegor != null)
            {
                Composite movementProvider = belphegor.Movement;
                if (movementProvider != null)
                {
                    try
                    {
                        if (movementProvider.LastStatus != RunStatus.Running)
                        {
                            movementProvider.Start(to);
                        }
                        movementProvider.Tick(to);
                        if (movementProvider.LastStatus != RunStatus.Running)
                        {
                            movementProvider.Stop(to);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.WriteVerbose("Error occured: {0}", e.Message);
                        Logging.WriteException(e);
                        movementProvider.Stop(to);
                    }
                    return;
                }
            }
            ZetaDia.Me.Movement.MoveActor(to);
        }

        public void MoveStop()
        {
            ZetaDia.Me.Movement.MoveActor(ZetaDia.Me.Position);
        }

        #endregion

        #region Timers

        private static readonly WaitTimer LagTimer = new WaitTimer(TimeSpan.FromSeconds(1));

        static BelphegorPlayerMover()
        {
            GameEvents.OnGameLeft += ResetTimers;
            GameEvents.OnPlayerDied += ResetTimers;
        }

        private static void ResetTimers(object sender, EventArgs e)
        {
            LagTimer.Stop();
        }

        #endregion
    }
}