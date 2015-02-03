using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour {

	// Rotation information
	public float speed;
	private float rotation = 0;
	public bool isRotating = false;
	public bool isArrived = false;
	private Vector3 focus;
	private Vector3 axis;

	// Position on the board
	public Vector2 place;
	public bool isFalling = false;
	public float fall;
	public int orientation;
	public int type;

	// Previous game states
	private float prevX = 0;
	private float prevY = 0;

	// Use this for initialization
	void Start () {

	}

	void Update () {
	}
	
	// Update is called once per frame
	public void PlayerUpdate () {
		float moveX = Input.GetAxis ("Horizontal");
		float moveY = Input.GetAxis ("Vertical");

		if (isRotating) {
			Rotate ();
		} else if (isFalling) {
			fall += 0.8f;
			transform.Translate(new Vector3(0, -fall * Time.deltaTime, 0), Space.World);
			isArrived = false;
		} else {
			if (moveY > 0 && prevY <= 0) {
				Move (0);
			} else if (moveY < 0  && prevY >= 0) {
				Move (1);
			} else if (moveX > 0  && prevX <= 0) {
				Move (2);
			} else if (moveX < 0 && prevX >= 0) {
				Move (3);
			}
			isArrived = false;
		}

		prevX = moveX;
		prevY = moveY;
	}

	void Move(int direction) {
		float focusX = 0.5f;
		float focusY = 0.5f;
		float focusZ = 0.5f;

		// Long block
		if (type == 2) {
			// Straight up
			if (orientation == 0)
				focusY = 1.0f;
			// Along X
			else if (orientation == 1)
				focusX = 1.0f;
			// Along Z
			else if (orientation == 2)
				focusZ = 1.0f;
		}
		if (direction == 0) {
			focus = transform.position + new Vector3 (0f, -focusY, focusZ);
			axis = Vector3.right;
			if (type == 1)
				place.y -= 1;
			else if (type == 2) {
				if (orientation == 0) {
					place.y -= 1;
					orientation = 2;
				} else if (orientation == 1) {
					place.y -= 1;
				} else if (orientation == 2) {
					place.y -= 2;
					orientation = 0;
				}
			}
		}
		else if (direction == 1) {
			focus = transform.position + new Vector3 (0f, -focusY, -focusZ);
			axis = Vector3.left;
			if (type == 1)
				place.y += 1;
			else if (type == 2) {
				if (orientation == 0) {
					place.y += 2;
					orientation = 2;
				} else if (orientation == 1) {
					place.y += 1;
				} else if (orientation == 2) {
					place.y += 1;
					orientation = 0;
				}
			}
		}
		else if (direction == 2) {
			focus = transform.position + new Vector3 (focusX, -focusY, 0f);
			axis = Vector3.back;
			if (type == 1)
				place.x += 1;
			else if (type == 2) {
				if (orientation == 0) {
					place.x += 1;
					orientation = 1;
				} else if (orientation == 1) {
					place.x += 2;
					orientation = 0;
				} else if (orientation == 2) {
					place.x += 1;
				}
			}
		}
		else {
			focus = transform.position + new Vector3 (-focusX, -focusY, 0f);
			axis = Vector3.forward;
			if (type == 1)
				place.x -= 1;
			else if (type == 2) {
				if (orientation == 0) {
					place.x -= 2;
					orientation = 1;
				} else if (orientation == 1) {
					place.x -= 1;
					orientation = 0;
				} else if (orientation == 2) {
					place.x -= 1;
				}
			}
		}
		isRotating = true;
	}

	void Rotate() {
		float theta = speed * Time.deltaTime;
		rotation += theta;
		if (rotation >= 90) {
			theta -= rotation - 90f;
			rotation = 0;
			isRotating = false;
			isArrived = true;
		}
		transform.RotateAround (focus, axis, theta);
	}
}
