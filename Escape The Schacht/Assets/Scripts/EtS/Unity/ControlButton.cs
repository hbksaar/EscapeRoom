using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EscapeTheSchacht;

using EscapeTheSchacht.Trigger;
using EscapeTheSchacht.Dynamite;
using EscapeTheSchacht.Crates;
using EscapeTheSchacht.Pipes;
using EscapeRoomFramework;

namespace EscapeTheSchacht {

    public class ControlButton : MonoBehaviour {

		private readonly float SignalSoundVolume = .5f;
        private readonly int SoundStart = 91;
        private readonly int SoundReset = 92;
        private readonly int SoundLighting = 93;
        private readonly int SoundMaintenance = 94;
        private readonly int SoundCancel = 95;
        private readonly int SoundShutdown = 98;

        public new AudioSystem audio;
        public LightingDirector lighting;
        public MaintenanceMode maintenance;

        public readonly float delayStart = 2.5f;
        public readonly float delayReset = 5f;
        public readonly float delayLighting = 7.5f;
        public readonly float delayMaintenance = 10f;
        public readonly float delayShutdown = 15f;
        public readonly float delayCancel = 17.5f;

        private float downTime = 0f;

        public int secondsToShutdown = 10;

        private EscapeRoom<IEtsInterface> room;

        private void Awake() {
            room = Ets.Room;
        }

        void Update() {
            bool buttonDown = room.Physical.IsControlButtonDown();

            if (buttonDown) {
                float newDownTime = downTime + Time.deltaTime;
                if (downTime < delayStart && newDownTime >= delayStart)
					audio.PlaySound(SoundStart, false, SignalSoundVolume);
                if (downTime < delayReset && newDownTime >= delayReset)
					audio.PlaySound(SoundReset, false, SignalSoundVolume);
                if (downTime < delayLighting && newDownTime >= delayLighting)
					audio.PlaySound(SoundLighting, false, SignalSoundVolume);
                if (downTime < delayMaintenance && newDownTime >= delayMaintenance)
					audio.PlaySound(SoundMaintenance, false, SignalSoundVolume);
                if (downTime < delayShutdown && newDownTime >= delayShutdown)
                    audio.PlaySound(SoundShutdown, false, SignalSoundVolume);
                if (downTime < delayCancel && newDownTime >= delayCancel) {
					audio.PlaySound(SoundCancel, false, SignalSoundVolume);
                    newDownTime = 0;
                }
                downTime = newDownTime;
                return;
            }

            Debug.Assert(!buttonDown, "ControlButton.Update: button is pressed, this should not execute");

            if (downTime >= delayShutdown) {
                System.Diagnostics.Process shutdown = new System.Diagnostics.Process();
                shutdown.StartInfo.FileName = "shutdown";
                shutdown.StartInfo.Arguments = "/s /f /t " + secondsToShutdown;
                shutdown.Start();

                Application.Quit();
            }

            if (downTime >= delayMaintenance) {
                if (room.State == RoomState.Running)
                    audio.PlaySound(SoundDirector.ErrorSound, false, SignalSoundVolume);
                else
                    maintenance.enabled = true;
            }

            else if (downTime >= delayLighting) {
                if (!maintenance.enabled)
                    room.Physical.OverrideLighting = !room.Physical.OverrideLighting;
            }

            else if (downTime >= delayReset) {
                maintenance.enabled = false;
                resetRoom();
            }

            else if (downTime >= delayStart) {
                if (room.State.CanTransition(RoomState.Running))
                    StartCoroutine(startRoom());
                else
                    audio.PlaySound(SoundDirector.ErrorSound, false, SignalSoundVolume);
            }

            downTime = 0;
        }

		private IEnumerator startRoom() {
			if (room.State == RoomState.Running) {
				yield break;
			}
			
            if (!room.State.CanTransition(RoomState.Running)) {
                audio.PlaySound(SoundDirector.ErrorSound);
                yield break;
            }

            enabled = false;
            yield return new WaitForSeconds(2f);
            enabled = true;

            room.Start();

            if (room.State == RoomState.Error) {
                yield break;
            }
        }

        private void resetRoom() {
            if (room.State.CanTransition(RoomState.Aborted))
                room.Abort();

            if (!room.State.CanTransition(RoomState.Initialized)) {
                audio.PlaySound(SoundDirector.ErrorSound);
                return;
            }

            room.Physical.OverrideLighting = false;
            room.Initialize();

            if (room.State == RoomState.Error) {
                audio.PlaySound(SoundDirector.ErrorSound);
                return;
            }
        }

    }

}