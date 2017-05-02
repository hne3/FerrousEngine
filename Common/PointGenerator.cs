using UnityEngine;

namespace FerrousEngine.Common
{
    public class PointGenerator : MonoBehaviour
    {
        // Requests a new point from an object every second. Used in MultiTargetMotion.cs as a demo of the motion management system.

        public Vector2 xRange;
        public Vector2 yRange;
        public float RefreshRate = 1.0f;

        [SerializeField]
        private MovementRequestManager manager;

        private void Start()
        {
            InvokeRepeating("RequestNewPoint", 0.0f, RefreshRate);
        }

        public void UpdateRefreshRate(float rate)
        {
            CancelInvoke("RequestNewPoint");
            RefreshRate = rate;
            InvokeRepeating("RequestNewPoint", 0.0f, RefreshRate);
        }

        private void RequestNewPoint()
        {
            Vector2 newPoint = new Vector2(manager.transform.position.x + Random.Range(xRange[0], xRange[1]), manager.transform.position.y + Random.Range(yRange[0], yRange[1]));
            manager.RequestMovementToPoint(newPoint, transform, 1.0f);
        }
    }

}