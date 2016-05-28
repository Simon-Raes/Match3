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

		checkMatches ();			
	}
	
	// Update is called once per frame
	void Update ()
	{
		checkClick ();

		// TODO don't really need to do this 60 times per second? - just at start and after a move


		
	}

	private void setupBoard(){

		// Reserve extra space above where new tiles will be inserted
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
				transform.x = hitCollider.transform.position.x;
				transform.y = hitCollider.transform.position.y;   
				selectionIndicator.transform.position = transform;

				//				ctionLocation

				Debug.Log ("Hit " + hitCollider.transform.name + " x " + hitCollider.transform.position.x + " y " + hitCollider.transform.position.y);    
			}
		}
	}

	private void checkMatches ()
	{
		HashSet<Tile> markedForDeletion = new HashSet<Tile> ();
		List<Tile> matches = new List<Tile> ();

		// Check columns
		for (int x = 0; x < boardSize; x++) {

			// Checking a new column, clear the list of matching tiles
			resetMatches (matches, markedForDeletion);

			for (int y = 0; y < boardSize; y++) {

				Tile currentObject = grid [x, y];

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

				Tile currentObject = grid [x, y];

				if (matches.Count > 0 && !matches [0].tag.Equals (currentObject.tag)) {

					// Tile is different from those in the current list of matches, reset
					resetMatches (matches, markedForDeletion);
				}

				matches.Add (currentObject);
			}
		}	
			
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

			// TODO move them 
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

//						Debug.Log ("will move " + x + "," + y);
						grid [(int)x, (int)y - movement] = tile;
						grid [(int)x, (int)y] = null;
						tile.setTileMovementListener (this);	
						tilesToMove++;
						tile.move (movement);
					}

				}

			}
//			Debug.Log (String.Format("will move {0} tiles", tilesToMove));


		} else {
			// if there are no matches: we're done - let player play again
		}
	}

	public void movementFinished()
	{
		movedTiles++;
		Debug.Log ("move finished " + movedTiles);
		if (movedTiles == tilesToMove) {
//			Debug.Log ("will now check matches again after moving");
			movedTiles = 0;
			tilesToMove = 0;
			checkMatches ();
		}
	}

//	private Boolean movesPossible(){
//	}

	private void checkPossibleMoves()
	{
		// TODO check if there are any moves left
		// TODO can this already be done above? doubt it 
	}




	// utils
	private Tile spawnRandomTile(Vector2 location)
	{
		Tile tile = tiles [UnityEngine.Random.Range (0, 4)];
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
