using System;
using System.Collections.Generic;
using UnityEngine;

namespace FerrousEngine.Common
{

    public class MutualFollow : MovementRequestManager
    {
        // A class that allows a set of objects to follow one another. They can have a predefined offset from the MutualFollow, but not from each other.
        #region Properties
        [Tooltip("Objects in the Followers list will follow one another.")]
        public bool xOffset;
        public bool yOffset;

        [SerializeField]
        private GameObject[] Followers;

        // Interim state for the new position
        private Vector3 newPos;
        // Storage for all relevant data
        private List<Transform> followers;
        private List<Rigidbody2D> followerRigidbodies;
        private List<Vector2> followerInitPos;
        private List<Vector2> followerRigidbodyInitPos;
        #endregion

        public override void Awake()
        {
            // Variable initialization from MotionRequestManager
            base.Awake();

            // Populate rigidbody array if we're respecting Unity's physics system
            if (RespectUnityPhysics)
            {
                followerRigidbodies = new List<Rigidbody2D>(Followers.Length);

                for (int i = 0; i < Followers.Length; i++)
                {
                    followerRigidbodies.Add(Followers[i].GetComponent<Rigidbody2D>());
                }

                // Populate the rigidbody initial position array
                followerRigidbodyInitPos = new List<Vector2>(Followers.Length);

                for (int i = 0; i < Followers.Length; i++)
                {
                    followerRigidbodyInitPos.Add(followerRigidbodies[i].position - rigidbody.position);
                }
            }

            // Populate the transform array
            followers = new List<Transform>(Followers.Length);

            for (int i = 0; i < Followers.Length; i++)
            {
                followers.Add(Followers[i].transform);
            }

            // Populate the initial position array
            followerInitPos = new List<Vector2>(Followers.Length);

            for (int i = 0; i < Followers.Length; i++)
            {
                followerInitPos.Add(followers[i].position - transform.position);
            }
        }

        public override void Update()
        {
            // If respecting Unity physics, calculate next based on rigidbody positions
            if (RespectUnityPhysics)
            {
                for (int i = 0; i < followerRigidbodies.Count; i++)
                {
                    newPos = average;

                    // If this object should be offset relative to the MutualFollow, adjust accordingly
                    if (xOffset) { newPos.x += followerRigidbodyInitPos[i].x; }
                    if (yOffset) { newPos.y += followerRigidbodyInitPos[i].y; }

                    newPos = Vector2.Lerp(followerRigidbodies[i].position, newPos, Speed);

                    followerRigidbodies[i].MovePosition(newPos);
                }
            }
            // Otherwise, calculate next based on transform positions
            else
            {
                for (int i = 0; i < followers.Count; i++)
                {
                    newPos = average;

                    if (xOffset) { newPos.x += followerInitPos[i].x; }
                    if (yOffset) { newPos.y += followerInitPos[i].y; }

                    newPos = Vector2.Lerp(followers[i].position, newPos, Speed);

                    followers[i].position = (Vector2)newPos;
                }
            }
            // Detect whether or not we're asleep. If so, broadcast to all event followers
            CheckAsleep();
        }

        // Public methods to register and deregister followers. Lots of overloads for cases where we need either a rigidbody or a transform.
        public bool RegisterFollower(Transform t)
        {
            return RegisterFollower(t, null);
        }

        public bool RegisterFollower(Rigidbody2D r)
        {
            return RegisterFollower(null, r);
        }

        public bool DeregisterFollower(Transform t)
        {
            return DeregisterFollower(t, null);
        }

        public bool DeregisterFollower(Rigidbody2D r)
        {
            return DeregisterFollower(null, r);
        }

        // Public methods to get an offset from a specified offset list. Useful to avoid cycles in positional physics.
        public Vector2 GetOffset(Transform t)
        {
            return followerInitPos[followers.IndexOf(t)];
        }

        public Vector2 GetOffset(Rigidbody2D r)
        {
            return followerRigidbodyInitPos[followerRigidbodies.IndexOf(r)];
        }

        // Private registration and deregistration methods where we use both a rigidbody and a transform. These are private because the user should not be
        // registering both a rigidbody and a transform as an input source.
        private bool RegisterFollower(Transform t, Rigidbody2D r)
        {
            if (t && (followers != null) && !followers.Contains(t))
            {
                followers.Add(t);
                return true;
            }
            if (r && (followerRigidbodies != null) && !followerRigidbodies.Contains(r))
            {
                followerRigidbodies.Add(r);
                return true;
            }
            return false;
        }

        private bool DeregisterFollower(Transform t, Rigidbody2D r)
        {
            if (t && (followers != null) && followers.Contains(t))
            {
                followers.Remove(t);
                return true;
            }

            if (r && (followerRigidbodies != null) && followerRigidbodies.Contains(r))
            {
                followerRigidbodies.Remove(r);
                return true;
            }
            return false;
        }
    }
}