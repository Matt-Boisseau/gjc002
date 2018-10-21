using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Nectar : MonoBehaviour {

	public float	consumeRate,
					happinessRate;

	private float amount = 1;
	
	private void OnTriggerStay(Collider other) {
		Butterfly butterfly = other.GetComponent<Butterfly>();
		if(butterfly) {
			amount -= consumeRate * Time.deltaTime;
			butterfly.adjustHappiness(happinessRate);
			if(amount <= 0) {
				Destroy(gameObject);
			}
		}
	}
}
