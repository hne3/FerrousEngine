using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FerrousEngine.Electricity {

	//Door that has an upper limit on current as well as a lower limit.
	public class ElectricDoorOverload : ElectricDoor {

		[SerializeField]
		[Tooltip("Value that current cannot exceed for door to be powered")]
		float overloadCurrent;

		protected override void Function() {
			if (Current() > overloadCurrent) {
				//Do nothing.  Can be modified to make door unusable.
			}
			else {
				gameObject.SetActive(false);
			}
		}
	}
}
