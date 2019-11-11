using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EscapeTheSchacht.Dynamite;
using EscapeTheSchacht.Trigger;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using EscapeRoomFramework;

namespace EscapeTheSchacht.Dynamite {

    public delegate void StickEventHandler(DynamiteGame sender, StickEventArgs args);
    public delegate void InstructionSolvedStateChangedHandler(DynamiteGame sender, InstructionSolvedStateChangedArgs args);

    public class StickEventArgs : EventArgs {
        /// <summary>
        /// The stick that has been inserted or removed.
        /// </summary>
        public DynamiteStick Stick { get; }
        /// <summary>
        /// The hole the stick has been inserted into or removed from.
        /// </summary>
        public int HoleIndex { get; }

        internal StickEventArgs(DynamiteStick stick, int holeIndex) {
            Stick = stick;
            HoleIndex = holeIndex;
        }
    }

    public class InstructionSolvedStateChangedArgs : EventArgs {
        /// <summary>
        /// The instruction.
        /// </summary>
        public int InstructionIndex { get; }
        /// <summary>
        /// True iff the instruction is solved now.
        /// </summary>
        public bool Solved { get; }
        
        internal InstructionSolvedStateChangedArgs(int instructionIndex, bool solved) {
            InstructionIndex = instructionIndex;
            Solved = solved;
        }
    }

    public class DynamiteGame : Game<IEtsInterface> {

        public const string GameId = "Dynamite";
        public static readonly int RowCount = DynamiteScenario.BottomRowIndex + 1;
        public static readonly int ColumnCount = DynamiteScenario.LastColumnIndex + 1;

        public static readonly int HoleCount = RowCount * ColumnCount;
        public static readonly int StickCount = DynamiteStick.Sticks.Length;

        public override string Id => GameId;

        /// <summary>
        /// The number of the scenario (1-6)
        /// </summary>
        public int ScenarioNumber => scenario.Number;
        public string ScenarioName => scenario.Name;
        public int InstructionCount => scenario.InstructionCount;

        private DynamiteWall wall;
        private DynamiteScenario scenario;
        private int[] lastHoleStates = new int[HoleCount];
        private bool[] lastInstructionsSolvedState;

        /// <summary>
        /// Is raised when a stick has been inserted into a hole.
        /// </summary>
        public event StickEventHandler OnStickInserted;
        /// <summary>
        /// Is raised when a stick has been removed from a hole.
        /// </summary>
        public event StickEventHandler OnStickRemoved;
        /// <summary>
        /// Is raised when the solution state of a scenario instruction has changed.
        /// </summary>
        public event InstructionSolvedStateChangedHandler OnInstructionSolvedStateChanged;

        public DynamiteGame() {
            wall = new DynamiteWall();
            Log.Info("Dynamite game set up.");
        }

        public string GetInstructionText(int instructionIndex) {
            return scenario.GetInstructionText(instructionIndex);
        }

        public bool GetInstructionSolvedState(int instructionIndex) {
            return lastInstructionsSolvedState[instructionIndex];
        }

        public int GetStickPosition(int stickIndex) {
            return Array.IndexOf(lastHoleStates, stickIndex);
        }

        public override bool CompensateTechnicalFailures(DiagnosticsReport report, List<string> affectedComponents) {
            return false;
        }

        protected override bool Initialize0(GameDifficulty difficulty, DiagnosticsReport diagnosticsReport) {
            // query hole states from physical interface
            int[] currentHoleStates = new int[HoleCount];
            for (int i = 0; i < HoleCount; i++)
                currentHoleStates[i] = Physical.GetHoleState(i);

            scenario = DynamiteScenario.ChooseScenario(difficulty, wall, currentHoleStates);
            Log.Info("Dynamite scenario '{0}' selected.", scenario.Name);
            lastInstructionsSolvedState = new bool[scenario.InstructionCount];

            for (int i = 0; i < lastHoleStates.Length; i++)
                lastHoleStates[i] = -1;

            for (int i = 0; i < TriggersGame.ButtonCount; i++)
                Physical.SetLEDColor(i, Color.black);

            return true;
        }

        protected override GameState Update0(float deltaTime) {
            // query hole states from physical interface
            int[] newHoleStates = new int[HoleCount];
            for (int i = 0; i < HoleCount; i++)
                newHoleStates[i] = Physical.GetHoleState(i);

            // update hole states in solution checker
            bool[] instructionsSolved = wall.CheckSolution(scenario, newHoleStates);

            // compare old and new hole states to send notifications on changes
            for (int i = 0; i < HoleCount; i++) {
                if (newHoleStates[i] != lastHoleStates[i]) {
                    if (lastHoleStates[i] == -1) { // if there was no stick and now there is
                        Log.Verbose("Dynamite: Stick {0} has been inserted into hole {1}.", newHoleStates[i], i);
                        OnStickInserted?.Invoke(this, new StickEventArgs(DynamiteStick.Sticks[newHoleStates[i]], i));
                    }
                    if (newHoleStates[i] == -1) { // if there was a stick and now there isn't
                        Log.Verbose("Dynamite: Stick {0} has been removed from hole {1}.", lastHoleStates[i], i);
                        OnStickRemoved?.Invoke(this, new StickEventArgs(DynamiteStick.Sticks[lastHoleStates[i]], i));
                    }
                }
                lastHoleStates[i] = newHoleStates[i];
            }

            for (int i = 0; i < lastInstructionsSolvedState.Length; i++) {
                if (lastInstructionsSolvedState[i] != instructionsSolved[i]) {
                    Log.Verbose("Dynamite: Instruction {0} has been {1}.", i, instructionsSolved[i] ? "solved" : "unsolved");
                    OnInstructionSolvedStateChanged?.Invoke(this, new InstructionSolvedStateChangedArgs(i, instructionsSolved[i]));
                }
                lastInstructionsSolvedState[i] = instructionsSolved[i];
            }

            if (instructionsSolved.Contains(false))
                return GameState.Running;

            return GameState.Completed;
        }

    }

}