using System;
using Zeta;
using Zeta.Common;
using Zeta.CommonBot.Profile;
using Zeta.Navigation;
using Zeta.TreeSharp;
using Zeta.XmlEngine;
using Action = Zeta.TreeSharp.Action;

namespace QuestTools
{
    /// <summary>
    /// The default Demonbuddy MoveToTag will often never complete due to a Navigator MoveResult of PathGenerating or PathGenerationFailed.
    /// This custom behavior will fail-safe if it cannot generate a path, and will also use a point of "LocalNavDistance" away between the player and destination 
    /// as its temporary destination. Usually a distance between 150-250 is ideal for pathing, and works in situations like random dungeons and between New Tristram and anywhere in that world.
    /// </summary>
    [XmlElement("SafeMoveTo")]
    class SafeMoveTo : ProfileBehavior
    {
        private bool _done = false;
        public override bool IsDone
        {
            get { return !IsActiveQuestStep || _done; }
        }

        [XmlAttribute("pathPrecision")]
        public int PathPrecision { get; set; }

        [XmlAttribute("straightLinePathing")]
        public bool StraightLinePathing { get; set; }

        [XmlAttribute("useNavigator")]
        public bool UseNavigator { get; set; }

        [XmlAttribute("x")]
        public float X { get; set; }

        [XmlAttribute("y")]
        public float Y { get; set; }

        [XmlAttribute("z")]
        public float Z { get; set; }

        public Vector3 Position { get { return new Vector3(X, Y, Z); } }

        /// <summary>
        /// This is used for very distance Position coordinates; where Demonbuddy cannot make a client-side pathing request 
        /// and has to contact the server. A value too large (over 300) will sometimes cause pathing requests to fail (PathGenerationFailed).
        /// </summary>
        [XmlAttribute("localNavDistance")]
        [XmlAttribute("pathPointLimit")]
        [XmlAttribute("raycastDistance")]
        public int PathPointLimit { get; set; }

        /// <summary>
        /// This will set a time in seconds that this tag is allowed to run for
        /// </summary>
        [XmlAttribute("timeout")]
        public int Timeout { get; set; }

        [XmlAttribute("allowLongDistance")]
        public bool AllowLongDistance { get; set; }

        private Vector3 NavTarget = Vector3.Zero;
        private MoveResult mr = MoveResult.Moved;
        private DateTime tagStartTime = DateTime.MinValue;
        private QTNavigator QTNavigator = new QTNavigator();

        public SafeMoveTo()
        {
            PathPrecision = 10;
            PathPointLimit = 175;
            Timeout = 180;
        }

        /// <summary>
        /// Main SafeMoveTo behavior
        /// </summary>
        /// <returns></returns>
        protected override Composite CreateBehavior()
        {
            return new Sequence(
                new Action(ret => Initialize()),
                new PrioritySelector(
                    new Decorator(ctx => DateTime.Now.Subtract(tagStartTime).TotalSeconds > Timeout,
                        new Sequence(
                            new Action(ctx => Logging.Write("[SafeMoveTo] Timeout of {0} seconds exceeded for Profile Behavior {1}", Timeout, status())),
                            new Action(ctx => _done = true)
                        )
                    ),
                    new PrioritySelector(
                        new Decorator(ctx => !AllowLongDistance && Position.Distance2D(ZetaDia.Me.Position) > 1500,
                            new Sequence(
                                new Action(ret => Logging.Write("[SafeMoveTo] Error! Destination distance is {0}", Position.Distance2D(ZetaDia.Me.Position))),
                                new Action(ret => _done = true)
                            )
                        ),
                        new Decorator(ret => Position.Distance2D(ZetaDia.Me.Position) > PathPrecision,
                            new Action(ret => Move())
                        ),
                        new PrioritySelector(
                            new Decorator(ctx => Position.Distance2D(ZetaDia.Me.Position) <= PathPrecision,
                                new Sequence(
                                    new Action(ctx => Logging.Write(String.Format("[SafeMoveTo]: Reached Destination! {0}", status()))),
                                    new Action(ctx => _done = true)
                                )
                            )
                        )
                    )
                )
            );
        }

        private DateTime lastGeneratedNavPoint = DateTime.MinValue;
        private double maxNavPointAgeMs = 15000;

        private RunStatus Move()
        {
            if (Position.Distance2D(ZetaDia.Me.Position) > PathPrecision)
            {
                NavTarget = Position;

                double timeSinceLastGenerated = DateTime.Now.Subtract(lastGeneratedNavPoint).TotalMilliseconds;

                if (Position.Distance2D(ZetaDia.Me.Position) > PathPointLimit && timeSinceLastGenerated > maxNavPointAgeMs)
                {
                    // generate a local client pathing point
                    NavTarget = MathEx.CalculatePointFrom(ZetaDia.Me.Position, Position, Position.Distance2D(ZetaDia.Me.Position) - PathPointLimit);
                }
                if (StraightLinePathing)
                {
                    // just "Click" 
                    Navigator.PlayerMover.MoveTowards(Position);
                }
                else
                {
                    // Use the Navigator or PathFinder
                    mr = QTNavigator.MoveTo(NavTarget, status(), true, UseNavigator);
                }
                LogStatus();

                return RunStatus.Success;

            }
            else
                return RunStatus.Failure;

        }

        public override void OnStart()
        {
            lastGeneratedNavPoint = DateTime.MinValue;
            QuestTools.PositionCache.Clear();
        }

        private bool _initialized = false;
        private void Initialize()
        {
            if (!_initialized)
            {
                tagStartTime = DateTime.Now;
                Navigator.Clear();
                _initialized = true;
                Logging.WriteVerbose("[SafeMoveTo] Initialized {0}", status());
            }
        }

        private void LogStatus()
        {
            if (QuestTools.EnableDebugLogging)
            {
                double fDistanceToTarget = Math.Round(Position.Distance2D(ZetaDia.Me.Position) / 10.0, 0) * 10;

                Logging.WriteDiagnostic(String.Format("[SafeMoveTo]: distance to target: {0:0} {1}", fDistanceToTarget, status()));
            }
        }

        /// <summary>
        /// Returns a friendly string of variables for logging purposes
        /// </summary>
        /// <returns></returns>
        private String status()
        {
            return String.Format("questId=\"{0}\" stepId=\"{1}\" x=\"{2}\" y=\"{3}\" z=\"{4}\" pathPrecision={5}",
                ZetaDia.CurrentQuest.QuestSNO, ZetaDia.CurrentQuest.StepId, X, Y, Z, PathPrecision);
        }

        public override void ResetCachedDone()
        {
            _done = false;
            lastGeneratedNavPoint = DateTime.MinValue;
        }
    }
}
