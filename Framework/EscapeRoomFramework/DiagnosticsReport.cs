using System;
using System.Collections.Generic;

namespace EscapeRoomFramework {

    /// <summary>
    /// Event arguments type for the <see cref="EscapeRoom{TPhysicalInterface}.OnDiagnosticsReportCreated"/> event.
    /// </summary>
    public class DiagnosticsReportCreatedEventArgs : EventArgs {
        /// <summary>
        /// The diagnostics report that has been created.
        /// </summary>
        public DiagnosticsReport Report { get; }

        internal DiagnosticsReportCreatedEventArgs(DiagnosticsReport report) {
            Report = report;
        }
    }

    /// <summary>
    /// The diagnostics report is created by the <see cref="EscapeRoom{TPhysicalInterface}"/> in its initialization phase and filled 
    /// in by the <see cref="IPhysicalInterface"/> implementation to record any problems with physical computing 
    /// components. If there are any problems the report is then forwarded to all registered game instances to
    /// allow them to circumvent the problems if possible.
    /// </summary>
    public class DiagnosticsReport {

        /// <summary>
        /// Signals whether technical failures have been detected. If this is <code>false</code>, the report can 
        /// be safely ignored.
        /// </summary>
        public bool FailuresDetected { get; private set; }

        /// <summary>
        /// The number of failed components.
        /// </summary>
        internal int AffectedComponentsCount => affectedComponents.Count;

        /// <summary>
        /// The number of games affected by failed components.
        /// </summary>
        internal int AffectedGamesCount => affectedGames.Count;

        private bool criticalRoomComponentAffected;

        private HashSet<string> affectedComponents = new HashSet<string>(); // <component id>
        private Dictionary<string, HashSet<string>> affectedGames = new Dictionary<string, HashSet<string>>(); // <game id, <component id>>
        private HashSet<string> compensatedGames = new HashSet<string>(); // <game id>

        public DiagnosticsReport() { }

        /// <summary>
        /// Clears this diagnostics report for reuse.
        /// </summary>
        internal void Clear() {
            FailuresDetected = false;
            criticalRoomComponentAffected = false;
            affectedComponents.Clear();
            affectedGames.Clear();
            compensatedGames.Clear();
        }

        /// <summary>
        /// Adds a broken physical computing component belonging to the room to the report.
        /// </summary>
        /// <param name="component">an identifier for the component</param>
        /// <param name="critical">indicates that the component is critical and the room cannot continue as intended</param>
        public void AddAffectedRoomComponent(string component, bool critical = false) {
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            affectedComponents.Add(component);
            criticalRoomComponentAffected |= critical;
            FailuresDetected = true;
        }

        /// <summary>
        /// Adds a broken physical computing component belonging to one of the games to the report.
        /// </summary>
        /// <param name="game">the game the component belongs to</param>
        /// <param name="component">an identifier for the component</param>
        public void AddAffectedGameComponent<TPhysicalInterface>(Game<TPhysicalInterface> game, string component) where TPhysicalInterface : IPhysicalInterface {
            if (game == null)
                throw new ArgumentNullException(nameof(game));

            AddAffectedGameComponent(game.Id, component);
        }

        /// <summary>
        /// Adds a broken physical computing component belonging to one of the games to the report.
        /// </summary>
        /// <param name="game">the id of the game the component belongs to</param>
        /// <param name="component">an identifier for the component</param>
        public void AddAffectedGameComponent(string gameId, string component) {
            if (gameId == null)
                throw new ArgumentNullException(nameof(gameId));
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            if (!affectedGames.ContainsKey(gameId))
                affectedGames[gameId] = new HashSet<string>();

            affectedComponents.Add(component);
            affectedGames[gameId].Add(component);

            FailuresDetected = true;
        }

        /// <summary>
        /// Returns <code>true</code> iff there are defective components belonging to the given game.
        /// </summary>
        /// <param name="game">the scope of the components to check</param>
        public bool IsAffected<TPhysicalInterface>(Game<TPhysicalInterface> game) where TPhysicalInterface: IPhysicalInterface {
            if (game == null)
                throw new ArgumentNullException(nameof(game));

            return affectedGames.ContainsKey(game.Id);
        }

        /// <summary>
        /// Returns <code>true</code> iff a component with the given identifier is defective.
        /// </summary>
        /// <param name="component">the identifier of a component</param>
        public bool IsAffected(string component) {
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            return affectedComponents.Contains(component);
        }

        /// <summary>
        /// Returns a list of the identifiers of all broken components belonging to the given game.
        /// </summary>
        /// <param name="game">the scope of the components to check</param>
        public List<string> GetAffectedComponents<TPhysicalInterface>(Game<TPhysicalInterface> game) where TPhysicalInterface : IPhysicalInterface {
            if (game == null)
                throw new ArgumentNullException(nameof(game));

            return new List<string>(affectedGames[game.Id]);
        }
        
        /// <summary>
        /// Signals to the report (and thus to the room) that the broken components of the given game are not critical. This means the game can work 
        /// around the broken components in a way such that the room can continue.
        /// </summary>
        /// <param name="game">the game to signal compensation for</param>
        internal void GameCompensated<TPhysicalInterface>(Game<TPhysicalInterface> game) where TPhysicalInterface : IPhysicalInterface {
            if (game == null)
                throw new ArgumentNullException(nameof(game));

            compensatedGames.Add(game.Id);
        }

        /// <summary>
        /// Returns <code>true</code> iff the room can continue as planned, i.e. there are either none or no critical failed components and all affected games can circumvent any broken components.
        /// </summary>
        internal bool CanContinue() {
            if (criticalRoomComponentAffected)
                return false;

            if (affectedGames.Count > compensatedGames.Count)
                return false;

            HashSet<string> uncompensatedGames = new HashSet<string>(affectedGames.Keys);
            uncompensatedGames.ExceptWith(compensatedGames);
            return uncompensatedGames.Count == 0;
        }

        /// <summary>
        /// Returns a list containing the identifiers of all failed components.
        /// </summary>
        /// <returns></returns>
        public List<string> GetAffectedComponents() {
            return new List<string>(affectedComponents);
        }

        /// <summary>
        /// Returns a list containing the ids of all affected games.
        /// </summary>
        /// <returns></returns>
        public List<string> GetAffectedGames() {
            return new List<string>(affectedGames.Keys);
        }

    }

}