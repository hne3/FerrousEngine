using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FerrousEngine.Electricity {
	public class DirectedPath : MonoBehaviour {

		public Path path { get; set; }
		public Path.Direction activeDirection { get; set; }

		public DirectedPath(Path path, Path.Direction direction) {
			this.path = path;
			this.activeDirection = direction;
		}

		public bool SameDirection() {
			if (activeDirection == path.GetDirection()) {
				return true;
			}
			else {
				return false;
			}
		}
	}
}
