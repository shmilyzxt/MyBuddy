using Zeta.Common;
using Zeta.CommonBot;
using Zeta.Internals.Actors;
using Zeta.TreeSharp;

namespace Belphegor.Helpers
{
    public static class Movement
    {
        public static Composite MoveToLineOfSight(ValueRetriever<DiaUnit> unit)
        {
            return
                new Decorator(ret => unit != null && !unit(ret).InLineOfSight,
                              CommonBehaviors.MoveToLos(unit, true)
                    );
        }

        public static Composite MoveTo(ValueRetriever<Vector3> position, float range)
        {
            return CommonBehaviors.MoveAndStop(position, range, true, "Target Position");
        }
    }
}