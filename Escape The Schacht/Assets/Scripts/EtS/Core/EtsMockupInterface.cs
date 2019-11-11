using EscapeTheSchacht;
using System;
using UnityEngine;
using EscapeTheSchacht.Dynamite;
using EscapeTheSchacht.Pipes;
using EscapeTheSchacht.Trigger;
using EscapeRoomFramework;
using EscapeTheSchacht.Crates;
using System.Text;

namespace EscapeTheSchacht {

    public class EtsMockupInterface : MonoBehaviour, IEtsInterface {

        public bool simulateTechnicalFailures;

		public bool OverrideLighting { // ignored
            get { return false; }
            set { Log.Verbose("EtsMockupInterface: OverrideLighting set to " + value); }
        }

        // room
        public bool IsOpen { get; private set; }
        private bool[] lights;
        private bool controlButtonNowPressed;
        private bool controlButtonBeenPressed;

        // pipes
        private bool[][] fanStates; // row, position
        private bool[][] valveStates; // row, position

        // crane
        private bool[] craneButtonsNowPressed = new bool[5]; // up, down, left, right, magnet
        private bool[] craneButtonsBeenPressed = new bool[5];

        // dynamite
        private int[] holeStates;
        private int selectedStick = -1;
        private int selectedHole = -1;

        // triggers
        private bool[] sequenceButtonsBeenPressed;
        private Color[] triggerLEDs; // unused

        void Awake() {
            lights = new bool[EtsInterface.LightsCount];

            fanStates = new bool[PipesGame.GridHeight][];
            valveStates = new bool[PipesGame.GridHeight][];
            for (int i = 0; i < PipesGame.GridHeight; i++) {
                fanStates[i] = new bool[PipesGame.GridWidth];
                valveStates[i] = new bool[PipesGame.GridWidth];
            }

            holeStates = new int[DynamiteGame.HoleCount];
            for (int i = 0; i < holeStates.Length; i++)
                holeStates[i] = -1;

            sequenceButtonsBeenPressed = new bool[TriggersGame.ButtonCount];
            triggerLEDs = new Color[TriggersGame.ButtonCount];
            for (int i = 0; i < triggerLEDs.Length; i++)
                triggerLEDs[i] = Color.black;
        }

        public void Open() {
            IsOpen = true;
        }

        public void RunDiagnostics(DiagnosticsReport report) {
            if (simulateTechnicalFailures) {
                report.AddAffectedRoomComponent("everything", true);
                report.AddAffectedGameComponent(Ets.Room.GetGame<PipesGame>(), "valves");
                report.AddAffectedGameComponent(Ets.Room.GetGame<CratesGame>(), "buttons");
                report.AddAffectedGameComponent(Ets.Room.GetGame<CratesGame>(), "screens");
                report.AddAffectedGameComponent(Ets.Room.GetGame<DynamiteGame>(), "hole_0_1");
                report.AddAffectedGameComponent(Ets.Room.GetGame<DynamiteGame>(), "hole_2_1");
                report.AddAffectedGameComponent(Ets.Room.GetGame<TriggersGame>(), "trigger_0");
                report.AddAffectedGameComponent(Ets.Room.GetGame<TriggersGame>(), "trigger_4");
            }
        }

        public void Close() {
            IsOpen = false;
        }

        public void Dispose() {
            IsOpen = false;
        }

        public void PhysicalUpdate(float deltaTime) {
            // room
            controlButtonBeenPressed = Input.GetKeyDown(KeyCode.Tab) ? true : false;
            controlButtonNowPressed = Input.GetKey(KeyCode.Tab);

            // pipes
            // update via ToggleValve

            // crane
            craneButtonsBeenPressed[0] = Input.GetKeyDown(KeyCode.UpArrow) ? true : false;
            craneButtonsBeenPressed[1] = Input.GetKeyDown(KeyCode.DownArrow) ? true : false;
            craneButtonsBeenPressed[2] = Input.GetKeyDown(KeyCode.LeftArrow) ? true : false;
            craneButtonsBeenPressed[3] = Input.GetKeyDown(KeyCode.RightArrow) ? true : false;
            craneButtonsBeenPressed[4] = Input.GetKeyDown(KeyCode.Return) ? true : false;
            craneButtonsNowPressed[0] = Input.GetKey(KeyCode.UpArrow);
            craneButtonsNowPressed[1] = Input.GetKey(KeyCode.DownArrow);
            craneButtonsNowPressed[2] = Input.GetKey(KeyCode.LeftArrow);
            craneButtonsNowPressed[3] = Input.GetKey(KeyCode.RightArrow);
            craneButtonsNowPressed[4] = Input.GetKey(KeyCode.Return);

            // dynamite
            selectedStick = CheckInputInRange(KeyCode.A, KeyCode.H, selectedStick);
            if (selectedStick != -1) {
                int holeIndex = Array.IndexOf(holeStates, selectedStick);
                if (holeIndex != -1) { // if stick inside a hole: remove it and done
                    holeStates[holeIndex] = -1;
                    selectedStick = -1;
                }
                else { // if stick not in hole: wait for hole selection
                    selectedHole = CheckInputInRange(KeyCode.F1, KeyCode.F12, selectedHole);
                    if (selectedHole != -1 && holeStates[selectedHole] == -1) { // if empty hole selected: put stick in hole
                        holeStates[selectedHole] = selectedStick;
                        selectedStick = selectedHole = -1;
                    }
                }
            }

            //StringBuilder sb = new StringBuilder("Mockup: trigger buttons = ");
            // triggers
            for (int i = 0; i < sequenceButtonsBeenPressed.Length; i++) {
                int kc = (int) KeyCode.Keypad0 + i;
                int kcA = (int) KeyCode.Alpha0 + i;
                sequenceButtonsBeenPressed[i] = Input.GetKeyDown((KeyCode) kc) || Input.GetKeyDown((KeyCode) kcA);
                //sb.Append(sequenceButtonsBeenPressed[i] + " ");
            }
            //print(sb);
        }

        private int CheckInputInRange(KeyCode kcFirst, KeyCode kcLast, int defaultValue) {
            int first = (int) kcFirst;
            int last = (int) kcLast;
            for (int kc = first; kc <= last; kc++)
                if (Input.GetKeyDown((KeyCode) kc))
                    return kc - first;
            return defaultValue;
        }

        public void ForcePipesUpdate() { }
        public void ForceCraneUpdate() { }
        public void ForceDynamiteUpdate() { }
        public void ForceTriggerUpdate() { }

        #region Room
        public bool GetLightState(Light light) {
            return lights[(int) light];
        }

        public void SetLightState(Light light, bool on) {
            lights[(int) light] = on;
            Log.Debug("PhysicalInterfaceMockup: Switched light {0} {1}", light, on ? "on" : "off");
        }

        public bool[] GetLightStates() {
            return lights;
        }

        public void SetLightStates(bool[] states) {
            for (int i = 0; i < states.Length; i++)
                lights[i] = states[i];
        }

        public void ChangeLightStates(Light[] lights, LightSetting[] change) {
            Debug.Assert(lights.Length == change.Length, "Given array arguments differ in length.");
            for (int i = 0; i < lights.Length; i++)
                if (change[i] != LightSetting.None)
                    this.lights[i] = change[i] == LightSetting.On;
        }

        public bool WasControlButtonPressed() {
            return controlButtonBeenPressed;
        }

        public bool IsControlButtonDown() {
            return controlButtonNowPressed;
        }
        #endregion Room

        #region Pipes
        public void ToggleValve(int row, int position) {
            valveStates[row][position] = !valveStates[row][position];
        }

        public bool GetValveState(int row, int position) {
            return valveStates[row][position];
        }

        public bool IsValveBroken(int row, int position) {
            return false; // no mockup implementation
        }

        public bool IsValveRotating(int row, int position) {
            return false; // no mockup implementation
        }

        public bool IsAnyValveRotating() {
            return false; // no mockup implementation
        }

        public bool GetFanState(int row, int position) {
            return fanStates[row][position];
        }

        public void SetFanState(int row, int position, bool active) {
            fanStates[row][position] = active;
        }
        #endregion

        #region Crane
        public bool WasCraneButtonPressed(int buttonIndex) {
            if (buttonIndex < 0 || buttonIndex >= craneButtonsBeenPressed.Length)
                throw new ArgumentException("Invalid argument: " + buttonIndex);

            return craneButtonsBeenPressed[buttonIndex];
        }

        public bool IsCraneButtonDown(int buttonIndex) {
            if (buttonIndex < 0 || buttonIndex >= craneButtonsNowPressed.Length)
                throw new ArgumentException("Invalid argument: " + buttonIndex);

            return craneButtonsNowPressed[buttonIndex];
        }
        #endregion Crane

        #region Dynamite
        public int GetHoleState(int holeIndex) {
            return holeStates[holeIndex];
        }
        #endregion Dynamite

        #region Trigger
        public bool WasSequenceButtonPressed(int buttonIndex) {
            if (buttonIndex < 0 || buttonIndex >= sequenceButtonsBeenPressed.Length)
                throw new ArgumentException("Invalid argument: " + buttonIndex);

            bool result = sequenceButtonsBeenPressed[buttonIndex];
            return result;
        }

        public void SetLEDColor(int ledIndex, Color color) {
            triggerLEDs[ledIndex] = color;
        }

        public void SetLEDColors(Color[] colors) {
            if (colors.Length != triggerLEDs.Length)
                throw new ArgumentException("array length == " + colors.Length);
            triggerLEDs = colors;
        }
        #endregion Trigger

    }

}