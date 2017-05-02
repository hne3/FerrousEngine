using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FerrousEngine.Electricity {
	/**
	 * Power sources
	 */
	public class Battery : Conductor {

		[SerializeField]
		[Tooltip("The voltage (power) of the battery")]
		float voltage;

		[SerializeField]
		[Tooltip("The direction through the battery, from negative to positive.")]
		Path.Direction direction;

		public float Voltage() {
			return voltage;
		}

		public Path.Direction Direction() {
			return direction;
		}
	}
}
