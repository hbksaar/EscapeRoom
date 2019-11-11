using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace EscapeRoomFramework {

    /// <summary>
    /// This is the main class of the framework. Once it is instantiated under specification of the <see cref="IPhysicalInterface"/> implementation, 
    /// game instances and modules must be registered using <see cref="EscapeRoom{TPhysicalInterface}.RegisterGame{TGame}(TGame)"/> and 
    /// <see cref="EscapeRoom{TPhysicalInterface}.RegisterModule{TModule}(TModule)"/>, respectively. After that, the room can be initialized and 
    /// started by calling <see cref="EscapeRoom{TPhysicalInterface}.Initialize"/> and <see cref="EscapeRoom{TPhysicalInterface}.Start"/> in that order.
    /// </summary>
    /// <typeparam name="TPhysicalInterface">the type of the custom physical computing interface implementation</typeparam>
    public class EscapeRoom<TPhysicalInterface> where TPhysicalInterface : IPhysicalInterface {

        private RoomState state = RoomState.Uninitialized;
        /// <summary>
        /// The current state of the room.
        /// </summary>
        public RoomState State {
            get { return state; }
            private set {
                RoomState previous = state;
                state = value;
                OnRoomStateChanged?.Invoke(this, new RoomStateChangedEventArgs(previous, value, DiagnosticsReport));
            }
        }

        /// <summary>
        /// The time in seconds the room has been in state <see cref="RoomState.Running"/>. This value is reset to 0 on initialization.
        /// </summary>
        public float ElapsedTime { get; private set; }

        /// <summary>
        /// A reference to the physical computing interface implementation.
        /// </summary>
        public TPhysicalInterface Physical { get; }

        /// <summary>
        /// The time of the last initialization. This is <code>null</code> before the first initialization.
        /// </summary>
        public DateTime? InitializationTime { get; private set; }

        /// <summary>
        /// The time the room has been started most recently.
        /// </summary>
        public DateTime? StartTime { get; private set; }

        private readonly Dictionary<string, Game<TPhysicalInterface>> gamesById = new Dictionary<string, Game<TPhysicalInterface>>();
        private readonly Dictionary<Type, Game<TPhysicalInterface>> gamesByType = new Dictionary<Type, Game<TPhysicalInterface>>(); // filled lazily in GetGame<TGame>()
        private readonly Dictionary<Type, IModule<TPhysicalInterface>> modulesbyType = new Dictionary<Type, IModule<TPhysicalInterface>>();

        private readonly List<Game<TPhysicalInterface>> gamesInRegisteredOrder = new List<Game<TPhysicalInterface>>();
        private readonly List<IModule<TPhysicalInterface>> modulesInRegisteredOrder = new List<IModule<TPhysicalInterface>>();

        /// <summary>
        /// The diagnostics report. This always refers to the same instance. However, the report is cleared on each room initialization.
        /// </summary>
        public DiagnosticsReport DiagnosticsReport { get; } = new DiagnosticsReport();

        public delegate void RoomStateChangeHandler(EscapeRoom<TPhysicalInterface> sender, RoomStateChangedEventArgs args);
        /// <summary>
        /// Is raised after the room has transition from one state to another.
        /// </summary>
        public event RoomStateChangeHandler OnRoomStateChanged;

        public delegate void DiagnosticsReportCreatedHandler(TPhysicalInterface sender, DiagnosticsReportCreatedEventArgs args);
        /// <summary>
        /// Is raised after the diagnostics report has been created in the room initialization step.
        /// </summary>
        public event DiagnosticsReportCreatedHandler OnDiagnosticsReportCreated;

        /// <summary>
        /// Creates a new instance with the given <typeparamref name="TPhysicalInterface"/>.
        /// </summary>
        /// <param name="physical">the custom physical computing interface implementation the <see cref="EscapeRoom{TPhysicalInterface}"/> and 
        /// all registered <see cref="Game{TPhysicalInterface}"/> instances will use.</param>
        /// <exception cref="ArgumentNullException">if <paramref name="physical"/> is <code>null</code></exception>
        public EscapeRoom(TPhysicalInterface physical) { 
            if (physical == null)
                throw new ArgumentNullException(nameof(physical));

            Physical = physical;
        }

        /// <summary>
        /// This method adds the given game to the escape room. There are multiple instances of each type <typeparamref name="TGame"/> allowed as long as their <see cref="Game{TPhysicalInterface}.Id"/>s are unique.
        /// Note that all games must be registered before any module is registered. If you attempt to add a game after any modules have been added, an exception will be thrown.
        /// </summary>
        /// <param name="game">the game to add</param>
        /// <exception cref="InvalidOperationException">if there are already modules registered with the room</exception>
        /// <exception cref="ArgumentNullException">if <paramref name="game"/> is <code>null</code></exception>
        /// <exception cref="ArgumentException">if there is another game with the same id already registered with the room</exception>
        public void RegisterGame<TGame>(TGame game) where TGame : Game<TPhysicalInterface> {
            if (modulesbyType.Count > 0)
                throw new InvalidOperationException("No games can be added at this point because there are already modules registered.");
            if (game == null)
                throw new ArgumentNullException(nameof(game));
            if (gamesById.ContainsKey(game.Id))
                throw new ArgumentException("Another game with the same id '" + game.Id + "' has already been registered.");

            game.DiagnosticsReport = DiagnosticsReport;
            game.Physical = Physical;
            gamesInRegisteredOrder.Add(game);
            gamesById[game.Id] = game;
        }

        /// <summary>
        /// Returns the one and only game instance of type <typeparamref name="TGame"/> that is registered with the room.
        /// Beware: Calling this method for a type <typeparamref name="TGame"/> of which multiple instances are registered with the room will result in an exception!
        /// </summary>
        /// <typeparam name="TGame">the type of the game to retrieve</typeparam>
        /// <exception cref="InvalidOperationException">if there are multiple games of the type <typeparamref name="TGame"/> registered with the room</exception>
        /// <exception cref="ArgumentException">if there is no game of the type <typeparamref name="TGame"/> registered with the room</exception>
        public TGame GetGame<TGame>() where TGame : Game<TPhysicalInterface> {
            // lazy initialization of the games-by-type list to prevent invalid usage
            // yes, this is not the best design choice, but boy do we go to great lengths for a bit of convenience (which is having this method)
            if (!gamesByType.ContainsKey(typeof(TGame))) {
                foreach (Game<TPhysicalInterface> game in gamesById.Values)
                    if (game is TGame) {
                        if (gamesByType.ContainsKey(typeof(TGame)))
                            throw new InvalidOperationException("Invalid method call: There are multiple games of type " + typeof(TGame) + ". Use GetGame(string).");
                        gamesByType[typeof(TGame)] = game;
                    }
            }

            // no game of this type? exception!
            if (!gamesByType.ContainsKey(typeof(TGame)))
                throw new ArgumentException("There is no game registered of the type '" + typeof(TGame) + "'.");

            // double check if what we retrieve actually is what we want
            Game<TPhysicalInterface> result = gamesByType[typeof(TGame)];
            Debug.Assert(result is TGame, "Game is not of type " + typeof(TGame) + " as registered.");

            return (TGame) result;
        }

        /// <summary>
        /// Returns the game instance with the given id.
        /// </summary>
        /// <param name="gameId">the id of the game to retrieve</param>
        /// <exception cref="ArgumentException">if there is no game with the given id registered with the room</exception>
        public Game<TPhysicalInterface> GetGame(string gameId) {
            if (!gamesById.ContainsKey(gameId))
                throw new ArgumentException("There is no game registered with the id '" + gameId + "'.");

            return gamesById[gameId];
        }

        /// <summary>
        /// Iterates over all registered games.
        /// </summary>
        public IEnumerable<Game<TPhysicalInterface>> GetGames() {
            return gamesById.Values;
        }

        /// <summary>
        /// Adds the given module to the room. Note that all games must be registered before any module is registered with the room.
        /// Only one module per type <typeparamref name="TModule"/> is allowed.
        /// </summary>
        /// <param name="module">the module to add</param>
        /// <exception cref="ArgumentNullException">if <paramref name="module"/> is <code>null</code></exception>
        /// <exception cref="ArgumentException">if there is another module of the same type already registered with the room</exception>
        public void RegisterModule<TModule>(TModule module) where TModule : IModule<TPhysicalInterface> {
            if (module == null)
                throw new ArgumentNullException(nameof(module));
            if (modulesbyType.ContainsKey(typeof(TModule)))
                throw new ArgumentException("A module of the same type '" + typeof(TModule) + "' has already been registered.");

            OnRoomStateChanged += module.OnRoomStateChanged;
            foreach (Game<TPhysicalInterface> game in gamesInRegisteredOrder)
                game.OnGameStateChanged += module.OnGameStateChanged;
            module.Setup(this, Physical);
            modulesbyType[typeof(TModule)] = module;
        }

        /// <summary>
        /// Returns the module of the given type <typeparamref name="TModule"/>.
        /// </summary>
        /// <typeparam name="TModule">the type of the module to retrieve</typeparam>
        /// <exception cref="ArgumentException">if there is no module of the given type</exception>
        public TModule GetModule<TModule>() where TModule : IModule<TPhysicalInterface> {
            if (!modulesbyType.ContainsKey(typeof(TModule)))
                throw new ArgumentException("There is no module registered of the type '" + typeof(TModule) + "'.");

            IModule<TPhysicalInterface> result = modulesbyType[typeof(TModule)];
            Debug.Assert(result is TModule, "Module is not of type " + typeof(TModule) + " as registered.");
            return (TModule) result;
        }

        /// <summary>
        /// This method is to be called regularly by the application's event loop once the room has been started (see <see cref="EscapeRoom{TPhysicalInterface}.Start"/>).
        /// </summary>
        /// <param name="deltaTime">the time in seconds since the last method call</param>
        /// <exception cref="InvalidOperationException">if the room is not in the state <see cref="RoomState.Running"/></exception>
        public void Update(float deltaTime) {
            if (State != RoomState.Running)
                throw new InvalidOperationException("Room is not running.");

            ElapsedTime += deltaTime;

            // update the physical interface
            Physical.PhysicalUpdate(deltaTime);

            // update running games
            foreach (Game<TPhysicalInterface> game in gamesInRegisteredOrder) {
                if (game.State == GameState.Running) {
                    GameState gameState = game.Update(deltaTime);
                    if (gameState == GameState.Error) {
                        State = RoomState.Error;
                        return;
                    }
                }
            }

            // update modules
            foreach (IModule<TPhysicalInterface> module in modulesInRegisteredOrder)
                module.Update(deltaTime);            
        }

        #region Initialize
        /// <summary>
        /// Call this method to initialize the room after all games and modules have been added. This resets all games and time counters, 
        /// (re)opens the physical computing interface and clears and commissions the diagnostics report. If any failures are detected, 
        /// the report is given to the affected games for failure compensation. If this is not possible, the room enters the state 
        /// <see cref="RoomState.Error"/>. Otherwise the method concludes with the room being in the state <see cref="RoomState.Initialized"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">if there are no games registered with the room</exception>
        /// <exception cref="IllegalRoomStateTransitionException">if initializing the room is not allowed at this time</exception>
        public void Initialize() {
            if (gamesInRegisteredOrder.Count == 0)
                throw new InvalidOperationException("Please register at least one game before initializing the room.");

            Log.Verbose("Initializing room...");

            InitializationTime = DateTime.Now;
            StartTime = null;
            State = RoomState.Uninitialized;

            // precondition
            if (!State.CanTransition(RoomState.Initialized))
                throw new IllegalRoomStateTransitionException(State, RoomState.Initialized);

            foreach (Game<TPhysicalInterface> game in gamesInRegisteredOrder)
                Debug.Assert(game.State != GameState.Running, "EscapeRoom.ResetRoom: game is running");

            Log.Verbose("Resetting games...");
            ElapsedTime = 0f;
            foreach (Game<TPhysicalInterface> game in gamesInRegisteredOrder)
                game.Reset();

            Log.Verbose("(Re)starting physical interface...");
            if (Physical.IsOpen)
                Physical.Close();
            try {
                Physical.Open();
            } catch (Exception e) {
                Log.Error(e);
                Log.Error("Error on opening physical interface. Room initialization discontinued.");
                State = RoomState.Error;
                return;
            }

            DiagnosticsReport.Clear();
            Physical.RunDiagnostics(DiagnosticsReport);

            if (DiagnosticsReport.FailuresDetected) {
                List<string> affectedGames = DiagnosticsReport.GetAffectedGames();
                foreach (string gameId in affectedGames) {
                    Game<TPhysicalInterface> game = gamesById[gameId];
                    if (DiagnosticsReport.IsAffected(game)) {
                        if (game.CompensateTechnicalFailures(DiagnosticsReport, DiagnosticsReport.GetAffectedComponents(game)))
                            DiagnosticsReport.GameCompensated(game);
                    }
                }

                OnDiagnosticsReportCreated?.Invoke(Physical, new DiagnosticsReportCreatedEventArgs(DiagnosticsReport));

                if (!DiagnosticsReport.CanContinue()) {
                    Log.Error("Physical interface has reported {0} technical failures, some critical. Room initialization discontinued.", DiagnosticsReport.AffectedComponentsCount);
                    State = RoomState.Error;
                    return;
                }
                else
                    Log.Warn("Physical interface has reported {0} technical failures, none critical. Room initialization continues.", DiagnosticsReport.AffectedComponentsCount);
            }

            Log.Info("Room initialized.");
            State = RoomState.Initialized;
        }
        #endregion Initialize

        #region Start
        /// <summary>
        /// Starts the room. Only when running, the room's update is allowed to be called.
        /// </summary>
        /// <exception cref="IllegalRoomStateTransitionException">if starting the room is not allowed at this time</exception>
        public void Start() {
            StartTime = DateTime.Now;

            Log.Verbose("Starting room...");

            // precondition
            if (!State.CanTransition(RoomState.Running))
                throw new IllegalRoomStateTransitionException(State, RoomState.Running);

            State = RoomState.Running;

            Log.Info("Room started.");
        }
        #endregion Start

        #region Abort
        /// <summary>
        /// Stops the room and sets its state to <see cref="RoomState.Aborted"/>. This also aborts all currently running games.
        /// </summary>
        /// <exception cref="IllegalRoomStateTransitionException">if aborting the room is not allowed at this time.</exception>
        public void Abort() {
            Log.Verbose("Aborting room...");

            // precondition
            if (!State.CanTransition(RoomState.Aborted))
                throw new IllegalRoomStateTransitionException(State, RoomState.Aborted);

            foreach (Game<TPhysicalInterface> game in gamesInRegisteredOrder)
                if (game.State == GameState.Running)
                    game.Abort();

            State = RoomState.Aborted;

            Log.Info("Room aborted.");
        }
        #endregion Abort

        #region Complete
        /// <summary>
        /// Stops the room and sets its state to <see cref="RoomState.Completed"/>. This also completes all currently running games.
        /// </summary>
        /// <exception cref="IllegalRoomStateTransitionException">if completing the room is not allowed at this time.</exception>
        public void Complete() {
            Log.Verbose("Completing room...");

            // precondition
            if (!State.CanTransition(RoomState.Completed))
                throw new IllegalRoomStateTransitionException(State, RoomState.Completed);

            foreach (Game<TPhysicalInterface> game in gamesInRegisteredOrder)
                if (game.State == GameState.Running)
                    game.Complete();

            State = RoomState.Completed;

            Log.Info("Room completed.");
        }
        #endregion Complete
        
    }

}