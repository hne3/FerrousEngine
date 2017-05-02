using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FerrousEngine.Electricity {
	public class Joint : MonoBehaviour {

		[SerializeField]
		[Tooltip("Paths going into the joint.")]
		List<Path> input;
		[SerializeField]
		[Tooltip("Paths going out of the joint.")]
		List<Path> output;

		public void SwapDirection(Path toSwap) {
			if (input.Contains(toSwap)) {
				input.Remove(toSwap);
				output.Add(toSwap);
			}
			else if (output.Contains(toSwap)) {
				output.Remove(toSwap);
				input.Add(toSwap);
			}
			else {
				//Not in list, give warning
				Debug.LogWarning("Path does not exist in given Joint");
			}
		}

		public Dictionary<Path, float> GetEquation() {
			//Calculates the joint equation given input and output paths.
			Dictionary<Path, float> equation = new Dictionary<Path, float>();
			//If input, add current.  If output, subtract
			foreach (Path p in input) {
				//Check for open paths
				if (p.GetDirection() == Path.Direction.OPEN) {
					//Ignore path in calculations
				}
				else {
					equation.Add(p, 1); //In joint equation, all input coefficients are 1
				}
			}
			foreach (Path p in output) {
				//Check for open paths
				if (p.GetDirection() == Path.Direction.OPEN) {
					//Ignore path in calculations
				}
				else {
					equation.Add(p, -1); //In joint equation, all output coefficients are -1
				}
			}
			return equation;
		}
	}
}
