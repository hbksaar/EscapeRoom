using UnityEngine;
using System;

namespace EscapeTheSchacht.Crates {

    public class Magnet : MonoBehaviour {

        public CraneMovement movement; // for stopping or moving upwards after collision with walls, floor, crate or ceiling

        private CraneScene scene;

        private Crate carriedCrate;
        private bool processCrateCollision = true; // this is necessary because multiple collisions occur between crate and floor each time the crane moves down, so the crate gets attached and immediately detached again

        public event CraneEventHandler OnCraneDeploymentReturning;
        public event CraneEventHandler OnCraneDeploymentFinished;
        public event CrateEventHandler OnCratePickedUp;
        public event CrateEventHandler OnCrateDropped;

        private void Awake() {
            scene = FindObjectOfType<CraneScene>();
        }

        public void Initialize() {
            if (carriedCrate != null) {
                carriedCrate.transform.parent = null;
                Destroy(carriedCrate.gameObject);
                carriedCrate = null;
            }
        }

        private void OnTriggerEnter(Collider other) {
            // we need the deployment barrier because the rope is not long enough for the magnet to reach the ground
            //Log.Debug("Magnet.OnTriggerEnter: " + other.gameObject.name);
            DeploymentBarrier barrier = other.gameObject.GetComponent<DeploymentBarrier>();
            Dropzone dropzone = other.gameObject.GetComponent<Dropzone>();

            if (barrier != null && carriedCrate == null) {
                movement.UnscaledVelocity = Vector3.up;
                OnCraneDeploymentReturning?.Invoke(scene.Game, new CraneEventArgs());
            }

            else if (dropzone != null && carriedCrate != null) {
                carriedCrate.isInDropzone = true;
                scene.CratesInDropzone++;
            }
        }

        private void OnTriggerExit(Collider other) {
            //Log.Debug("Magnet.OnTriggerExit: " + other.gameObject.name);
            Dropzone dropzone = other.gameObject.GetComponent<Dropzone>();

            if (dropzone != null && carriedCrate != null) {
                carriedCrate.isInDropzone = false;
                scene.CratesInDropzone--;
            }
        }

        private void OnCollisionEnter(Collision collision) {
            Ceiling ceiling = collision.gameObject.GetComponent<Ceiling>();
            Floor floor = collision.gameObject.GetComponent<Floor>();
            Crate crate = collision.gameObject.GetComponent<Crate>();
           
			//Log.Debug("Magnet.OnCollisionEnter: {0} {1} {2} (processCrateCollision={3})", ceiling, floor, crate, processCrateCollision);

            if (ceiling != null)
                HandleCollision(ceiling);

            else if (floor != null && processCrateCollision) {
                HandleCollision(floor);
                processCrateCollision = false;
            }

            else if (crate != null && processCrateCollision) {
                HandleCollision(crate);
                processCrateCollision = false;
            }
        }

        private void HandleCollision(Crate crate) {
            // suppress collisions with the carried crate (this should never happen)
            if (crate == carriedCrate) {
                Log.Warn("Magnet: Collision with carried crate (this should not have happened and could mean that the program is broken)");
                return;
            }

            // if no other crate is carried by the crane, pick it up
            if (carriedCrate == null) {
                //Debug.Log("Magnet: Attaching crate: " + crate.name);

                // remove the crate's rigidbody to make it not react to physics on its own and forward collisions to the crane
                Destroy(crate.GetComponent<Rigidbody>());

                // attach the crate
                crate.transform.parent = this.transform;
                carriedCrate = crate;
                if (carriedCrate.isInDropzone) {
                    carriedCrate.isInDropzone = false;
                    scene.CratesInDropzone--;
                    OnCratePickedUp?.Invoke(scene.Game, new CrateEventArgs(crate.id, true));
                }
                else
                    OnCratePickedUp?.Invoke(scene.Game, new CrateEventArgs(crate.id, false));
            }

            // in any case: move the crane upwards
            movement.UnscaledVelocity = Vector3.up;
            OnCraneDeploymentReturning?.Invoke(scene.Game, new CraneEventArgs());
        }

        private void HandleCollision(Floor floor) {
            // if a crate is carried by the crane and the floor is a dropzone, drop the crate
            if (carriedCrate != null) {
                //Debug.Log("Crane: Detaching crate: " + carriedCrate);

                // add a rigidbody to the crate to make it react to physics
                carriedCrate.gameObject.AddComponent<Rigidbody>();

                // detach the crate
                Crate crate = carriedCrate;
                carriedCrate.transform.parent = carriedCrate.CrateHub;
                carriedCrate = null;
                OnCrateDropped?.Invoke(scene.Game, new CrateEventArgs(crate.id, crate.isInDropzone));
            }

            // in any case: move the crane upwards
            movement.UnscaledVelocity = Vector3.up;
            OnCraneDeploymentReturning?.Invoke(scene.Game, new CraneEventArgs());
        }

        private void HandleCollision(Ceiling ceiling) {
            //Log.Debug("handleCollision: ceiling");

            // stop (upwards) movement
            movement.UnscaledVelocity = Vector3.zero;

            // allow crate collision processing again when the crane reached its origin y position
            processCrateCollision = true;

            OnCraneDeploymentFinished?.Invoke(scene.Game, new CraneEventArgs());
        }

    }

}