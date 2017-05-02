using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FerrousEngine.Electricity {
	public class Loop : MonoBehaviour {

		[SerializeField]
		[Tooltip("Paths that form the loop (in either clockwise or counter-clockwise order.")]
		List<Path> pathLoop;
		[SerializeField]
		[Tooltip("Directions to calculate the loop in, as determined by circuit diagram.  Please enter a separate direction for each Path, even if the direction is the same.")]
		List<Path.Direction> loopDirections; //List needed for overarching loops that stretch across Paths of different directions

		Queue<DirectedPath> actualLoop; //Only used internally, for storing acting direction as well as given.


		private void Awake() {
			actualLoop = new Queue<DirectedPath>();
			int i = 0;
			//Take given queue of paths and turn it into a queue of directed paths
			foreach (Path p in pathLoop) {
				actualLoop.Enqueue(new DirectedPath(p, loopDirections[i]));
				i++;
			}
		}

		public Dictionary<Path, float> GetEquation(out float voltage) {
			//Returns an equation for the loop where values are added if traveling in the same direction.
			Dictionary<Path, float> equation = new Dictionary<Path, float>();
			float volt = 0; //Voltage calculation should exist for the whole loop
			float resistance; //Resistance depends on which path we are on (because of the current)
			foreach (DirectedPath p in actualLoop) {
				//Check that path should be utilized
				if (p.path.GetDirection() == Path.Direction.OPEN) {
					//Cannot get equation for this loop.  Throw exception to be caught in Circuit eventually.
					throw new System.Exception("Path open.");
				}
				resistance = 0;
				bool add = false;
				if (p.SameDirection()) {
					//Subtract resistances
					add = false;
				}
				foreach (Conductor c in p.path.Conductors()) {
					Battery batt = c as Battery;
					if (batt != null) {
						//If c is a battery, check direction again to decide whether to add or subtract voltage
						if (batt.Direction() == p.activeDirection) {
							//Add voltage
							volt += batt.Voltage();
						}
						else {
							//Subtract voltage
							volt -= batt.Voltage();
						}
					}
					else {
						//c is not a battery, go as normal
						if (add) {
							//Add
							resistance += c.Resistance();
						}
						else {
							//Subtract
							resistance -= c.Resistance();
						}
					}
				}
				equation.Add(p.path, resistance);
			}
			//Sets output voltage to calculated voltage
			voltage = volt;

			//Return final equation
			return equation;
		}
	}
}
