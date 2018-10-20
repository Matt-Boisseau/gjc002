using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Butterfly : MonoBehaviour {

	// properties
	public GameObject	leftWing,
						rightWing;

	public KeyCode	forwardKey,
					backKey,
					leftKey,
					rightKey,
					cwKey,
					ccwKey;

	[Range(0, 1)]
	public float	bounciness;

	public float	colliderSize,
					upwardForce,
					yawSpeed,
					rollSpeed,
					pitchSpeed,
					maxRoll,
					maxPitch,
					rotationDamping,
					airFriction,
					terminalVelocity;

	public Vector3	gravity;

	private float dYaw, dRoll, dPitch, yaw, roll, pitch;

	// fields
	private Vector3 motion;

	private void Update() {

		animateWings();

		move();

		doYaw();
		doPitch();
		transform.localRotation = Quaternion.Euler(pitch, yaw, roll);
	}

	private void animateWingFlap(GameObject wing, int downAngle, int upAngle, float tweenTime) {
		LeanTween.cancel(wing);
		LeanTween
			.rotateLocal(wing, Vector3.forward * downAngle, tweenTime)
			.setOnComplete(delegate() {
				LeanTween.rotateLocal(wing, Vector3.forward * upAngle, tweenTime);
			});
	}

	private void animateWings() {

		float tweenTime = .1f;

		if(Input.GetKeyDown(leftKey)) {
			animateWingFlap(leftWing, 5, -80, tweenTime);
		}

		if(Input.GetKeyDown(rightKey)) {
			animateWingFlap(rightWing, -5, 80, tweenTime);
		}
	}

	private void move() {

		// move up/down when flapping
		Vector3 upwardMotion = transform.rotation * Vector3.up * upwardForce * (keyDownValue(leftKey) + keyDownValue(rightKey));
		motion += upwardMotion * Time.deltaTime;

		// move down due to gravity
		motion += gravity * Time.deltaTime;

		// damp x and z motion
		motion.x -= motion.x * airFriction * Time.deltaTime;
		motion.z -= motion.z * airFriction * Time.deltaTime;

		// apply terminal velocity
		motion.y = Mathf.Clamp(motion.y, -terminalVelocity, terminalVelocity);

		// appply motion
		if(!Physics.SphereCast(new Ray(transform.position, motion), colliderSize, (motion * Time.deltaTime).magnitude)) {
			transform.Translate(motion * Time.deltaTime, Space.World);
		}
		else {
			motion = -motion * bounciness;
		}
	}

	private void doYaw() {

		// rotate left/right when flapping
		dYaw += keyDownValue(leftKey, rightKey) * yawSpeed;

		// damp rotation (ugly bad code)
		Vector3 rotation = new Vector3(0, dYaw, 0);
		rotation -= rotation.normalized * rotationDamping * Time.deltaTime;
		dYaw = rotation.y;

		// apply rotation
		yaw += dYaw * Time.deltaTime;
	}

	private void doPitch() {

		// pitch and roll suck
		dPitch = keyValue(forwardKey, backKey) * pitchSpeed * Time.deltaTime;

		// apply rotation
		pitch += dPitch * Time.deltaTime;
		pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);

		// damp rotation (ugly bad code)
		Vector3 rotation = new Vector3(pitch, 0, 0);
		rotation -= rotation.normalized * rotationDamping * Time.deltaTime;
		pitch = rotation.x;
	}

	// returns 1 if positiveKey held, -1 if negativeKey held, and 0 in other cases
	// omit negative key to get only 1 or 0
	private int keyValue(KeyCode positiveKey, KeyCode negativeKey = KeyCode.None) {
		int positiveValue = Input.GetKey(positiveKey) ? 1 : 0;
		if(negativeKey != KeyCode.None) {
			int negativeValue = Input.GetKey(negativeKey) ? 1 : 0;
			return positiveValue - negativeValue;
		}
		else {
			return positiveValue;
		}
	}
	private int keyDownValue(KeyCode positiveKey, KeyCode negativeKey = KeyCode.None) {
		int positiveValue = Input.GetKeyDown(positiveKey) ? 1 : 0;
		if(negativeKey != KeyCode.None) {
			int negativeValue = Input.GetKeyDown(negativeKey) ? 1 : 0;
			return positiveValue - negativeValue;
		}
		else {
			return positiveValue;
		}
	}
}
