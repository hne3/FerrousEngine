using System;
using System.Collections.Generic;
using UnityEngine;

namespace FerrousEngine.Common
{

    // An event handler for when an object goes to sleep.
    public delegate void AsleepHandler(object sender, EventArgs e);
    // An event handler for when the object wakes up.
    public delegate void AwakeHandler(object sender, EventArgs e);

    public class MovementRequestManager : MonoBehaviour
    {
        // A class that takes input sources and decides where it should go based on an average of those sources and their weights.
        // Also includes an asleep/debounce system, used in Lodus to decide when to stop casting (e.g. when the player is attached
        // to an object and is not moving, there is no need to keep casting, so we deactivate casting on asleep.)
        #region Public Properties
        // The more input sources are allowed, the less likely the object is to reach its destination.
        public int MaxInputSources = 2;
        // The number of inputs currently requesting resources
        [HideInInspector]
        public int Count { get; private set; }
        // Do we want detailed error messages?
        public bool DetailedErrorReporting = true;
        // How fast do we want the system to move?
        [Range(0.0f, 1.0f)]
        public float Speed = 1.0f;
        // Event that occurs whenever an object goes to sleep.
        public event AsleepHandler AsleepEvent;
        // Event that occurs whenever an object wakes up.
        public event AwakeHandler AwakeEvent;

        // Should we lock the x or y coords of the object?
        public bool xLock = false;
        public bool yLock = false;
        // Do you want to use Unity's collision detection? If yes, each follower object must have a Rigidbody2D component.
        public bool RespectUnityPhysics = false;
        // Rigidbody for if we're respecting Unity physics. Use: RequestMovementToPoint
        public new Rigidbody2D rigidbody;
        #endregion

        #region Protected/Private Properties
        // State tracker for the average point value based on input points and weights
        protected Vector2 average;

        // State tracker for the distance we've moved, and for the transform inputs.
        protected Dictionary<Transform, float> inputDeltas;
        protected Dictionary<Transform, Vector2> inputs;

        // State: How many consecutive "bounces" have we encountered? Use: Debounce
        private int bounces = 0;
        // How many consecutive bounces do we want to allow before we consider an object to no longer be bouncing, but active?
        private int maxBounces = 10;
        // Have we exceeded our bounce limit?
        private bool bouncedOut = false;
        // State: Is the object asleep, and was it previously asleep? Use: CheckAsleep
        private bool asleep = false;
        private bool prevAsleep = false;
        // State: Tracker for current point. Use: RequestMovementToPoint
        private Vector2 current = Vector2.zero;
        #endregion

        // Just variable initialization.
        public virtual void Awake()
        {
            Count = 0;
            inputs = new Dictionary<Transform, Vector2>(MaxInputSources);
            inputDeltas = new Dictionary<Transform, float>(MaxInputSources);
            rigidbody = GetComponent<Rigidbody2D>();
        }

        // I just included these checks in update instead of using InvokeRepeating because the processing time of a boolean check each frame
        // is small in comparison to the positional math. Might want to change for polish.
        public virtual void Update()
        {
            if (RespectUnityPhysics)
            {
                rigidbody.MovePosition(Vector2.Lerp(rigidbody.position, average, Speed));
            }
            else
            {
                transform.position = Vector2.Lerp(transform.position, average, Speed);
            }

            CheckAsleep();
        }

        // Request movement to a target point from an input source. Will register sender if it hasn't already been registered.
        // Lots of overloads to allow user freedom.
        public virtual void RequestMovementToPoint(Vector2 point, Transform sender)
        {
            RequestMovementToPoint(point, sender, 1);
        }

        public virtual void RequestMovementToPoint(Vector2 point, Rigidbody2D r)
        {
            RequestMovementToPoint(point, r.transform, r, 1);
        }

        public virtual void RequestMovementToPoint(Vector2 point, float speed, Transform sender)
        {
            RequestMovementToPoint(point, sender, speed, 1);
        }

        public virtual void RequestMovementToPoint(Vector2 point, float speed, Rigidbody2D r)
        {
            RequestMovementToPoint(point, r, speed, 1);
        }

        public virtual void RequestMovementToPoint(Vector2 point, Transform sender, float speed, float weight)
        {
            Speed = speed;

            RequestMovementToPoint(point, sender, weight);
        }

        public virtual void RequestMovementToPoint(Vector2 point, Rigidbody2D r, float speed, float weight)
        {
            Speed = speed;

            RequestMovementToPoint(point, r, weight);
        }

        public virtual void RequestMovementToPoint(Vector2 point, Rigidbody2D r, float weight)
        {
            RequestMovementToPoint(point, r.transform, r, weight);
        }
        // In polish, would like to cache this GetComponent.
        public virtual void RequestMovementToPoint(Vector2 point, Transform sender, float weight)
        {
            RequestMovementToPoint(point, sender, sender.GetComponent<Rigidbody2D>(), weight);
        }

        public virtual void RequestMovementToPoint(Vector2 point, Transform sender, Rigidbody2D r, float weight)
        {
            // Using current to decide where to get the locked position from
            current = RespectUnityPhysics ? r.position : (Vector2)sender.position;

            // Now, using current to get the desired lerped point
            current = Vector2.Lerp(current, point, weight);

            // Register sender if this is their first input request
            if (!inputs.ContainsKey(sender))
            {
                RegisterSender(sender, r, current);
            }
            // Otherwise, update sender value in inputs and inputDeltas
            else
            {
                inputDeltas[sender] = (inputs[sender] - current).magnitude;
                inputs[sender] = current;
            }

            GetNewAverage();
        }

        // Register an input source that will provide target data to the object. Will fail if you're using RespectUnityPhysics but object doesn't have a Rigidbody2D attached.
        public virtual bool RegisterSender(Transform sender, Vector2 point)
        {
            return RegisterSender(sender, sender.GetComponent<Rigidbody2D>(), point);
        }

        // I included this line as detailed error reporting in case a user wants to register senders without checking input source counts first
        // (e.g. object pooling; since I already check the count here, why bother doing it on the user's side?)
        public virtual bool RegisterSender(Transform sender, Rigidbody2D r, Vector2 point)
        {
            if (Count >= MaxInputSources)
            {
                if (DetailedErrorReporting)
                {
                    Debug.LogError("MovementRequestManager error: Maximum number of inputs exceeded for object " + transform.name);
                }
                return false;
            }

            inputs.Add(sender, point);
            inputDeltas.Add(sender, 0.0f);

            ++Count;

            return true;
        }

        // Deregister an input source from the motion manager.
        public virtual bool DeregisterSender(Transform sender)
        {
            if (inputs.ContainsKey(sender))
            {
                inputs.Remove(sender);
                inputDeltas.Remove(sender);
                --Count;
                return true;
            }
            return false;
        }

        // Is the object asleep (has it stopped moving significantly?)
        public virtual bool CheckAsleep()
        {
            asleep = true;

            foreach (float val in inputDeltas.Values)
            {
                if (!Mathf.Approximately(val, 0))
                {
                    asleep = false;
                    break;
                }
            }

            bouncedOut = Debounce(asleep);

            if (bouncedOut && !prevAsleep) OnAsleep();
            if (!bouncedOut && prevAsleep) OnAwake();

            prevAsleep = bouncedOut;

            return bouncedOut;
        }

        // Helper method to debounce the object. Returns true if the object is has had enough bounces that it's asleep, false otherwise.
        private bool Debounce(bool asleep)
        {
            bounces = asleep ? bounces + 1 : 0;
            return !(bounces < maxBounces);
        }

        // Event that gets called when the object goes to sleep.
        private void OnAsleep()
        {
            if (AsleepEvent != null)
            {
                AsleepEvent(this, EventArgs.Empty);
            }
        }

        // Event that gets called when the object wakes up. This is a seperate event from OnAsleep to get rid of notifications scripts might not care about,
        // even if they're set to ignore one or the other.
        private void OnAwake()
        {
            if (AwakeEvent != null)
            {
                AwakeEvent(this, EventArgs.Empty);
            }
        }

        // Gets the new average point value to assign to the object.
        private void GetNewAverage()
        {
            float sumX = 0.0f;
            float sumY = 0.0f;

            foreach (Vector2 val in inputs.Values)
            {
                sumX += val.x;
                sumY += val.y;
            }

            average.x = sumX / Count;
            average.y = sumY / Count;

            if (xLock)
            {
                average.x = RespectUnityPhysics ? rigidbody.position.x : transform.position.x;
            }
            if (yLock)
            {
                average.y = RespectUnityPhysics ? rigidbody.position.y : transform.position.y;
            }
        }

        // Some gizmos for visualization. Draws each of the input points and the result of their averaging.
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (UnityEditor.EditorApplication.isPlaying)
        {
            Gizmos.color = Color.red;

            foreach (Vector2 val in inputs.Values)
            {
                Gizmos.DrawWireSphere(val, 1.0f);
            }

            Gizmos.color = Color.green;

            Gizmos.DrawWireSphere(average, 0.3f);
        }
    }
#endif
    }

}