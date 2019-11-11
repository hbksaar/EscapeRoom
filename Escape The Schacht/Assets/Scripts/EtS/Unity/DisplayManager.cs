using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EscapeTheSchacht.Crates;
using EscapeRoomFramework;

namespace EscapeTheSchacht {

	public class DisplayManager : MonoBehaviour {

	    public int displayCount = 4;

        public Camera[] craneCameras;


        private void Awake() {
            Ets.Room.OnRoomStateChanged += OnRoomStateChanged;
            Ets.Room.GetGame<CratesGame>().OnGameStateChanged += OnGameStateChanged;

            if (Display.displays.Length < displayCount)
                Log.Warn("CraneScene: Crane game needs {1} displays (only {0} available)", Display.displays.Length, displayCount);
            for (int i = 1; i < displayCount && i < Display.displays.Length; i++)
                Display.displays[i].Activate();

            //OnRoomStateChanged(room, room.State, room.State);
        }

		public void OnRoomStateChanged(EscapeRoom<IEtsInterface> sender, RoomStateChangedEventArgs e) {
			if (e.NewState == RoomState.Completed || e.NewState == RoomState.Initialized)
				foreach (Camera cam in craneCameras)
					;
                   // cam.enabled = false;
        }

        public void OnGameStateChanged(Game<IEtsInterface> sender, GameStateChangedEventArgs e) {
            if (e.NewState == GameState.Initialized)
                foreach (Camera cam in craneCameras)
                    cam.enabled = true;
        }

    }

}