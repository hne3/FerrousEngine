using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FerrousEngine.Electricity {
	public class Path : MonoBehaviour {

		#region Direction
		public enum Direction { FORWARD, BACKWARD, OPEN }; //Forward and Backward are to be defined the same for the whole circuit, Open is to be used when a Path should be ignored

		[SerializeField]
		Direction direction;

		public Direction GetDirection() {
			return direction;
		}

		public void SetDirection(Direction value) {
			direction = value;
			if (value == Direction.OPEN) {
				SetCurrent(0);
			}
		}

		public Direction Reverse(Direction dir) {
			switch (dir) {
				case Direction.FORWARD:
					return Direction.BACKWARD;
				case Direction.BACKWARD:
					return Direction.FORWARD;
				default:
					return Direction.OPEN;
			}
		}
		#endregion

		[SerializeField]
		List<Conductor> conductors;

		public List<Conductor> Conductors() {
			return conductors;
		}

		[SerializeField]
		float current;

		public float Current() {
			return current;
		}

		public void SetCurrent(float value) {
			current = value;
			TransferCurrent();
		}

		public Path(List<Conductor> path, Direction forward) : this(path, forward, 0) {
		}

		public Path(List<Conductor> path, Direction forward, float current) {
			conductors = path;
			direction = forward;
			this.current = current;
		}

		public void ReversePath() {
			direction = Reverse(direction);
		}

		public void TransferCurrent() {
			foreach (Conductor c in conductors) {
				c.SetCurrent(current);
			}
		}
	}
}
