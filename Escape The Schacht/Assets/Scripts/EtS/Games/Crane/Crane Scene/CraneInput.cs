using UnityEngine;

namespace EscapeTheSchacht.Crates {

    public class CraneInput : MonoBehaviour {

        public CraneMovement movement;

        private CraneScene scene;

        private Vector3[] directions = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right, Vector3.down };

        public bool MovementBlocked { get; internal set; }

        public event CraneEventHandler OnCraneMovementStarted;
        public event CraneEventHandler OnCraneMovementStopped;
        public event CraneEventHandler OnCraneDeploymentStarted;

        private void Awake() {
            scene = FindObjectOfType<CraneScene>();
        }

        public void Initialize() {
            directions.Shuffle();
        }

        void FixedUpdate() {
            if (movement.IsDeployed) // no input processing when magnet is deployed
                return;

            // remove any movement suppression when all buttons are released
            bool anyButtonDown = false;
            for (int i = 0; i < directions.Length && !anyButtonDown; i++)
                if (scene.Physical.IsCraneButtonDown(i))
                    anyButtonDown = true;
            if (!anyButtonDown)
                MovementBlocked = false;

            // apply crane velocity changes for all recently pressed and released buttons
            ProcessCraneButtons();
        }

        private void ProcessCraneButtons() {
            Vector3 direction = Vector3.zero;

            // sum all direction vectors resulting from pressed buttons
            for (int i = 0; i < directions.Length; i++)
                if (scene.Physical.IsCraneButtonDown(i))
                    direction += directions[i];

            // if y is -1, then Vector3.down is included, i.e. the button for deployment has been pressed
            if (direction.y < 0f) {
                movement.UnscaledVelocity = Vector3.down;
                OnCraneDeploymentStarted?.Invoke(scene.Game, new CraneEventArgs());
                return;
            }

            Debug.Assert(direction.y == 0f, "CraneInput.ProcessCaneButtons(): Crane movement y > 0");

            bool isMovingBefore = movement.IsMovingXZ; // for observer notification later on

            // when we have hit a wall, cancel any movement command
            if (MovementBlocked)
                direction = Vector3.zero;

            // apply the velocity
            movement.UnscaledVelocity = direction;

            // notify observers
            bool isMovingAfter = movement.IsMovingXZ;
            //Log.Debug("CraneInput: blocked == {0}, mv before == {3}, mv after = {4}, x vel == {1}, z vel == {2}", MovementBlocked, xMount.UnscaledVelocity, zMount.UnscaledVelocity, isMovingBefore, isMovingAfter);
            if (!isMovingBefore && isMovingAfter)
                OnCraneMovementStarted?.Invoke(scene.Game, new CraneEventArgs());
            if (isMovingBefore && !isMovingAfter)
                OnCraneMovementStopped?.Invoke(scene.Game, new CraneEventArgs());
        }

    }

}
