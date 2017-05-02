using MoreMountains.Tools;
using UnityEngine;

namespace FerrousEngine.Common
{

    public class MultiTargetMotion : MonoBehaviour
    {
        // A demo script for MovementRequestManager. Takes randomly generated points and evenly weights them in the manager.
        public bool OverrideRanges = false;
        public bool OverrideRefreshRates = false;

        public Vector2 xRange;
        public Vector2 yRange;
        public float RefreshRate;

        public PointGenerator[] PointGenerators;

        private Vector2[] originalXranges;
        private Vector2[] originalYranges;
        private float[] originalRates;

        private void Start()
        {
            originalXranges = new Vector2[PointGenerators.Length];
            originalYranges = new Vector2[PointGenerators.Length];
            originalRates = new float[PointGenerators.Length];

            for (int i = 0; i < PointGenerators.Length; i++)
            {
                originalRates[i] = PointGenerators[i].RefreshRate;
                originalXranges[i] = PointGenerators[i].xRange;
                originalYranges[i] = PointGenerators[i].yRange;
            }
        }

        private void OnValidate()
        {
            if (OverrideRefreshRates)
            {
                foreach (PointGenerator p in PointGenerators)
                {
                    p.UpdateRefreshRate(RefreshRate);
                }
            }
            if (OverrideRanges)
            {
                foreach (PointGenerator p in PointGenerators)
                {
                    p.xRange = xRange;
                    p.yRange = yRange;
                }
            }
        }
    }

}