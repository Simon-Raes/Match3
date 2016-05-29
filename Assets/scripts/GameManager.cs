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
		setupBoard();

		checkAndRemoveMatches (grid);			
	}
	
	// Update is called once per frame
	void Update ()
	{
		checkClick ();
	}

	private void setupBoard(){

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

//			Debug.Log ("mouse pos " + mousePosition.x + " y " + mousePosition.y + " ");    


			if (hitCollider != null) {


				var transform = selectionIndicator.transform.position;

				if (transform.x == -1 && transform.y == -1) {
					transform.x = hitCollider.transform.position.x;
					transform.y = hitCollider.transform.position.y;  

					selectionIndicator.transform.position = transform;

				} else {

		
					//todo check if it next to the current selection
					float xDistance = Math.Abs (transform.x - hitCollider.transform.position.x);
					float yDistance = Math.Abs (transform.y - hitCollider.transform.position.y);

					if ((xDistance == 0 && yDistance == 1) || (xDistance == 1 && yDistance == 0)) {
						transform.x = -1;
						transform.y = -1;


						//TODO check validity of move
						//TODO update checkMatches to accept a grid of items?
						// That way we can swap the two tiles, run the check and use that new grid if there are matches
						// and switch back to the old one if there are none
						// have checkmatches return the list of pendingdeletions
						// that way you don't have to run checkmatches twice.
						checkMatches(grid);

					} else {
						transform.x = hitCollider.transform.position.x;
						transform.y = hitCollider.transform.position.y;  
					}
  
					selectionIndicator.transform.position = transform;
				}

			}
		}
	}

	private void checkAndRemoveMatches(Tile[,] inGrid){
		HashSet<Tile> matches = checkMatches (inGrid);
		clearMatches (matches);
	}


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

	private bool clearMatches(HashSet<Tile> markedForDeletion){
			
		// Start deleting them
		if (markedForDeletion.Count > 0) {

			// todo reuse instead of making a new list every time
			Dictionary<float, int> newTilesForRow = new Dictionary<float,int> ();

//			Debug.Log ("gameobjects: " + grid.Length);
//			Debug.Log ("marked for deletion: " + markedForDeletion.Count);


			foreach (Tile g in markedForDeletion) {

//				Debug.Log ("will delete tile at : " + g.gameObject.transform.position.x +","+g.gameObject.transform.position.y);

				float tileXPosition = g.transform.position.x;
				int previousValue = 0;

				if (newTilesForRow.ContainsKey (tileXPosition)) {
					previousValue = newTilesForRow [tileXPosition];
					newTilesForRow [tileXPosition]++;
				} else {
					newTilesForRow.Add (tileXPosition, 1);
				}


				// TODO delete them with an animation

				Destroy (g.gameObject);
			}
				

			// Spawn new tiles to fill the gaps
			foreach(KeyValuePair<float, int> entry in newTilesForRow)
			{
				for (int i = boardSize; i < boardSize + entry.Value; i++) {

					int x = (int)entry.Key;
					int y = i;

					grid [x,y] = spawnRandomTile (new Vector2 (x,y));
				}
			}

			int counter = 0;
			foreach (Tile tile in grid) {
				if (tile != null && !markedForDeletion.Contains(tile)) {
					float x = tile.transform.position.x;
					float y = tile.transform.position.y;

					int movement = 0;


					foreach (Tile deletedTile in markedForDeletion){
						if (deletedTile.transform.position.x == x) {
							if (deletedTile.transform.position.y < y) {
								movement++;
							}
						}
					}
					if (movement > 0) {
						// Already move this tile to his target location in the grid, will be used when checking for matches later.
						grid [(int)x, (int)y - movement] = tile;
						grid [(int)x, (int)y] = null;
						tile.setTileMovementListener (this);	
						tilesToMove++;
						// Start animation, movementFinished callback will be called when it finishes
						tile.move (movement);
					}

				}

			}

			return true;
		
		} else {
			Debug.Log ("checked , no matches");
			// if there are no matches: we're done - let player play again
			return false;
		}
	}


	/**Tile callback*/

	public void movementFinished()
	{
		movedTiles++;
		Debug.Log ("move finished " + movedTiles);
		if (movedTiles == tilesToMove) {
			movedTiles = 0;
			tilesToMove = 0;
			checkAndRemoveMatches (grid);
		}
	}

	private void checkPossibleMoves()
	{
		// TODO check if there are any moves left
		// TODO can this already be done above? doubt it 
	}




	// utils
	private Tile spawnRandomTile(Vector2 location)
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
