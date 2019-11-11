using EscapeRoomFramework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EscapeTheSchacht.Crates {

    public delegate void CraneDistanceMovedHandler(CratesGame sender, CraneDistanceMovedEventArgs args);
    public delegate void CraneEventHandler(CratesGame sender, CraneEventArgs args);
    public delegate void CrateEventHandler(CratesGame sender, CrateEventArgs args);
    
    public class CraneEventArgs : EventArgs {
        // no parameters

        internal CraneEventArgs() { }
    }

    public class CraneDistanceMovedEventArgs : EventArgs {
        /// <summary>
        /// The vector the crane moved in the last frame.
        /// </summary>
        public Vector3 Vector;
        /// <summary>
        /// The distance the crane moved in the last frame.
        /// </summary>
        public float Distance;

        internal CraneDistanceMovedEventArgs(Vector3 vector) {
            Vector = vector;
            Distance = vector.magnitude;
        }
    }

    public class CrateEventArgs : EventArgs {
        /// <summary>
        /// The crate that is affected.
        /// </summary>
        public int CrateId { get; }
        /// <summary>
        /// True iff the crate is (picked up from or dropped) in the dropzone.
        /// </summary>
        public bool InDropzone { get; }

        internal CrateEventArgs(int crateId, bool inDropzone) {
            CrateId = crateId;
            InDropzone = inDropzone;
        }
    }

    public class CratesGame : Game<IEtsInterface> {

        public const string GameId = "Crane";
        public static readonly int ButtonCount = 5;

        public override string Id => GameId;

        public float TotalMoveDistance { get { return CraneScene.TotalMoveDistance; } }

        public int CrateCount { get { return CraneScene.CrateCount; } }

        public int CratesInDropzone { get { return CraneScene.CratesInDropzone; } }

        internal CraneScene CraneScene { get; private set; }

        public event CraneDistanceMovedHandler OnCraneDistanceMoved {
            add { CraneScene.OnCraneDistanceMoved += value; }
            remove { CraneScene.OnCraneDistanceMoved -= value; }
        }
        public event CraneEventHandler OnCraneMovementStarted {
            add { CraneScene.input.OnCraneMovementStarted += value; }
            remove { CraneScene.input.OnCraneMovementStarted -= value; }
        }
        public event CraneEventHandler OnCraneMovementStopped {
            add { CraneScene.input.OnCraneMovementStopped += value; }
            remove { CraneScene.input.OnCraneMovementStopped -= value; }
        }
        public event CraneEventHandler OnCraneDeploymentStarted {
            add { CraneScene.input.OnCraneDeploymentStarted += value; }
            remove { CraneScene.input.OnCraneDeploymentStarted -= value; }
        }
        public event CraneEventHandler OnCraneDeploymentReturning {
            add { CraneScene.magnet.OnCraneDeploymentReturning += value; }
            remove { CraneScene.magnet.OnCraneDeploymentReturning -= value; }
        }
        public event CraneEventHandler OnCraneDeploymentFinished {
            add { CraneScene.magnet.OnCraneDeploymentFinished += value; }
            remove { CraneScene.magnet.OnCraneDeploymentFinished -= value; }
        }
        public event CrateEventHandler OnCratePickedUp {
            add { CraneScene.magnet.OnCratePickedUp += value; }
            remove { CraneScene.magnet.OnCratePickedUp -= value; }
        }
        public event CrateEventHandler OnCrateDropped {
            add { CraneScene.magnet.OnCrateDropped += value; }
            remove { CraneScene.magnet.OnCrateDropped -= value; }
        }

        public CratesGame(CraneScene scene) {
            CraneScene = scene;
            scene.Game = this;

            Log.Info("Crane game set up.");
        }

        public override bool CompensateTechnicalFailures(DiagnosticsReport report, List<string> affectedComponents) {
            return false;
        }

        protected override bool Initialize0(GameDifficulty difficulty, DiagnosticsReport diagnosticsReport) {
            CraneScene.Physical = Physical;
            CraneScene.Initialize(difficulty);
            return true;
        }

        protected override void Start0() {
            CraneScene.input.enabled = true;
        }

        protected override void Abort0() {
            CraneScene.input.enabled = false;
            CraneScene.StopCraneMovement();
        }

        protected override GameState Update0(float deltaTime) {
            if (CraneScene.CratesInDropzone == CraneScene.CrateCount) {
                CraneScene.StopCraneMovement();
                CraneScene.input.enabled = false;
                return GameState.Completed;
            }

            return GameState.Running;
        }

        protected override void Complete0() {
            CraneScene.input.enabled = false;
        }

    }

}