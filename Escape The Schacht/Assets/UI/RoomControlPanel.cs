using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EscapeTheSchacht;
using UnityEngine.UI;
using System;
using EscapeRoomFramework;

namespace EscapeTheSchacht.UI {

    public class RoomControlPanel : MonoBehaviour {
        
        public static readonly Color TextUninitialized = new Color(.66f, .66f, .66f); // System.Drawing.Color.DarkGray
        public static readonly Color TextReady = Color.green; // new Color(0, .5f, 0); // Green
        public static readonly Color TextRunning = new Color(0, 0, 1f); // Blue
        public static readonly Color TextAborted = new Color(1, .55f, 0); // DarkOrange
        public static readonly Color TextCompleted = new Color(.2f, .2f, .2f); // default text color in Unity UI elements
        public static readonly Color TextError = new Color(1, 0, 0); // Red
        
        public MaintenanceMode maintenanceMode;

        private Text lblRoomState;
        private Button btnStart, btnComplete, btnAbort, btnReset, btnShutdown;
        private Toggle chkLighting, chkMaintenance;

        private EscapeRoom<IEtsInterface> room;

        // Use this for initialization
        void Awake() {
            btnStart = transform.Find("btnStart").GetComponent<Button>();
            btnComplete = transform.Find("btnComplete").GetComponent<Button>();
            btnAbort = transform.Find("btnAbort").GetComponent<Button>();
            btnReset = transform.Find("btnReset").GetComponent<Button>();
            btnShutdown = transform.Find("btnShutdown").GetComponent<Button>();

            lblRoomState = transform.Find("pnlRoomSettings").Find("lblRoomState").GetComponent<Text>();
            chkLighting = transform.Find("pnlRoomSettings").Find("chkLighting").GetComponent<Toggle>();
            chkMaintenance = transform.Find("pnlRoomSettings").Find("chkMaintenance").GetComponent<Toggle>();

            room = Ets.Room;
            room.OnRoomStateChanged += OnRoomStateChanged;
        }

        public void OnRoomStateChanged(EscapeRoom<IEtsInterface> sender, RoomStateChangedEventArgs e) {
            lblRoomState.text = e.NewState.ToString();
            lblRoomState.color = ColorCode(room.State);
            btnStart.interactable = room.State.CanTransition(RoomState.Running);
            btnComplete.interactable = room.State.CanTransition(RoomState.Completed);
            btnAbort.interactable = room.State.CanTransition(RoomState.Aborted);
            btnReset.interactable = room.State.CanTransition(RoomState.Initialized);
            chkMaintenance.isOn = maintenanceMode.enabled;
            chkLighting.isOn = room.Physical.OverrideLighting;
        }

        private void DisableButtons() {
            btnReset.interactable = btnStart.interactable = btnAbort.interactable = false;
        }

        private Color ColorCode(RoomState state) {
            switch (state) {
            case RoomState.Uninitialized:
                return TextUninitialized;
            case RoomState.Initialized:
                return TextReady;
            case RoomState.Running:
                return TextRunning;
            case RoomState.Aborted:
                return TextAborted;
            case RoomState.Completed:
                return TextCompleted;
            case RoomState.Error:
                return TextError;
            default:
                throw new NotImplementedException();
            }
        }

        public void OnButtonClick(Button sender) {
            //disableButtons();

            if (sender == btnStart)
                room.Start();
            else if (sender == btnComplete)
                room.Complete();
            else if (sender == btnAbort)
                room.Abort();
            else if (sender == btnReset)
                room.Initialize();
            else if (sender == btnShutdown)
                Application.Quit();
        }

        public void OnToggleClick(Toggle sender) {
            if (sender == chkLighting)
                room.Physical.OverrideLighting = chkLighting.isOn;
            else if (sender == chkMaintenance)
                maintenanceMode.enabled = chkMaintenance.isOn;
        }

    }

}