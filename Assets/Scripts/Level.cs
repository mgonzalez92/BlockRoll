using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;

public class Level : MonoBehaviour {

	// The level
	private char[,] level;
	private GameObject[,] tiles;
	private float[,] delays;
	private int levelNum = 1;
	private int worldNum = 1;
	private const int LEVELNUM = 16;
	private const int WORLDNUM = 3;

	// Menu Resources
	private Transform mainMenu;
	private Transform packSelect;
	private Transform levelSelect;
	private GameObject[] packButtons;
	private GameObject[] levelButtons;
	private Transform pauseButton;

	// Resources
	private Material tile;
	private Material end;
	private Material off;
	private GameObject player;

	public bool isPack = false;
	public bool isLoading = false;
	private bool isStarted = false;
	private bool isPaused = false;
	private bool isCompleted = false;
	private Vector2 start = Vector2.zero;
	private float time;
	private const int SCROLLSPEED = 30;

	// Camera values
	private static Vector3 CAMERAPOS = new Vector3 (-11f, -12f, -25f);

	// Use this for initialization
	void Start () {
		// Load material resources
		tile = Resources.Load<Material> ("Textures/tile");
		end = Resources.Load<Material> ("Textures/end");
		off = Resources.Load<Material> ("Textures/off");

		mainMenu = transform.Find ("Main Menu");
		packSelect = transform.Find ("Pack Select");
		levelSelect = transform.Find ("Level Select");
		pauseButton = transform.Find ("Background").transform.FindChild ("Pause Button");
	}

	// Change from the main menu to the level pack select
	public void PackSelect() {
		mainMenu.gameObject.SetActive (false);
		packSelect.gameObject.SetActive (true);
		isPack = true;

		packButtons = new GameObject[WORLDNUM];
		for (int i = 1; i <= WORLDNUM; i++) {
			packButtons[i - 1] = (GameObject)Instantiate(Resources.Load ("Prefabs/Pack Button"));
			packButtons[i - 1].transform.SetParent(packSelect.transform, false);
			packButtons[i - 1].GetComponent<RectTransform>().localPosition = new Vector2(-300 + 600 * (i - 1), -200);
			packButtons[i - 1].transform.FindChild("Pack Text").GetComponent<Text>().text = "Pack " + i;
			int k = i;
			packButtons[i - 1].GetComponent<Button>().onClick.AddListener(() => LevelSelect(k));
		}
	}

	// Change from the level pack select to the level select
	public void LevelSelect(int worldNum) {
		this.worldNum = worldNum;
		packSelect.gameObject.SetActive (false);
		levelSelect.gameObject.SetActive (true);
		isPack = false;

		levelButtons = new GameObject[LEVELNUM];
		for (int i = 1; i <= LEVELNUM; i++) {
			levelButtons[i - 1] = (GameObject)Instantiate(Resources.Load ("Prefabs/Level Button"));
			levelButtons[i - 1].transform.SetParent(levelSelect.transform, false);
			levelButtons[i - 1].GetComponent<RectTransform>().localPosition = new Vector2(
				-225 + 150 * ((i - 1) % 4), 100 - 150 * ((i - 1) / 4));
			levelButtons[i - 1].transform.FindChild("Level Text").GetComponent<Text>().text = i.ToString();
			int k = i;
			levelButtons[i - 1].GetComponent<Button>().onClick.AddListener(() => StartGame(k));
		}
	}

	public void StartGame(int levelNum) {
		this.levelNum = levelNum;
		levelSelect.gameObject.SetActive (false);
		LoadLevel ("Levels/level" + this.worldNum.ToString() + "-" + this.levelNum.ToString());
		pauseButton.gameObject.SetActive (true);
	}

	void LoadLevel (string filename) {
		// Load text file
		var fileContents = Resources.Load<TextAsset> (filename);
		
		// Process loaded text
		char[] delimiters = { '\n' };
		string[] lines = fileContents.text.Split (delimiters);
		level = new char[lines[lines.Length - 1].Length, lines.Length - 1];
		tiles = new GameObject[lines[lines.Length - 1].Length, lines.Length - 1];
		delays = new float[lines[lines.Length - 1].Length, lines.Length - 1];
		for (int y = 0; y < level.GetLength (1); y++) {
			for (int x = 0; x < level.GetLength (0); x++) {
				// Load text into the level array
				level[x,y] = lines[y+1][x];

				if (level[x,y] != '.')
					delays[x,y] = Random.value;
			}
		}
		GameObject camera = transform.Find ("Main Camera").gameObject;
		camera.transform.localPosition = CAMERAPOS +
			new Vector3 (level.GetLength (0) / 2f, 0f, -level.GetLength (1) / 2f);
		camera.transform.camera.orthographicSize = Mathf.Pow (Mathf.Max (
			level.GetLength (0) * 9f / 16f, level.GetLength (1)) + 3, 0.7f);

		isLoading = true;
		time = Time.time;
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
			player.transform.FindChild("Cube1").transform.localPosition = new Vector3(0, -0.5f, 0);
			player.transform.FindChild("Cube2").transform.localPosition = new Vector3(0, 0.5f, 0);
			player.transform.position = new Vector3(start.x, 1.0f, -start.y);
			player.transform.rotation = Quaternion.identity;
		} else if (worldNum == 3) {
			player.transform.FindChild("Cube1").transform.localPosition = new Vector3(0, -1f, 0);
			player.transform.FindChild("Cube2").transform.localPosition = new Vector3(0, 1f, 0);
			player.transform.position = new Vector3(start.x, 1.5f, -start.y);
			player.transform.rotation = Quaternion.identity;
		} 
		play.orientation = 0;
		play.isFalling = -1;
		play.fall = 0;
	}
	
	// Update is called once per frame
	void Update () {
		if (isPack && Input.GetMouseButton(0)) {
			float mouseMovement = Input.GetAxis ("Mouse X");
			for (int i = 0; i < WORLDNUM; i++) {
				packButtons[i].GetComponent<RectTransform>().localPosition = new Vector2(
					packButtons[i].GetComponent<RectTransform>().localPosition.x + mouseMovement * SCROLLSPEED,
					packButtons[i].GetComponent<RectTransform>().localPosition.y);
			}
		}
		else if (isLoading) {
			float loadTime = Time.time;
			for (int y = 0; y < level.GetLength (1); y++) {
				for (int x = 0; x < level.GetLength (0); x++) {
					// Create game objects for level
					if (level[x,y] != '.' && loadTime - time > delays[x,y] * 0.5f) {
						if (tiles[x,y] == null) {
							tiles[x,y] = GameObject.CreatePrimitive(PrimitiveType.Cube);
							tiles[x,y].transform.localScale = new Vector3(1f, 0.25f, 1f);
							tiles[x,y].transform.position = new Vector3(x, -0.125f - 10, -y);
							if (level[x,y] == 'x')
								tiles[x,y].renderer.material = tile;
							else if (level[x,y] == 'e')
								tiles[x,y].renderer.material = end;
							else if (level[x,y] == 's') {
								if (worldNum == 1) {
									tiles[x,y].renderer.material = off;
									player = (GameObject)Instantiate(Resources.Load ("Prefabs/Cube"), new Vector3(x, 0.5f - 10, -y), Quaternion.identity);
								} else if (worldNum == 2) {
									tiles[x,y].renderer.material = tile;
									player = (GameObject)Instantiate(Resources.Load ("Prefabs/Long"), new Vector3(x, 1.0f - 10, -y), Quaternion.identity);
								} else if (worldNum == 3) {
									tiles[x,y].renderer.material = tile;
									player = (GameObject)Instantiate(Resources.Load ("Prefabs/Split"), new Vector3(x, 1.5f - 10, -y), Quaternion.identity);
								}
								var play = player.GetComponent<PlayerController> ();
								start = new Vector2(x, y);
								play.place = start;
								play.type = worldNum;
								play.orientation = 0;
								play.isFalling = -1;
							}
						} else if (tiles[x,y].transform.position.y < -0.125f) {
							tiles[x,y].transform.position += new Vector3(0, 0.5f, 0);
							if (level[x,y] == 's')
								player.transform.position += new Vector3(0, 0.5f, 0);
						}
					}
				}
			}
			if (loadTime - time > 1) {
				isLoading = false;
				isStarted = true;
			}
		}
		else if (isStarted && !isPaused) {
			// Update player
			var play = player.GetComponent<PlayerController> ();
			play.PlayerUpdate();

			// If player has fallen off the edge
			if (play.isArrived) {
				// If player has reached the exit
				if (play.place.x >= 0 && play.place.x < level.GetLength(0) &&
				    play.place.y >= 0 && play.place.y < level.GetLength(1) &&
				    level[(int)play.place.x, (int)play.place.y] == 'e' &&
				         (play.type == 1 || (play.type == 2 || play.type == 3) && play.orientation == 0)) {
					isStarted = false;
					isCompleted = true;
					time = Time.time;
					transform.Find ("Win Menu").gameObject.SetActive (true);
				}
				/************************
				 * Falling off the edge * 
				 ***********************/
				// Square block
				if (play.type == 1 &&
				    (play.place.x < 0 || play.place.x >= level.GetLength(0) ||
				 play.place.y < 0 || play.place.y >= level.GetLength(1) ||
				 level[(int)play.place.x, (int)play.place.y] == '.')) {
					play.isFalling = 0;
					time = Time.time;
				} // Long block
				else if (play.type == 2) {
					// Straight fall
					if ((play.orientation == 0 &&
					     (play.place.x < 0 || play.place.x >= level.GetLength(0) ||
					 play.place.y < 0 || play.place.y >= level.GetLength(1) ||
					 level[(int)play.place.x, (int)play.place.y] == '.')) ||
					    (play.orientation == 1 &&
					 (play.place.x < -1 || // Both blocks off left side
					 play.place.x >= level.GetLength(0) || // Both blocks off right side
					 play.place.y < 0 || // Both blocks off top side
					 play.place.y >= level.GetLength(1) || // Both blocks off bottom side
					 play.place.x < 0 && level[(int)play.place.x + 1, (int)play.place.y] == '.' || // Block 1 is off left, block 2 is air
					 level[(int)play.place.x, (int)play.place.y] == '.' && play.place.x >= level.GetLength(0) - 1 || // Block 1 is air, block 2 is off right
					 level[(int)play.place.x, (int)play.place.y] == '.' && level[(int)play.place.x + 1, (int)play.place.y] == '.')) || // Both blocks are air
					    (play.orientation == 2 &&
					 (play.place.x < 0 || // Both blocks off left side
					 play.place.x >= level.GetLength(0) || // Both blocks off right side
					 play.place.y < 0 || // Both blocks off top side
					 play.place.y >= level.GetLength(1) + 1 || // Both blocks off bottom side
					 play.place.y < 1 && level[(int)play.place.x, (int)play.place.y] == '.' || // Block 2 is off top, block 1 is air
					 level[(int)play.place.x, (int)play.place.y - 1] == '.' && play.place.y >= level.GetLength(1) || // Block 2 is air, block 1 is off bottom
					 level[(int)play.place.x, (int)play.place.y] == '.' && level[(int)play.place.x, (int)play.place.y - 1] == '.'))) { // Both blocks are air
						play.isFalling = 0;
						time = Time.time;
					} // Block 1 fall
					else if (play.place.x < 0 || // Block 1 is off left side
					         play.place.y >= level.GetLength(1) || // Block 1 is off bottom side
					         level[(int)play.place.x, (int)play.place.y] == '.') { // Block 1 is air
						play.isFalling = 1;
						time = Time.time;
					} // Block 2 fall
					/*else if (play.place.x + 1 >= level.GetLength(0) || // Block 2 is off right side
					         play.place.y - 1 >= level.GetLength(1) || // Block 2 is off top side
					         play.orientation == 1 && level[(int)play.place.x + 1, (int)play.place.y] == '.' || // Block 2 is air
					         play.orientation == 2 && level[(int)play.place.x, (int)play.place.y - 1] == '.') { // Block 2 is air
						play.isFalling = 2;
						time = Time.time;
					}*/
				} // Split block
				else if (play.type == 3 && ((play.orientation == 0 &&
				                             (play.place.x < 0 || play.place.x >= level.GetLength(0) ||
				 play.place.y < 0 || play.place.y >= level.GetLength(1) ||
				 level[(int)play.place.x, (int)play.place.y] == '.')) ||
				                            (play.orientation == 1 &&
				 (play.place.x < 0 || play.place.x >= level.GetLength(0) - 2 ||
				 play.place.y < 0 || play.place.y >= level.GetLength(1) ||
				 level[(int)play.place.x, (int)play.place.y] == '.' ||
				 level[(int)play.place.x + 2, (int)play.place.y] == '.')) ||
				                            (play.orientation == 2 &&
				 (play.place.x < 0 || play.place.x >= level.GetLength(0) ||
				 play.place.y < 2 || play.place.y >= level.GetLength(1) ||
				 level[(int)play.place.x, (int)play.place.y] == '.' ||
				 level[(int)play.place.x, (int)play.place.y - 2] == '.')))) {
					play.isFalling = 0;
					time = Time.time;
				}
				// When player reaches a new tile
				if (play.type == 1) {
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
			if (play.isFalling != -1) {
				float fallTime = Time.time;
				if (fallTime - time > 1)
					ReloadLevel();
			}

			// Check for pause
			if (Input.GetKeyDown (KeyCode.P)) {
				isPaused = true;
				transform.Find ("Pause Menu").gameObject.SetActive (true);
			}
		}
		else if (isCompleted) {
			// Make platforms float up
			var play = player.GetComponent<PlayerController> ();
			float loadTime = Time.time;
			for (int y = 0; y < level.GetLength (1); y++) {
				for (int x = 0; x < level.GetLength (0); x++) {
					if (level[x,y] != '.' && level[x,y] != 'e' && loadTime - time > delays[x,y] * 4f) {
						tiles[x,y].transform.Translate(Vector3.up * Time.deltaTime * (2 - delays[x,y]), Space.World);
						if ((x + y) % 2 == 0)
							tiles[x,y].transform.Rotate(50 * Time.deltaTime * Vector3.right);
						else
							tiles[x,y].transform.Rotate(50 * Time.deltaTime * Vector3.left);
						if ((x + y + 1) % 2 == 0)
							tiles[x,y].transform.Rotate(50 * Time.deltaTime * Vector3.forward);
						else
							tiles[x,y].transform.Rotate(50 * Time.deltaTime * Vector3.back);
					}
				}
			}
			if (loadTime - time > 0.3f) {
				player.transform.Translate(Vector3.up * Time.deltaTime * 2, Space.World);
				player.transform.Rotate(100 * Time.deltaTime * Vector3.right, Space.World);
				player.transform.Rotate(50 * Time.deltaTime * Vector3.forward, Space.World);
			} if (loadTime - time > 0.5f) {
				tiles[(int)play.place.x, (int)play.place.y].transform.Translate(Vector3.up * Time.deltaTime * 1.5f, Space.World);
				tiles[(int)play.place.x, (int)play.place.y].transform.Rotate(50 * Time.deltaTime * Vector3.left);
				tiles[(int)play.place.x, (int)play.place.y].transform.Rotate(50 * Time.deltaTime * Vector3.back);
			}
		}
	}

	// Pause the game
	public void PauseGame() {
		if (isStarted && !isPaused) {
			isPaused = true;
			transform.Find ("Pause Menu").gameObject.SetActive (true);
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
		isCompleted = false;
		if (transform.Find ("Pause Menu").gameObject.activeSelf)
			transform.Find ("Pause Menu").gameObject.SetActive (false);
		else if (transform.Find ("Win Menu").gameObject.activeSelf)
			transform.Find ("Win Menu").gameObject.SetActive (false);
		pauseButton.gameObject.SetActive (false);
		transform.Find ("Level Select").gameObject.SetActive (true);
	}

	public void RetryLevel() {
		// Destroy game objects
		Destroy (player);
		for (int y = 0; y < tiles.GetLength(1); y++) {
			for (int x = 0; x < tiles.GetLength(0); x++) {
				Destroy (tiles[x, y]);
			}
		}
		
		isStarted = false;
		isPaused = false;
		isCompleted = false;
		transform.Find ("Win Menu").gameObject.SetActive (false);

		LoadLevel ("Levels/level" + worldNum.ToString() + "-" + levelNum.ToString());
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
			isCompleted = false;
			if (transform.Find ("Pause Menu").gameObject.activeSelf)
				transform.Find ("Pause Menu").gameObject.SetActive (false);
			else if (transform.Find ("Win Menu").gameObject.activeSelf)
				transform.Find ("Win Menu").gameObject.SetActive (false);

			levelNum--;
			LoadLevel ("Levels/level" + worldNum.ToString() + "-" + levelNum.ToString());
		}
	}

	public void NextLevel() {
		if ((worldNum == 1 && levelNum < 4) ||
		    (worldNum == 2 && levelNum < 25) ||
		    (worldNum == 3 && levelNum < 25)) {
			// Destroy game objects
			Destroy (player);
			for (int y = 0; y < tiles.GetLength(1); y++) {
				for (int x = 0; x < tiles.GetLength(0); x++) {
					Destroy (tiles[x, y]);
				}
			}

			isStarted = false;
			isPaused = false;
			isCompleted = false;
			if (transform.Find ("Pause Menu").gameObject.activeSelf)
				transform.Find ("Pause Menu").gameObject.SetActive (false);
			else if (transform.Find ("Win Menu").gameObject.activeSelf)
				transform.Find ("Win Menu").gameObject.SetActive (false);
			
			levelNum++;
			LoadLevel ("Levels/level" + worldNum.ToString() + "-" + levelNum.ToString());
		}
	}
}
