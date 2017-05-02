using FerrousEngine.Magnetism;
using System;
using System.Collections;
using UnityEngine;

namespace FerrousEngine.Magnetism
{

    public class PullableShell : MonoBehaviour
    {
        // Used for shell enemies in Lodus. Handles the shell disappearing when it's pulled off. Feel free to use it for whatever it comes in handy for.

        [SerializeField]
        private DeregisterOnFieldEnter DeregisterHandler;
        [SerializeField]
        private GameObject DissolvePrefab;
        [SerializeField]
        private float TimeDelay;

        private GameObject dissolve;

        private void Awake()
        {
            DeregisterHandler.Deregistered += new DeregisteredHandler(OnDeregistered);
        }

        private void OnDeregistered(object sender, EventArgs e)
        {
            Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("Player"));
            StartCoroutine(WaitUntilExplosion());
        }

        private IEnumerator WaitUntilExplosion()
        {
            yield return new WaitForSeconds(TimeDelay);
            dissolve = Instantiate(DissolvePrefab, transform.position, transform.rotation, transform.parent) as GameObject;
            gameObject.SetActive(false);
            yield return null;
        }
        private void OnDisable()
        {
            DeregisterHandler.Deregistered -= new DeregisteredHandler(OnDeregistered);
        }
    }
}
