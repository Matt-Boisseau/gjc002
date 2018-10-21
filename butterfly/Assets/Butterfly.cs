using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Butterfly : MonoBehaviour {

	// properties
	public GameObject	leftWing,
						rightWing,
						model;

	public KeyCode	forwardKey,
					backKey,
					leftKey,
					rightKey,
					cheatFlyKey;

	public float	colliderSize,
					upwardForce,
					yawSpeed,
					rollSpeed,
					pitchSpeed,
					maxRoll,
					maxPitch,
					rotationDamping,
					airFriction,
					terminalVelocity,
					rotationAnimationTime,
					landingControlLockoutTime,
					startingHappiness,
					happinessDecayRate;

	public int happinessThresholdsPerLevel;

	public Sprite[] happinessLevels;

	public SpriteRenderer happinessLevelIndicator;

	public Vector3	gravity,
					happinessLevelIndicatorOffset;

	public LayerMask collisionLayerMask;

	// fields
	private float	dYaw,
					dRoll,
					dPitch,
					yaw,
					roll,
					pitch,
					happiness;

	private int happinessThreshold;

	private bool	landed,
					controlLockout;
	private Vector3 motion,
					landedVector;

	private void Start() {
		happiness = startingHappiness;
		happinessThreshold = Mathf.FloorToInt(startingHappiness);
	}

	private void Update() {

		adjustHappiness(-happinessDecayRate);
		happinessLevelIndicatorFollow();

		animateWings();

		move();

		if(!landed) {
			doYaw();
			doPitch();
			setRotation(Quaternion.Euler(pitch, yaw, roll));
		}
	}

	public void adjustHappiness(float rate) {

		// increment
		happiness += rate * Time.deltaTime;

		// keep it above 0
		if(happiness < 0) {
			happiness++;
		}

		// keep it below max
		happiness = Mathf.Clamp(happiness, 0, (happinessThresholdsPerLevel * happinessLevels.Length) - .1f);

		// pop up when a threshold is crossed
		if(Mathf.Floor(happiness) != happinessThreshold) {
			happinessThreshold = Mathf.FloorToInt(happiness);
			int currentLevel = Mathf.FloorToInt(happinessThreshold / happinessThresholdsPerLevel);
			StartCoroutine(happinessPopup(currentLevel));
		}
	}

	private void happinessLevelIndicatorFollow() {
		happinessLevelIndicator.transform.position = transform.position + happinessLevelIndicatorOffset;
		happinessLevelIndicator.transform.LookAt(Camera.main.transform);
		happinessLevelIndicator.transform.rotation *= Quaternion.Euler(0, 180, 0);
	}

	private IEnumerator happinessPopup(int level) {
		happinessLevelIndicator.sprite = happinessLevels[level];
		yield return new WaitForSeconds(1);
		happinessLevelIndicator.sprite = null;
	}

	private void animateWingFlap(GameObject wing, int downAngle, int upAngle, float tweenTime) {
		LeanTween.cancel(wing);
		LeanTween
			.rotateLocal(wing, Vector3.forward * downAngle, tweenTime)
			.setOnComplete(() => {
				LeanTween.rotateLocal(wing, Vector3.forward * upAngle, tweenTime);
			});
	}

	private void animateWings() {

		float tweenTime = .1f;
		float longTweenTime = Random.Range(.4f, 1f);

		if(Input.GetKeyDown(leftKey)) {
			animateWingFlap(leftWing, 5, -80, tweenTime);
		}

		if(Input.GetKeyDown(rightKey)) {
			animateWingFlap(rightWing, -5, 80, tweenTime);
		}

		if(!LeanTween.isTweening(leftWing) && !LeanTween.isTweening(rightWing)) {
			animateWingFlap(leftWing, 5, -80, longTweenTime);
			animateWingFlap(rightWing, -5, 80, longTweenTime);
		}
	}

	private void setRotation(Quaternion newRotation) {
		model.transform.SetParent(null);
		transform.localRotation = newRotation;
		model.transform.SetParent(transform);
		LeanTween.cancel(model);
		LeanTween.rotateLocal(model, Vector3.zero, rotationAnimationTime);
	}

	private void move() {

		// move up/down when flapping
		if(!controlLockout) {
			Vector3 upwardMotion = transform.rotation * Vector3.up * upwardForce * (keyDownValue(leftKey) + keyDownValue(rightKey) + keyValue(cheatFlyKey) * 3);
			motion += upwardMotion * Time.deltaTime;
		}

		// move down due to gravity
		if(!landed) {
			motion += gravity * Time.deltaTime;
		}

		// damp x and z motion
		motion.x -= motion.x * airFriction * Time.deltaTime;
		motion.z -= motion.z * airFriction * Time.deltaTime;

		// apply terminal velocity
		motion.y = Mathf.Clamp(motion.y, -terminalVelocity, terminalVelocity);

		RaycastHit hit;
		if(Physics.SphereCast(new Ray(transform.position, motion), colliderSize, out hit, (motion * Time.deltaTime).magnitude, collisionLayerMask)) {
			
			// stop moving and set landed
			StartCoroutine(landingControlLockout());
			landed = true;
			motion = Vector3.zero;

			// save previous rotation for use in animation
			Quaternion previousRotation = transform.localRotation;

			// calculate new rotation
			transform.localRotation = Quaternion.LookRotation(hit.normal);
			transform.localRotation *= Quaternion.Euler(90, 0, 0);
			transform.localRotation *= Quaternion.Euler(0, -transform.localEulerAngles.y, 0);
			transform.localRotation *= Quaternion.Euler(0, yaw, 0);

			// upside down
			float x = transform.localRotation.eulerAngles.x % 360;
			float z = transform.localRotation.eulerAngles.z % 360;
			if((90 < x && x < 270) || (90 < z && z < 270)) {
				transform.localRotation *= Quaternion.Euler(0, 180, 0);
			}

			// undo rotation and animate it instead
			Quaternion newRotation = transform.localRotation;
			transform.localRotation = previousRotation;
			setRotation(newRotation);

			// assign correct position
			transform.position = hit.point + transform.rotation * (Vector3.up * colliderSize);
		}
		else {
			
			// appply motion
			transform.Translate(motion * Time.deltaTime, Space.World);
		}
		
		if(landed && motion.magnitude > 0 && !Physics.SphereCast(new Ray(transform.position, motion), colliderSize, (motion * Time.deltaTime).magnitude, collisionLayerMask)) {
			landed = false;
		}
	}

	private IEnumerator landingControlLockout() {
		controlLockout = true;
		yield return new WaitForSeconds(landingControlLockoutTime);
		controlLockout = false;
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
