using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CraneMovement : MonoBehaviour {

    public Vector3 moveSpeed;
    public Vector3 clampMin, clampMax;

    public Transform[] moveAlongZ, moveAlongXZ, moveAlongXYZ;

    private Dictionary<Transform, Vector3> posOffset = new Dictionary<Transform, Vector3>();

    private Vector3 origin;
    private Rigidbody rb;

    private Vector3 unscaledVelocity;
    public Vector3 UnscaledVelocity {
        get { return unscaledVelocity; }
        set {
            unscaledVelocity = value;
            Vector3 scaledVelocity = new Vector3(
                unscaledVelocity.x * moveSpeed.x,
                unscaledVelocity.y * moveSpeed.y,
                unscaledVelocity.z * moveSpeed.z
            );
            rb.velocity = scaledVelocity;
        }
    }

    public bool IsMovingXZ => unscaledVelocity.x != 0f || unscaledVelocity.z != 0f;
    public bool IsDeployed => unscaledVelocity.y != 0f;

    void OnValidate() {
        if (moveSpeed.x == 0f)
            moveSpeed.x = 1f;
        if (moveSpeed.y == 0f)
            moveSpeed.y = 1f;
        if (moveSpeed.z == 0f)
            moveSpeed.z = 1f;
    }

    public void Initialize() {
        rb.position = origin;
        unscaledVelocity = Vector3.zero;
    }

    void Start() {
        rb = GetComponent<Rigidbody>();
        origin = rb.position;

        calculateOffsetPositions(moveAlongZ, posOffset);
        calculateOffsetPositions(moveAlongXZ, posOffset);
        calculateOffsetPositions(moveAlongXYZ, posOffset);
    }

    private void calculateOffsetPositions(Transform[] transforms, Dictionary<Transform, Vector3> addTo) {
        foreach (Transform t in transforms)
            if (!posOffset.ContainsKey(t))
                posOffset[t] = t.position - rb.position;
    }

    private void FixedUpdate() {
        if (rb == null)
            return;

        rb.MovePosition(new Vector3(
            Mathf.Clamp(rb.position.x, clampMin.x, clampMax.x),
            Mathf.Clamp(rb.position.y, clampMin.y, clampMax.y),
            Mathf.Clamp(rb.position.z, clampMin.z, clampMax.z)
        ));

        //print(rb.position + " / " + transform.position + " / " + transform.localPosition);

        foreach (Transform t in moveAlongZ)
            t.position = new Vector3(origin.x, origin.y, rb.position.z) + posOffset[t];
        foreach (Transform t in moveAlongXZ)
            t.position = new Vector3(rb.position.x, origin.y, rb.position.z) + posOffset[t];
        foreach (Transform t in moveAlongXYZ)
            t.position = new Vector3(rb.position.x, rb.position.y, rb.position.z) + posOffset[t];
    }

}
