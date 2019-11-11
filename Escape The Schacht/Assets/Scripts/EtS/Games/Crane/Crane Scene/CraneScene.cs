using EscapeRoomFramework;
using System;
using UnityEngine;

namespace EscapeTheSchacht.Crates {

    public class CraneScene : MonoBehaviour {

        internal CratesGame Game { get; set; }

        internal IEtsInterface Physical { get; set; }

        public CraneInput input;

        public Magnet magnet;
        public CraneMovement movement;

        public GameObject prefabEasy, prefabMedium, prefabHard;
        private GameObject sceneSetup;

        public float TotalMoveDistance { get; private set; }
        public int CrateCount { get; private set; }
        public int CratesInDropzone { get; internal set; }

        public event CraneDistanceMovedHandler OnCraneDistanceMoved;

        public void Initialize(GameDifficulty difficulty) {
            input.enabled = false;

            magnet.Initialize();
            movement.Initialize();
            input.Initialize();

            if (sceneSetup != null)
                Destroy(sceneSetup);

            switch (difficulty) {
            case GameDifficulty.Easy:
                sceneSetup = Instantiate(prefabEasy, transform);
                break;
            case GameDifficulty.Medium:
                sceneSetup = Instantiate(prefabMedium, transform);
                break;
            case GameDifficulty.Hard:
                sceneSetup = Instantiate(prefabHard, transform);
                break;
            }
            TotalMoveDistance = 0f;
            CratesInDropzone = 0;
            CrateCount = sceneSetup.GetComponentsInChildren<Crate>().Length;
        }

        void Update() {
            Vector3 totalVelocity = movement.UnscaledVelocity;
            if (totalVelocity != Vector3.zero) {
                TotalMoveDistance += totalVelocity.magnitude * Time.deltaTime;
                OnCraneDistanceMoved?.Invoke(Game, new CraneDistanceMovedEventArgs(totalVelocity * Time.deltaTime));
            }
        }

        public void StopCraneMovement() {
            // if the crane is currently deployed, the movement should not be interrupted
            if (movement.IsDeployed)
                return;

            // otherwise it should
            movement.UnscaledVelocity = Vector3.zero;
        }

    }

}