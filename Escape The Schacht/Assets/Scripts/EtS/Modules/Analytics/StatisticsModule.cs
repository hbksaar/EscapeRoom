using EscapeRoomFramework;
using EscapeTheSchacht.Crates;
using EscapeTheSchacht.Dynamite;
using EscapeTheSchacht.Pipes;
using EscapeTheSchacht.Trigger;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace EscapeTheSchacht {

    public class Statistics {

        public Dictionary<int, int> valvesTurnedPerSubsystem = new Dictionary<int, int>();
        public Dictionary<int, int> runningFansPerSubsystem = new Dictionary<int, int>();

        public int pipesRunningFans;
        public int pipesMaxRunningFans;
        public int pipesSolvedSubsystems;
        public float pipesTime;

        public int craneCrateCount;
        public int craneCratesInDropzone;
        public int craneCratePickups;
        public int craneButtonsPressed;
        public float craneTime;

        public int dynamiteSticksInHoles;
        public int dynamiteInstructionsSolved;
        public int dynamiteMaxInstructionsSolved;
        public int dynamiteStickInsertions;
        public int dynamiteStickRemovals;
        public float dynamiteTime;

        public int triggerSequenceProgress;
        public int triggerMaxSequenceProgress;
        public int triggerButtonsPressed;
        public float triggerTime;

        private string[] aCaptions = {
            "Pipes game time", "Valves turned per subsystem", "Running fans per subsystem", "Total running fans", "Max total running fans", "Solved subsystems",
            "Crane game time", "Crate count", "Crates in dropzone", "Crate pickups", "Crane buttons pressed",
            "Dynamite game time", "Sticks in holes", "Instructions solved", "Max instructions solved", "Stick insertions", "Stick removals",
            "Trigger game time", "Sequence progress", "Max sequence progress", "Trigger buttons pressed"
        };

        private string[] values() {
            StringBuilder valvesPerSubsystem = new StringBuilder();
            foreach (KeyValuePair<int, int> valves in valvesTurnedPerSubsystem) {
                if (valvesPerSubsystem.Length > 0)
                    valvesPerSubsystem.Append(" / ");
                valvesPerSubsystem.Append(valves.Key).Append(':').Append(valves.Value);
            }

            StringBuilder fansPerSubsystem = new StringBuilder();
            foreach (KeyValuePair<int, int> fans in runningFansPerSubsystem) {
                if (fansPerSubsystem.Length > 0)
                    fansPerSubsystem.Append(" / ");
                fansPerSubsystem.Append(fans.Key).Append(':').Append(fans.Value);
            }

            return new string[] {
                pipesTime.ToString("0.0"), valvesPerSubsystem.ToString(), fansPerSubsystem.ToString(), pipesRunningFans.ToString(), pipesMaxRunningFans.ToString(), pipesSolvedSubsystems.ToString(),
                craneTime.ToString("0.0"), craneCrateCount.ToString(), craneCratesInDropzone.ToString(), craneCratePickups.ToString(), craneButtonsPressed.ToString(),
                dynamiteTime.ToString("0.0"), dynamiteSticksInHoles.ToString(), dynamiteInstructionsSolved.ToString(), dynamiteMaxInstructionsSolved.ToString(), dynamiteStickInsertions.ToString(), dynamiteStickRemovals.ToString(),
                triggerTime.ToString("0.0"), triggerSequenceProgress.ToString(), triggerMaxSequenceProgress.ToString(), triggerButtonsPressed.ToString()
            };
        }

        internal void WriteCSV(string targetFile) {
            using (StreamWriter sw = new StreamWriter(targetFile)) {
                string[] aValues = values();
                Debug.Assert(aValues.Length == aCaptions.Length, "Values array length (" + aValues.Length + ") != captions array length (" + aCaptions.Length + ")");

                sw.WriteLine(string.Join("; ", aCaptions));
                sw.WriteLine(string.Join("; ", aValues));
            }
        }
    }

    public class StatisticsModule : Module<IEtsInterface> {

        private static readonly string filename = "statistics.csv";

        public Statistics Statistics { get; private set; }

        private CratesGame crane;

        protected override void Setup() {
            PipesGame pipes = Room.GetGame<PipesGame>();
            pipes.OnValveTurned += onValveTurned;
            pipes.OnFanTriggered += onFanTriggered;

            crane = Room.GetGame<CratesGame>();
            crane.OnCratePickedUp += onCratePickedUp;
            crane.OnCrateDropped += onCrateDropped;

            DynamiteGame dynamite = Room.GetGame<DynamiteGame>();
            dynamite.OnStickInserted += onStickInserted;
            dynamite.OnStickRemoved += onStickRemoved;
            dynamite.OnInstructionSolvedStateChanged += onInstructionSolvedStateChanged;

            TriggersGame trigger = Room.GetGame<TriggersGame>();
            trigger.OnButtonPressed += onButtonPressed;
        }

        public override void OnRoomStateChanged(EscapeRoom<IEtsInterface> sender, RoomStateChangedEventArgs e) {
            if (e.NewState == RoomState.Uninitialized) {
                // start a new record
                Statistics = new Statistics();

                PipesGame pipes = sender.GetGame<PipesGame>();
                foreach (PipeSystem ps in pipes.PipeSystems()) {
                    Statistics.valvesTurnedPerSubsystem[ps.Index] = 0;
                    Statistics.runningFansPerSubsystem[ps.Index] = 0;
                }
            }

            if (e.NewState == RoomState.Completed || e.NewState == RoomState.Aborted) {
                // write active record to file
                Statistics.WriteCSV(Ets.AnalyticsPath + filename);
            }
        }

        public override void OnGameStateChanged(Game<IEtsInterface> sender, GameStateChangedEventArgs e) {
            switch (sender.Id) {
            case PipesGame.GameId:
                Statistics.pipesTime = sender.ElapsedTime;
                break;
            case CratesGame.GameId:
                Statistics.craneTime = sender.ElapsedTime;
                break;
            case DynamiteGame.GameId:
                Statistics.dynamiteTime = sender.ElapsedTime;
                break;
            case TriggersGame.GameId:
                Statistics.triggerTime = sender.ElapsedTime;
                break;
            default:
                throw new NotImplementedException("Game not handled: " + sender.Id);
            }

            if (sender is CratesGame && e.NewState == GameState.Initialized)
                Statistics.craneCrateCount = ((CratesGame) sender).CrateCount;
        }


        #region Pipes
        public void onValveTurned(PipesGame sender, ValveTurnedEventArgs e) {
            Statistics.valvesTurnedPerSubsystem[e.System.Index]++;
        }

        public void onFanTriggered(PipesGame sender, FanTriggeredEventArgs e) {
            if (e.Fan.IsRunning) {
                Statistics.pipesRunningFans++;
                Statistics.runningFansPerSubsystem[e.System.Index]++;
                if (Statistics.pipesRunningFans > Statistics.pipesMaxRunningFans)
                    Statistics.pipesMaxRunningFans = Statistics.pipesRunningFans;
            }
            else {
                Statistics.pipesRunningFans--;
                Statistics.runningFansPerSubsystem[e.System.Index]--;
            }

            if (e.System.RunningFansCount == e.System.FanCount)
                Statistics.pipesSolvedSubsystems++;
        }
        #endregion Pipes

        #region Crane
        public void onCratePickedUp(CratesGame sender, CrateEventArgs e) {
            Statistics.craneCratePickups++;
            Statistics.craneCratesInDropzone = sender.CratesInDropzone;
        }

        public void onCrateDropped(CratesGame sender, CrateEventArgs e) {
            Statistics.craneCratesInDropzone = sender.CratesInDropzone;
        }

        private bool[] lastCraneButtons = new bool[CratesGame.ButtonCount];

        public override void Update(float deltaTime) {
            if (crane.State == GameState.Running) {
                for (int i = 0; i < CratesGame.ButtonCount; i++) {
                    if (!lastCraneButtons[i] && Physical.IsCraneButtonDown(i))
                        Statistics.craneButtonsPressed++;
                    lastCraneButtons[i] = Physical.IsCraneButtonDown(i);
                }
            }
        }
        #endregion Crane

        #region Dynamite
        private void onStickInserted(DynamiteGame sender, StickEventArgs e) {
            Statistics.dynamiteStickInsertions++;
            Statistics.dynamiteSticksInHoles++;
        }

        private void onStickRemoved(DynamiteGame sender, StickEventArgs e) {
            Statistics.dynamiteStickRemovals++;
            Statistics.dynamiteSticksInHoles--;
        }

        private void onInstructionSolvedStateChanged(DynamiteGame sender, InstructionSolvedStateChangedArgs e) {
            if (e.Solved)
                Statistics.dynamiteInstructionsSolved++;
            else
                Statistics.dynamiteInstructionsSolved--;

            if (Statistics.dynamiteInstructionsSolved > Statistics.dynamiteMaxInstructionsSolved)
                Statistics.dynamiteMaxInstructionsSolved = Statistics.dynamiteInstructionsSolved;
        }
        #endregion Dynamite

        #region Trigger
        private void onButtonPressed(object sender, ButtonPressedEventArgs e) {
            Statistics.triggerButtonsPressed++;

            if (e.Correct)
                Statistics.triggerSequenceProgress++;
            else
                Statistics.triggerSequenceProgress--;

            if (Statistics.triggerSequenceProgress > Statistics.triggerMaxSequenceProgress)
                Statistics.triggerMaxSequenceProgress = Statistics.triggerSequenceProgress;
        }
        #endregion Trigger

    }

}