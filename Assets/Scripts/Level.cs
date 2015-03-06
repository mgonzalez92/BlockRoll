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
	private const int LEVELNUM = 50;
	private int blockType = 2;

	// Menu Resources
	private Transform mainMenu;
	private Transform levelSelect;
	private Transform HUD;
	private GameObject[] levelButtons;

	// Materials
	private Material Tile;
	private Material EndTile;
	private Material Player;
	private string theme = "Default";
	private const float COLORALT = 0.1f;

	// Resources
	private GameObject LevelButton;
	private TextAsset[] Levels;
	private GameObject CubeBlock;
	private GameObject LongBlock;
	private GameObject SplitBlock;
	private GameObject TileBlock;
	private GameObject player;

	// Level Select
	private const int STARTX = 300;
	private const int SPACEX = 400;

	// Backgrounds
	private RawImage Background;
	private Texture MenuBackground;
	private Texture DefaultBackground;
	private Texture NightBackground;

	private const int POOLSIZE = 100;
	private GameObject[] tilePool;
	private int poolDepth = 0;
	
	public bool isLoading = false;
	private bool isStarted = false;
	private bool isPaused = false;
	private bool isCompleted = false;
	private Vector2 start = Vector2.zero;
	private float time;
	private const int SCROLLSPEED = 30;
	private const float MAXFALLTIME = 0.7f;

	// Moves
	private int moves = 0;
	private int maxMoves = 0;
	private Text MovesText;

	// Camera values
	private static Vector3 CAMERAPOS = new Vector3 (-11f, -12f, -25f);

	// Use this for initialization
	void Start () {
		// Load material resources
		Tile = Resources.Load<Material> ("Textures/" + theme + "/tile");
		EndTile = Resources.Load<Material> ("Textures/" + theme + "/end");
		Player = Resources.Load<Material> ("Textures/" + theme + "/player");

		// Load backgrounds
		Background = (RawImage)transform.FindChild ("Background").FindChild ("RawImage").GetComponent<RawImage> ();
		MenuBackground = Resources.Load<Texture> ("UI/tessellate");
		DefaultBackground = Resources.Load<Texture> ("UI/daysky");
		NightBackground = Resources.Load<Texture> ("UI/nightsky");

		// Load button resources
		LevelButton = Resources.Load<GameObject> ("Prefabs/Level Button");

		// Load 3d resources
		TileBlock = Resources.Load<GameObject> ("Prefabs/Tile");

		// Instantiate player
		CubeBlock = (GameObject)Instantiate (Resources.Load<GameObject> ("Prefabs/Cube"), new Vector3 (0, -50, 0), Quaternion.identity);
		LongBlock = (GameObject)Instantiate (Resources.Load<GameObject> ("Prefabs/Long"), new Vector3 (0, -50, 0), Quaternion.identity);
		SplitBlock = (GameObject)Instantiate (Resources.Load<GameObject> ("Prefabs/Split"), new Vector3 (0, -50, 0), Quaternion.identity);
		CubeBlock.renderer.material = Player;
		LongBlock.transform.FindChild ("Cube1").renderer.material = Player;
		LongBlock.transform.FindChild ("Cube2").renderer.material = Player;
		SplitBlock.transform.FindChild ("Cube1").renderer.material = Player;
		SplitBlock.transform.FindChild ("Cube2").renderer.material = Player;

		// Instantiate tile pool
		tilePool = new GameObject [POOLSIZE];
		for (int i = 0; i < POOLSIZE; i++) {
			tilePool[i] = (GameObject)Instantiate(TileBlock, new Vector3(0, -50, 0), Quaternion.identity);
		}

		mainMenu = transform.Find ("Main Menu");
		levelSelect = transform.Find ("Level Select");
		HUD = transform.Find ("HUD");
		MovesText = HUD.FindChild ("Moves Backdrop").FindChild ("Number").GetComponent<Text> ();
	}

	// Change from the level pack select to the level select
	public void LevelSelect() {
		mainMenu.gameObject.SetActive (false);
		levelSelect.gameObject.SetActive (true);

		Transform ScrollArea = levelSelect.FindChild ("Scroll Rect").FindChild ("Mask").FindChild ("Scroll Area");
		//ScrollArea.GetComponent<RectTransform> ().sizeDelta = new Vector2 (STARTX + SPACEX * LEVELNUM, Screen.height);
		if (levelButtons == null) {
			levelButtons = new GameObject[LEVELNUM];
			for (int i = 1; i <= LEVELNUM; i++) {
				levelButtons[i - 1] = (GameObject)Instantiate(LevelButton);
				levelButtons[i - 1].transform.SetParent(ScrollArea.transform, false);
				levelButtons[i - 1].GetComponent<RectTransform>().localPosition = new Vector2(
					STARTX + SPACEX * (i - 1), Mathf.Sin(i) * 100 + Screen.height * 0.5f + 50);
				levelButtons[i - 1].transform.FindChild("Level Text").GetComponent<Text>().text = i.ToString();
				int k = i;
				levelButtons[i - 1].GetComponent<Button>().onClick.AddListener(() => StartGame(k));
			}
		}

		Levels = new TextAsset[LEVELNUM];
		for (int i = 1; i <= LEVELNUM; i++) {
			Levels[i - 1] = Resources.Load<TextAsset> ("Levels/" + i.ToString ());
		}
	}

	public void StartGame(int levelNum) {
		this.levelNum = levelNum;
		levelSelect.gameObject.SetActive (false);
		HUD.gameObject.SetActive (true);

		// Set background
		if (theme == "Night")
			Background.texture = NightBackground;
		else
			Background.texture = DefaultBackground;

		LoadLevel ();
	}

	void LoadLevel () {
		// Process loaded text
		string[] lines = Levels[levelNum - 1].text.Split ('\n');
		string[] info = lines [0].Split (' ');
		maxMoves = int.Parse(info [0]);
		blockType = int.Parse (info [1]);
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
			new Vector3 (level.GetLength (0) * 0.5f + 6, 24f, -level.GetLength (1) * 0.5f + 15);
		camera.transform.camera.orthographicSize = Mathf.Log (
			Mathf.Max (level.GetLength (0) * Screen.width / Screen.height, level.GetLength (1))) * 1.9f;

		isLoading = true;
		poolDepth = 0;
		moves = maxMoves * 3;
		MovesText.text = moves.ToString ();
		MovesText.color = new Color(0.1f, 0.1f, 0.1f);
		time = Time.time;
	}

	void ReloadLevel () {
		var play = player.GetComponent<PlayerController> ();

		// Reset tiles
		if (play.type == 1) {
			for (int y = 0; y < level.GetLength(1); y++) {
				for (int x = 0; x < level.GetLength(0); x++) {
					if (level[x,y] == '-') {
						level[x,y] = 'x';
						tiles[x,y].renderer.material = Tile;
					}
				}
			}
		}

		// Reset player position
		play.placeX = (int)start.x;
		play.placeY = (int)start.y;
		if (blockType == 1) {
			player.transform.position = new Vector3(start.x, 0.5f, -start.y);
		} else if (blockType == 2) {
			player.transform.FindChild("Cube1").transform.localPosition = new Vector3(0, -0.5f, 0);
			player.transform.FindChild("Cube2").transform.localPosition = new Vector3(0, 0.5f, 0);
			player.transform.position = new Vector3(start.x, 1.0f, -start.y);
			player.transform.rotation = Quaternion.identity;
		} else if (blockType == 3) {
			player.transform.FindChild("Cube1").transform.localPosition = new Vector3(0, -1f, 0);
			player.transform.FindChild("Cube2").transform.localPosition = new Vector3(0, 1f, 0);
			player.transform.position = new Vector3(start.x, 1.5f, -start.y);
			player.transform.rotation = Quaternion.identity;
		} 
		play.orientation = 0;
		play.isFalling = -1;
		play.fall = 0;

		// Reset score
		moves = maxMoves * 3;
		MovesText.text = moves.ToString ();
		MovesText.color = new Color(0.1f, 0.1f, 0.1f);
	}
	
	// Update is called once per frame
	void Update () {
		if (isLoading) {
			float loadTime = Time.time;
			for (int y = 0; y < level.GetLength (1); y++) {
				for (int x = 0; x < level.GetLength (0); x++) {
					// Create game objects for level
					if (level[x,y] != '.' && loadTime - time > delays[x,y] * 0.5f) {
						if (tiles[x,y] == null) {
							tiles[x,y] = tilePool[poolDepth];
							tiles[x,y].transform.localPosition = new Vector3(x, -0.125f - 10, -y);
							poolDepth += 1;
							if (level[x,y] == 'x' || level[x,y] == 's') {
								tiles[x,y].renderer.material = Tile;
								tiles[x,y].renderer.material.color -= Color.white * Random.value * COLORALT;
							} else if (level[x,y] == 'e') {
								tiles[x,y].renderer.material = EndTile;
							}
							if (level[x,y] == 's') {
								if (blockType == 1) {
									player = CubeBlock;
									player.transform.localPosition = new Vector3(x, 0.5f - 10, -y);
								} else if (blockType == 2) {
									player = LongBlock;
									player.transform.localPosition = new Vector3(x, 1.0f - 10, -y);
								} else if (blockType == 3) {
									player = SplitBlock;
									player.transform.localPosition = new Vector3(x, 1.5f - 10, -y);
								}
								var play = player.GetComponent<PlayerController> ();
								start = new Vector2(x, y);
								play.placeX = (int)start.x;
								play.placeY = (int)start.y;
								play.type = blockType;
								play.orientation = 0;
								play.isFalling = -1;
							}
						} else if (tiles[x,y].transform.position.y < -0.125f) {
							float distance = Mathf.Min (Time.deltaTime * 70, -0.125f - tiles[x,y].transform.position.y);
							tiles[x,y].transform.position += Vector3.up * distance;
							if (level[x,y] == 's')
								player.transform.position += Vector3.up * distance;
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
			bool wasRotating = play.isRotating;
			play.PlayerUpdate();

			// If player just began moving
			if (play.isRotating && !wasRotating) {
				/** Deplete moves **/
				if (play.isFalling < 0) {
					moves--;
					MovesText.text = moves.ToString();
					if (moves <= 5) {
						MovesText.color = new Color(0.9f, 0, 0);
					}
				}
			}
			// If player has fallen off the edge
			if (play.isArrived) {
				// If player has reached the exit
				if (play.placeX >= 0 && play.placeX < level.GetLength(0) &&
				    play.placeY >= 0 && play.placeY < level.GetLength(1) &&
				    level[play.placeX, play.placeY] == 'e' &&
				         (play.type == 1 || (play.type == 2 || play.type == 3) && play.orientation == 0)) {
					isStarted = false;
					isCompleted = true;
					time = Time.time;
					transform.Find ("Win Menu").FindChild("Win Text").GetComponent<Text>().text =
						"Level " + levelNum + "\nComplete!";
					transform.Find ("Win Menu").gameObject.SetActive (true);
				}
				/************************
				 * Falling off the edge * 
				 ***********************/
				time = Time.time;
				// Square block
				if (play.type == 1 &&
				    (play.placeX < 0 || play.placeX >= level.GetLength(0) ||
				 play.placeY < 0 || play.placeY >= level.GetLength(1) ||
				 level[play.placeX, play.placeY] == '.')) {
					play.isFalling = 0;
				} // Long block
				else if (play.type == 2 || play.type == 3) {
					int i = play.type - 1;
					// Both blocks off map
					if (play.orientation == 0 && (
					    play.placeX < 0 || play.placeX >= level.GetLength (0) ||
					    play.placeY < 0 || play.placeY >= level.GetLength (1)) ||
					    play.orientation == 1 && (
					    play.placeX + i < 0 || play.placeX >= level.GetLength (0) ||
					    play.placeY < 0 || play.placeY >= level.GetLength (1)) ||
					    play.orientation == 2 && (
						play.placeX < 0 || play.placeX >= level.GetLength (0) ||
						play.placeY + i < 0 || play.placeY >= level.GetLength (1))) {
						play.isFalling = 0;
					} // a block off map, a block on air
					else if (play.orientation == 1 && (
						play.placeX < 0 && level[play.placeX + i, play.placeY] == '.' ||
						play.placeX + i >= level.GetLength (0) && level[play.placeX, play.placeY] == '.') ||
						play.orientation == 2 && (
						play.placeY < 0 && level[play.placeX, play.placeY + i] == '.' ||
						play.placeY + i >= level.GetLength (1) && level[play.placeX, play.placeY] == '.')) {
						play.isFalling = 0;
					} // block 1 NOT on tile, block 2 on tile
					else if (play.orientation == 1 && (
						play.placeX < 0 && level[play.placeX + i, play.placeY] != '.' ||
						play.placeX >= 0 && play.placeX + i < level.GetLength (0) &&
						play.placeY >= 0 && play.placeY < level.GetLength (1) &&
						level[play.placeX, play.placeY] == '.' && level[play.placeX + i, play.placeY] != '.') ||
					    play.orientation == 2 && (
						play.placeY + i >= level.GetLength (1) && level[play.placeX, play.placeY] != '.' ||
						play.placeX >= 0 && play.placeX < level.GetLength (0) &&
						play.placeY >= 0 && play.placeY + i < level.GetLength (1) &&
						level[play.placeX, play.placeY + i] == '.' && level[play.placeX, play.placeY] != '.')) {
						play.isFalling = 1;
					} // block 1 on tile, block 2 NOT on tile
					else if (play.orientation == 1 && (
						play.placeX + i >= level.GetLength (0) && level[play.placeX, play.placeY] != '.' ||
						play.placeX >= 0 && play.placeX + i < level.GetLength (0) &&
						play.placeY >= 0 && play.placeY < level.GetLength (1) &&
						level[play.placeX + i, play.placeY] == '.' && level[play.placeX, play.placeY] != '.') ||
					    play.orientation == 2 && (
						play.placeY < 0 && level[play.placeX, play.placeY + i] != '.' ||
						play.placeX >= 0 && play.placeX < level.GetLength (0) &&
						play.placeY >= 0 && play.placeY + i < level.GetLength (1) &&
						level[play.placeX, play.placeY] == '.' && level[play.placeX, play.placeY + i] != '.')) {
						play.isFalling = 2;
					} // block on map but not on tiles
					else if (play.orientation == 0 && level[play.placeX, play.placeY] == '.' ||
						play.orientation == 1 && level[play.placeX, play.placeY] == '.' && level[play.placeX + i, play.placeY] == '.' ||
					    play.orientation == 2 && level[play.placeX, play.placeY] == '.' && level[play.placeX, play.placeY + i] == '.') {
						play.isFalling = 0;
					}
				}

				// Reset if out of moves
				if (play.isFalling < 0 && moves <= 0) {
					ReloadLevel ();
				}
			}

			// Catch player after they fall off the edge
			if (play.isFalling != -1) {
				float fallTime = Time.time;
				if (fallTime - time > MAXFALLTIME)
					ReloadLevel ();
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
				tiles[play.placeX, play.placeY].transform.Translate(Vector3.up * Time.deltaTime * 1.5f, Space.World);
				tiles[play.placeX, play.placeY].transform.Rotate(50 * Time.deltaTime * Vector3.left);
				tiles[play.placeX, play.placeY].transform.Rotate(50 * Time.deltaTime * Vector3.back);
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
		player.GetComponent<PlayerController> ().touchStart = Vector2.zero;
	}

	public void VoidObjects() {
		// Void player
		player.transform.localPosition = new Vector3 (0, -50, 0);
		player.transform.localRotation = Quaternion.identity;

		// Void tiles
		for (int i = 0; i < poolDepth; i++) {
			tilePool[i].transform.localPosition = new Vector3(0, -50, 0);
			tilePool[i].transform.localRotation = Quaternion.identity;
		}
	}

	public void QuitGame() {
		isStarted = false;
		isPaused = false;
		isCompleted = false;

		// Destroy game objects
		VoidObjects ();

		if (transform.Find ("Pause Menu").gameObject.activeSelf)
			transform.Find ("Pause Menu").gameObject.SetActive (false);
		else if (transform.Find ("Win Menu").gameObject.activeSelf)
			transform.Find ("Win Menu").gameObject.SetActive (false);
		HUD.gameObject.SetActive (false);
		transform.Find ("Level Select").gameObject.SetActive (true);
		Background.texture = MenuBackground;
	}

	public void RetryLevel() {
		isStarted = false;
		isPaused = false;
		isCompleted = false;

		// Destroy game objects
		VoidObjects ();
		
		transform.Find ("Win Menu").gameObject.SetActive (false);

		LoadLevel ();
	}

	public void PreviousLevel() {
		if (levelNum > 1) {
			isStarted = false;
			isPaused = false;
			isCompleted = false;

			// Destroy game objects
			VoidObjects ();

			if (transform.Find ("Pause Menu").gameObject.activeSelf)
				transform.Find ("Pause Menu").gameObject.SetActive (false);
			else if (transform.Find ("Win Menu").gameObject.activeSelf)
				transform.Find ("Win Menu").gameObject.SetActive (false);

			levelNum--;
			LoadLevel ();
		}
	}

	public void NextLevel() {
		if (levelNum < LEVELNUM) {
			isStarted = false;
			isPaused = false;
			isCompleted = false;

			// Destroy game objects
			VoidObjects ();

			if (transform.Find ("Pause Menu").gameObject.activeSelf)
				transform.Find ("Pause Menu").gameObject.SetActive (false);
			else if (transform.Find ("Win Menu").gameObject.activeSelf)
				transform.Find ("Win Menu").gameObject.SetActive (false);
			
			levelNum++;
			LoadLevel ();
		}
	}
}
