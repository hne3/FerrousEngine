using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FerrousEngine.Electricity {
	/**
	 * A simple class for objects that conduct electricity.  All electric classes extend this
	 */
	public class Conductor : MonoBehaviour {

		[SerializeField]
		float current; //The current flowing through the conductor.  Will eventually be tempered by resistance.
		[SerializeField]
		[Tooltip("The resistance of the conductor, in ohms")]
		float resistance; //The resistance of the conductor.

		public void SetCurrent(float i) {
			current = i;
		}

		public float Current() {
			return current;
		}

		public float Resistance() {
			return resistance;
		}
	}
}
