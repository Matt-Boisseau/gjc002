using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MrQuaternion : MonoBehaviour {
	
	private void Start() {

		RaycastHit hit;
		float yaw = transform.rotation.eulerAngles.y;
		Quaternion yRotation = Quaternion.Euler(0, yaw, 0);

		if(Physics.Raycast(transform.position, Vector3.down, out hit, 100)) {

			// move down to touch the ground, mr quaternion
			transform.position = hit.point;

			// align with normal
			transform.localRotation = Quaternion.LookRotation(hit.normal);

			// set upright (normal is off by 90deg)
			transform.localRotation *= Quaternion.Euler(90, 0, 0);

			// set world forward
			transform.localRotation *= Quaternion.Euler(0, -transform.localEulerAngles.y, 0);

			// re-apply yaw
			transform.localRotation *= Quaternion.Euler(0, yaw, 0);

			// congratulate yourself
			Debug.Log("NICE!!!");
		}
	}
}
