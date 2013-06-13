using System;
using System.Linq;
using Zeta;
using Zeta.Common;
using Zeta.CommonBot;
using Zeta.CommonBot.Profile;
using Zeta.Internals.Actors;
using Zeta.Navigation;
using Zeta.TreeSharp;
using Zeta.XmlEngine;
using Action = Zeta.TreeSharp.Action;

namespace QuestTools
{
    [XmlElement("MoveToActor")]
    class MoveToActor : ProfileBehavior
    {
        private bool _done = false;
        public override bool IsDone
        {
            get { return !IsActiveQuestStep || _done; }
        }

        [XmlAttribute("x")]
        public float X { get; set; }

        [XmlAttribute("y")]
        public float Y { get; set; }

        [XmlAttribute("z")]
        public float Z { get; set; }

        public Vector3 Position
        {
            get { return new Vector3(X, Y, Z); }
            set { X = value.X; Y = value.Y; Z = value.Z; }
        }

        /// <summary>
        /// Defines how close we need to be to the actor in order to interact with it. Default=10
        /// </summary>
        [XmlAttribute("pathPrecision")]
        [XmlAttribute("interactRange")]
        public int InteractRange { get; set; }

        [XmlAttribute("straightLinePathing")]
        public bool StraightLinePathing { get; set; }

        [XmlAttribute("useNavigator")]
        public bool UseNavigator { get; set; }

        /// <summary>
        /// The ActorSNO of the object you're looking for - optional
        /// </summary>
        [XmlAttribute("actorId")]
        public int ActorId { get; set; }

        /// <summary>
        /// The number of interact attempts before giving up. Default=5
        /// </summary>
        [XmlAttribute("interactAttempts")]
        public int InteractAttempts { get; set; }

        /// <summary>
        /// The "safe" distance that we will request a dynamic nav point to. We will never actually reach this nav point as it's always going to be <see cref="pathPointLimit"/> away.
        /// If the target is closer than this distance, we will just move to the target.
        /// </summary>
        [XmlAttribute("pathPointLimit")]
        public int pathPointLimit { get; set; }

        /// <summary>
        /// Boolean defining special portal handling
        /// </summary>
        [XmlAttribute("isPortal")]
        public bool IsPortal { get; set; }

        /// <summary>
        /// Required if using IsPortal
        /// </summary>
        [XmlAttribute("destinationWorldId")]
        public int DestinationWorldId { get; set; }

        /// <summary>
        /// When searching for An ActorID at Position, what's the maximum distance from Position that will result in a valid Actor?
        /// </summary>
        [XmlAttribute("maxSearchDistance")]
        public int MaxSearchDistance { get; set; }

        /// <summary>
        /// This is the longest time this behavior can run for. Default is 180 seconds (3 minutes).
        /// </summary>
        [XmlAttribute("timeout")]
        public int Timeout { get; set; }

        /// <summary>
        /// If the given actor has an animation that is matching this, the behavior will end
        /// </summary>
        [XmlAttribute("endAnimation")]
        public string EndAnimation { get; set; }

        // Special configuration if you want to tweak things:
        private bool bVerbose = false;
        private int interactWaitSeconds = 1;

        // private runtime variables:
        private int completedInteractions = 0;
        private int startingWorldId = 0;
        private bool _initComplete = false;

        private DateTime lastInteract = DateTime.MinValue;
        private DateTime lastPositionUpdate = DateTime.Now;
        private DateTime tagStartTime = DateTime.MinValue;
        private DiaObject actor = null;
        private Vector3 startInteractPosition = Vector3.Zero;
        private Vector3 lastPosition = Vector3.Zero;
        private Vector3 NavTarget = Vector3.Zero;

        private Zeta.SNOAnim endAnimation = SNOAnim.Invalid;

        /// <summary>
        /// Main Behavior
        /// </summary>
        /// <returns></returns>
        protected override Composite CreateBehavior()
        {
            return new PrioritySelector(
                new Decorator(ret => !_initComplete,
                    new Action(ret => Initialize())
                ),
                new Decorator(ret => DateTime.Now.Subtract(tagStartTime).TotalSeconds > Timeout,
                    new Sequence(
                        new Action(ret => Logging.Write("[MoveToActor] Timeout of {0} seconds exceeded for Profile Behavior {1}", Timeout, status())),
                        new Action(ret => _done = true)
                    )
                ),
                new Sequence(
                    new Action(ret => SafeUpdateActor()),
                    new DecoratorContinue(ret => Vector3.Distance(lastPosition, ZetaDia.Me.Position) > 5f,
                        new Sequence(
                            new Action(ret => lastPositionUpdate = DateTime.Now),
                            new Action(ret => lastPosition = ZetaDia.Me.Position)
                        )
                    ),
                    new PrioritySelector(
                        new Decorator(ret => (actor == null || !actor.IsValid) && Position == Vector3.Zero && !WorldHasChanged(),
                            new Sequence(
                                new Action(ret => Logging.WriteDiagnostic("[MoveToActor] ERROR: Could not find an actor or position to move to, finished! {0}", status())),
                                new Action(ret => _done = true)
                            )
                        ),
                        new Decorator(ret => IsPortal && WorldHasChanged(),
                            new Sequence(
                                new PrioritySelector(
                                    new Decorator(ret => DestinationWorldId > 0 && ZetaDia.CurrentWorldId != DestinationWorldId && ZetaDia.CurrentWorldId != startingWorldId,
                                        new Action(ret => Logging.WriteDiagnostic("[MoveToActor] Error! We used a portal intending to go from WorldId={0} to WorldId={1} but ended up in WorldId={2} {3}",
                                            startingWorldId, DestinationWorldId, ZetaDia.CurrentWorldId, status()))
                                    ),
                                    new Action(ret => Logging.WriteDiagnostic("[MoveToActor] Successfully used portal {0} to WorldId {1} {2}", ActorId, ZetaDia.CurrentWorldId, status()))
                                ),
                                new Action(ret => _done = true)
                            )
                        ),
                        new Decorator(ret => (actor == null || !actor.IsValid) && ((MaxSearchDistance > 0 && WithinMaxSearchDistance()) || WithinInteractRange()),
                            new Sequence(
                                new DecoratorContinue(ret => QuestTools.EnableDebugLogging,
                                    new Action(ret => Logging.WriteDiagnostic("[MoveToActor] Finished: Actor {0} not found, within InteractRange {1} and  MaxSearchDistance {2} of Position {3} {4}",
                                        ActorId, InteractRange, MaxSearchDistance, Position, status())
                                    )
                                ),
                                new Action(ret => _done = true)
                            )
                        ),
                        new Decorator(ret => Position.Distance(ZetaDia.Me.Position) > 1500,
                            new Sequence(
                                new Action(ret => Logging.Write("[MoveToActor] ERROR: Position distance is {0} - this is too far! {1}", Position.Distance(ZetaDia.Me.Position), status())),
                                new Action(ret => _done = true)
                            )
                        ),
                        new Decorator(ret => (actor == null || !actor.IsValid),
                            new PrioritySelector(
                                new Decorator(ret => MaxSearchDistance > 0 && !WithinMaxSearchDistance(),
                                    new Action(ret => Move(Position))
                                ),
                                new Decorator(ret => InteractRange > 0 && !WithinInteractRange(),
                                    new Action(ret => Move(Position))
                                )
                            )
                        ),
                        new Decorator(ret => ((!IsPortal && completedInteractions >= InteractAttempts && InteractAttempts > 0) || (IsPortal && WorldHasChanged()) || AnimationMatch() || ConfirmationDialogVisible()),
                            new Sequence(
                                new DecoratorContinue(ret => QuestTools.EnableDebugLogging,
                                    new Action(ret => Logging.WriteDiagnostic("[MoveToActor] Successfully interacted with Actor {0} at Position {1}", actor.ActorSNO, actor.Position))
                                ),
                                new Action(ret => _done = true)
                            )
                        ),
                        new Decorator(ret => InteractAttempts <= 0 && WithinInteractRange(),
                            new Sequence(
                                new DecoratorContinue(ret => QuestTools.EnableDebugLogging,
                                    new Action(ret => Logging.WriteDiagnostic("[MoveToActor] Actor is within interact range {1:0} - no interact attempts", actor.Distance))
                                ),
                                new Action(ret => _done = true)
                            )
                        ),
                        new Decorator(ret => completedInteractions >= InteractAttempts,
                            new Sequence(
                                new Action(ret => Logging.Write("[MoveToActor] Interaction failed after {0} interact attempts", completedInteractions)),
                                new Action(ret => _done = true)
                            )
                        ),
                        new Decorator(ret => !WithinInteractRange(),
                            new Action(ret => Move(actor.Position))
                        ),
                        new Wait(interactWaitSeconds, cnd => ShouldWaitForInteraction(),
                            new Decorator(ret => (WithinInteractRange() || DateTime.Now.Subtract(lastPositionUpdate).TotalMilliseconds > 1500) && completedInteractions < InteractAttempts,
                                InteractSequence()
                            )
                        ),
                        new Action(ret => Logging.WriteDiagnostic("[MoveToActor] No action taken"))
                    )
                )
            );

        }

        private void Initialize(bool reset = false)
        {
            if (InteractRange == 0)
                InteractRange = 10;
            if (InteractAttempts == 0)
                InteractAttempts = 5;
            if (pathPointLimit == 0)
                pathPointLimit = 250;
            startingWorldId = ZetaDia.CurrentWorldId;
            tagStartTime = DateTime.Now;

            if (!String.IsNullOrEmpty(EndAnimation))
            {
                try
                {
                    Enum.TryParse<Zeta.SNOAnim>(EndAnimation, out endAnimation);
                }
                catch
                {
                    endAnimation = SNOAnim.Invalid;
                }
            }

            Navigator.Clear();

            Timeout = 180;

            bVerbose = true;

            interactWaitSeconds = 3;

            completedInteractions = 0;
            startingWorldId = 0;
            lastInteract = DateTime.MinValue;
            actor = null;
            startInteractPosition = Vector3.Zero;
            lastPosition = ZetaDia.Me.Position;
            lastPositionUpdate = DateTime.Now;
            NavTarget = Vector3.Zero;

            if (!_initComplete && !reset)
                Logging.WriteDiagnostic("[MoveToActor] Initialized {0}", status());
            _initComplete = true;
        }

        /// <summary>
        /// Will only update actor if found (useful for some portals which tend to disappear when you stand next to them)
        /// </summary>
        private void SafeUpdateActor()
        {
            DiaObject newActor = null;
            DiaUnit newUnit = null;

            // Find closest actor if we have a position and MaxSearchDistance (only actors within radius MaxSearchDistance from Position)
            if (Position != Vector3.Zero && MaxSearchDistance > 0)
            {
                newActor = ZetaDia.Actors.GetActorsOfType<DiaObject>(true, false)
                    .Where(o => o.ActorSNO == ActorId && o.Position.Distance(Position) <= MaxSearchDistance)
                    .OrderBy(o => Position.Distance(o.Position)).FirstOrDefault();
            }
            // Otherwise just OrderBy distance from Position (any actor found)
            else if (Position != Vector3.Zero)
            {
                newActor = ZetaDia.Actors.GetActorsOfType<DiaObject>(true, false)
                   .Where(o => o.ActorSNO == ActorId)
                   .OrderBy(o => Position.Distance(o.Position)).FirstOrDefault();
            }
            // If all else fails, get first matching Actor closest to Player
            else
            {
                newActor = ZetaDia.Actors.GetActorsOfType<DiaObject>(true, false)
                   .Where(o => o.ActorSNO == ActorId)
                   .OrderBy(o => o.Distance).FirstOrDefault();
            }

            if (newActor != null && newActor.IsValid && newActor.Position != Vector3.Zero)
            {
                Position = newActor.Position;
                actor = newActor;

                switch (newActor.ActorType)
                {
                    case Zeta.Internals.SNO.ActorType.Unit:
                        {
                            newUnit = (DiaUnit)newActor;
                            if (!newUnit.IsDead)
                            {
                                actor = newActor;
                            }
                            else
                                actor = null;
                            break;
                        }
                }
            }
            else
            {
                actor = null;
            }
        }

        private bool ShouldWaitForInteraction()
        {
            return Math.Abs(DateTime.Now.Subtract(lastInteract).TotalSeconds) > interactWaitSeconds;
        }

        /// <summary>
        /// Perform interaction on the given Actor
        /// </summary>
        /// <returns></returns>
        private double DoInteract()
        {
            actor.Interact();

            System.Threading.Thread.Sleep(1000);

            if (startInteractPosition == Vector3.Zero)
                startInteractPosition = ZetaDia.Me.Position;


            completedInteractions++;
            lastInteract = DateTime.Now;

            double lastInteractDuration = Math.Abs(DateTime.Now.Subtract(lastInteract).TotalSeconds);
            if (QuestTools.EnableDebugLogging)
            {
                Logging.WriteDiagnostic("[MoveToActor] Interacting with Object: {0} {1} attempt: {2}, lastInteractDuration: {3:0}",
                    actor.ActorSNO, status(), completedInteractions, lastInteractDuration);
            }
            return lastInteractDuration;
        }

        private Sequence InteractSequence()
        {
            return new Sequence(
                new WaitContinue(3, canRun => !ZetaDia.Me.Movement.IsMoving,
                    new Sleep(250)
                ),
                new DecoratorContinue(ret => actor.ActorType == Zeta.Internals.SNO.ActorType.Gizmo,
                    new PrioritySelector(
                        new Decorator(ret => actor.ActorInfo.GizmoType == Zeta.Internals.SNO.GizmoType.BossPortal,
                            new Action(ret => ZetaDia.Me.UsePower(SNOPower.GizmoOperatePortalWithAnimation, actor.Position))
                        ),
                        new Decorator(ret => actor.ActorInfo.GizmoType == Zeta.Internals.SNO.GizmoType.GizmoGroup_Portal47,
                            new Action(ret => ZetaDia.Me.UsePower(SNOPower.GizmoOperatePortalWithAnimation, actor.Position))
                        ),
                        new Decorator(ret => actor.ActorInfo.GizmoType == Zeta.Internals.SNO.GizmoType.Portal,
                            new Action(ret => ZetaDia.Me.UsePower(SNOPower.GizmoOperatePortalWithAnimation, actor.Position))
                        ),
                        new Decorator(ret => actor.ActorInfo.GizmoType == Zeta.Internals.SNO.GizmoType.DungeonStonePortal,
                            new Action(ret => ZetaDia.Me.UsePower(SNOPower.GizmoOperatePortalWithAnimation, actor.Position))
                        ),
                         new Decorator(ret => actor.ActorInfo.GizmoType == Zeta.Internals.SNO.GizmoType.WeirdGroup57,
                            new Action(ret => ZetaDia.Me.UsePower(SNOPower.Axe_Operate_Gizmo, actor.Position))
                        ),
                        new Decorator(ret => actor.ActorInfo.GizmoType == Zeta.Internals.SNO.GizmoType.Door,
                            new Action(ret => ZetaDia.Me.UsePower(SNOPower.Axe_Operate_Gizmo, actor.Position))
                        ),
                        new Decorator(ret => actor.ActorInfo.GizmoType == Zeta.Internals.SNO.GizmoType.Portal,
                            new Action(ret => ZetaDia.Me.UsePower(SNOPower.Axe_Operate_Gizmo, actor.Position))
                        ),
                        new Action(ret => ZetaDia.Me.UsePower(SNOPower.Axe_Operate_Gizmo, actor.Position))
                   )
                ),
                new DecoratorContinue(ret => actor.ActorType == Zeta.Internals.SNO.ActorType.Unit,
                    new Action(ret => ZetaDia.Me.UsePower(SNOPower.Axe_Operate_NPC, actor.Position))
                ),
                new WaitContinue(1,
                    new Action(ret => actor.Interact())
                ),
                new Action(ret => new Sleep(1500)),
                new DecoratorContinue(cnd => startInteractPosition == Vector3.Zero,
                    new Action(ret => startInteractPosition = ZetaDia.Me.Position)
                ),
                new Action(ret => lastPosition = ZetaDia.Me.Position),
                new Action(ret => completedInteractions++),
                new Action(ret => lastInteract = DateTime.Now),
                new Action(ret => Logging.WriteDiagnostic("[MoveToActor] Interacting with Object: {0} {1} attempt: {2}, lastInteractDuration: {3:0}",
                    actor.ActorSNO, status(), completedInteractions, Math.Abs(DateTime.Now.Subtract(lastInteract).TotalSeconds))),
                new Sleep(500),
                new Action(ret => GameEvents.FireWorldTransferStart())
                );
        }

        private bool WithinMaxSearchDistance()
        {
            return ZetaDia.Me.Position.Distance(Position) < MaxSearchDistance;
        }

        private string interactReason = "";
        private bool WithinInteractRange()
        {
            if (actor != null)
            {
                float distance = ZetaDia.Me.Position.Distance2D(actor.Position);
                float radiusDistance = actor.Distance - actor.CollisionSphere.Radius;
                Vector3 radiusPoint = MathEx.CalculatePointFrom(actor.Position, ZetaDia.Me.Position, actor.CollisionSphere.Radius);
                if (moveResult == MoveResult.ReachedDestination)
                {
                    interactReason = "ReachedDestination";
                    return true;
                }
                if (distance < 7.5f)
                {
                    interactReason = "Distance < 7.5f";
                    return true;
                }
                else if (distance < InteractRange && actor.InLineOfSight && !Navigator.Raycast(ZetaDia.Me.Position, radiusPoint))
                {
                    interactReason = "InLoSRaycast";
                    return true;
                }
                else if (radiusDistance < 2.5f)
                {
                    interactReason = "Radius < 2.5f";
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                interactReason = "DefaultInteractRange";
                return ZetaDia.Me.Position.Distance(Position) < InteractRange;
            }
        }

        /// <summary>
        /// Checks to see if the Confirmation OK Dialog box is visible
        /// </summary>
        /// <returns></returns>
        private bool ConfirmationDialogVisible()
        {
            try
            {
                Zeta.Internals.UIElement okBtn = Zeta.Internals.UIElements.ConfirmationDialogOkButton;

                if (okBtn != null && okBtn.IsValid && okBtn.IsVisible)
                    return true;
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks to see if animation on actor matches EndAnimation
        /// </summary>
        /// <returns></returns>
        private bool AnimationMatch()
        {
            try
            {
                bool match = false;

                if (endAnimation != SNOAnim.Invalid && actor.CommonData.CurrentAnimation == endAnimation)
                {
                    match = true;
                }

                return match;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check to see if isPortal is true and destinationWorldId is defined and we've changed worlds
        /// </summary>
        /// <returns>True if we're in the new, desired world</returns>
        private bool WorldHasChanged()
        {
            try
            {
                if (IsPortal && DestinationWorldId > 0)
                {
                    return completedInteractions > 0 && ZetaDia.CurrentWorldId == DestinationWorldId && ZetaDia.Me.Position.Distance(startInteractPosition) > InteractRange;
                }
                else if (IsPortal && startInteractPosition != Vector3.Zero)
                {
                    return completedInteractions > 0 && ZetaDia.Me.Position.Distance(startInteractPosition) > InteractRange;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }
        private QTNavigator QTNavigator = new QTNavigator();
        private MoveResult moveResult = MoveResult.Moved;
        private bool Move(Vector3 NavTarget)
        {
            bool result = false;
            if (lastPosition != Vector3.Zero && lastPosition.Distance(ZetaDia.Me.Position) >= 1)
            {
                lastPosition = ZetaDia.Me.Position;
            }
            if (NavTarget.Distance(ZetaDia.Me.Position) > pathPointLimit)
                NavTarget = MathEx.CalculatePointFrom(ZetaDia.Me.Position, NavTarget, NavTarget.Distance(ZetaDia.Me.Position) - pathPointLimit);

            if (StraightLinePathing)
            {
                Navigator.PlayerMover.MoveTowards(Position);
                moveResult = MoveResult.Moved;
            }
            else
            {
                moveResult = QTNavigator.MoveTo(NavTarget, status(), true, UseNavigator);
            }
            switch (moveResult)
            {
                case MoveResult.Moved:
                case MoveResult.ReachedDestination:
                case MoveResult.PathGenerated:
                case MoveResult.PathGenerating:
                    result = true;
                    break;
                case MoveResult.PathGenerationFailed:
                case MoveResult.UnstuckAttempt:
                case MoveResult.Failed:
                    break;
            }
            lastPosition = ZetaDia.Me.Position;

            if (QuestTools.EnableDebugLogging)
            {
                Logging.WriteDiagnostic("[MoveToActor] MoveResult: {0} {1}", moveResult.ToString(), status());
            }
            return result;
        }
        private String status()
        {
            String status = "";

            if (bVerbose)
            {
                status = String.Format(
                    "questId=\"{0}\" stepId=\"{1}\" actorId=\"{10}\" x=\"{2:0}\" y=\"{3:0}\" z=\"{4:0}\" interactRange=\"{5}\" interactAttempts={11} distance=\"{6:0}\" maxSearchDistance={7} rayCastDistance={8} lastPosition={9}, isPortal={12} destinationWorldId={13}, startInteractPosition={14} completedInteractAttempts={15} interactReason={16}",
                    ZetaDia.CurrentQuest.QuestSNO, ZetaDia.CurrentQuest.StepId, X, Y, Z, InteractRange, 
                    (actor != null ? actor.Distance : Position.Distance(ZetaDia.Me.Position)),
                    this.MaxSearchDistance, this.pathPointLimit, this.lastPosition, this.ActorId, this.InteractAttempts, this.IsPortal, this.DestinationWorldId, startInteractPosition, completedInteractions, interactReason);
            }
            else
            {
                status = String.Format("questId=\"{0}\" stepId=\"{1}\" x=\"{2:0}\" y=\"{3:0}\" z=\"{4:0}\" interactRange=\"{5}\" interactAttempts={11} maxSearchDistance={7} rayCastDistance={8} lastPosition={9}, actorId=\"{10}\" isPortal={11} destinationWorldId={12}",
                    ZetaDia.CurrentQuest.QuestSNO, ZetaDia.CurrentQuest.StepId, X, Y, Z, InteractRange,
                    this.MaxSearchDistance, this.pathPointLimit, this.lastPosition, this.ActorId, this.InteractAttempts, this.IsPortal, this.DestinationWorldId);
            }
            try
            {
                if (actor != null && actor.IsValid && actor.CommonData != null && actor.CommonData.Position != null)
                {
                    status += String.Format(" actorId=\"{0}\", Name={1} InLineOfSight={2} ActorType={3} Position= {4}",
                        actor.ActorSNO, actor.Name, actor.InLineOfSight, actor.ActorType, QuestTools.GetProfileCoordinates(actor.Position));
                }
            }
            catch
            {
            }
            return status;

        }
        public override void ResetCachedDone()
        {
            Initialize(true);
            _done = false;
            actor = null;
            tagStartTime = DateTime.Now;
            completedInteractions = 0;
            base.ResetCachedDone();
        }
    }
}
