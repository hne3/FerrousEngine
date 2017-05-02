using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FerrousEngine.Electricity {
	/**
	 * Objects that are powered by electricity.  
	 */
	public abstract class Electric : Conductor {

		[SerializeField]
		[Tooltip("Current through the object must be larger than this value to function")]
		float threshold = 0;

		// Use this for initialization
		void Start() {

		}

		// Update is called once per frame
		void Update() {
			if (Current() > threshold) {
				Function();
			}
		}

		protected virtual void Function() {

		}

	}
}
