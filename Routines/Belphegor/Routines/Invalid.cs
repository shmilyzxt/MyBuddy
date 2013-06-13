using Belphegor.Dynamics;
using Belphegor.Utilities;
using Zeta.Internals.Actors;
using Zeta.TreeSharp;

namespace Belphegor.Routines
{
    public class Invalid
    {
        [Class(ActorClass.Invalid)]
        [Behavior(BehaviorType.All)]
        public static Composite InvalidWrapper()
        {
            return new PrioritySelector(
                //If the class is invalid then its probably in the menu so it dosnt matter as our class will be fetched when we enter
                //new Action(ret => Logger.Write("Bot have run into a problem, most likely bot was started while being in the menu, or restarted with a plugin. Currently Belphegor All-In-One does not support this."))
                );
        }
    }
}