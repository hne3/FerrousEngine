using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FerrousEngine.Magnetism
{
    public class Rotate : MonoBehaviour
    {
        // Rotates an object according to its motion.
        private Vector3 target = Vector3.zero;

        private void Start()
        {
            target = transform.position;
        }

        private void FixedUpdate()
        {
            Vector2 diff = (target - transform.position);
            // If rotation is on, calculate z rotation.
            float zRot = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
            zRot += diff.x * Mathf.Rad2Deg;
            transform.Rotate(0, 0, zRot * Time.deltaTime);
        }

    }
}