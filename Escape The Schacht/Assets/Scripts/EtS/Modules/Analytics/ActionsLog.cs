using EscapeRoomFramework;
using EscapeTheSchacht.Crates;
using EscapeTheSchacht.Dynamite;
using EscapeTheSchacht.Pipes;
using EscapeTheSchacht.Trigger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace EscapeTheSchacht {

    public class ActionsLog : Module<IEtsInterface> {

        private static readonly string roomScope = "Room";
        private static readonly string filename = "actions.xml";
        private static readonly string rootTag = "ActionsLog";

        private StreamWriter writer;
        private CratesGame crates;

        protected override void Setup() {
            PipesGame pipes = Room.GetGame<PipesGame>();
            pipes.OnValveTurned += OnValveTurned;
            pipes.OnFanTriggered += OnFanTriggered;

            crates = Room.GetGame<CratesGame>();
            crates.OnCrateDropped += OnCrateDropped;
            crates.OnCratePickedUp += OnCratePickedUp;

            DynamiteGame dynamite = Room.GetGame<DynamiteGame>();
            dynamite.OnInstructionSolvedStateChanged += OnInstructionSolvedStateChanged;
            dynamite.OnStickInserted += OnStickInserted;
            dynamite.OnStickRemoved += OnStickRemoved;

            TriggersGame trigger = Room.GetGame<TriggersGame>();
            trigger.OnButtonPressed += OnButtonPressed;
        }

        private void StartRecord() {
            writer = new StreamWriter(Ets.AnalyticsPath + filename);
            writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            writer.WriteLine("<{0} timestamp=\"{1}\">", rootTag, DateTime.Now);
        }

        private void Record(ActionLogEntry action) {
            Debug.Log(action);
            Debug.Log(action.ToXml());
            writer.Write("  ");
            writer.WriteLine(action.ToXml());
        }

        private void EndRecord() {
            writer.WriteLine("</{0}>", rootTag);
            writer.Close();
            writer = null;
        }

        public override void OnRoomStateChanged(EscapeRoom<IEtsInterface> sender, RoomStateChangedEventArgs e) {
            if (e.NewState == RoomState.Uninitialized) {
                Debug.Assert(writer == null);
                //if (writer != null)
                //    endRecord();
                StartRecord();
            }

            Record(new RoomStateTransition(e.OldState, e.NewState));

            if (e.NewState == RoomState.Initialized && e.DiagnosticsReport.FailuresDetected)
                Record(new TechnicalFailuresNotification(e.DiagnosticsReport));

            if (e.NewState == RoomState.Completed || e.NewState == RoomState.Aborted)
                EndRecord();

            if (e.NewState == RoomState.Error) {
                Record(new TechnicalFailuresNotification(e.DiagnosticsReport));
                EndRecord();
            }
        }

        /*
        public override void OnRoomStateChanged(EscapeRoom<IEtsInterface> sender, RoomStateChangedEventArgs e) {
            if (writer == null) {
                Debug.Assert(e.NewState == RoomState.Initialized || e.NewState == RoomState.Error, "writer null, but room not in expected state");

                startRecord();
            }

            record(new RoomStateTransition(e.OldState, e.NewState));

            switch (e.NewState) {
            case RoomState.Uninitialized:
            case RoomState.Running:
                break;

            case RoomState.Initialized:
                if (e.DiagnosticsReport.FailuresDetected)
                    record(new TechnicalFailuresNotification(e.DiagnosticsReport));
                break;

            case RoomState.Completed:
            case RoomState.Aborted:
                endRecord();
                break;

            case RoomState.Error:
                record(new TechnicalFailuresNotification(e.DiagnosticsReport));
                endRecord();
                break;

            default:
                throw new NotImplementedException("Unhandled room state: " + e.NewState);
            }
        }
        */

        public override void OnGameStateChanged(Game<IEtsInterface> sender, GameStateChangedEventArgs e) {
            Record(new GameStateTransition(sender.Id, e.OldState, e.NewState));

            switch (e.NewState) {
            case GameState.Initialized:
                Record(new GameInitialization(sender.Id, sender.Difficulty));
                break;

            case GameState.Uninitialized:
            case GameState.Running:
            case GameState.Completed:
            case GameState.Aborted:
            case GameState.Error:
                break;

            default:
                throw new NotImplementedException("Unhandled game state: " + e.NewState);
            }
        }

        #region Pipes
        private void OnValveTurned(PipesGame sender, ValveTurnedEventArgs e) {
            Record(new ValveStateChange(e.System.Index, e.Valve.Row, e.Valve.PositionInRow, e.Valve.IsOpen));
        }

        private void OnFanTriggered(PipesGame sender, FanTriggeredEventArgs e) {
            Record(new FanTriggered(e.System.Index, e.Fan.Row, e.Fan.PositionInRow, e.Fan.IsRunning));
        }
        #endregion Pipes

        #region Crates
        private void OnCratePickedUp(CratesGame sender, CrateEventArgs e) {
            Record(new CratePickup(e.CrateId, sender.CratesInDropzone));
        }
        
        private void OnCrateDropped(CratesGame sender, CrateEventArgs e) {
            Record(new CrateDrop(e.CrateId, sender.CratesInDropzone));
        }

        private readonly bool[] lastCraneButtons = new bool[CratesGame.ButtonCount];

        public override void Update(float deltaTime) {
            if (crates.State == GameState.Running) {
                for (int i = 0; i < CratesGame.ButtonCount; i++) {
                    if (!lastCraneButtons[i] && Physical.IsCraneButtonDown(i))
                        Record(new CraneButtonPress(i));
                    if (lastCraneButtons[i] && !Physical.IsCraneButtonDown(i))
                        Record(new CraneButtonRelease(i));
                    lastCraneButtons[i] = Physical.IsCraneButtonDown(i);
                }
            }
        }
        #endregion Crates

        #region Dynamite
        private void OnStickInserted(DynamiteGame sender, StickEventArgs e) {
            Record(new StickInsertion(e.HoleIndex, e.Stick.Index, e.Stick.Holes, e.Stick.Grooves, e.Stick.Length, e.Stick.Weight));
        }

        private void OnStickRemoved(DynamiteGame sender, StickEventArgs e) {
            Record(new StickRemoval(e.HoleIndex, e.Stick.Index, e.Stick.Holes, e.Stick.Grooves, e.Stick.Length, e.Stick.Weight));
        }

        private void OnInstructionSolvedStateChanged(DynamiteGame sender, InstructionSolvedStateChangedArgs e) {
            Record(new InstructionStateChange(e.InstructionIndex, e.Solved));
        }
        #endregion Dynamite

        #region Trigger
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e) {
            Record(new TriggerButtonPress(e.ButtonIndex, e.SequenceIndex, e.Correct));
        }
        #endregion Trigger

        #region Helper structures

        internal abstract class ActionLogEntry {
            public string Tag { get; }
            public string Scope { get; }
            public DateTime Timestamp { get; }

            protected ActionLogEntry(string tag, string scope) {
                Tag = tag;
                Scope = scope;
                Timestamp = DateTime.Now;
            }

            public string ToXml() {
                return string.Format("<{0} scope=\"{1}\" timestamp=\"{2}\" {3} />", Tag, Scope, Timestamp, AttributesXml());
            }

            protected abstract string AttributesXml();
        }

        internal class RoomStateTransition : ActionLogEntry {
            public RoomState From { get; }
            public RoomState To { get; }

            internal RoomStateTransition(RoomState oldState, RoomState newState) : base("RoomStateTransition", roomScope) {
                From = oldState;
                To = newState;
            }

            protected override string AttributesXml() {
                return string.Format("from=\"{0}\" to=\"{1}\"", From, To);
            }
        }

        internal class TechnicalFailuresNotification : ActionLogEntry {
            public List<string> AffectedComponents { get; }
            public List<string> AffectedGames { get; }

            internal TechnicalFailuresNotification(DiagnosticsReport report) : base("TechnicalFailureNotification", roomScope) {
                AffectedComponents = report.GetAffectedComponents();
                AffectedGames = report.GetAffectedGames();
            }

            protected override string AttributesXml() {
                string components = string.Join(", ", AffectedComponents);
                string games = string.Join(", ", AffectedGames);
                return string.Format("components=\"{0}\" games=\"{1}\"", components, games);
            }
        }

        internal class GameStateTransition : ActionLogEntry {
            public GameState From { get; }
            public GameState To { get; }

            internal GameStateTransition(string gameId, GameState oldState, GameState newState) : base("GameStateTransition", gameId) {
                From = oldState;
                To = newState;
            }

            protected override string AttributesXml() {
                return string.Format("from=\"{0}\" to=\"{1}\"", From, To);
            }
        }

        internal class GameInitialization : ActionLogEntry {
            public GameDifficulty Difficulty { get; }

            internal GameInitialization(string gameId, GameDifficulty difficulty) : base("GameInitialization", gameId) {
                Difficulty = difficulty;
            }

            protected override string AttributesXml() {
                return string.Format("difficulty=\"{0}\"", Difficulty);
            }
        }

        internal class FanTriggered : ActionLogEntry {
            public int PipeSystem { get; }
            public int Row { get; }
            public int PositionInRow { get; }
            public bool IsRunning { get; }

            internal FanTriggered(int index, int row, int positionInRow, bool isRunning) : base("FanTriggered", PipesGame.GameId) {
                PipeSystem = index;
                Row = row;
                PositionInRow = positionInRow;
                IsRunning = isRunning;
            }

            protected override string AttributesXml() {
                return string.Format("subsystem=\"{0}\" row=\"{1}\" position=\"{2}\" running=\"{3}\"", PipeSystem, Row, PositionInRow, IsRunning);
            }
        }

        internal class ValveStateChange : ActionLogEntry {
            public int PipeSystem { get; }
            public int Row { get; }
            public int PositionInRow { get; }
            public bool IsOpen { get; }

            internal ValveStateChange(int index, int row, int positionInRow, bool isOpen) : base("ValveTurned", PipesGame.GameId) {
                PipeSystem = index;
                Row = row;
                PositionInRow = positionInRow;
                IsOpen = isOpen;
            }

            protected override string AttributesXml() {
                return string.Format("subsystem=\"{0}\" row=\"{1}\" position=\"{2}\" open=\"{3}\"", PipeSystem, Row, PositionInRow, IsOpen);
            }
        }

        internal class CraneButtonPress : ActionLogEntry {
            public int ButtonIndex { get; }

            internal CraneButtonPress(int buttonIndex) : base("CraneButtonPress", CratesGame.GameId) {
                ButtonIndex = buttonIndex;
            }

            protected override string AttributesXml() {
                return string.Format("buttonIndex=\"{0}\"", ButtonIndex);
            }
        }

        internal class CraneButtonRelease : ActionLogEntry {
            public int Button { get; }

            internal CraneButtonRelease(int button) : base("CraneButtonRelease", CratesGame.GameId) {
                Button = button;
            }

            protected override string AttributesXml() {
                return string.Format("button=\"{0}\"", Button);
            }
        }

        internal class CratePickup : ActionLogEntry {
            public int CrateId { get; }
            public int CratesInDropzone { get; }

            internal CratePickup(int crateId, int cratesInDropzone) : base("CratePickup", CratesGame.GameId) {
                CrateId = crateId;
                CratesInDropzone = cratesInDropzone;
            }

            protected override string AttributesXml() {
                return string.Format("crate=\"{0}\" inDropzone=\"{1}\"", CrateId, CratesInDropzone);
            }
        }

        internal class CrateDrop : ActionLogEntry {
            public int CrateId { get; }
            public int CratesInDropzone { get; }

            internal CrateDrop(int crateId, int cratesInDropzone) : base("CrateDrop", CratesGame.GameId) {
                CrateId = crateId;
                CratesInDropzone = cratesInDropzone;
            }

            protected override string AttributesXml() {
                return string.Format("crate=\"{0}\" inDropzone=\"{1}\"", CrateId, CratesInDropzone);
            }
        }

        internal class StickInsertion : ActionLogEntry {
            public int HoleIndex { get; }
            public int StickIndex { get; }
            public int Holes { get; }
            public int Grooves { get; }
            public int Length { get; }
            public int Weight { get; }

            internal StickInsertion(int holeIndex, int stickIndex, int holes, int grooves, int length, int weight) : base("StickInsertion", CratesGame.GameId) {
                HoleIndex = holeIndex;
                StickIndex = stickIndex;
                Holes = holes;
                Grooves = grooves;
                Length = length;
                Weight = weight;
            }

            protected override string AttributesXml() {
                return string.Format("holeIndex=\"{0}\" stickIndex=\"{1}\" holes=\"{2}\" grooves=\"{3}\" length=\"{4}\" weight=\"{5}\"", HoleIndex, StickIndex, Holes, Grooves, Length, Weight);
            }
        }

        internal class StickRemoval : ActionLogEntry {
            public int HoleIndex { get; }
            public int StickIndex { get; }
            public int Holes { get; }
            public int Grooves { get; }
            public int Length { get; }
            public int Weight { get; }

            internal StickRemoval(int holeIndex, int stickIndex, int holes, int grooves, int length, int weight) : base("StickRemoval", DynamiteGame.GameId) {
                HoleIndex = holeIndex;
                StickIndex = stickIndex;
                Holes = holes;
                Grooves = grooves;
                Length = length;
                Weight = weight;
            }

            protected override string AttributesXml() {
                return string.Format("holeIndex=\"{0}\" stickIndex=\"{1}\" holes=\"{2}\" grooves=\"{3}\" length=\"{4}\" weight=\"{5}\"", HoleIndex, StickIndex, Holes, Grooves, Length, Weight);
            }
        }

        internal class InstructionStateChange : ActionLogEntry {
            public int InstructionIndex { get; }
            public bool Solved { get; }

            internal InstructionStateChange(int instructionIndex, bool solved) : base("InstructionStateChange", DynamiteGame.GameId) {
                InstructionIndex = instructionIndex;
                Solved = solved;
            }

            protected override string AttributesXml() {
                return string.Format("instruction=\"{0}\" solved=\"{1}\"", InstructionIndex, Solved);
            }
        }

        internal class TriggerButtonPress : ActionLogEntry {
            public int ButtonIndex { get; }
            public int SequenceIndex { get; }
            public bool Correct { get; }

            internal TriggerButtonPress(int buttonIndex, int sequenceIndex, bool correct) : base("TriggerButtonPress", TriggersGame.GameId) {
                ButtonIndex = buttonIndex;
                SequenceIndex = sequenceIndex;
                Correct = correct;
            }

            protected override string AttributesXml() {
                return string.Format("buttonIndex=\"{0}\" sequenceIndex=\"{1}\" correct=\"{2}\"", ButtonIndex, SequenceIndex, Correct);
            }
        }
        #endregion Helper structures

    }

}
