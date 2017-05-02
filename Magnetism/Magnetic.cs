using FerrousEngine.Common;
using System;
using System.Collections.Generic;
using UnityEngine;
/// An event handler for when the magnetic field changes.
public delegate void FieldChangedHandler(object sender, EventArgs e);

namespace FerrousEngine.Magnetism
{
    public class Magnetic : MonoBehaviour
    {
        // A class that assigns the property "Magnetic" to an object. The object should have two empty child objects denoting the north 
        // and south poles, which are assigned in-editor. The magnet has a small float value "strength" denoting magnet strength.
        // The magnet also has a "forward end" which determines which side of the magnet acts first in the simulation.

        // Pole state. Includes an error case for user convenience.
        public enum Pole
        {
            POSITIVE,
            NEGATIVE,
            ERROR
        };

        // What state is the magnet in? Initialization state included for user convenience.
        public enum State
        {
            INIT,
            ON,
            IN_FIELD,
            NOT_IN_FIELD,
            OFF
        }

        #region Public Properties
        // If the user can flip the staff, enable.
        public bool UserCanFlip = true;
        // Should we assume Unity's physics is enabled?
        [Tooltip("If you want to respect Unity's physics (e.g. your magnet has a Rigidbody2D component and you want collisions with other objects to be handled automatically), enable this. Otherwise, Ferrous Engine ignores collisions.")]
        public bool RespectUnityPhysics = true;
        // The radius of the magnet's circle cast.
        public float Radius = 1.0f;
        // This is public because we want a script to be able to switch it when the user flips the staff.
        public Pole ForwardEnd = Pole.POSITIVE;
        // An event that occurs whenever the player enters or exits a magnetic field.
        public event FieldChangedHandler FieldChanged;
        // The location of the magnet's positive end.
        public Transform PositiveEnd;
        // The location of the magnet's negative end.
        public Transform NegativeEnd;
        // Audio source for when the magnet is activated.
        public AudioSource MagnetActivatedSfx;
        // Audio source for when the magnet finds a field.
        public AudioSource MagnetFieldSfx;
        // Audio source for when the magnet is deactivated.
        public AudioSource MagnetDeactivatedSfx;
        #endregion

        #region State Properties
        // State: For determining an approximation of strength relative to distance.
        private float scalingFactor;
        // State: Whether or not we should cast. Used when the magnet is at rest to cut down on casting time.
        private bool shouldCast = true;
        // State: Is this object offset by its motion manager?
        private Vector2 managerOffset = Vector2.zero;
        // State: The current state of the object.
        public State currentState = State.INIT;
        // State: The previous state of the object. Use: Figuring out when we've changed state.
        private State prevState = State.INIT;
        #endregion

        #region Other Private Properties
        // The strength of the magnet.
        [SerializeField]
        private float Strength = 0.05f;
        // Whether or not we should have gizmos active.
        private bool activeGizmos = false;
        // Have we hit a magnet in this call?
        private bool hitMagnetInThisCall;
        // What layers should we cast on?
        private int layerMask;
        // What end of the magnet is facing forward?
        private Transform forwardEnd;
        // What magnetic surface did we hit in this call?
        private MagneticSurface hitMagneticSurface;
        // What magnetic surface did we hit in the last call? (Use: Determining if we've switched surfaces.)
        private MagneticSurface prevMagneticSurface;
        // What's our current magnet hit?
        private MagnetHit hit;
        // What is our magnet's target position?
        private Vector3 targetPosition;
        // What position were we at previously? (Use: Determining how far we should move if we hit a magnet in the next call.)
        private Vector3 prevPosition;
        // An optional offset to make sure your physics system doesn't favor sinking one object over the other when the magnet touches its target.
        private static Vector3 offset = new Vector3(1f, 0.5f);
        // Our rigidbody, if there is one.
        private new Rigidbody2D rigidbody;
        // For use in caching which magnets we've recently touched. Uses MagnetHit so we can switch between magnets and magnetic surfaces.
        private Dictionary<Transform, MagnetHit> trackedMagnets = new Dictionary<Transform, MagnetHit>();
        // The motion manager for this object, if it has one.
        private MovementRequestManager manager;
        #endregion

        private void Awake()
        {
            // Positional state initialization.
            targetPosition = transform.position;
            prevPosition = transform.position;
            // Variable initialization.
            rigidbody = GetComponent<Rigidbody2D>();
            manager = GetComponentInParent<MovementRequestManager>();
            // Subscribe to when the object sleeps.
            if (manager)
            {
                manager.AwakeEvent += new AwakeHandler(ObjectAwake);
                manager.AsleepEvent += new AsleepHandler(ObjectAsleep);
            }
            // Initialize our magnet's forward end.
            SwitchForwardEnd(ForwardEnd);
            // Our layer mask should consist of everything with the sequence "Magnet" in it.
            layerMask = (1 << LayerMask.NameToLayer("Magnetic Surfaces")) | (1 << LayerMask.NameToLayer("Magnetic Surface Spikes") | 1 << LayerMask.NameToLayer("Magnetic Moving Platforms"));
        }

        private void Start()
        {
            // Offset population.
            if (manager)
            {
                if (manager.GetType().Equals(typeof(MutualFollow)))
                {
                    MutualFollow m = (MutualFollow)manager;
                    if (m.xOffset) { managerOffset.x = (m.RespectUnityPhysics ? m.GetOffset(rigidbody).x : m.GetOffset(transform).x); }
                    if (m.yOffset) { managerOffset.y = (m.RespectUnityPhysics ? m.GetOffset(rigidbody).y : m.GetOffset(transform).y); }
                }
            }
        }

        // If we should cast, cast.
        private void FixedUpdate()
        {
            if (shouldCast) { MagnetCheck(); }
        }

        // Helper method switches the forward end of the magnet
        public void SwitchForwardEnd(Pole end)
        {
            forwardEnd = (end == Pole.POSITIVE) ? PositiveEnd : NegativeEnd;
        }

        public Vector3 GetTargetPos()
        {
            return Vector2.Lerp(transform.position, targetPosition, scalingFactor);
        }

        private void MagnetCheck()
        {
            hitMagneticSurface = null;
            // Switch for whichever pole is active (facing forward) at the time
            Vector3 pos = forwardEnd.position;
            // Cast a field in our radius.
            if (MagneticPhysics.CastField(pos, Radius, forwardEnd.forward, layerMask, out hit))
            {
                // Figure out what kind of magnet we're dealing with and handle appropriately.
                switch (hit.transform.tag)
                {
                    case "Magnetic Surface":
                        {
                            HandleMagneticSurfaces(hit, pos, layerMask);
                            return;
                        }
                    case "Magnetic Surface Spikes":
                        {
                            HandleMagneticSurfaces(hit, pos, layerMask);
                            return;
                        }
                    case "Magnetic Moving Platforms":
                        {
                            HandleMagneticSurfaces(hit, pos, layerMask);
                            return;
                        }
                    case "Track-Draggable Platforms":
                        {
                            hitMagnetInThisCall = true;
                            hitMagneticSurface = hit.transform.GetComponent<MagneticSurface>();
                            hitMagneticSurface.AffectingMagnet = this;
                            FieldReset();
                            return;
                        }
                    case "Magnet":
                        {
                            HandleMagnets(hit);
                            break;
                        }
                    default:
                        // Stay where we are. Neglect Unity physics because we don't care if we're idle.
                        targetPosition = transform.position;
                        break;
                }
            }
            // Clean the magnet's state.
            currentState = State.NOT_IN_FIELD;
            FieldReset();
        }

        // When the object falls asleep and we're not still searching for a surface, stop casting.
        private void ObjectAsleep(object sender, EventArgs e)
        {
            if (hitMagneticSurface) { shouldCast = false; }
        }

        // When the object wakes up, cast.
        private void ObjectAwake(object sender, EventArgs e)
        {
            shouldCast = true;
        }

        // Clean the magnet's field state. Called often. Polish might include making this more efficient.
        private void FieldReset()
        {
            // If our state has changed, notify subscribers.
            if (prevState != currentState)
            {
                OnFieldChanged(EventArgs.Empty);
            }

            prevState = currentState;
            // If we hit a surface but it's not active, activate it.
            if (hitMagneticSurface && !hitMagneticSurface.Active)
            {
                hitMagneticSurface.ToggleActive(true);
            }
            // Otherwise, if we haven't hit a surface but we were just interacting with one, deactivate it.
            else if (!hitMagneticSurface && prevMagneticSurface && prevMagneticSurface.Active)
            {
                prevMagneticSurface.ToggleActive(false);
                if (manager) { manager.DeregisterSender(transform); }
            }
            // Update the surface state.
            prevMagneticSurface = hitMagneticSurface;
        }

        // Helper method for when the magnetic field changes.
        private void OnFieldChanged(EventArgs e)
        {
            if (FieldChanged != null)
            {
                FieldChanged(this, e);
            }
        }

        // Populate the magnet's state.
        private void OnEnable()
        {
            if (MagnetActivatedSfx)
            {
                if (MagnetDeactivatedSfx) { MagnetDeactivatedSfx.Stop(); }
                MagnetActivatedSfx.Play();
            }

            activeGizmos = true;

            currentState = State.ON;
            prevState = State.ON;
            shouldCast = true;

            targetPosition = transform.position;
            prevPosition = transform.position;

        }

        // Clean the magnet's state.
        private void OnDisable()
        {
            if (MagnetDeactivatedSfx)
            {
                if (MagnetActivatedSfx) { MagnetActivatedSfx.Stop(); }
                MagnetDeactivatedSfx.Play();
            }

            if (prevMagneticSurface)
            {
                prevMagneticSurface.ToggleActive(false);
                prevMagneticSurface = null;
            }

            if(hitMagneticSurface)
            {
                hitMagneticSurface.ToggleActive(false);
                hitMagneticSurface = null;
            }

            if (manager)
            {
                manager.DeregisterSender(transform);
            }

            activeGizmos = false;
            currentState = State.OFF;
            prevState = State.OFF;
            shouldCast = false;
        }

        private void HandleMagnets(MagnetHit hit)
        {
            // If it's an actual magnet, determine attraction and repulsion
            // Note that we only need to do this user-side as the other magnet will also have this script attached to it,
            // and will run these calculations when it collides with the player's magnet.
            Magnetic otherMagnet = null;
            // Check if this magnet is cached. If not, cache it.
            if (!trackedMagnets.ContainsKey(hit.transform))
            {
                otherMagnet = hit.transform.GetComponent<Magnetic>();
            }
            else
            {
                otherMagnet = trackedMagnets[hit.transform].magnet;
                trackedMagnets[hit.transform] = new MagnetHit(hit.point, hit.distance, otherMagnet, hit.transform);
            }
            // Set our target position appropriately.
            if (otherMagnet && otherMagnet.transform != transform)
            {
                if (otherMagnet.ForwardEnd == ForwardEnd)
                {
                    targetPosition = forwardEnd.position + (transform.position - hit.point) * Strength;
                }
                else
                {
                    targetPosition = forwardEnd.position - hit.point;
                }

                MoveMagnet(targetPosition);
            }
        }

        private void HandleMagneticSurfaces(MagnetHit hit, Vector3 pos, int layerMask)
        {
            MagneticSurface surf = null;
            // Check if this surface is already cached. If it's not, cache it.
            if (!trackedMagnets.ContainsKey(hit.transform))
            {
                surf = hit.transform.GetComponent<MagneticSurface>();
            }
            else
            {
                surf = trackedMagnets[hit.transform].surface;
            }
            // Check whether or not we successfully found a MagneticSurface. No error case because the script should error out if we don't,
            // because it means tags/layers haven't been set up right.
            if (surf)
            {
                hitMagneticSurface = surf;
                hitMagneticSurface.AffectingMagnet = this;
                // If the surface is fixed, drag the magnet to/away from it.
                if (surf.IsFixed)
                {
                    currentState = State.IN_FIELD;

                    if (surf.Charge > 0)
                    {
                        targetPosition = hit.point;
                    }
                    else if (surf.Charge < 0)
                    {
                        targetPosition = forwardEnd.position + (forwardEnd.position - hit.point) * Strength;
                    }

                    scalingFactor = Strength / hit.distance;

                    MoveMagnet(targetPosition);

                    // Switch the forward side of the magnet based on distance to target
                    Vector3 positiveDist = targetPosition - PositiveEnd.position;
                    Vector3 negativeDist = targetPosition - NegativeEnd.position;

                    if (!UserCanFlip && Mathf.Abs(positiveDist.magnitude - negativeDist.magnitude) > 0.1f)
                    {
                        ForwardEnd = (positiveDist.magnitude > negativeDist.magnitude) ? Pole.POSITIVE : Pole.NEGATIVE;
                        SwitchForwardEnd(ForwardEnd);
                    }

                    prevPosition = targetPosition;
                }
                // Otherwise, drag the surface to/away from the magnet.
                else
                {
                    if (surf.Charge > 0)
                    {
                        surf.target = forwardEnd.position + offset;
                    }
                    else if (surf.Charge < 0)
                    {
                        surf.target = surf.transform.position + (surf.transform.position - hit.point) * Strength;
                    }
                }
            }
            FieldReset();
        }

        private void MoveMagnet(Vector3 targetPos)
        {
            if (MagnetFieldSfx && targetPosition != prevPosition) { MagnetActivatedSfx.Play(); }

            // There are three options for this:
            // 1. If the motion is managed by a manager, just send a request to the manager.
            if (manager)
            {
                manager.RequestMovementToPoint(targetPos - (Vector3)managerOffset - forwardEnd.localPosition, transform);
            }
            // 2. If the motion is autonomous, but we're respecting Unity's physics system, move the rigidbody.
            else if (RespectUnityPhysics)
            {
                rigidbody.MovePosition(Vector2.Lerp(transform.position, targetPosition, scalingFactor));
            }
            // 3. If the motion is autonomous and doesn't respect Unity's physics system, move the transform.
            else
            {
                transform.position = Vector2.Lerp(transform.position, targetPosition, scalingFactor);
            }
        }

        // Gizmos if we want them.
        private void OnDrawGizmos()
        {
            if (activeGizmos)
            {
                // Draw magnetic field
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(transform.position, Radius);

                // Draw target position
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(targetPosition, 1.0f);

                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(forwardEnd.position, 1.0f);
            }
        }
    }
}
