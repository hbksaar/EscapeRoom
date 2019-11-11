using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HideCursor : MonoBehaviour {

    public float timeout = 5f;
    private float countdown = 0f;

    private Vector3 lastMousePos;

	void Start () {
        countdown = timeout;
        lastMousePos = Input.mousePosition;
	}
	
	void Update () {
        bool mouseMoved = (Input.mousePosition - lastMousePos) != Vector3.zero;
        lastMousePos = Input.mousePosition;
        if (mouseMoved) {
            Cursor.visible = true;
            countdown = timeout;
        }
        else {
            countdown -= Time.deltaTime;
            if (countdown <= 0f)
                Cursor.visible = false;
        }
	}

}
