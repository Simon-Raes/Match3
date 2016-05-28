using UnityEngine;
using System.Collections;
using AssemblyCSharp;

public class Tile : MonoBehaviour
{
	private bool moving;

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



	// Use this for initialization
	void Start ()
	{
		rotateAngle = Random.Range (rotateRangeMin, rotateRangeMax);
		rotateSpeed = Random.Range (rotateSpeedMin, rotateSpeedMax);

		StartCoroutine ("Rotate");

	}

	public void setTileMovementListener(TileMovementListener listener)
	{
		this.listener = listener;
	}
	
	// Update is called once per frame
	void Update ()
	{



		if (moving) {

			float distCovered = (Time.time - moveStartTime) * 2;
			float fracJourney = distCovered / moveDistance;
			transform.position = Vector3.Lerp (startPosition, targetPosition, fracJourney);

			if (transform.position.y == targetPosition.y) {
				moving = false;

				listener.movementFinished ();
			}
		}
	}

	public void move (int distance)
	{
		moving = true;

		startPosition = transform.position;
		targetPosition = transform.position;
		targetPosition.y -= distance;

		moveStartTime = Time.time;
		moveDistance = Vector2.Distance (startPosition, targetPosition);
	}


	IEnumerator Rotate() {
		while (true) {

			rotateTime = rotateTime + Time.deltaTime;

			float degrees = Mathf.Sin(rotateTime * rotateSpeed);
		
			transform.localRotation = Quaternion.Euler( new Vector3(0, 0, 1) * degrees * rotateAngle);

			yield return null;

		}
	}
}
