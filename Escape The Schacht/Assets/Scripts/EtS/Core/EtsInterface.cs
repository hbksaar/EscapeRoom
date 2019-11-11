using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using System;
using EscapeTheSchacht.Pipes;
using EscapeTheSchacht.Crates;
using EscapeTheSchacht.Dynamite;
using EscapeTheSchacht.Trigger;
using EscapeRoomFramework;

namespace EscapeTheSchacht {

    #pragma warning disable 0649

    public class EtsInterface : MonoBehaviour, IEtsInterface {

        #region JSON conversion helper structs
        private struct BoolResponse {
            public bool @return;
        }

        private struct PhysicalStateResponse {
            public PhysicalState @return;
        }

        private struct PhysicalStateQuery {
            public bool control;
            public bool lights;
            public bool pipes;
            public bool crane;
            public bool dynamite;
            public bool blasting;

            public PhysicalStateQuery(bool control, bool lights, bool pipes, bool crane, bool dynamite, bool blasting) {
                this.control = control;
                this.lights = lights;
                this.pipes = pipes;
                this.crane = crane;
                this.dynamite = dynamite;
                this.blasting = blasting;
            }
        }

        private struct PhysicalState {
            public ControlState control;
            public LightsState lights;
            public TriggerState blasting;
            public CraneState crane;
            public DynamiteState dynamite;
            public PipesState pipes;
        }

        private struct ControlState {
			public bool control_button_been_pressed;
            public bool control_button_now_pressed;
        }

        private struct LightsState {
            public bool[] light_states;
        }

        private struct DynamiteState {
            public Stick[] hole_states;
        }

		private class Stick {
			public int id;
			public string tag;
			public int weight;
			public bool @long;
			public bool @rippled;
		}

        private struct TriggerState {
            public bool[] sequence_buttons_been_pressed;
			public bool[] sequence_buttons_now_pressed;
        }

        private struct CraneState {
            public bool[] direction_buttons_been_pressed;
			public bool[] direction_buttons_now_pressed;
        }

        private struct PipesState {
            public bool[][] valve_states;
            public bool[][] fan_states;
            public bool[][] valve_rotations;
            public bool[][] valves_broken;
        }
        #endregion JSON conversion helper structs

        public static int LightsCount { get; } = Enum.GetValues(typeof(Light)).Length;

        private static readonly byte[] Ip = { 127, 0, 0, 1 }; // localhost
        private static readonly int Port = 5005;
		private static readonly int Timeout = 5000; // ms

        public string etsiPath;
        public bool showConsoleWindow;

        private Socket server, socket;

        private PhysicalState state; // updated each frame
        private PhysicalStateQuery query; // the last query (determines what variables have been updated in the physical since the last frame)

        public bool IsOpen { get; private set; }

		public bool OverrideLighting { get; set; }

		public bool overrideLighting; // for debugging in Unity editor only

        public MaintenanceMode maintenanceMode;

        private PipesGame pipes;
        private CratesGame crane;
        private DynamiteGame dynamite;
        private TriggersGame trigger;

        private void Start() {
            pipes = Ets.Room.GetGame<PipesGame>();
            crane = Ets.Room.GetGame<CratesGame>();
            dynamite = Ets.Room.GetGame<DynamiteGame>();
            trigger = Ets.Room.GetGame<TriggersGame>();
        }

        void OnValidate() {
			OverrideLighting = overrideLighting;
		}

        /// <summary>
        /// See <see cref="IEtsInterface.Open"/>
        /// </summary>
        public void Open() {
            Log.Verbose("ETSI: Starting client...");
            string etsiLogFile = Log.Instance.FilePath.Replace(".log", " etsi.log");

            System.Diagnostics.Process client = new System.Diagnostics.Process();
            client.StartInfo.FileName = "py";
            client.StartInfo.Arguments = "start_server.py > " + etsiLogFile;
            client.StartInfo.WorkingDirectory = etsiPath;
            client.StartInfo.CreateNoWindow = !showConsoleWindow;
            client.Start();

            Log.Verbose("ETSI: Starting server...");
            server = new Socket(SocketType.Stream, ProtocolType.Tcp);
            server.Bind(new IPEndPoint(new IPAddress(Ip), Port));
            //server.ReceiveTimeout = TIMEOUT;
            //server.SendTimeout = TIMEOUT;
            server.Listen(1);

            Log.Verbose("ETSI: Waiting for physical interface connection...");
            socket = server.Accept();
            socket.SendTimeout = Timeout;
            socket.ReceiveTimeout = Timeout;
            IsOpen = true;
            Log.Verbose("ETSI: Physical interface connected.");

            // query the full physical game state once to ensure no arrays in state are null and to check for technical failures
            Log.Verbose("ETSI: Querying intial state and checking for technical failures...");
            query = new PhysicalStateQuery(true, true, true, true, true, true);
            PhysicalStateResponse response = SendQuery<PhysicalStateResponse>("get_game_state", query);
            state = response.@return;
        }

        public void RunDiagnostics(DiagnosticsReport result) {
            int brokenValves = 0;
            foreach (bool[] row in state.pipes.valves_broken)
                brokenValves += row.Count(true);
            if (brokenValves > 0) {
                result.AddAffectedGameComponent(pipes, "valves");
            }
        }

        /// <summary>
        /// See <see cref="IEtsInterface.Close"/>
        /// </summary>
        public void Close() {
            if (socket != null) {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
				socket = null;
            }

            if (server != null) {
                server.Shutdown(SocketShutdown.Both);
                server.Close();
				server = null;
            }

            IsOpen = false;
        }

        /// <summary>
        /// See <see cref="IEtsInterface.Update()"/>. Sends a request via socket to ETSI to query the 
        /// physical state. Also waits for, receives and parses the response.
        /// </summary>
        public void PhysicalUpdate(float deltaTime) {
            // query state
            query = new PhysicalStateQuery(true, true,
                pipes.State == GameState.Initialized || pipes.State == GameState.Running || maintenanceMode.enabled,
                crane.State == GameState.Initialized || crane.State == GameState.Running || maintenanceMode.enabled,
                dynamite.State == GameState.Initialized || dynamite.State == GameState.Running || maintenanceMode.enabled,
                trigger.State == GameState.Initialized || trigger.State == GameState.Running || maintenanceMode.enabled
            );
            PhysicalStateResponse response = SendQuery<PhysicalStateResponse>("get_game_state", query);

            PhysicalState newState = response.@return;
            if (query.pipes)
                state.pipes = newState.pipes;
            if (query.crane)
                state.crane = newState.crane;
            if (query.dynamite)
                state.dynamite = newState.dynamite;
            if (query.blasting)
                state.blasting = newState.blasting;
			if (query.lights)
				state.lights = newState.lights;
			if (query.control)
				state.control = newState.control;

            if (OverrideLighting && GetLightStates().Contains(false)) {
				bool[] lights = new bool[LightsCount];
				for (int i = 0; i < lights.Length; i++)
					lights[i] = true;
				
				OverrideLighting = false;
				SetLightStates(lights);
				OverrideLighting = true;
			}
		}

        public void ForcePipesUpdate() {
            PhysicalStateQuery selectiveQuery = new PhysicalStateQuery(false, false, true, false, false, false);
            PhysicalStateResponse response = SendQuery<PhysicalStateResponse>("get_game_state", selectiveQuery);
            query.pipes = true;
            state.pipes = response.@return.pipes;
        }

        public void ForceCraneUpdate() {
            PhysicalStateQuery selectiveQuery = new PhysicalStateQuery(false, false, false, true, false, false);
            PhysicalStateResponse response = SendQuery<PhysicalStateResponse>("get_game_state", selectiveQuery);
            query.crane = true;
            state.crane = response.@return.crane;
        }

        public void ForceDynamiteUpdate() {
            PhysicalStateQuery selectiveQuery = new PhysicalStateQuery(false, false, false, false, true, false);
            PhysicalStateResponse response = SendQuery<PhysicalStateResponse>("get_game_state", selectiveQuery);
            query.dynamite = true;
            state.dynamite = response.@return.dynamite;
        }

        public void ForceTriggerUpdate() {
            PhysicalStateQuery selectiveQuery = new PhysicalStateQuery(false, false, false, false, false, true);
            PhysicalStateResponse response = SendQuery<PhysicalStateResponse>("get_game_state", selectiveQuery);
            query.blasting = true;
            state.blasting = response.@return.blasting;
        }

        /// <summary>
        /// Sends queries to ETSI and waits for, receives and parses the response to the given type RT.
        /// </summary>
        /// <typeparam name="RT">the type to convert the response to</typeparam>
        /// <param name="qCommand">the name of the command</param>
        /// <param name="qArgs">the argument list</param>
        /// <returns></returns>
        private RT SendQuery<RT>(string qCommand, object qArgs) {
            if (!socket.Connected) {
                Log.Warn("ETSI: Connection is closed, rejecting command: " + qCommand);
                return default(RT);
            }

			//if (qCommand != "get_game_state")
			//	print ("ETSI.SendQuery: " + qCommand + " / " + qArgs.ToString ());
            // compose and send the request
            var query = new { command = qCommand, args = qArgs };
            string json = JsonConvert.SerializeObject(query);
			//if (qCommand != "get_game_state")
			//	print ("ETSI.SendQuery 2: " + json);
            byte[] data = Encoding.UTF8.GetBytes(json + '\n');

            //Debug.Log("ETSI: Sending query: " + json);
            socket.Send(data);

            // receive the answer
            MemoryStream received = new MemoryStream();
            byte[] buffer = new byte[32];
            //Debug.Log("ETSI: Waiting for response...");
            do {
                int bytesCount = socket.Receive(buffer);
                received.Write(buffer, 0, bytesCount);
            } while (socket.Available > 0);

            // convert received bytes to string
            string sResponse = Encoding.UTF8.GetString(received.ToArray());
            //Debug.Log("ETSI: Received response: " + sResponse);

            // parse json string and deserialize to return type
            return JsonConvert.DeserializeObject<RT>(sResponse);
        }

        #region Room
        public bool GetLightState(Light light) {
            if (!query.lights)
                Log.Warn("ETSI: Light state accessed but not queried");
            return state.lights.light_states[(int) light];
        }

        public void SetLightState(Light light, bool on) {
			if (OverrideLighting) {
				Log.Warn ("ETSI: Light control override active");
				return;
			}
			
			var args = new { index = (int) light, active = on };
            BoolResponse response = SendQuery<BoolResponse>("lights_set_light_state", args);
            if (!response.@return)
                Log.Warn("ETSI: SetLightState({0}) response was {1}", args, response.@return);
            state.lights.light_states[(int) light] = on;
        }

        public bool[] GetLightStates() {
            if (!query.lights)
                Log.Warn("ETSI: Light state accessed but not queried");
            bool[] result = new bool[LightsCount];
            for (int i = 0; i < result.Length; i++)
				result[i] = state.lights.light_states[i];
            return result;
        }

        public void SetLightStates(bool[] states) {
			if (OverrideLighting) {
				Log.Warn ("ETSI: Light control override active");
				return;
			}
				
			int[] iStates = new int[LightsCount];
			for (int i = 0; i < states.Length; i++) {
				if (states [i])
					iStates [i] = 1;
				else
					iStates [i] = -1;
			}
			var args = new { states = iStates };
			BoolResponse response = SendQuery<BoolResponse>("lights_set_light_states", args);
			if (!response.@return)
				Log.Warn("ETSI: SetLightStates({0}) response was {1}", args, response.@return);

			for (int i = 0; i < states.Length; i++)
				state.lights.light_states [i] = states [i];
        }

        public void ChangeLightStates(Light[] lights, LightSetting[] changes) {
			if (OverrideLighting) {
				Log.Warn ("ETSI: Light control override active");
				return;
			}

            if (lights.Length != changes.Length)
                throw new ArgumentException("Given arrays differ in length.");

            for (int i = 0; i < lights.Length; i++)
                if (changes[i] != LightSetting.None)
                    SetLightState(lights[i], changes[i] == LightSetting.On);
        }

        /// <summary>
        /// See <see cref="IEtsInterface.WasControlButtonPressed"/>
        /// </summary>
        public bool WasControlButtonPressed() {
            if (!query.control)
                Log.Warn("ETSI: Control button state accessed but not queried");
            return state.control.control_button_been_pressed;
        }

        /// <summary>
        /// See <see cref="IEtsInterface.IsControlButtonDown"/>
        /// </summary>
        public bool IsControlButtonDown() {
            if (!query.control)
                Log.Warn("ETSI: Control button state accessed but not queried");
            return state.control.control_button_now_pressed;
        }
        #endregion Room

        #region Pipes
        /// <summary>
        /// See <see cref="IEtsInterface.SetFanState(int, int, bool)"/>
        /// </summary>
        public bool GetFanState(int row, int position) {
            if (!query.pipes)
                Log.Warn("ETSI: Pipes state accessed but not queried");
            return state.pipes.fan_states[row][position];
        }

        /// <summary>
        /// See <see cref="IEtsInterface.GetFanState(int, int)"/>
        /// </summary>
        public void SetFanState(int row, int position, bool active) {
            var args = new { row, position, active };
            BoolResponse response = SendQuery<BoolResponse>("pipes_set_fan_state", args);
            if (!response.@return)
                Log.Warn("ETSI: SetFanState({0}) response was {1}", args, response.@return);
            state.pipes.fan_states[row][position] = active;
        }

        /// <summary>
        /// See <see cref="IEtsInterface.GetValveState(int, int)"/>
        /// </summary>
        public bool GetValveState(int row, int position) {
            if (!query.pipes)
                Log.Warn("ETSI: Pipes state accessed but not queried");
            return state.pipes.valve_states[row][position];
        }

        /// <summary>
        /// See <see cref="IEtsInterface.IsValveBroken(int, int)"/>
        /// </summary>
        public bool IsValveBroken(int row, int position) {
            if (!query.pipes)
                Log.Warn("ETSI: Pipes state accessed but not queried");
            return state.pipes.valves_broken[row][position];
        }

        /// <summary>
        /// See <see cref="IEtsInterface.IsValveRotating(int, int)"/>
        /// </summary>
        public bool IsValveRotating(int row, int position) {
            if (!query.pipes)
                Log.Warn("ETSI: Pipes state accessed but not queried");
            return state.pipes.valve_rotations[row][position];
        }

        /// <summary>
        /// See <see cref="IEtsInterface.IsAnyValveRotating()"/>
        /// </summary>
        public bool IsAnyValveRotating() {
            if (!query.pipes)
                Log.Warn("ETSI: Pipes state accessed but not queried");
            foreach (bool[] positions in state.pipes.valve_rotations)
                foreach (bool rotating in positions)
                    if (rotating)
                        return true;
            return false;
        }
        #endregion Pipes

        #region Crane
        /// <summary>
        /// See <see cref="IEtsInterface.IsCraneButtonDown(int)"/>
        /// </summary>
        public bool IsCraneButtonDown(int buttonIndex) {
            if (!query.crane)
                Log.Warn("ETSI: Crane state accessed but not queried");
            return state.crane.direction_buttons_now_pressed[buttonIndex];
        }

        /// <summary>
        /// See <see cref="IEtsInterface.WasCraneButtonPressed(int)"/>
        /// </summary>
        public bool WasCraneButtonPressed(int buttonIndex) {
            if (!query.crane)
                Log.Warn("ETSI: Crane state accessed but not queried");
            return state.crane.direction_buttons_been_pressed[buttonIndex];
        }
        #endregion Crane

        #region Dynamite
        /// <summary>
        /// See <see cref="IEtsInterface.GetHoleState(int)"/>
        /// </summary>
        public int GetHoleState(int holeIndex) {
            if (!query.dynamite)
                Log.Warn("ETSI: Dynamite state accessed but not queried");
            Stick stick = state.dynamite.hole_states[holeIndex];
			return stick != null ? stick.id : -1;
        }
        #endregion Dynamite

        #region Trigger
        /// <summary>
        /// See <see cref="IEtsInterface.WasSequenceButtonPressed(int)"/>
        /// </summary>
        public bool WasSequenceButtonPressed(int buttonIndex) {
            if (!query.blasting)
                Log.Warn("ETSI: Trigger state accessed but not queried");
            return state.blasting.sequence_buttons_been_pressed[buttonIndex];
        }

        /// <summary>
        /// See <see cref="IEtsInterface.SetLEDColor(int, Color)"/>
        /// </summary>
        public void SetLEDColor(int ledIndex, Color color) {
            int r = Mathf.RoundToInt(color.r * 255);
            int g = Mathf.RoundToInt(color.g * 255);
            int b = Mathf.RoundToInt(color.b * 255);

            var args = new { led_index = ledIndex, r, g, b };
            BoolResponse response = SendQuery<BoolResponse>("blasting_set_led_color", args);
            if (!response.@return)
                Log.Warn("ETSI: SetLEDColor({0}) response was {1}", args, response.@return);
        }

        /// <summary>
        /// See <see cref="IEtsInterface.SetLEDColors(Color)"/>
        /// </summary>
        /// <param name="colors"></param>
        public void SetLEDColors(Color[] colors) {
            //var led_values = new { new {r, g, b}, ...}

            object[] values = new object[colors.Length];
            for (int i = 0; i < colors.Length; i++) {
                int r = Mathf.RoundToInt(colors[i].r * 255);
                int g = Mathf.RoundToInt(colors[i].g * 255);
                int b = Mathf.RoundToInt(colors[i].b * 255);
                values[i] = new { r, g, b };
            }

            var args = new { led_values = values };

            BoolResponse response = SendQuery<BoolResponse>("blasting_set_led_colors", args);
            if (!response.@return)
                Log.Warn("SPI: SetLEDColors({0}) response was {1}", args, response);
        }
        // command= ..., args = [{}, {}]

        #endregion Trigger 

    }
}