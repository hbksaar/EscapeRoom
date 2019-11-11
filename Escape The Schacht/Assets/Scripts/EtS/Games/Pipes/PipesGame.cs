using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EscapeTheSchacht.Pipes;
using System.Linq;
using System.Text;
using EscapeRoomFramework;

namespace EscapeTheSchacht.Pipes {

    public delegate void ValveTurnedHandler(PipesGame sender, ValveTurnedEventArgs args);
    public delegate void FanTriggeredHandler(PipesGame sender, FanTriggeredEventArgs args);

    public class ValveTurnedEventArgs : EventArgs {
        /// <summary>
        /// The pipe system the valve belongs to.
        /// </summary>
        public PipeSystem System { get; }

        /// <summary>
        /// The valve that has been turned.
        /// </summary>
        public Valve Valve { get; }

        internal ValveTurnedEventArgs(PipeSystem system, Valve valve) {
            System = system;
            Valve = valve;
        }
    }

    public class FanTriggeredEventArgs : EventArgs {
        /// <summary>
        /// The pipe system the fan belongs to.
        /// </summary>
        public PipeSystem System { get; }

        /// <summary>
        /// The fan that has been activated or deactivated.
        /// </summary>
        public Fan Fan { get; }

        internal FanTriggeredEventArgs(PipeSystem system, Fan fan) {
            System = system;
            Fan = fan;
        }
    }

    public class PipesGame : Game<IEtsInterface> {

        public const string GameId = "Pipes";
        public static readonly int GridWidth = 16;
        public static readonly int GridHeight = 6;

        public override string Id => GameId;

        /// <summary>
        /// The total number of valves in the game.
        /// </summary>
        public int ValveCount { get; private set; }
        /// <summary>
        /// The total number of fans in the game.
        /// </summary>
        public int FanCount { get; private set; }
        /// <summary>
        /// The total number of running fans in the game.
        /// </summary>
        public int RunningFansCount { get; private set; }

        private PipeSystem[] systems;

        /// <summary>
        /// Is raised when the state of a valve has changed.
        /// </summary>
        public event ValveTurnedHandler OnValveTurned;

        /// <summary>
        /// Is raised when the state of a fan has changed.
        /// </summary>
        public event FanTriggeredHandler OnFanTriggered;

        public PipesGame() {
            systems = new PipeSystem[] {
                PipeSystem.CreateV3e1(0),
                PipeSystem.CreateV3e2(1),
                PipeSystem.CreateV3e3(2),
                PipeSystem.CreateV3e4(3)
            };

            FanCount = ValveCount = 0;
            foreach (PipeSystem ps in systems) {
                FanCount += ps.FanCount;
                ValveCount += ps.ValveCount;
            }

            Log.Info("Pipes game set up.");
        }
        /*
        /// <summary>
        /// Returns the IPipeSystemInfo of the pipe system with the given index.
        /// </summary>
        public IPipeSystemInfo GetPipeSystemInfo(int index) {
            return systems[index];
        }
        */

        /// <summary>
        /// Iterates over all pipe systems.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<PipeSystem> PipeSystems() {
            return systems;
        }

        /// <summary>
        /// Iterates over all fans of the game.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Fan> Fans() {
            foreach (PipeSystem ps in systems)
                foreach (Fan f in ps.Fans())
                    yield return f;
        }

        /// <summary>
        /// Iterates over all valves of the game.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Valve> Valves() {
            foreach (PipeSystem ps in systems)
                foreach (Valve v in ps.Valves())
                    yield return v;
        }
        /*
        public IFanInfo GetFanInfo(int i) {
            foreach (PipeSystem ps in systems) {
                if (i < ps.fans.Count)
                    return ps.fans[i];
                i -= ps.fans.Count;
            }
            throw new ArgumentException(i.ToString() + " " + FanCount);
        }

        public IValveInfo GetValveInfo(int i) {
            foreach (PipeSystem ps in systems) {
                if (i < ps.valves.Count)
                    return ps.valves[i];
                i -= ps.valves.Count;
            }
            throw new ArgumentException(i.ToString() + " " + ValveCount);
        }
        */

        public override bool CompensateTechnicalFailures(DiagnosticsReport report, List<string> affectedComponents) {
            //if (affectedComponents.Count == 1 && affectedComponents[0] == "valves")
            //    return true;

            return false;
        }

        protected override bool Initialize0(GameDifficulty difficulty, DiagnosticsReport diagnosticsReport) {
            // difficulty is ignored for this game -> set to default
            Difficulty = GameDifficulty.Medium;
            
            // to set the logical valve states in the initialization of the subsystems we need up-to-date physical data
            Physical.ForcePipesUpdate();

            RunningFansCount = 0;
            foreach (PipeSystem pipes in systems) {
                // reset valves
                pipes.Initialize(Physical);
                RunningFansCount += pipes.RunningFansCount;

                // reset fans
                foreach (Fan f in pipes.Fans()) {
                    //bool oldState = physical.GetFanState(f.row, f.position);
                    Physical.SetFanState(f.Row, f.PositionInRow, f.IsRunning);
                }
            }

            return true;
        }
        
        protected override GameState Update0(float deltaTime) {
            bool allSolved = true;
            RunningFansCount = 0;
            foreach (PipeSystem ps in systems) {
                allSolved = allSolved & UpdatePipes(ps);
                RunningFansCount += ps.RunningFansCount;
            }

            if (allSolved)
                return GameState.Completed;

            return GameState.Running;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pipes"></param>
        /// <returns>true iff the pipe system is solved</returns>
        private bool UpdatePipes(PipeSystem pipes) {
			// solved systems cannot be manipulated
            if (pipes.RunningFansCount >= pipes.FanCount)
                return true;

            // update pipe system to get all manipulated valves (and fans with consequentially changed states)
            HashSet<Vertex> changedFansAndValves = pipes.Update(Physical);

            // notify observers about changes
            foreach (Vertex vertex in changedFansAndValves) {
                Log.Verbose("{0} state changed @ row={1} pos={2}", vertex.GetType(), vertex.Row, vertex.PositionInRow);
                if (vertex is Valve) {
                    OnValveTurned?.Invoke(this, new ValveTurnedEventArgs(pipes, (Valve) vertex));
                }
                else if (vertex is Fan)
                    OnFanTriggered?.Invoke(this, new FanTriggeredEventArgs(pipes, (Fan) vertex));
                else
                    throw new InvalidOperationException(vertex.Type.ToString());
            }

            bool systemSolved = pipes.RunningFansCount == pipes.FanCount;
            if (systemSolved)
                Log.Info("Pipe system {0} now solved.", pipes.Index);

            return systemSolved;
        }

    }
}