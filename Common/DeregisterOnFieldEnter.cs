using FerrousEngine.Common;
using System;
using System.Collections;
using UnityEngine;

public delegate void DeregisteredHandler(object sender, EventArgs e);

namespace FerrousEngine.Magnetism
{

    [RequireComponent(typeof(MutualFollow))]
    public class DeregisterOnFieldEnter : MonoBehaviour
    {
        // Use this class when you want an object to follow another object until it enters a magnetic field. In Lodus, we used this
        // for the magnetic shell enemies.

        //  An event that occurs when the object is completely deregistered.
        public event DeregisteredHandler Deregistered;

        // The affecting magnet or magnetic surface.
        [SerializeField]
        private Magnetic Magnet;
        [SerializeField]
        private MagneticSurface MagneticSurface;
        // The MutualFollow used to determine position.
        private MutualFollow follower;

        private void Start()
        {
            follower = GetComponent<MutualFollow>();
            // Subscribe to the object's field changed event.
            if (Magnet)
            {
                Magnet.FieldChanged += new FieldChangedHandler(OnFieldEnter);
            }

            if (MagneticSurface)
            {
                MagneticSurface.SurfaceFieldChanged += new SurfaceFieldChangedHandler(OnFieldEnter);
            }
        }
        // When the field is changed, start the deregistration process.
        private void OnFieldEnter(object sender, EventArgs e)
        {
            StartCoroutine(Deregister());
        }
        // An event for objects waiting for deregistration.
        private void OnDeregisted(EventArgs e)
        {
            if (Deregistered != null)
            {
                Deregistered(this, e);
            }
        }
        // Deregistration process. Waits to deregister until the end of frame so that state cleanup can happen.
        private IEnumerator Deregister()
        {
            if (Magnet)
            {
                Magnet.FieldChanged -= new FieldChangedHandler(OnFieldEnter);
                Magnet.enabled = false;
                yield return new WaitForEndOfFrame();
                follower.DeregisterSender(Magnet.transform);
            }

            if (MagneticSurface)
            {
                MagneticSurface.SurfaceFieldChanged -= new SurfaceFieldChangedHandler(OnFieldEnter);
                MagneticSurface.Manager = null;
                yield return new WaitForEndOfFrame();
                follower.DeregisterSender(MagneticSurface.transform);
                follower.DeregisterFollower(MagneticSurface.transform);
            }
            // Let subscribers know that the object has been deregistered.
            OnDeregisted(EventArgs.Empty);
            yield return null;
        }
    }
}
