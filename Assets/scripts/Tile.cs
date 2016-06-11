using UnityEngine;
using System.Collections;
using AssemblyCSharp;

public class Tile : MonoBehaviour
{
	private bool moving;

	private TileMovementListener listener;

	private const float DEFAULT_SCALE = 2;

	private const float ROTATE_RANGE_MIN = 5;
	private const float ROTATE_RANGE_MAX = 7;
	private float rotateAngle;

	private const float ROTATE_SPEED_MIN = 10;
	private const float ROTATE_SPEED_MAX = 12;
	private float rotateSpeed;

	private const float SCALE_SPEED_HINT = 7;

	private const float SPEED_FALL = 5.5f;
	private const float SPEED_SWAP = 5;

	private float speed = 1;

	private const float DELETE_DURATION = .35f;

	void Start ()
	{
		rotateAngle = Random.Range (ROTATE_RANGE_MIN, ROTATE_RANGE_MAX);
		rotateSpeed = Random.Range (ROTATE_SPEED_MIN, ROTATE_SPEED_MAX);

		StartCoroutine ("AnimateIdle");
	}

	public void setTileMovementListener (TileMovementListener listener)
	{
		this.listener = listener;
	}
	
	void Update ()
	{
		
	}

	/// <summary>
	/// Plays the match animation and deletes the gameObject.
	/// </summary>
	public void delete ()
	{
		StopCoroutine ("AnimateIdle");
		StartCoroutine (AnimateDeletion ());
	}

	public void move (float x, float y)
	{
		speed = SPEED_SWAP;

		StopCoroutine ("AnimateHint");

		StartCoroutine (AnimateMovement (x, y));
	}

	public void fall (int distance)
	{
		speed = SPEED_FALL;

		StartCoroutine (AnimateMovement (0, -distance));
	}

	public void hint(){
		StopCoroutine ("AnimateIdle");
		StartCoroutine ("AnimateHint");
	}

	public void stopHint(){
		transform.localScale = new Vector2(DEFAULT_SCALE, DEFAULT_SCALE);
		StopCoroutine ("AnimateHint");
		StartCoroutine ("AnimateIdle");
	}

	/// <summary>
	/// Can I get a head wobble?
	/// </summary>
	IEnumerator AnimateIdle ()
	{
		float rotateTime = 0;

		while (true) {
			rotateTime = rotateTime + Time.deltaTime;

			float degrees = Mathf.Sin (rotateTime * rotateSpeed);
		
			transform.localRotation = Quaternion.Euler (new Vector3 (0, 0, 1) * degrees * rotateAngle);

			yield return null;
		}
	}

	/// <summary>
	/// Moves the tile the desired x and y distance.
	/// </summary>
	/// <param name="x">The x distance to move.</param>
	/// <param name="y">The y distance to move.</param>
	IEnumerator AnimateMovement (float x, float y)
	{
		Vector2 startPosition = transform.position;
		Vector2 targetPosition = transform.position;

		targetPosition.x += x;
		targetPosition.y += y;

		float moveStartTime = Time.time;
		float moveDistance = Vector2.Distance (startPosition, targetPosition);

		while (transform.position.y != targetPosition.y || transform.position.x != targetPosition.x) {
			float distCovered = (Time.time - moveStartTime) * speed;
			float fracJourney = distCovered / moveDistance;
			transform.position = Vector3.Lerp (startPosition, targetPosition, fracJourney);

			yield return null;
		}
			
		listener.movementFinished (this);

		yield return null;
	}

	/// <summary>
	/// Also actually deletes the gameObject.
	/// </summary>
	IEnumerator AnimateDeletion ()
	{
		float remainingTime = DELETE_DURATION;
		float startScale = transform.localScale.x;
		float scale = 1;

		while (remainingTime > 0) {

			remainingTime -= Time.deltaTime;

			scale = (remainingTime / DELETE_DURATION) * startScale;

			transform.localScale = new Vector2 (scale, scale);

			transform.Rotate (Vector3.forward * -1000 * Time.deltaTime);

			yield return null;
		}
			
		Destroy (gameObject);

		listener.deletionFinished ();

		yield return null;
	}

	IEnumerator AnimateHint(){
		float scaleTime = 0;

		while (true) {
			scaleTime = scaleTime + Time.deltaTime;

			float scale = 1 + .25f * Mathf.Sin (scaleTime * SCALE_SPEED_HINT);
			Debug.Log (scale);

			transform.localScale = new Vector2 (2 * scale, 2 * scale);

			yield return null;
		}
	}
}
