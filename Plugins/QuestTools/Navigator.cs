using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Zeta;
using Zeta.Internals;
using Zeta.Common;
using Zeta.CommonBot;
using Zeta.Pathfinding;
using Zeta.Internals.SNO;
using Zeta.Navigation;
using System.Drawing;

namespace QuestTools
{
    public class QTNavigator : INavigationProvider
    {
        private DateTime m_LastGeneratedRoute = DateTime.MinValue;

        private static ISearchAreaProvider m_gp
        {
            get
            {
                return Navigator.SearchGridProvider as MainGridProvider;
            }
        }

        public QTNavigator()
        {
            PathPrecision = 10f;
        }

        public bool Clear()
        {
            Navigator.Clear();
            return true;
        }

        public MoveResult MoveTo(Vector3 destination, string destinationName = null, bool useRaycast = true)
        {
            return MoveTo(destination, destinationName, useRaycast, true);
        }

        private List<int> ForcePathFindingLevelAreaIds = new List<int>()
        {
            19953,
            62752,
        };

        private List<int> ForceNavigatorWorldSNOs = new List<int>()
        {
            71150,
            70885, 
            95804, 
            109513,
            75049,
            50585,
        };

        public MoveResult MoveTo(Vector3 destination, string destinationName = null, bool useRaycast = true, bool useNavigator = false)
        {
            if (!ZetaDia.IsInGame || !ZetaDia.Me.IsValid || ZetaDia.Me.IsDead || ZetaDia.IsLoadingWorld || !ZetaDia.Service.IsValid || !ZetaDia.WorldInfo.IsValid)
            {
                return MoveResult.Failed;
            }

            return Navigator.MoveTo(destination, destinationName, useRaycast);
        }

        public float PathPrecision { get; set; }
    }
}
