using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace EscapeRoomFramework {

    /// <summary>
    /// The various difficulty levels for a game.
    /// </summary>
    public enum GameDifficulty {
        Easy = 0, Medium = 1, Hard = 2
    }

    /// <summary>
    /// This is the base class for all games providing common infrastructure and verification 
    /// of pre- and postconditions of game state changes.
    /// </summary>
    public abstract class Game<TPhysicalInterface> where TPhysicalInterface: IPhysicalInterface {

        /// <summary>
        /// An id to identify the game. This id must be unique among all games. If there are 
        /// multiple instances of one game class, they must have different ids.
        /// </summary>
        public abstract string Id { get; }

        private GameState state = GameState.Uninitialized;
        /// <summary>
        /// The current state of the game.
        /// </summary>
        public GameState State {
            get { return state; }
            private set {
                if (value == state)
                    return;
                GameState previous = State;
                state = value;
                OnGameStateChanged?.Invoke(this, new GameStateChangedEventArgs(previous, value));
            }
        }

        /// <summary>
        /// The time in seconds this game has been in state <see cref="GameState.Running"/>. This value is reset to 0 on initialization.
        /// </summary>
        public float ElapsedTime { get; private set; }

        /// <summary>
        /// The difficulty level the game was initialized with.
        /// </summary>
        public GameDifficulty Difficulty { get; protected set; }

        /// <summary>
        /// The diagnostics report is created when the escape room is initialized and stored here to hand it to the game implementation 
        /// in <see cref="Initialize0(GameDifficulty, DiagnosticsReport)"/> for error compensation.
        /// </summary>
        internal DiagnosticsReport DiagnosticsReport { get; set; }

        /// <summary>
        /// This property contains a reference to the physical computing interface instance after the escape room has been initialized.
        /// </summary>
        protected internal TPhysicalInterface Physical { get; internal set; }

        public delegate void GameStateChangeHandler(Game<TPhysicalInterface> sender, GameStateChangedEventArgs args);
        /// <summary>
        /// Is raised after the game has transitioned from one state to another.
        /// </summary>
        public event GameStateChangeHandler OnGameStateChanged;

        protected Game() { }

        /// <summary>
        /// This method is called when the physical interface has detected technical failures in components that are 
        /// relevant for this game. The implementation should return <code>true</code> iff all failures can be compensated, 
        /// e.g. by adapting the logical game state in a way that the affected components can be safely ignored.
        /// </summary>
        /// <param name="report">the diagnostics report</param>
        /// <param name="affectedComponents">a list containing the identifiers of all failed components that are relevant for this game</param>
        /// <returns></returns>
        public abstract bool CompensateTechnicalFailures(DiagnosticsReport report, List<string> affectedComponents);

        /// <summary>
        /// Puts the game in the state <see cref="GameState.Uninitialized"/> for a new room run.
        /// </summary>
        /// <exception cref="IllegalGameStateTransitionException">if resetting the game is not allowed at this time</exception>
        public void Reset() {
            if (!State.CanTransition(GameState.Uninitialized))
                throw new IllegalGameStateTransitionException(State, GameState.Uninitialized);

            State = GameState.Uninitialized;
        }

        #region Initialize
        /// <summary>
        /// Initializes the game by forwarding the call to <see cref="Game{TPhysicalInterface}.Initialize0(GameDifficulty, DiagnosticsReport)"/> with the given difficulty and the 
        /// current diagnostics report. Also resets the time counter.
        /// </summary>
        /// <param name="difficulty">the target difficulty for the initialization</param>
        /// <exception cref="IllegalGameStateTransitionException">if initializing the game is not allowed at this time</exception>
        public void Initialize(GameDifficulty difficulty) {
			Log.Verbose("Initializing {0} game with difficulty {1}...", Id, difficulty);

            // precondition
            if (!State.CanTransition(GameState.Initialized)) 
                throw new IllegalGameStateTransitionException(State, GameState.Initialized);

            Difficulty = difficulty;
            ElapsedTime = 0f;

            try {
                bool success = Initialize0(difficulty, DiagnosticsReport);
                if (!success) {
                    State = GameState.Error;
                    return;
                }
            } catch (Exception e) {
                Log.Error("Game.Initialize0() raised an exception: " + e.Message);
                State = GameState.Error;
                return;
            }

            State = GameState.Initialized;

            Log.Info("{0} game with difficulty {1} initialized.", Id, difficulty);
        }

        /// <summary>
        /// This method is for the custom game implementation to do any necessary initialization work. See <see cref="Game{TPhysicalInterface}.Initialize(GameDifficulty)"/>-
        /// </summary>
        protected abstract bool Initialize0(GameDifficulty difficulty, DiagnosticsReport diagnosticsReport);
        #endregion Initialize

        #region Start
        /// <summary>
        /// Starts or continues the game. Only in the state <see cref="GameState.Running"/> the game's update method is allowed to be called.
        /// </summary>
        /// <exception cref="IllegalGameStateTransitionException">if starting the game is not allowed at this time</exception>
        public void Start() {
            Log.Verbose("Starting {0} game...", Id);

            // precondition
            if (!State.CanTransition(GameState.Running))
                throw new IllegalGameStateTransitionException(State, GameState.Running);

            try {
                Start0();
            } catch (Exception e) {
                Log.Error("Game.Start0() raised an exception: " + e.Message);
                State = GameState.Error;
                return;
            }
            State = GameState.Running;

            Log.Info("{0} game started.", Id);
        }

        /// <summary>
        /// This method is for the custom game implementation to do any necessary work for starting the game. See <see cref="Game{TPhysicalInterface}.Start"/>.
        /// </summary>
        protected virtual void Start0() { }
        #endregion Start

        #region Abort
        /// <summary>
        /// Stops the game and sets its state to <see cref="GameState.Aborted"/>.
        /// </summary>
        /// <exception cref="IllegalGameStateTransitionException">if aborting the game is not allowed at this time</exception>
        public void Abort() {
            Log.Verbose("Aborting {0} game...", Id);

            // precondition
            if (!State.CanTransition(GameState.Aborted))
                throw new IllegalGameStateTransitionException(State, GameState.Aborted);

            try {
                Abort0();
            } catch (Exception e) {
                Log.Error("Game.Abort0() raised an exception: " + e.Message);
                State = GameState.Error;
                return;
            }

            State = GameState.Aborted;

            Log.Info("{0} game aborted; elapsed time: {1}", Id, ElapsedTime);
        }

        /// <summary>
        /// This method is for the custom game implementation to do any necessary work for when the game is aborted. See <see cref="Game{TPhysicalInterface}.Abort"/>.
        /// </summary>
        protected virtual void Abort0() { }
        #endregion Abort

        #region Complete
        /// <summary>
        /// Stops the game and sets its state to <see cref="GameState.Completed"/>.
        /// </summary>
        /// <exception cref="IllegalGameStateTransitionException">if completing the game is not allowed at this time</exception>
        public void Complete() {
            Log.Verbose("Completing {0} game...", Id);

            // precondition
            if (!State.CanTransition(GameState.Completed))
                throw new IllegalGameStateTransitionException(State, GameState.Completed);

            try {
                Complete0();
            } catch (Exception e) {
                Log.Error("Game.Complete0() raised an exception: " + e.Message);
                State = GameState.Error;
                return;
            }

            State = GameState.Completed;

            Log.Info("{0} game completed; elapsed time: {1}", Id, ElapsedTime);
        }

        /// <summary>
        /// This method is for the custom game implementation to do any necessary work for when the game is completed. See <see cref="Game{TPhysicalInterface}.Complete"/>
        /// </summary>
        protected virtual void Complete0() { }
        #endregion Complete

        #region Update
        /// <summary>
        /// This method is called by the <see cref="EscapeRoom{TPhysicalInterface}"/> it's registered to when it's in the state <see cref="GameState.Running"/>. Returns
        /// the state of the game after the current update cycle as provided by <see cref="Game{TPhysicalInterface}.Update0(float)"/>.
        /// </summary>
        /// <param name="deltaTime">the number of seconds which have elapsed since the last update</param>
        /// <exception cref="IllegalGameStateTransitionException">if entering the state returned by <see cref="Game{TPhysicalInterface}.Update0(float)"/> is not allowed at this time</exception>
        /// <exception cref="InvalidOperationException">if the game is not in the state <see cref="GameState.Running"/></exception>
        internal GameState Update(float deltaTime) {
            // precondition
            if (State != GameState.Running)
                throw new InvalidOperationException("Invalid game state: " + State); 

            ElapsedTime += deltaTime;
            GameState newState;
            try {
                newState = Update0(deltaTime);
            } catch (Exception e) {
                Log.Error("Game.Update0() raised an exception: " + e.Message);
                newState = GameState.Error;
            }

            // postcondition
            if (newState != GameState.Running && !States.CanTransition(GameState.Running, newState))
                throw new IllegalGameStateTransitionException(State, newState);

            if (State == GameState.Completed)
                Log.Info("{0} game completed; elapsed time: ", Id, ElapsedTime);
            if (State == GameState.Aborted)
                Log.Info("{0} game aborted; elapsed time: ", Id, ElapsedTime);
            if (State == GameState.Error)
                Log.Error("{0} game returned error state in last Update0()", Id);

            State = newState;
            return State;
        }

        /// <summary>
        /// This method is for the custom game implementation to do any necessary work in the update cycle. It must return the state the game is supposed to be in after the update.
        /// </summary>
        /// <param name="deltaTime">the number of seconds which have elapsed since the last update</param>
        protected abstract GameState Update0(float deltaTime);
        #endregion Update

    }

}