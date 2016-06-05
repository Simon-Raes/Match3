using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using AssemblyCSharp;
using UnityEngine.UI;

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
	// The array of possible faces that can be spawned



	private Tile[,] grid;

	private int score = 0;
	private int combo = 1;

	private int tilesToMove = 0;
	private int movedTiles = 0;

	private Dictionary<Tile, Int16> pendingMovements = new Dictionary<Tile, Int16> ();

	private Text textViewScore;
	private Text textViewCombo;

	void Awake ()
	{
		textViewScore = GameObject.Find ("TextViewScore").GetComponent<Text> ();
		textViewScore.text = "Score: " + score;
		textViewCombo = GameObject.Find ("TextViewCombo").GetComponent<Text> ();
		textViewCombo.text = combo + "x combo";
	}

	void Start ()
	{
		setupBoard ();
		checkAndRemoveMatches (grid);	
	}

	void Update ()
	{
		checkClick ();
	}

	private void setupBoard ()
	{
		grid = new Tile[boardSize, boardSize];

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

			if (hitCollider != null) {

				int clickedX = (int)hitCollider.transform.position.x;
				int clickedY = (int)hitCollider.transform.position.y;

				var indicatorPosition = selectionIndicator.transform.position;

				int indicatorX = (int)indicatorPosition.x;
				int indicatorY = (int)indicatorPosition.y;

				if (indicatorPosition.x == -1 && indicatorPosition.y == -1) {
					indicatorPosition.x = (float)clickedX;
					indicatorPosition.y = (float)clickedY;  

					selectionIndicator.transform.position = indicatorPosition;

				} else {
					
					float xDistance = Math.Abs (indicatorPosition.x - clickedX);
					float yDistance = Math.Abs (indicatorPosition.y - clickedY);

					if ((xDistance == 0 && yDistance == 1) || (xDistance == 1 && yDistance == 0)) {
						indicatorPosition.x = -1;
						indicatorPosition.y = -1;

						Tile tOne = grid [indicatorX, indicatorY];
						Tile tTwo = grid [clickedX, clickedY];

						grid [indicatorX, indicatorY] = tTwo;
						grid [clickedX, clickedY] = tOne;


						// TODO would be nice to have a method that only checks the possible matches for that move instead of going over the full board
						// then we can also get rid of this ugly updateScore boolean
						HashSet<Tile> matches = findMatches (grid, false);

						if (matches.Count > 0) {


							// Reset combo for the new chain
							combo = 1;

							// Move tiles to their new locations

							float tOneXMovement = tTwo.transform.position.x - tOne.transform.position.x;
							float tOneYMovement = tTwo.transform.position.y - tOne.transform.position.y;
							moveTile (tOne, tOneXMovement, tOneYMovement);

							float tTwoXMovement = tOne.transform.position.x - tTwo.transform.position.x;
							float tTwoYMovement = tOne.transform.position.y - tTwo.transform.position.y;
							moveTile (tTwo, tTwoXMovement, tTwoYMovement);
					

						} else {
							// No matches, revert to previous locations
							grid [indicatorX, indicatorY] = tOne;
							grid [clickedX, clickedY] = tTwo;
						}

					} else {
						indicatorPosition.x = hitCollider.transform.position.x;
						indicatorPosition.y = hitCollider.transform.position.y;  
					}
  
					selectionIndicator.transform.position = indicatorPosition;
				}
			}
		}
	}

	/// <summary>
	/// Moves a tile the desired distance. The tile will be removed from the game grid while moving.
	/// movementFinished will be called once the movement finishes and will add the tile back to the grid at its new location.
	/// </summary>
	/// <param name="tile">Th tile to move.</param>
	/// <param name="xMovement">X movement.</param>
	/// <param name="yMovement">Y movement.</param>
	private void moveTile (Tile tile, float xMovement, float yMovement)
	{
		// Remove it from the grid while it moves
		grid [(int)tile.transform.position.x, (int)tile.transform.position.y] = null;
		tile.setTileMovementListener (this);	
		tilesToMove++;
		tile.move (xMovement, yMovement);
	}

	/// <summary>
	/// Checks the grid for any matches and removes them if there are any. 
	/// New tiles will be added to fill in the empty spaces.
	/// Grid will be automatically checked for matches once the tiles have moved into their position. 
	/// </summary>
	/// <param name="inGrid">The grid to check.</param>
	private void checkAndRemoveMatches (Tile[,] inGrid)
	{
		HashSet<Tile> matches = findMatches (inGrid, true);
		clearMatches (matches);
	}

	/// <summary>
	/// Checks the grid for matches.
	/// </summary>
	/// <returns>The matches found in the grid.</returns>
	/// <param name="inGrid">The grid to check for matches.</param>
	/// <param name="updateScore">Whether or not the user's score should be increased for any matches found.</param>
	private HashSet<Tile> findMatches (Tile[,] inGrid, bool updateScore)
	{
		HashSet<Tile> markedForDeletion = new HashSet<Tile> ();
		List<Tile> matches = new List<Tile> ();

		// Check columns
		for (int x = 0; x < boardSize; x++) {

			// Checking a new column, clear the list of matching tiles
			if (matches.Count >= minTilesForMatch) {
				Debug.Log ("reset 1");
			}
			resetMatches (matches, markedForDeletion, updateScore);

			for (int y = 0; y < boardSize; y++) {
					
				Tile currentObject = inGrid [x, y];

				if (matches.Count > 0 && !matches [0].tag.Equals (currentObject.tag)) {

					// Tile is different from those in the current list of matches, reset
					if (matches.Count >= minTilesForMatch) {
						Debug.Log ("reset 2");
					}
					resetMatches (matches, markedForDeletion, updateScore);
				}

				matches.Add (currentObject);
			}
		}

		// Done checking columns, reset
		if (matches.Count >= minTilesForMatch) {
			Debug.Log ("reset 3");
		}
		resetMatches (matches, markedForDeletion, updateScore);


		// Check rows
		for (int y = 0; y < boardSize; y++) {

			// Checking a new column, clear the list of matching tiles
			if (matches.Count >= minTilesForMatch) {
				Debug.Log ("reset 4");
			}
			resetMatches (matches, markedForDeletion, updateScore);

			for (int x = 0; x < boardSize; x++) {

				Tile currentObject = inGrid [x, y];

				if (matches.Count > 0 && !matches [0].tag.Equals (currentObject.tag)) {

					// Tile is different from those in the current list of matches, reset
					if (matches.Count >= minTilesForMatch) {
						Debug.Log ("reset 5");
					}
					resetMatches (matches, markedForDeletion, updateScore);
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


				g.setTileMovementListener (this);	
				tilesToMove++;
				g.delete ();
			}
				
			List<Tile> newTiles = new List<Tile> ();
			// Spawn new tiles to fill the gaps
			foreach (KeyValuePair<float, int> entry in newTilesForRow) {
				for (int i = boardSize; i < boardSize + entry.Value; i++) {

					int x = (int)entry.Key;
					int y = i;

					newTiles.Add (spawnRandomTile (new Vector2 (x, y)));

				}
			}
				
			foreach (Tile tile in grid) {
				initTileMovement (tile, markedForDeletion);
			}

			// Also do this for the newly spawned tiles
			foreach (Tile tile in newTiles) {
				initTileMovement (tile, markedForDeletion);
			}

			tilesToMove = markedForDeletion.Count;



		} 
	}



	private void initTileMovement (Tile tile, HashSet<Tile> markedForDeletion)
	{
		if (tile != null && !markedForDeletion.Contains (tile)) {
			float x = tile.transform.position.x;
			float y = tile.transform.position.y;

			Int16 movement = 0;

			// Check how far this tile has to move down (could be 0)
			int maxHeight = Math.Min ((int)y, boardSize - 1);
			for (int i = maxHeight; i >= 0; i--) {

				if (markedForDeletion.Contains (grid [(int)x, i])) {
					movement++;
				}
			}

			if (movement > 0) {
				// Remove it from the grid while it moves (if it is part of the grid, newTiles are not)
				if (x < boardSize && y < boardSize) {
					grid [(int)x, (int)y] = null;
				}
				tile.setTileMovementListener (this);
//				Debug.Log ("added tile " + tile.transform.position.x + "," + tile.transform.position.y);
				pendingMovements.Add (tile, movement);		
			}
		}
	}

	public void deletionFinished ()
	{
		movedTiles++;

//		Debug.Log ("deletions: " + movedTiles + " / " + tilesToMove);

		if (movedTiles == tilesToMove) {

//			Debug.Log ("cleared pendingMovements");

			movedTiles = 0;
			tilesToMove = pendingMovements.Count;

			foreach (KeyValuePair<Tile, Int16> pair in pendingMovements) {
				pair.Key.fall (pair.Value);
			}

			pendingMovements.Clear ();
		}
	}



	/// <summary>
	/// Callback when a tile has finished moving to its target position.
	/// </summary>
	/// <param name="tile">The tile that has finished moving. Will be at its target position. </param>
	public void movementFinished (Tile tile)
	{
		// Place it back into the grid at the new position
		grid [(int)tile.transform.position.x, (int)tile.transform.position.y] = tile;
		movedTiles++;
//		Debug.Log ("moves " + movedTiles + "/" + tilesToMove);
//		Debug.Log (tile.tag + " now at " + tile.transform.position.x + "," + tile.transform.position.y);
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



	/// <summary>
	/// Clears the list of matches being checked right now. If there were enough for a match, they will be added to the list of
	/// pending deletions.
	/// </summary>
	/// <param name="checking">List of matching tiles. If more than #minTilesForMatch, these will be added to markedForDeletion.</param>
	/// <param name="markedForDeletion">The list of tiles that will be removed after checking the entire grid for matches</param>
	private void resetMatches (List<Tile> checking, HashSet<Tile> markedForDeletion, bool updateScore)
	{
		if (checking.Count >= minTilesForMatch) {
			markedForDeletion.UnionWith (checking);

			if (updateScore) {
				
				score += (100 * (Int32)Math.Pow (checking.Count, 2)) * combo;
				Debug.Log ("score is " + score);
				textViewScore.text =  "Score: " + score.ToString();

				textViewCombo.text = combo + "x combo";
				combo++;
			}
		}

		checking.Clear ();
	}
}
