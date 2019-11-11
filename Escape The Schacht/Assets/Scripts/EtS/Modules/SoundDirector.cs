using EscapeTheSchacht.Crates;
using EscapeTheSchacht.Dynamite;
using EscapeTheSchacht.Pipes;
using EscapeTheSchacht.Trigger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EscapeRoomFramework;
using UnityEngine;
using System.Collections;

namespace EscapeTheSchacht {

    public class SoundDirector : MbModule<IEtsInterface> {

        public static readonly int ConfirmationSound = 96;
        public static readonly int ErrorSound = 97;

        private static readonly float TurningValveVolume = .5f;

        private PipesGame pipes;
        private static readonly float DrillVolumeStep = .10f;
        private float drillVolume;
        private bool[] solvedPipeSystems;

        public new AudioSystem audio;

        protected override void Setup() {
            pipes = Room.GetGame<PipesGame>();
            pipes.OnValveTurned += OnValveTurned;
            pipes.OnFanTriggered += OnFanTriggered;

            CratesGame crates = Room.GetGame<CratesGame>();
            crates.OnCraneDeploymentFinished += OnCraneDeploymentFinished;
            crates.OnCraneDeploymentStarted += OnCraneDeploymentStarted;
            crates.OnCraneMovementStarted += OnCraneMovementStarted;
            crates.OnCraneMovementStopped += OnCraneMovementStopped;
            crates.OnCrateDropped += OnCrateDropped;
            crates.OnCratePickedUp += OnCratePickedUp;

            DynamiteGame dynamite = Room.GetGame<DynamiteGame>();
            dynamite.OnStickInserted += OnStickInserted;
            dynamite.OnStickRemoved += OnStickRemoved;

            TriggersGame trigger = Room.GetGame<TriggersGame>();
            trigger.OnButtonPressed += OnButtonPressed;
        }

        public override void OnRoomStateChanged(EscapeRoom<IEtsInterface> sender, RoomStateChangedEventArgs e) {
            if (e.NewState == RoomState.Initialized)
                audio.PlaySound(ConfirmationSound);

            if (e.NewState == RoomState.Error)
                audio.PlaySound(ErrorSound);
        }

        public override void OnGameStateChanged(Game<IEtsInterface> sender, GameStateChangedEventArgs e) {
            if (e.NewState == GameState.Error) {
                audio.StopAllSounds();
                audio.StopAllVoice();
                audio.PlaySound(ErrorSound);
            }

            if (sender is DynamiteGame && e.NewState == GameState.Completed)
                audio.PlaySound(33);

            if (sender is PipesGame) {
                switch (e.NewState) {
                case GameState.Initialized:
                    int pipeSystemCount = 0;
                    foreach (PipeSystem ps in pipes.PipeSystems())
                        pipeSystemCount++;
                    solvedPipeSystems = new bool[pipeSystemCount];
                    drillVolume = 0f;
                    break;

                case GameState.Running:
                    drillVolume = DrillVolumeStep;
                    audio.PlaySound(02, true, drillVolume);
                    break;

                case GameState.Completed:
                case GameState.Aborted:
                    audio.StopSound(11);
                    audio.StopSound(02);
                    break;

                case GameState.Uninitialized:
                case GameState.Error:
                    break;

                default:
                    throw new NotImplementedException("Unhandled game state: " + e.NewState);
                }
            }
        }

        #region Pipes
        public void OnValveTurned(PipesGame sender, ValveTurnedEventArgs e) {
            audio.PlaySound(13);
        }

        public void OnFanTriggered(PipesGame sender, FanTriggeredEventArgs e) {
            // if the system with this fan is solved for the first time...
            if (e.System.RunningFansCount == e.System.FanCount && !solvedPipeSystems[e.System.Index]) {
                solvedPipeSystems[e.System.Index] = true;

                // only make the drill sound louder if the game is not solved
                if (solvedPipeSystems.Contains(false)) {
                    drillVolume += DrillVolumeStep;
                    audio.ChangeSoundVolume(02, drillVolume);
                }
            }
        }

        public override void UbmUpdate(float deltaTime) {
            // valve rotation sound for Pipes
            if (pipes.State == GameState.Running) {

                bool playTurningSound = false;
                foreach (Valve valve in pipes.Valves()) {
                    PipeSystem pipeSystem = valve.PipeSystem;
                    if (pipeSystem.RunningFansCount >= pipeSystem.FanCount)
                        continue;
                    if (Ets.Room.Physical.IsValveRotating(valve.Row, valve.PositionInRow)) {
                        playTurningSound = true;
                        break;
                    }
                }

                bool playing = audio.IsSoundPlaying(11);
                if (playTurningSound && !playing) {
                    audio.PlaySound(11, true, TurningValveVolume);
                    //print ("activate valve turning sound");
                }
                else if (!playTurningSound && playing) {
                    audio.StopSound(11);
                    //print ("deactivate valve turning sound");
                }
            }
        }
        #endregion Pipes

        #region Crane
        private bool craneMoving = false;

        public void OnCraneMovementStarted(CratesGame sender, CraneEventArgs e) {
            if (sender.State == GameState.Running) {
                craneMoving = true;
                StartCoroutine(craneMovementStart());
            }
        }

        private IEnumerator craneMovementStart() {
            audio.PlaySound(21);
            yield return new WaitForSeconds(audio.GetSoundLength(21) - .05f);
            if (craneMoving)
                audio.PlaySound(22, true);
        }

        public void OnCraneMovementStopped(CratesGame sender, CraneEventArgs e) {
            if (sender.State == GameState.Running || sender.State == GameState.Completed || sender.State == GameState.Aborted) {
                audio.StopSound(21);
                audio.StopSound(22);
                audio.PlaySound(23);
                craneMoving = false;
            }
        }

        public void OnCraneDeploymentStarted(CratesGame sender, CraneEventArgs e) {
            if (sender.State == GameState.Running)
                OnCraneMovementStarted(sender, e);
        }

        public void OnCraneDeploymentFinished(CratesGame sender, CraneEventArgs e) {
            if (sender.State == GameState.Running || sender.State == GameState.Completed || sender.State == GameState.Aborted)
                OnCraneMovementStopped(sender, e);
        }

        public void OnCratePickedUp(CratesGame sender, CrateEventArgs e) {
            if (sender.State == GameState.Running)
                audio.PlaySound(24);
        }

        public void OnCrateDropped(CratesGame sender, CrateEventArgs e) {
            if (sender.State == GameState.Running || sender.State == GameState.Completed || sender.State == GameState.Aborted)
                audio.PlaySound(24);
        }
        #endregion Crane

        #region Dynamite
        public void OnStickInserted(DynamiteGame sender, StickEventArgs e) {
            audio.PlaySound(31);
        }

        public void OnStickRemoved(DynamiteGame sender, StickEventArgs e) {
            audio.PlaySound(32);
        }
        #endregion Dynamite

        #region Trigger
        private void OnButtonPressed(TriggersGame sender, ButtonPressedEventArgs e) {
            if (sender.State == GameState.Running) {
                if (e.Correct)
                    audio.PlaySound(42);
                else
                    audio.PlaySound(41);
            }

            if (sender.State == GameState.Completed) {
                StartCoroutine(triggerWon());
            }
        }
        private IEnumerator triggerWon() {
            audio.PlaySound(44);
            yield return new WaitForSeconds(3.736f);
            audio.PlaySound(45);
        }
        #endregion Trigger

    }

}
