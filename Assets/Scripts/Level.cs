using UnityEngine;
using System.Collections;
using System.IO;

public class Level : MonoBehaviour {

	// The level
	private char[,] level;
	private GameObject[,] tiles;
	private int levelNum = 1;
	private int worldNum = 1;

	// Resources
	private Material tile;
	private Material end;
	private Material off;
	private GameObject player;

	private bool isStarted = false;
	private bool isPaused = false;
	private Vector2 start = Vector2.zero;

	// Use this for initialization
	void Start () {
		// Load material resources
		tile = Resources.Load<Material> ("Textures/tile");
		end = Resources.Load<Material> ("Textures/end");
		off = Resources.Load<Material> ("Textures/off");
	}

	// Changr from the main menu to the level pack select
	public void PackSelect() {
		transform.Find ("Main Menu").gameObject.SetActive (false);
		transform.Find ("Pack Select").gameObject.SetActive (true);
	}

	// Change from the level pack select to the level select
	public void LevelSelect(int wrldNum) {
		worldNum = wrldNum;
		transform.Find ("Pack Select").gameObject.SetActive (false);
		transform.Find ("Level Select").gameObject.SetActive (true);
	}

	public void StartGame(int lvlNum) {
		levelNum = lvlNum;
		transform.Find ("Level Select").gameObject.SetActive (false);
		LoadLevel ("Levels/level" + worldNum.ToString() + "-" + levelNum.ToString());
		isStarted = true;
	}

	void LoadLevel (string filename) {
		// Load text file
		var fileContents = Resources.Load<TextAsset> (filename);
		
		// Process loaded text
		char[] delimiters = { '\n' };
		string[] lines = fileContents.text.Split (delimiters);
		level = new char[lines[lines.Length - 1].Length, lines.Length];
		tiles = new GameObject[lines[lines.Length - 1].Length, lines.Length];
		for (int y = 0; y < level.GetLength (1); y++) {
			for (int x = 0; x < level.GetLength (0); x++) {
				// Load text into the level array
				level[x,y] = lines[y][x];
				
				// Create game objects for level
				if (level[x,y] != '.') {
					tiles[x,y] = GameObject.CreatePrimitive(PrimitiveType.Cube);
					tiles[x,y].transform.localScale = new Vector3(1f, 0.25f, 1f);
					tiles[x,y].transform.position = new Vector3(x, -0.125f, -y);
					if (level[x,y] == 'x')
						tiles[x,y].renderer.material = tile;
					else if (level[x,y] == 'e')
						tiles[x,y].renderer.material = end;
					else if (level[x,y] == 's') {
						if (worldNum == 1) {
							player = (GameObject)Instantiate(Resources.Load ("Prefabs/Cube"), new Vector3(x, 0.5f, -y), Quaternion.identity);
						} 
						else if (worldNum == 2) {
							player = (GameObject)Instantiate(Resources.Load ("Prefabs/Long"), new Vector3(x, 1.0f, -y), Quaternion.identity);
						}
						var play = player.GetComponent<PlayerController> ();
						start = new Vector2(x, y);
						play.place = start;
						play.type = worldNum;
						play.orientation = 0;
						tiles[x,y].renderer.material = off;
					}
				}
			}
		}
	}

	void ReloadLevel () {
		for (int y = 0; y < level.GetLength(1); y++) {
			for (int x = 0; x < level.GetLength(0); x++) {
				if (level[x,y] == '-') {
					level[x,y] = 'x';
					tiles[x,y].renderer.material = tile;
				}
			}
		}

		var play = player.GetComponent<PlayerController> ();
		play.place = start;
		if (worldNum == 1) {
			player.transform.position = new Vector3(start.x, 0.5f, -start.y);
		} else if (worldNum == 2) {
			player.transform.position = new Vector3(start.x, 1.0f, -start.y);
			player.transform.rotation = Quaternion.identity;
		}
		play.orientation = 0;
		play.isFalling = false;
		play.fall = 0;
	}
	
	// Update is called once per frame
	void Update () {
		if (isStarted && !isPaused) {
			// Update player
			var play = player.GetComponent<PlayerController> ();
			play.PlayerUpdate();

			// If player has fallen off the edge
			if (play.isArrived) {
				// Square block
				if (play.type == 1 &&
				   (play.place.x < 0 || play.place.x >= level.GetLength(0) ||
				    play.place.y < 0 || play.place.y >= level.GetLength(1) ||
				    level[(int)play.place.x, (int)play.place.y] == '.')) {
					play.isFalling = true;
				} // Long block
				else if (play.type == 2 && (play.orientation == 0 &&
				        	(play.place.x < 0 || play.place.x >= level.GetLength(0) ||
				 		 	play.place.y < 0 || play.place.y >= level.GetLength(1) ||
				 		 	level[(int)play.place.x, (int)play.place.y] == '.')) ||
				        (play.orientation == 1 &&
				        	(play.place.x < 0 || play.place.x >= level.GetLength(0) - 1 ||
				 			play.place.y < 0 || play.place.y >= level.GetLength(1) ||
				 			level[(int)play.place.x, (int)play.place.y] == '.' ||
				 			level[(int)play.place.x + 1, (int)play.place.y] == '.')) ||
				        (play.orientation == 2 &&
							(play.place.x < 0 || play.place.x >= level.GetLength(0) ||
							play.place.y < 1 || play.place.y >= level.GetLength(1) ||
							level[(int)play.place.x, (int)play.place.y] == '.' ||
							level[(int)play.place.x, (int)play.place.y - 1] == '.'))) {
					play.isFalling = true;
				}
				// If player has reached the exit
				else if ((play.type == 1 || play.type == 2 && play.orientation == 0) &&
				         level[(int)play.place.x, (int)play.place.y] == 'e') {
					ReloadLevel();
				}
				// When player reaches a new tile
				else if (play.type == 1) {
					// If it was a blank tile
					if (level[(int)play.place.x, (int)play.place.y] == 'x') {
						level[(int)play.place.x, (int)play.place.y] = '-';
						tiles[(int)play.place.x, (int)play.place.y].renderer.material = off;
					}
					// If it was a used tile
					else if (level[(int)play.place.x, (int)play.place.y] == '-' ||
					         level[(int)play.place.x, (int)play.place.y] == 's') {
						ReloadLevel();
					}
				}
			}

			// Catch player after they fall off the edge
			if (play.isFalling && player.transform.position.y < -15) {
				ReloadLevel();
			}

			// Check for pause
			if (Input.GetKeyDown (KeyCode.P)) {
				isPaused = true;
				transform.Find ("Pause Menu").gameObject.SetActive (true);
			}
		}
	}

	// Unpause the game
	public void ResumeGame() {
		isPaused = false;
		transform.Find ("Pause Menu").gameObject.SetActive (false);
	}

	public void QuitGame() {
		// Destroy game objects
		Destroy (player);
		for (int y = 0; y < tiles.GetLength(1); y++) {
			for (int x = 0; x < tiles.GetLength(0); x++) {
				Destroy (tiles[x, y]);
			}
		}

		isStarted = false;
		isPaused = false;
		transform.Find ("Pause Menu").gameObject.SetActive (false);
		transform.Find ("Level Select").gameObject.SetActive (true);
	}

	public void PreviousLevel() {
		if (levelNum > 1) {
			// Destroy game objects
			Destroy (player);
			for (int y = 0; y < tiles.GetLength(1); y++) {
				for (int x = 0; x < tiles.GetLength(0); x++) {
					Destroy (tiles[x, y]);
				}
			}
			
			isStarted = false;
			isPaused = false;
			transform.Find ("Pause Menu").gameObject.SetActive (false);

			levelNum--;
			LoadLevel ("Levels/level" + worldNum.ToString() + "-" + levelNum.ToString());
			isStarted = true;
		}
	}

	public void NextLevel() {
		if ((worldNum == 1 && levelNum < 4) ||
		    (worldNum == 2 && levelNum < 8)) {
			// Destroy game objects
			Destroy (player);
			for (int y = 0; y < tiles.GetLength(1); y++) {
				for (int x = 0; x < tiles.GetLength(0); x++) {
					Destroy (tiles[x, y]);
				}
			}
			
			isStarted = false;
			isPaused = false;
			transform.Find ("Pause Menu").gameObject.SetActive (false);
			
			levelNum++;
			LoadLevel ("Levels/level" + worldNum.ToString() + "-" + levelNum.ToString());
			isStarted = true;
		}
	}
}
