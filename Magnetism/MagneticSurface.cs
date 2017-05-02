using FerrousEngine.Common;
using FerrousEngine.Magnetism;
using System;
using UnityEngine;

public delegate void SurfaceFieldChangedHandler(object sender, EventArgs e);

namespace FerrousEngine.Magnetism
{
    public class MagneticSurface : MonoBehaviour
    {
        // A class that assigns the property "Magnetic Surface" to an object. For now, magnetic surfaces
        // are treated as point charges. To implement poles, you can add two magnetic surface objects
        // to opposite sides of an object. This will change as the physics simulation becomes more
        // robust.

        // The type of magnetic surface. As of right now, only platforms and ball ferromagnets are available.
        public enum Type
        {
            PLATFORM,
            BALL
        };

        #region Public Properties
        // An event that occurs whenever the magnetic surface enters or exits a magnetic field.
        public event SurfaceFieldChangedHandler SurfaceFieldChanged;
        // Whether or not the magnetic surface should be of fixed position and rotation.
        public bool IsFixed = false;
        // Whether or not the magnetic surface should rotate.
        public bool Rotates = false;
        // Is the surface active (being moved) at this time?
        public bool Active = false;
        // Should we keep gizmos on?
        public bool ActiveGizmos = true;
        // The charge of the magnetic surface. Sign determines positive/negative. 
        public float Charge = 1.0f;
        // The strength of the magnetic surface.
        public float Strength = 1.0f;
        // What is our target position?
        public Vector3 target;
        // What type of magnetic surface is this?
        public Type type;
        // What magnet is affecting the surface?
        public Magnetic AffectingMagnet;
        // Is the surface;s motion managed?
        public MovementRequestManager Manager;
        #endregion

        #region Private Properties
        // State: What position were we just in?
        private Vector3 prevPosition;
        // Do we have a rigidbody? What is it?
        private new Rigidbody2D rigidbody;
        #endregion

        private void OnEnable()
        {
            target = transform.position;
            prevPosition = transform.position;

            if (Manager && Manager.RespectUnityPhysics)
            {
                rigidbody = GetComponent<Rigidbody2D>();
            }
        }

        // Toggle the activity state of the surface so we know when to stop acting.
        public void ToggleActive(bool active)
        {
            bool a = Active;
            Active = active;

            // If we're active, start requesting movement right away.
            if (Active && Manager)
            {
                Manager.RequestMovementToPoint(target, transform, rigidbody, Strength);
            }
            // If we're not active, deregister the sender.
            else if (Manager)
            {
                Manager.DeregisterSender(transform);
            }
            
            // Handle field changed state
            if (a != active)
            {
                OnSurfaceFieldChanged(EventArgs.Empty);
            }

        }

        private void FixedUpdate()
        {
            Vector3 diff = (target - transform.position);
            // If rotation is on, calculate z rotation.
            if (Rotates)
            {
                float zRot = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
                // If we're dealing with a ball magnet, the z rotation should increase with the distance between our current position and
                // our previous position.
                if (type == Type.BALL)
                {
                    zRot += diff.x * Mathf.Rad2Deg;
                }

                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0.0f, 0.0f, zRot), Strength);
            }

            // Only move the surface if we're active and fixed. Otherwise, magnets will stick to us.
            if (Active && !IsFixed)
            {
                float dist = (target - prevPosition).magnitude;
                // Approximation: Magnets have a 1 / distance reationship with their strength (e.g. the magnetic force
                // grows the closer two magnetic objects get.) Modeled here as a scaling factor.
                float scalingFactor = Strength / diff.magnitude;

                // If our motion isn't managed, handle situations where we do or don't respect rigidbody physics.
                if (!Manager && !MagneticPhysics.Approximately(transform.position, target))
                {
                    transform.position = Vector2.Lerp(transform.position, target, scalingFactor);
                }
                else if (!MagneticPhysics.Approximately(transform.position, target))
                {
                    Manager.RequestMovementToPoint(Vector2.Lerp(transform.position, target, scalingFactor), transform, rigidbody, Strength);
                }

                prevPosition = transform.position;
            }
        }
        
        // Let subscribers know our field has changed.
        private void OnSurfaceFieldChanged(EventArgs e)
        {
            if (SurfaceFieldChanged != null)
            {
                SurfaceFieldChanged(this, e);
            }
        }

        // Draw gizmos when appropriate. Shows the surface's target.
        private void OnDrawGizmos()
        {
            if (ActiveGizmos)
            {
                // Draw magnetic field
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(target, 1.0f);
            }
        }
    }
}
