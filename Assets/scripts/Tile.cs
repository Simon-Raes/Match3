using UnityEngine;
using System.Collections;
using AssemblyCSharp;

public class Tile : MonoBehaviour
{
	private bool moving;
	private bool falling;

	private Vector2 startPosition;
	private Vector2 targetPosition;

	private float moveDistance;
	private float moveStartTime;

	private TileMovementListener listener;


	private float rotateRangeMin = 5;
	private float rotateRangeMax = 7;
	private float rotateAngle;

	private float rotateSpeedMin = 10;
	private float rotateSpeedMax = 12;
	private float rotateSpeed;

	private float rotateTime;

	private const float SPEED_FALL = 5.5f;
	private const float SPEED_SWAP = 5;

	private float speed = 1;

	private const float DELETE_DURATION = .35f;

	// Use this for initialization
	void Start ()
	{
		rotateAngle = Random.Range (rotateRangeMin, rotateRangeMax);
		rotateSpeed = Random.Range (rotateSpeedMin, rotateSpeedMax);

		StartCoroutine ("AnimateIdle");
	}

	public void setTileMovementListener(TileMovementListener listener)
	{
		this.listener = listener;
	}
	
	// Update is called once per frame
	void Update ()
	{
		// TODO replace with coroutine?
		if (moving) {

			float distCovered = (Time.time - moveStartTime) * speed;
			float fracJourney = distCovered / moveDistance;
			transform.position = Vector3.Lerp (startPosition, targetPosition, fracJourney);

			if (transform.position.y == targetPosition.y && transform.position.x == targetPosition.x) {
				moving = false;

				listener.movementFinished (this);
			}
		}
	}

	/// <summary>
	/// Plays the match animation and deletes the gameObject.
	/// </summary>
	public void delete()
	{
		StopCoroutine ("AnimateIdle");
		StartCoroutine (AnimateDeletion());
	}
		
	public void move (float x, float y)
	{
		speed = SPEED_SWAP;

		moveInternal (x, y);
	}

	public void fall(int distance)
	{
		speed = SPEED_FALL;

		moveInternal (0, -distance);
	}

	private void moveInternal(float x, float y)
	{
		moving = true;

		startPosition = transform.position;
		targetPosition = transform.position;

		targetPosition.x += x;
		targetPosition.y += y;

		moveStartTime = Time.time;
		moveDistance = Vector2.Distance (startPosition, targetPosition);
	}


	IEnumerator AnimateIdle() {

		while (true) {

			rotateTime = rotateTime + Time.deltaTime;

			float degrees = Mathf.Sin(rotateTime * rotateSpeed);
		
			transform.localRotation = Quaternion.Euler( new Vector3(0, 0, 1) * degrees * rotateAngle);

			yield return null;
		}
	}

	// TODO proper animation with some scaling and alpha and full rotation
	IEnumerator AnimateDeletion() {

		float remainingTime = DELETE_DURATION;
		float rotateSpeed;
		float startScale = transform.localScale.x;
		float scale = 1;

		while (remainingTime > 0) {

//			rotateSpeed = 2000 * ((DELETE_DURATION - remainingTime) / DELETE_DURATION);

			remainingTime -= Time.deltaTime;

			scale = (remainingTime / DELETE_DURATION) * startScale;

			transform.localScale = new Vector2(scale, scale);

			transform.Rotate (Vector3.forward * -1000 * Time.deltaTime);

			yield return null;
		}
			
		Destroy (gameObject);

		listener.deletionFinished ();

		yield return null;
	}
}
