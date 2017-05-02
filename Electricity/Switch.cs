using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FerrousEngine.Electricity {
	/**
	 * When closed, acts as a wire (and thus extends Wire).  When open, ignores current flowing in.  Should be a child of the circuit its path belongs to.
	 */
	public class Switch : Wire {

		[SerializeField]
		[Tooltip("The sprite to used when the switch is open.")]
		private Sprite openSprite;

		[SerializeField]
		[Tooltip("The sprite to used when the switch is closed.")]
		private Sprite closedSprite;

		[SerializeField]
		[Tooltip("When closed, current flows through.  When open, current does not flow")]
		protected bool closed; //Is the switch closed?

		[SerializeField]
		[Tooltip("Which path the switch opens or closes.")]
		private Path path;

		//ONLY used for testing
		//public void Update() {
			//if (Input.GetKeyDown(KeyCode.O)) {
			//	FlipSwitch(!closed);
			//}
		//}

		public void FlipSwitch(bool closed) {
			this.closed = closed;
			if (!closed) {
				path.SetDirection(Path.Direction.OPEN);
				this.GetComponent<SpriteRenderer>().sprite = openSprite;
			}
			else {
				//Set to a direction, then recalculate
				path.SetDirection(Path.Direction.FORWARD);
				this.GetComponent<SpriteRenderer>().sprite = closedSprite;
			}
			gameObject.SendMessageUpwards("Recalculate");
		}
	}
}