using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EscapeRoomFramework {

    #region RoomState
    /// <summary>
    /// The states the room can be in.
    /// </summary>
    public enum RoomState {
        Uninitialized, Initialized, Running, Completed, Aborted, Error
    }

    /// <summary>
    /// Event arguments type for the <see cref="EscapeRoom{TPhysicalInterface}.OnRoomStateChanged"/> event.
    /// </summary>
    public class RoomStateChangedEventArgs : EventArgs {

        /// <summary>
        /// The previous state.
        /// </summary>
        public RoomState OldState { get; }

        /// <summary>
        /// The state after the change.
        /// </summary>
        public RoomState NewState { get; }

        /// <summary>
        /// The diagnostics report with details about any detected failures. It is passed in every 
        /// <see cref="EscapeRoom{TPhysicalInterface}.OnRoomStateChanged"/> event, but only updated 
        /// in the initialization phase of the room. Therefore it should be checked whenever the room 
        /// enters <see cref="RoomState.Initialized"/>. See also <seealso cref="EscapeRoom{TPhysicalInterface}.Initialize"/>.
        /// </summary>
        public DiagnosticsReport DiagnosticsReport { get; }

        internal RoomStateChangedEventArgs(RoomState oldState, RoomState newState, DiagnosticsReport report) {
            Debug.Assert(report != null, "report is null");
            OldState = oldState;
            NewState = newState;
            DiagnosticsReport = report;
        }

    }

    /// <summary>
    /// This exception is thrown when the room attempts an illegal <see cref="RoomState"/> transition. See <see cref="States.CanTransition(RoomState, RoomState)"/>.
    /// </summary>
    public class IllegalRoomStateTransitionException : Exception {
       
        /// <summary>
        /// The previous state.
        /// </summary>
        public RoomState OldState { get; }

        /// <summary>
        /// The target state of the attempted transition.
        /// </summary>
        public RoomState NewState { get; }

        public IllegalRoomStateTransitionException(RoomState from, RoomState to) : base($"Invalid room state transition from {from.ToString()} to {to.ToString()}.") {
            OldState = from;
            NewState = to;
        }

    }
    #endregion RoomState

    #region GameState
    /// <summary>
    /// The states a game can be in.
    /// </summary>
    public enum GameState {
        Uninitialized, Initialized, Running, Completed, Aborted, Error
    }

    /// <summary>
    /// Event arguments type for the <see cref="Game{TPhysicalInterface}.OnGameStateChanged"/> event.
    /// </summary>
    public class GameStateChangedEventArgs : EventArgs {

        /// <summary>
        /// The previous state.
        /// </summary>
        public GameState OldState { get; }

        /// <summary>
        /// The state after the change.
        /// </summary>
        public GameState NewState { get; }

        internal GameStateChangedEventArgs(GameState oldState, GameState newState) {
            OldState = oldState;
            NewState = newState;
        }

    }

    /// <summary>
    /// This exception is thrown when a game attempts an illegal <see cref="GameState"/> transition. See <see cref="States.CanTransition(GameState, GameState)"/>.
    /// </summary>
    public class IllegalGameStateTransitionException : Exception {

        /// <summary>
        /// The previous state.
        /// </summary>
        public GameState OldState { get; }

        /// <summary>
        /// The target state of the attempted transition.
        /// </summary>
        public GameState NewState { get; }

        public IllegalGameStateTransitionException(GameState from, GameState to) : base($"Invalid game state transition from {from.ToString()} to {to.ToString()}.") {
            OldState = from;
            NewState = to;
        }
    }

    #endregion GameState

    #region Transition rules
    /// <summary>
    /// Contains methods to enforce the transition rules for <see cref="RoomState"/> and <see cref="GameState"/>.
    /// </summary>
    public static class States {

        /// <summary>
        /// Returns <code>true</code> iff the room state transition specified by the given arguments is valid.
        /// </summary>
        /// <param name="from">the origin room state</param>
        /// <param name="to">the target room state</param>
        public static bool CanTransition(this RoomState from, RoomState to) {
            switch (to) {
            case RoomState.Uninitialized:
                return false;
            case RoomState.Initialized:
                return from == RoomState.Uninitialized || from == RoomState.Completed || from == RoomState.Aborted;
            case RoomState.Running:
                return from == RoomState.Initialized || from == RoomState.Running;
            case RoomState.Completed:
            case RoomState.Aborted:
                return from == RoomState.Running;
            case RoomState.Error:
                return true;
            default:
                throw new NotImplementedException(to.ToString());
            }
        }

        /// <summary>
        /// Returns <code>true</code> iff the game state transition specified by the given arguments is valid.
        /// </summary>
        /// <param name="from">the origin game state</param>
        /// <param name="to">the target game state</param>
        /// <returns></returns>
        public static bool CanTransition(this GameState from, GameState to) {
            switch (to) {
            case GameState.Uninitialized:
                return from == GameState.Uninitialized || from == GameState.Completed || from == GameState.Aborted;
            case GameState.Initialized:
                return from == GameState.Uninitialized || from == GameState.Initialized;
            case GameState.Running:
                return from == GameState.Initialized;
            case GameState.Completed:
            case GameState.Aborted:
                return from == GameState.Running;
            case GameState.Error:
                return true;
            default:
                throw new NotImplementedException(to.ToString());
            }
        }

    }
    #endregion Transition rules

}
