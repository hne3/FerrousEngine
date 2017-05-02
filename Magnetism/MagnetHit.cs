using UnityEngine;

namespace FerrousEngine.Magnetism
{
    // A simple class to store relevant magnet hit data, and discard the data from raycast/circlecast hits we don't need.
    public class MagnetHit : MonoBehaviour
    {
        public Vector3 point;
        public float distance;
        public new Transform transform;
        public MagneticSurface surface;
        public Magnetic magnet;

        // Versions available for a magnetic surface or a magnet.
        public MagnetHit(Vector3 p, float d, MagneticSurface m, Transform t)
        {
            point = p;
            distance = d;
            surface = m;
            magnet = null;
            transform = t;
        }

        public MagnetHit(Vector3 p, float d, Magnetic m, Transform t)
        {
            point = p;
            distance = d;
            surface = null;
            magnet = m;
            transform = t;
        }
    }
}
