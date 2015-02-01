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
				focus = transform.position + new Vector3 (0f, -0.5f, 0.5f);
				axis = Vector3.right;
				isRotating = true;
				place.y -= 1;
			} else if (moveY < 0  && prevY >= 0) {
				focus = transform.position + new Vector3 (0f, -0.5f, -0.5f);
				axis = Vector3.left;
				isRotating = true;
				place.y += 1;
			} else if (moveX > 0  && prevX <= 0) {
				focus = transform.position + new Vector3 (0.5f, -0.5f, 0f);
				axis = Vector3.back;
				isRotating = true;
				place.x += 1;
			} else if (moveX < 0 && prevX >= 0) {
				focus = transform.position + new Vector3 (-0.5f, -0.5f, 0f);
				axis = Vector3.forward;
				isRotating = true;
				place.x -= 1;
			}
			isArrived = false;
		}

		prevX = moveX;
		prevY = moveY;
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
