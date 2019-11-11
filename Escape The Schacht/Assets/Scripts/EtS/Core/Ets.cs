using System;
using EscapeRoomFramework;
using EscapeTheSchacht.Pipes;
using EscapeTheSchacht.Crates;
using EscapeTheSchacht.Dynamite;
using EscapeTheSchacht.Trigger;
using UnityEngine;
using EscapeTheSchacht.GameMaster;
using System.IO;

namespace EscapeTheSchacht {

    public class Ets : MonoBehaviour {

        private static EscapeRoom<IEtsInterface> room;

        /// <summary>
        /// Returns the singleton instance of the "Schacht und Heim" escape room.
        /// </summary>
        public static EscapeRoom<IEtsInterface> Room {
            get {
                if (room == null)
                    throw new InvalidOperationException("Escape room has not been instantiated.");
                return room;
            }
            private set {
                if (room != null)
                    throw new InvalidOperationException("An instance of the escape room has already been created. No other instances are permitted.");
                room = value;
            }
        }

        private static readonly string pathFormat = "{0:yyyy-MM-dd HH-mm-ss.fff}";
        public static string AnalyticsPath {
            get {
                string path = Log.LogFilePath + string.Format(pathFormat, Room.InitializationTime) + "/";
                Directory.CreateDirectory(path);
                return path;
            }
        }

        public EtsInterface physicalInterface;
        public EtsMockupInterface physicalInterfaceMockup;

        public SoundDirector soundDirector;
        public LightingDirector lightingDirector;

        public CraneScene craneScene; // the 3D scene of the crane game

        public RoomMaster roomMaster;
        public PipesMaster pipesMaster;
        public CratesMaster cratesMaster;
        public DynamiteMaster dynamiteMaster;
        public TriggersMaster triggerMaster;

        public bool useMockupInterface;

        public bool enableGameMasters, enableSoundDirector, enableLightingDirector;
        public bool enableStatistics, enableScoring, enableActionsLog;

        private void Awake() {
            // instantiate room
            if (useMockupInterface)
                Room = new EscapeRoom<IEtsInterface>(physicalInterfaceMockup);
            else
                Room = new EscapeRoom<IEtsInterface>(physicalInterface);

            // register games
            Room.RegisterGame(new PipesGame());
            Room.RegisterGame(new CratesGame(craneScene));
            Room.RegisterGame(new DynamiteGame());
            Room.RegisterGame(new TriggersGame());

            // register modules (order of registration is order of execution)
            if (enableSoundDirector)
                Room.RegisterModule(soundDirector);
            if (enableLightingDirector)
                Room.RegisterModule(lightingDirector);
            if (enableActionsLog)
                Room.RegisterModule(new ActionsLog());
            if (enableStatistics)
                Room.RegisterModule(new StatisticsModule());
            if (enableScoring)
                Room.RegisterModule(new ScoringModule());
            if (enableGameMasters) {
                Room.RegisterModule(roomMaster);
                Room.RegisterModule(pipesMaster);
                Room.RegisterModule(cratesMaster);
                Room.RegisterModule(dynamiteMaster);
                Room.RegisterModule(triggerMaster);
            }
        }

        private void Start() {
            Room.Initialize();
        }

        private void Update() {
            if (Room.State == RoomState.Running)
                Room.Update(Time.deltaTime);
        }

        private void OnApplicationQuit() {
            if (Room.State == RoomState.Running)
                Room.Abort();
        }

    }

}