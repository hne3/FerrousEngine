using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CSML;

namespace FerrousEngine.Electricity {
	public class Circuit : MonoBehaviour {

		[SerializeField]
		[Tooltip("List of ALL the loops possible in the circuit.")]
		List<Loop> loops;
		[SerializeField]
		[Tooltip("List of ALL the joints possible in the circuit.")]
		List<Joint> joints;

		// Use this for initialization
		void Start() {
			Recalculate();
		}

		public void Recalculate() {
			//Recalculates currents for entire circuit.
			//TODO: Probably make an IEnumerator for better frame rate
			if (loops.Count == 0) {
				//No loops, no current.  Throw an exception
				throw new System.Exception("Cannot find loops.  Current cannot be calculated.");
			}

			//Gets updated equations from loops and joints
			Dictionary<Path, float>[] loopEquations = new Dictionary<Path, float>[loops.Count];
			Dictionary<Path, float>[] jointEquations = new Dictionary<Path, float>[joints.Count];
			float[] voltages = new float[loops.Count];
			int index = 0; //Used in case of open loops.
			foreach (Loop loop in loops) {
				try {
					loopEquations[index] = loop.GetEquation(out voltages[index]);
					index++; //Will only increment if no exceptions thrown
				}
				catch (System.Exception e) {
					//Loop is open; ignore.
					continue;
				}
			}
			if (index == 0) {
				//No loops.  Do not calculate.
				return;
			}
			index = 0;
			foreach (Joint joint in joints) {
				jointEquations[index] = joint.GetEquation();
				index++;
			}

			//Create list of Paths where current must be calculated
			List<Path> currents = new List<Path>();
			foreach (Dictionary<Path, float> dict in loopEquations) {
				if (dict == null) {
					//Out of equations.  Ignore.
					break;
				}
				Path[] paths = new Path[dict.Keys.Count];
				dict.Keys.CopyTo(paths, 0);
				for (int j = 0; j < paths.Length; j++) {
					if (paths[j].GetDirection() == Path.Direction.OPEN) {
						//Open path, do not need to calculate.  Simply set current to 0
						//NOTE: This code should not be traversable, but is here as a failsafe.
						paths[j].SetCurrent(0);
					}
					else if (currents.Contains(paths[j])) {
						//Path already in list, ignore
					}
					else {
						//Path must be added to list
						currents.Add(paths[j]);
					}
				}
			}
			foreach (Dictionary<Path, float> dict in jointEquations) {
				if (dict == null) {
					//Out of equations.  Ignore.
					break;
				}
				Path[] paths = new Path[dict.Keys.Count];
				dict.Keys.CopyTo(paths, 0);
				for (int j = 0; j < paths.Length; j++) {
					if (paths[j].GetDirection() == Path.Direction.OPEN) {
						//Open path, do not need to calculate.  Simply set current to 0
						paths[j].SetCurrent(0);
					}
					else if (currents.Contains(paths[j])) {
						//Path already in list, ignore
					}
					else {
						//Path must be added to list
						currents.Add(paths[j]);
					}
				}
			}

			//Turn given equations into a square double array with obtained number of currents as height and width.  Should have one less joint than loop OR the same number of both.
			int numLoops;
			int numJoints;
			if (Mathf.Ceil((float)currents.Count / 2) != Mathf.Floor((float)currents.Count / 2)) {
				//If odd, have one less joint than loop
				numLoops = currents.Count / 2 + 1;
				numJoints = currents.Count / 2;
			}
			else {
				//If even, have same number of joints and loops
				numLoops = currents.Count / 2;
				numJoints = currents.Count / 2;
			}
			Dictionary<Path, float>[] requiredEquations = new Dictionary<Path, float>[numLoops + numJoints];
			index = 0; //Current index of required equations
			for (int i = 0; i < loopEquations.Length; i++) {
				//Get loops first
				if (index < numLoops) {
					if (loopEquations[i] != null) {
						requiredEquations[index] = loopEquations[i];
						index++;
					}
				}
			}
			for (int i = 0; i < jointEquations.Length; i++) {
				//Then get the joints
				if (index < numLoops + numJoints) {
					requiredEquations[index] = jointEquations[i];
					index++;
				}
			}
			double[,] matrix = new double[currents.Count, currents.Count];
			//Initialize to all 0's
			for (int j = 0; j < currents.Count; j++) {
				for (int k = 0; k < currents.Count; k++) {
					matrix[j, k] = 0;
				}
			}
			for (int j = 0; j < currents.Count; j++) {
				for (int k = 0; k < requiredEquations.Length; k++) {
					Path[] paths = new Path[requiredEquations[k].Keys.Count];
					requiredEquations[k].Keys.CopyTo(paths, 0);
					for (int l = 0; l < paths.Length; l++) {
						if (paths[l].Equals(currents[j])) {
							//This path's equation goes into the current place on the matrix
							float value;
							requiredEquations[k].TryGetValue(paths[l], out value);
							matrix[k, j] = value;
							break; //Don't need to check the other paths listed in the equation, as each will only be listed once.
						}
					}
				}
			}

			//Create "solution" matrix with voltages
			double[] solve = new double[requiredEquations.Length];
			for (int i = 0; i < solve.Length; i++) {
				if (i < numLoops) {
					//Voltage list length will be less than equation list since joint equations do not return a voltage value, as it is 0.
					solve[i] = voltages[i];
				}
				else {
					//Voltage from joint equation; list as 0
					solve[i] = 0;
				}
			}

			//Inputs said equations into some matrix math thing that then outputs the values needed
			Matrix A = new Matrix(matrix);
			Matrix b = new Matrix(solve);
			Matrix solution = Matrix.Solve(A, b); //Solves matrix equation Ax = b, where x is the currents we need.

			//Send output values back to appropriate paths
			for (int i = 0; i < currents.Count; i++) {
				//If current is negative, flip path direction.  Use absolute value for current transfer.
				if (solution[i + 1].Re < 0) {
					currents[i].ReversePath();
				}
				currents[i].SetCurrent(Mathf.Abs((float)solution[i + 1].Re));
			}
		}
	}
}
