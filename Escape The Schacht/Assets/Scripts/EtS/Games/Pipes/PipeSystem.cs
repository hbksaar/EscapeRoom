using QuickGraph;
using System.Collections.Generic;
using System;
using UnityEngine;
using EscapeRoomFramework;

namespace EscapeTheSchacht.Pipes {

    public class PipeSystem {

        public UndirectedGraph<Vertex, Edge> graph = new UndirectedGraph<Vertex, Edge>();

        private List<Inlet> inlets = new List<Inlet>();
        private List<Outlet> outlets = new List<Outlet>();
        private List<Valve> valves = new List<Valve>();
        private List<Fan> fans = new List<Fan>();

        private Valve[] openValvesAtStart;

        private System.Random r = new System.Random();

        /// <summary>
        /// The index of the pipe system.
        /// </summary>
        public int Index { get; private set; }
        /// <summary>
        /// The number of valves in the pipe system.
        /// </summary>
        public int ValveCount => valves.Count;
        /// <summary>
        /// The number of fans in the pipe system.
        /// </summary>
        public int FanCount => fans.Count;
        /// <summary>
        /// The number of running fans in the pipe system.
        /// </summary>
        public int RunningFansCount { get; private set; }

        private PipeSystem(int index, Vertex[] vertices, Vertex[][] edges) : this(index, vertices, edges, null) { }

        private PipeSystem(int index, Vertex[] vertices, Vertex[][] edges, Valve[] openValvesAtStart) {
            Debug.Assert(edges.Length == vertices.Length, "Parameter edges must specify edges for all vertices (even if the list is empty)");

            Index = index;

            graph.AddVertexRange(vertices);

            foreach (Vertex[] vEdges in edges)
                for (int i = 1; i < vEdges.Length; i++)
                    graph.AddEdge(new Edge(vEdges[0], vEdges[i]));

            foreach (Vertex vertex in vertices) {
                if (vertex is Fan) {
                    Fan f = (Fan) vertex;
                    f.PipeSystem = this;
                    fans.Add(f);
                }
                if (vertex is Valve) {
                    Valve v = (Valve) vertex;
                    v.PipeSystem = this;
                    valves.Add(v);
                }
                if (vertex is Inlet) {
                    Inlet i = (Inlet) vertex;
                    i.PipeSystem = this;
                    inlets.Add(i);
                }
                if (vertex is Outlet) {
                    Outlet o = (Outlet) vertex;
                    o.PipeSystem = this;
                    outlets.Add(o);
                }
            }

            this.openValvesAtStart = openValvesAtStart;

            Log.Verbose("Created pipe system with {0} inlets, {1} outlets, {2} valves and {3} fans.", inlets.Count, outlets.Count, valves.Count, fans.Count);
        }

        /// <summary>
        /// Iterates over all fans of the pipe system.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Fan> Fans() {
            return fans;
        }

        /// <summary>
        /// Iterates over all valves of the pipe system.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Valve> Valves() {
            return valves;
        }

        /// <summary>
        /// Iterates over all inlets of the pipe system.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Inlet> Inlets() {
            return inlets;
        }

        /// <summary>
        /// Iterates over all outlets of the pipe system.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Outlet> Outlets() {
            return outlets;
        }

        /// <summary>
        /// This resets the valves to be open or closed according to the predefined initial configuration, or a random
        /// state with no running fans if no preset was specified.
        /// To determine the logical closed state of a valve, its current physical state is read for reference. The
        /// logical closed state is then adjusted to match or deviate from the current physical states.
        /// See <see cref="Valve.IsOpen"/>.
        /// </summary>
        /// <param name="physical">the physical interface to get the physical valve states from</param>
        public void Initialize(IEtsInterface physical) {
            Log.Verbose("Initializing pipe system " + Index);

            // store the current physical states of each valve for reference
            foreach (Valve v in valves) {
                v.currentPhysicalState = physical.GetValveState(v.Row, v.PositionInRow);
                //Log.Debug("Initialized valve with physical state " + v.currentPhysicalState);
            }
            
            // if the set of open valves is predefined: apply the preset
            if (openValvesAtStart != null) {
                foreach (Valve v in valves) {
                    // if the valve is not reported as broken by the physical interface, set the logical closed state as intended
                    if (!physical.IsValveBroken(v.Row, v.PositionInRow))
                        v.logicalClosedState = openValvesAtStart.Contains(v) ? !v.currentPhysicalState : v.currentPhysicalState;

                    // otherwise, override it such that the system can be solved without turning this valve (i.e. closed iff the valve is connected to an outlet)
                    else {
                        bool connectedToOutlet = false;
                        foreach (Edge e in graph.AdjacentEdges(v))
                            if (e.GetOtherVertex((Vertex) v) is Outlet)
                                connectedToOutlet = true;
                        v.logicalClosedState = connectedToOutlet ? v.currentPhysicalState : !v.currentPhysicalState;
                    }
                }
                RunningFansCount = findCorrectlyConnectedFans().Count;
            }
            // otherwise: set up a random configuration with no running fans
            else {
                int runningFans = 1;
                while (runningFans > 0) {
                    foreach (Valve v in valves)
                        v.currentPhysicalState = r.Next(0, 1) == 1;
                    RunningFansCount = findCorrectlyConnectedFans().Count;
                    if (RunningFansCount > 0)
                        Log.Debug("Discarding initial valves configuration with {0} running fans.", runningFans);
                }
            }
        }
        
        public HashSet<Vertex> Update(IEtsInterface physical) {
            HashSet<Vertex> changed = new HashSet<Vertex>();
            
            // update valves by querying physical interface
            foreach (Valve v in valves) {
                bool newState = physical.GetValveState(v.Row, v.PositionInRow);
                //Log.Debug("Pipes: Querying physical state of valve @ (row={0}, pos={1}) -> {2} (previous was {3})", v.row, v.position, newState, v.currentPhysicalState);
                if (v.currentPhysicalState != newState) {
                    v.currentPhysicalState = newState;
                    changed.Add(v);
                    Log.Verbose("Pipes: {0} state changed, now {1}", v, v.IsOpen ? "open" : "closed");
                }
            }

            // update fans by computing connections
            HashSet<Fan> fansToBeRunning = findCorrectlyConnectedFans();
            RunningFansCount = fansToBeRunning.Count;

            //Log.Debug("Updating fans.")
            // start newly connected fans
            foreach (Fan f in fansToBeRunning)
                if (!f.IsRunning) {
                    f.IsRunning = true;
                    physical.SetFanState(f.Row, f.PositionInRow, true);
                    changed.Add(f);
                    Log.Verbose("Pipes: {0} state changed, now running", f);
                }

            // stop newly unconnected fans
            foreach (Fan f in fans)
                if (f.IsRunning && !fansToBeRunning.Contains(f)) {
                    f.IsRunning = false;
                    physical.SetFanState(f.Row, f.PositionInRow, false);
                    changed.Add(f);
                    Log.Verbose("Pipes: {0} state changed, now stopped", f);
                }

            //Debug.Log("Changed vertices: " + changed.ToString1());
            return changed;
        }

        private HashSet<Fan> findCorrectlyConnectedFans() {
            //Log.Debug("Identifying correctly connected fans.");
            HashSet<Fan> connectedToInlet = new HashSet<Fan>();
            foreach (Inlet inlet in inlets)
                connectedToInlet.UnionWith(findConnectedFans(inlet));

            HashSet<Fan> connectedToOutlet = new HashSet<Fan>();
            foreach (Outlet outlet in outlets)
                connectedToOutlet.UnionWith(findConnectedFans(outlet));

            HashSet<Fan> correctlyConnected = connectedToInlet;
            correctlyConnected.ExceptWith(connectedToOutlet);

            return correctlyConnected;
        }

        private HashSet<Fan> findConnectedFans(Vertex inOrOutlet) {
            //Log.Debug("Looking for fans connected to {0}.", inOrOutlet);
            if (inOrOutlet.Type != VertexType.Inlet && inOrOutlet.Type != VertexType.Outlet)
                throw new ArgumentException("Given vertex must be Inlet or Outlet but is " + inOrOutlet.Type);

            HashSet<Fan> result = new HashSet<Fan>();

            Queue<Vertex> open = new Queue<Vertex>();
            HashSet<Vertex> closed = new HashSet<Vertex>();
            open.Enqueue(inOrOutlet);

            while (open.Count > 0) {
                Vertex v = open.Dequeue();
                if (closed.Contains(v))
                    continue;
                closed.Add(v);

                if (v is Valve && !((Valve) v).IsOpen)
                    continue;

                if (v is Fan)
                    result.Add((Fan) v);

                IEnumerable<Edge> edges = graph.AdjacentEdges(v);
                foreach (Edge e in edges)
                    open.Enqueue(e.Source == v ? e.Target : e.Source);
            }

            return result;
        }

        #region static
        public static PipeSystem CreateV3e1(int psIndex) {
            Log.Debug("Creating pipe system 'V3e1'.");

            Inlet v00 = new Inlet(0, -1);
            Valve v01 = new Valve(1, 0, 24, -130);
            Fan v02 = new Fan(2, 0, 24, -158);
            Valve v04 = new Valve(4, 0, 24, -214);
            Outlet v05 = new Outlet(5, -1);

            Vertex[] vertices = { v00, v01, v02, v04, v05 };

            Vertex[][] edges = {
                new Vertex[] { v00, v01 },
                new Vertex[] { v01, v02 },
                new Vertex[] { v02, v04 },
                new Vertex[] { v04, v05 },
                new Vertex[] { v05 }
            };

            Valve[] openValvesAtStart = { v04 };

            PipeSystem result = new PipeSystem(psIndex, vertices, edges, openValvesAtStart);
            Debug.Assert(result.graph.VertexCount == 5, "wrong vertex count");
            Debug.Assert(result.graph.EdgeCount == 4, "wrong edge count");
            return result;
        }

        public static PipeSystem CreateV3e2(int psIndex) {
            Log.Debug("Creating pipe system 'V3e2'.");

            Valve v21 = new Valve(1, 1, 64, -130);
            Fan v23 = new Fan(3, 0, 64, -158);
            Valve v32 = new Valve(2, 0, 90, -154);
            Valve v34 = new Valve(4, 1, 90, -214);
            Inlet v40 = new Inlet(0, -1);
            Fan v41 = new Fan(1, 0, 118, -112);
            Fan v42 = new Fan(2, 1, 104, -176);
            Valve v43 = new Valve(3, 0, 120, -196);
            Outlet v45 = new Outlet(5, -1);

            Vertex[] vertices = { v21, v23, v32, v34, v40, v41, v42, v43, v45 };

            Vertex[][] edges = {
                new Vertex[] { v21, v23, v32, v41 },
                new Vertex[] { v23, v34 },
                new Vertex[] { v32, v41, v42 },
                new Vertex[] { v34, v43, v45 },
                new Vertex[] { v40, v41 },
                new Vertex[] { v41 },
                new Vertex[] { v42, v43 },
                new Vertex[] { v43, v45 },
                new Vertex[] { v45 },
            };

            Valve[] openValvesAtStart = { v21, v32, v34, v43 };

            PipeSystem result = new PipeSystem(psIndex, vertices, edges, openValvesAtStart);
            Debug.Assert(result.graph.VertexCount == 9, "wrong vertex count");
            Debug.Assert(result.graph.EdgeCount == 11, "wrong edge count");
            return result;
        }

        public static PipeSystem CreateV3e3(int psIndex) {
            Log.Debug("Creating pipe system 'V3e3'.");

            Inlet v50 = new Inlet(0, -1);
            Fan v52 = new Fan(2, 2, 160, -166);
            Valve v54 = new Valve(4, 2, 160, -194);
            Fan v60 = new Fan(0, 0, 180, -120);
            Valve v61 = new Valve(1, 2, 180, -146);
            Valve v63 = new Valve(3, 1, 188, -194);
            Valve v70 = new Valve(0, 0, 208, -120);
            Outlet v75 = new Outlet(5, -1);
            Fan v80 = new Fan(0, 1, 235, -120);
            Valve v81 = new Valve(1, 3, 246, -146);
            Fan v83 = new Fan(3, 1, 246, -184);
            Valve v84 = new Valve(4, 3, 246, -214);

            Vertex[] vertices = { v50, v52, v54, v60, v61, v63, v70, v75, v80, v81, v83, v84 };

            Vertex[][] edges = {
                new Vertex[] { v50, v60 },
                new Vertex[] { v52, v54, v61 },
                new Vertex[] { v54, v63, v75, v84 },
                new Vertex[] { v60, v70 },
                new Vertex[] { v61, v63, v81, v83 },
                new Vertex[] { v63, v75, v81, v83, v84 },
                new Vertex[] { v70, v80 },
                new Vertex[] { v75, v84 },
                new Vertex[] { v80, v81 },
                new Vertex[] { v81, v83 },
                new Vertex[] { v83, v84 },
                new Vertex[] { v84 },
            };

            Valve[] openValvesAtStart = { v54, v70, v81, v84 };

            PipeSystem result = new PipeSystem(psIndex, vertices, edges, openValvesAtStart);
            Debug.Assert(result.graph.VertexCount == 12, "wrong vertex count");
            Debug.Assert(result.graph.EdgeCount == 19, "wrong edge count");
            return result;
        }

        public static PipeSystem CreateV3e4(int psIndex) {
            Log.Debug("Creating pipe system 'V3e4'.");

            Valve v90 = new Valve(0, 1, 288, -120);
            Fan v91 = new Fan(1, 1, 288, -144);
            Valve v94 = new Valve(4, 4, 288, -202);
            Inlet v100 = new Inlet(0, -1);
            Valve v102 = new Valve(2, 1, 326, -158);
            Fan v104 = new Fan(4, 0, 306, -222);
            Valve v110 = new Valve(0, 2, 312, -102);
            Valve v113 = new Valve(3, 2, 326, -204);
            Fan v120 = new Fan(0, 2, 344, -102);
            Valve v122 = new Valve(2, 2, 352, -176);
            Fan v123 = new Fan(3, 2, 354, -206);
            Valve v125 = new Valve(5, 0, 350, -244);
            Valve v131 = new Valve(1, 4, 370, -138);
            Valve v133 = new Valve(3, 3, 392, -206);
            Fan v141 = new Fan(1, 2, 396, -138);
            Valve v142 = new Valve(2, 3, 412, -156);
            Valve v150 = new Valve(0, 3, 428, -120);
            Outlet v155 = new Outlet(5, -1);

            Vertex[] vertices = { v90, v91, v94, v100, v102, v104, v110, v113, v120, v122, v123, v125, v131, v133, v141, v142, v150, v155 };

            Vertex[][] edges = {
                new Vertex[] { v90, v91, v100 },
                new Vertex[] { v91, v94, v102 },
                new Vertex[] { v94, v102, v104 },
                new Vertex[] { v100, v110 },
                new Vertex[] { v102, v113, v122 },
                new Vertex[] { v104, v113, v125 },
                new Vertex[] { v110, v120 },
                new Vertex[] { v113, v122, v123, v125 },
                new Vertex[] { v120, v131, v150 },
                new Vertex[] { v122, v131 },
                new Vertex[] { v123, v133 },
                new Vertex[] { v125, v133, v142, v150, v155 },
                new Vertex[] { v131, v141, v150 },
                new Vertex[] { v133, v142, v150, v155 },
                new Vertex[] { v141, v142 },
                new Vertex[] { v142, v150, v155 },
                new Vertex[] { v150, v155 },
                new Vertex[] { v155 },
            };

            Valve[] openValvesAtStart = { v90, v94, v102, v122, v125, v131, v133, v142 };

            PipeSystem result = new PipeSystem(psIndex, vertices, edges, openValvesAtStart);
            Debug.Assert(result.graph.VertexCount == 18, "wrong vertex count");
            Debug.Assert(result.graph.EdgeCount == 32, "wrong edge count");
            return result;
        }
        #endregion static
    }

    public enum VertexType {
        Fan, Valve, Inlet, Outlet
    }

    public class Vertex : IComparable<Vertex> {

        /// <summary>
        /// The row coordinate of the vertex.
        /// </summary>
        public int Row { get; }
        /// <summary>
        /// The position of the vertex in the row. Example: PositionInRow==1 means it's the second vertex in its Row.
        /// </summary>
        public int PositionInRow { get; }
        /// <summary>
        /// The type of the vertex.
        /// </summary>
        public VertexType Type { get; }
        /// <summary>
        /// Pixel coordinates of the fan for positioning in the UI.
        /// </summary>
        public Vector2 PxCoords { get; }

        /// <summary>
        /// Returns the index of the pipe system this vertex belongs to.
        /// </summary>
        public PipeSystem PipeSystem { get; internal set; }

        protected Vertex(int row, int positionInRow, VertexType type, Vector2 pxCoords) {
            Row = row;
            PositionInRow = positionInRow;
            Type = type;
            PxCoords = pxCoords;
        }

        public int CompareTo(Vertex that) {
            if (this.Row != that.Row)
                return this.Row.CompareTo(that.Row);
            return this.PositionInRow.CompareTo(that.PositionInRow);
        }

        public override string ToString() {
            return string.Format("{0}(row={1}, pos={2})", Type, Row, PositionInRow);
        }
    }

    public class Valve : Vertex {
        /// <summary>
        /// i.e. valve is open iff physical.GetValveState != closedState
        /// </summary>
        internal bool logicalClosedState;
        internal bool currentPhysicalState;

        /// <summary>
        /// The state of the valve; true iff the valve is open, i.e. the current physical state differs from the logical closed state.
        /// </summary>
        public bool IsOpen { get { return currentPhysicalState != logicalClosedState; } }

        public Valve(int row, int positionInRow, float pxX, float pxY) : base(row, positionInRow, VertexType.Valve, new Vector2(pxX, pxY)) { }
    }

    public class Fan : Vertex {
        /// <summary>
        /// The state of the fan; true iff the fan is running.
        /// </summary>
        public bool IsRunning { get; internal set; }

        public Fan(int row, int positionInRow, float pxX, float pxY) : base(row, positionInRow, VertexType.Fan, new Vector2(pxX, pxY)) { }
    }

    public class Inlet : Vertex {
        public Inlet(int row, int positionInRow) : base(row, positionInRow, VertexType.Inlet, Vector2.zero) { }
    }

    public class Outlet : Vertex {
        public Outlet(int row, int positionInRow) : base(row, positionInRow, VertexType.Outlet, Vector2.zero) { }
    }

    public class Edge : IEdge<Vertex> {
        public Vertex Source { get; set; }
        public Vertex Target { get; set; }

        public Edge(Vertex v1, Vertex v2) {
            Source = v1;
            Target = v2;
        }
    }

}
