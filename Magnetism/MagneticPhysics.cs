using UnityEngine;

namespace FerrousEngine.Magnetism
{
    // A static class usable in a similar way to Physics2D. Use this to cast magnetic fields and make rough approximations.
    public static class MagneticPhysics : object
    {
        // Cast a magnetic field.
        public static bool CastField(Vector3 position, float radius, Vector3 direction, int layerMask, out MagnetHit result)
        {
            result = null;

            // First pass: Find closest magnetic object
            RaycastHit2D hit = Physics2D.CircleCast(position, radius, direction, 1.0f, layerMask);
            if (!hit) { return false; }

            // Second pass: Get closest point on object
            Vector3 diff = ((Vector3)hit.point - position);
            RaycastHit2D r = Physics2D.Raycast(position, diff, diff.magnitude, layerMask);
            if (!r) { return false; }

            // Final pass: Get nearest magnet or magnetic surface
            Magnetic m = r.transform.GetComponent<Magnetic>();
            float distance = (position - (Vector3)r.point).magnitude;

            Debug.DrawLine(position, r.point);
            // Return either a magnetic surface or a magnet.
            if (m)
            {
                result = new MagnetHit(r.point, distance, m, r.transform);
            }
            else
            {
                result = new MagnetHit(r.point, distance, r.transform.GetComponent<MagneticSurface>(), r.transform);
            }
            return true;
        }

        // I found myself needing extra approximation methods that Mathf doesn't provide, so they are included
        // here for the convenience of the user.
        public static bool Approximately(float a, float b, float tolerance)
        {
            if (Mathf.Abs(a - b) < tolerance) return true;
            return false;
        }

        public static bool Approximately(Vector3 a, Vector3 b)
        {
            if (Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y)) return true;
            return false;
        }

        public static bool Approximately(Vector3 a, Vector3 b, float tolerance)
        {
            if (Mathf.Abs(a.x - b.x) < tolerance && Mathf.Abs(a.y - b.y) < tolerance) return true;
            return false;
        }
    }
}