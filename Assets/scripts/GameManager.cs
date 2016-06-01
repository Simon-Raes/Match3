using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using AssemblyCSharp;

public class GameManager : MonoBehaviour, TileMovementListener
{
	[SerializeField]
	private int minTilesForMatch = 3;

	[SerializeField]
	private int boardSize = 8;

	[SerializeField]
	private GameObject selectionIndicator;

	[SerializeField]
	private Tile[] tiles;

	private Tile[,] grid;

	private Vector2 selectionLocation = new Vector2 (-1, -1);

	private int tilesToMove = 0;
	private int movedTiles = 0;

	// Use this for initialization
	void Start ()
	{

		// columns, rows
		setupBoard ();

		checkAndRemoveMatches (grid);			
	}
	
	// Update is called once per frame
	void Update ()
	{
		checkClick ();
	}

	private void setupBoard ()
	{

		// Reserve extra space above where new tiles can be inserted
		grid = new Tile[boardSize, boardSize * 2];

		for (int x = 0; x < boardSize; x++) {
			for (int y = 0; y < boardSize; y++) {	

				grid [x, y] = spawnRandomTile (new Vector2 (x, y));
			}
		}
	}

	private void checkClick ()
	{
		if (Input.GetMouseButtonDown (0)) {
			Vector2 mousePosition = Camera.main.ScreenToWorldPoint (Input.mousePosition);
			Collider2D hitCollider = Physics2D.OverlapPoint (mousePosition);

			int clickedX = (int)hitCollider.transform.position.x;
			int clickedY = (int)hitCollider.transform.position.y;


			if (hitCollider != null) {


				var transform = selectionIndicator.transform.position;

				int indicatorX = (int)transform.x;
				int indicatorY = (int)transform.y;

				if (transform.x == -1 && transform.y == -1) {
					transform.x = (float)clickedX;
					transform.y = (float)clickedY;  

					Debug.Log ("moved indicator to " + transform);
					selectionIndicator.transform.position = transform;

				} else {
					
					float xDistance = Math.Abs (transform.x - clickedX);
					float yDistance = Math.Abs (transform.y - clickedY);

					if ((xDistance == 0 && yDistance == 1) || (xDistance == 1 && yDistance == 0)) {
						transform.x = -1;
						transform.y = -1;

						Tile tOne = grid [indicatorX, indicatorY];
						Tile tTwo = grid [clickedX, clickedY];

						grid [indicatorX, indicatorY] = tTwo;
						grid [clickedX, clickedY] = tOne;


						// TODO would be nice to have a method that only checks the possible matches for that move instead of going over the full board
						HashSet<Tile> matches = checkMatches (grid);

						if (matches.Count > 0) {

							// Move tiles to their new locations

							float tOneXMovement = tTwo.transform.position.x - tOne.transform.position.x;
							float tOneYMovement = tTwo.transform.position.y - tOne.transform.position.y;

							// Remove it from the grid while it moves
							grid [(int)tOne.transform.position.x, (int)tOne.transform.position.y] = null;
							tOne.setTileMovementListener (this);	
							tilesToMove++;
							// Start animation, movementFinished callback will be called when it finishes
							tOne.move (tOneXMovement, tOneYMovement);


							float tTwoXMovement = tOne.transform.position.x - tTwo.transform.position.x;
							float tTwoYMovement = tOne.transform.position.y - tTwo.transform.position.y;

							// Remove it from the grid while it moves
							grid [(int)tTwo.transform.position.x, (int)tTwo.transform.position.y] = null;
							tTwo.setTileMovementListener (this);	
							tilesToMove++;
							// Start animation, movementFinished callback will be called when it finishes
							tTwo.move (tTwoXMovement, tTwoYMovement);


						} else {
							// No matches, revert to previous locations
							grid [indicatorX, indicatorY] = tOne;
							grid [clickedX, clickedY] = tTwo;
						}

					} else {
						transform.x = hitCollider.transform.position.x;
						transform.y = hitCollider.transform.position.y;  
					}
  
					selectionIndicator.transform.position = transform;
				}
			}
		}
	}

	/// <summary>
	/// Checks the grid for any matches and removes them if there are any. 
	/// New tiles will be added to fill in the empty spaces.
	/// Grid will be automatically checked for matches once the tiles have moved into their position. 
	/// </summary>
	/// <param name="inGrid">The grid to check.</param>
	private void checkAndRemoveMatches (Tile[,] inGrid)
	{
		HashSet<Tile> matches = checkMatches (inGrid);
		clearMatches (matches);
	}

	/// <summary>
	/// Checks the grid for matches.
	/// </summary>
	/// <returns>The matches found in the grid.</returns>
	/// <param name="inGrid">The grid to check for matches.</param>
	private HashSet<Tile> checkMatches (Tile[,] inGrid)
	{
		HashSet<Tile> markedForDeletion = new HashSet<Tile> ();
		List<Tile> matches = new List<Tile> ();

		// Check columns
		for (int x = 0; x < boardSize; x++) {

			// Checking a new column, clear the list of matching tiles
			resetMatches (matches, markedForDeletion);

			for (int y = 0; y < boardSize; y++) {
					
				Tile currentObject = inGrid [x, y];

				if (matches.Count > 0 && !matches [0].tag.Equals (currentObject.tag)) {

					// Tile is different from those in the current list of matches, reset
					resetMatches (matches, markedForDeletion);
				}

				matches.Add (currentObject);
			}
		}

		// Done checking columns, reset
		resetMatches (matches, markedForDeletion);


		// Check rows
		for (int y = 0; y < boardSize; y++) {

			// Checking a new column, clear the list of matching tiles
			resetMatches (matches, markedForDeletion);

			for (int x = 0; x < boardSize; x++) {

				Tile currentObject = inGrid [x, y];

				if (matches.Count > 0 && !matches [0].tag.Equals (currentObject.tag)) {

					// Tile is different from those in the current list of matches, reset
					resetMatches (matches, markedForDeletion);
				}

				matches.Add (currentObject);
			}
		}	

		return markedForDeletion;
	}

	private void clearMatches (HashSet<Tile> markedForDeletion)
	{
		// Start deleting them
		if (markedForDeletion.Count > 0) {
		
			Dictionary<float, int> newTilesForRow = new Dictionary<float,int> ();

			foreach (Tile g in markedForDeletion) {

				float tileXPosition = g.transform.position.x;
				int previousValue = 0;

				if (newTilesForRow.ContainsKey (tileXPosition)) {
					previousValue = newTilesForRow [tileXPosition];
					newTilesForRow [tileXPosition]++;
				} else {
					newTilesForRow.Add (tileXPosition, 1);
				}


				// TODO delete them with an animation
//				Debug.Log ("soon deleting tile " + g.transform.position.x + "," + g.transform.position.y);
				Destroy (g.gameObject);
			}
				
			List<Tile> newTiles = new List<Tile> ();
			// Spawn new tiles to fill the gaps
			foreach (KeyValuePair<float, int> entry in newTilesForRow) {
				for (int i = boardSize; i < boardSize + entry.Value; i++) {

					int x = (int)entry.Key;
					int y = i;

					grid [x, y] = spawnRandomTile (new Vector2 (x, y));

					// TODO use this later
//					newTiles.Add(spawnRandomTile (new Vector2 (x, y)));

				}
			}

			int counter = 0;
			foreach (Tile tile in grid) {
				if (tile != null && !markedForDeletion.Contains (tile)) {
					float x = tile.transform.position.x;
					float y = tile.transform.position.y;

					int movement = 0;

					// Check how far this tile has to move down (could be 0)
					for (int i = (int)y; i >= 0; i--) {
						
						if (markedForDeletion.Contains (grid [(int)x, i])) {
							movement++;
						}
					}

					if (movement > 0) {
						// Remove it from the grid while it moves
						grid [(int)x, (int)y] = null;
						tile.setTileMovementListener (this);	
						tilesToMove++;
						// Start animation, movementFinished callback will be called when it finishes
						tile.fall (movement);
					}

				}

			}
		} 
	}


	/**Tile callback*/

	public void movementFinished (Tile t)
	{
		// Place it back into the grid at the new position
		grid [(int)t.transform.position.x, (int)t.transform.position.y] = t;
		movedTiles++;
		Debug.Log ("move finished " + movedTiles);
		Debug.Log (t.tag + " now at " + t.transform.position.x + "," + t.transform.position.y);
		if (movedTiles == tilesToMove) {
			movedTiles = 0;
			tilesToMove = 0;
			checkAndRemoveMatches (grid);
		}
	}

	private void checkPossibleMoves ()
	{
		// TODO check if there are any moves left
		// TODO can this already be done above? doubt it 
	}




	// utils
	private Tile spawnRandomTile (Vector2 location)
	{
		Tile tile = tiles [UnityEngine.Random.Range (0, tiles.Length)];
		return Instantiate (tile, location, Quaternion.identity) as Tile;
	}


	/*
	* Clears the list of matches being checked right now. If there were enough for a match, they will be added to the list of
	* pending deletions.
	*/
	private void resetMatches (List<Tile> checking, HashSet<Tile> markedForDeletion)
	{
		if (checking.Count >= minTilesForMatch) {
			markedForDeletion.UnionWith (checking);
		}

		checking.Clear ();
	}

}
