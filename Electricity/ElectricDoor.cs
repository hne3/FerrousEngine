using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FerrousEngine.Electricity {
	//Door that opens when powered.
	public class ElectricDoor : Electric {

        [SerializeField]
        private GameObject Effect;
		protected override void Function() {
			gameObject.SetActive(false);
            GetComponent<AudioSource>().Play();
            Instantiate(Effect, transform.position, transform.rotation);
		}
	}
}
