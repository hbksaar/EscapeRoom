using EscapeTheSchacht.Crates;
using EscapeTheSchacht.Dynamite;
using EscapeTheSchacht.Pipes;
using EscapeTheSchacht.Trigger;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Newtonsoft.Json;
using System.Text;
using System.IO;
using EscapeRoomFramework;

namespace EscapeTheSchacht {

    public class MaintenanceMode : MonoBehaviour {

        private struct MaintenanceRecord {
            //public bool controlButton;
            //public bool[] lights = new bool[LightingDirector.INSIDE_LIGHTS_COUNT];

            public bool[][] valvesPerPipeSystem_Rotating;
            public bool[][] valvesPerPipeSystem_StateChanges;
            public bool[][] fansPerPipeSystem;

            public bool[] craneButtons;

            public bool[] holes;

            public bool[] triggerButtons;
        }

        private string filename = "maintenance.txt";
        
        private EscapeRoom<IEtsInterface> room;
        private PipesGame pipes;

        public new AudioSystem audio;
        public LightingDirector lighting;

        public int actionRegisteredSound = 42;

        private MaintenanceRecord record;
        private bool[][] initialValveStates;
        private int[] initialHoleStates;
        private bool readInitialState;
        private bool success;

        private void OnEnable() {
            Log.Verbose("Starting maintenance mode...");

            room = Ets.Room;
            pipes = room.GetGame<PipesGame>();

            lighting.SwitchAllLights(LightSetting.On);
            success = false;
            readInitialState = true;
            record = new MaintenanceRecord();

            // pipes
            List<int> valvesPerPipeSystem = new List<int>();
            List<int> fansPerPipeSystem = new List<int>();
            foreach (PipeSystem ps in pipes.PipeSystems()) {
                valvesPerPipeSystem.Add(ps.ValveCount);
                fansPerPipeSystem.Add(ps.FanCount);
            }

            initialValveStates = new bool[valvesPerPipeSystem.Count][];

            record.valvesPerPipeSystem_Rotating = new bool[valvesPerPipeSystem.Count][];
            record.valvesPerPipeSystem_StateChanges = new bool[valvesPerPipeSystem.Count][];

            for (int i = 0; i < valvesPerPipeSystem.Count; i++) {
                initialValveStates[i] = new bool[valvesPerPipeSystem[i]];
                record.valvesPerPipeSystem_Rotating[i] = new bool[valvesPerPipeSystem[i]];
                record.valvesPerPipeSystem_StateChanges[i] = new bool[valvesPerPipeSystem[i]];
            }

            record.fansPerPipeSystem = new bool[fansPerPipeSystem.Count][];
            for (int i = 0; i < fansPerPipeSystem.Count; i++)
                record.fansPerPipeSystem[i] = new bool[fansPerPipeSystem[i]];

            foreach (Fan f in pipes.Fans())
                room.Physical.SetFanState(f.Row, f.PositionInRow, true);

            // crane
            record.craneButtons = new bool[CratesGame.ButtonCount];

            // dynamite
            initialHoleStates = new int[DynamiteGame.HoleCount];

            record.holes = new bool[DynamiteGame.HoleCount];

            // trigger
            record.triggerButtons = new bool[TriggersGame.ButtonCount];

            Color[] colors = new Color[TriggersGame.LedCount];
            for (int i = 0; i < colors.Length; i++)
                colors[i] = Color.blue;
            room.Physical.SetLEDColors(colors);

            Log.Verbose("Maintenance mode started. Waiting for input...");
        }

        private void OnDisable() {
            string file = Log.LogFilePath + filename;
            Log.Verbose("Maintenance mode completed (successful ? {0}), see log: {1}", success, filename);

            try {
                string json = JsonConvert.SerializeObject(record, Formatting.Indented);
                using (StreamWriter sw = new StreamWriter(file, false, Encoding.UTF8)) {
                    sw.WriteLine(json);
                }
            } catch {
                audio.PlaySound(SoundDirector.ErrorSound);
                throw;
            }
        }

        private void Update() {
            if (readInitialState) {
                room.Physical.ForcePipesUpdate();
                int psi = 0;
                foreach (PipeSystem ps in pipes.PipeSystems()) {
                    int i = 0;
                    foreach (Valve v in ps.Valves()) {
                        initialValveStates[psi][i] = room.Physical.GetValveState(v.Row, v.PositionInRow);
                        i++;
                    }
                    psi++;
                }

                room.Physical.ForceDynamiteUpdate();
                for (int i = 0; i < DynamiteGame.HoleCount; i++)
                    initialHoleStates[i] = room.Physical.GetHoleState(i);

                readInitialState = false;
            }

            else {
                updateRecord(room.Physical);
                checkRecord(record);
            }
        }

        private void updateRecord(IEtsInterface p) {
            //record.controlButton |= p.WasControlButtonPressed();
            //for (int i = 0; i < record.lights.Length; i++)
                //record.lights[i] |= p.GetLightState(i);
            
            bool anyActionRegistered = false;

            p.ForcePipesUpdate();
            int psi = 0;
            foreach (PipeSystem ps in pipes.PipeSystems()) {
                int vi = 0;
                foreach (Valve v in ps.Valves()) {
                    record.valvesPerPipeSystem_Rotating[psi][vi] |= p.IsValveRotating(v.Row, v.PositionInRow);
                    anyActionRegistered |= record.valvesPerPipeSystem_Rotating[psi][vi];
                    record.valvesPerPipeSystem_StateChanges[psi][vi] |= (p.GetValveState(v.Row, v.PositionInRow) != initialValveStates[psi][vi]);
                    anyActionRegistered |= record.valvesPerPipeSystem_StateChanges[psi][vi];
                    vi++;
                }
                int fi = 0;
                foreach (Fan f in ps.Fans()) {
                    record.fansPerPipeSystem[psi][fi] |= p.GetFanState(f.Row, f.PositionInRow);
                    anyActionRegistered |= record.fansPerPipeSystem[psi][fi];
                    fi++;
                }
                psi++;
            }

            p.ForceCraneUpdate();
            for (int i = 0; i < CratesGame.ButtonCount; i++) {
                record.craneButtons[i] |= p.WasCraneButtonPressed(i);
                anyActionRegistered |= record.craneButtons[i];
            }

            p.ForceDynamiteUpdate();
            for (int i = 0; i < DynamiteGame.HoleCount; i++) {
                record.holes[i] |= (p.GetHoleState(i) != initialHoleStates[i]);
                anyActionRegistered |= record.holes[i];
            }

            p.ForceTriggerUpdate();
            for (int i = 0; i < TriggersGame.ButtonCount; i++) {
                record.triggerButtons[i] |= p.WasSequenceButtonPressed(i);
                anyActionRegistered |= record.triggerButtons[i];
            }

            if (anyActionRegistered)
                audio.PlaySound(actionRegisteredSound);
        }

        private void checkRecord(MaintenanceRecord record) {
            int pipesMissing = countPipesMissing(record);
            int craneMissing = record.craneButtons.Count(false);
            int dynamiteMissing = record.holes.Count(false);
            int triggerMissing = record.triggerButtons.Count(false);

            if (pipesMissing == 0)
                lighting.SwitchLights(LightSetting.Off, Light.PipesLeft, Light.PipesRight);
            if (craneMissing == 0)
                lighting.SwitchLights(LightSetting.Off, Light.CraneLeft, Light.CraneRight);
            if (dynamiteMissing == 0)
                lighting.SwitchLights(LightSetting.Off, Light.Dynamite);
            if (triggerMissing == 0)
                lighting.SwitchLights(LightSetting.Off, Light.Trigger);

            if (pipesMissing == 0 && craneMissing == 0 && dynamiteMissing == 0 && triggerMissing == 0) {
                audio.PlaySound(SoundDirector.ConfirmationSound);
                success = true;
                enabled = false;
            }
        }

        private int countPipesMissing(MaintenanceRecord record) {
            int sum = 0;
            foreach (bool[] ba in record.valvesPerPipeSystem_Rotating)
                sum += ba.Count(false);
            foreach (bool[] ba in record.valvesPerPipeSystem_StateChanges)
                sum += ba.Count(false);
            foreach (bool[] ba in record.fansPerPipeSystem)
                sum += ba.Count(false);
            return sum;
        }
    }

}