using EscapeRoomFramework;
using System.Collections.Generic;
using System;
using EscapeTheSchacht.Pipes;
using EscapeTheSchacht.Crates;
using EscapeTheSchacht.Dynamite;
using EscapeTheSchacht.Trigger;
using System.IO;

namespace EscapeTheSchacht {

    public class Scores {

        private Dictionary<string, int> scores = new Dictionary<string, int>();

        /*
        public int GetScore(string gameId) {
            if (gameId == null)
                throw new ArgumentNullException(nameof(gameId));
            if (!scores.ContainsKey(gameId))
                throw new ArgumentOutOfRangeException(nameof(gameId));

            return scores[gameId];
        }
        */

        public int GetScore<TPhysicalInterface>(Game<TPhysicalInterface> game) where TPhysicalInterface : IPhysicalInterface {
            if (game == null)
                throw new ArgumentNullException(nameof(game));
            if (!scores.ContainsKey(game.Id))
                throw new ArgumentOutOfRangeException(nameof(game.Id));

            return scores[game.Id];
        }

        public void SetScore<TPhysicalInterface>(Game<TPhysicalInterface> game, int score) where TPhysicalInterface : IPhysicalInterface {
            if (game == null)
                throw new ArgumentNullException(nameof(game));

            scores[game.Id] = score;
        }

        public bool ContainsScore<TPhysicalInterface>(Game<TPhysicalInterface> game) where TPhysicalInterface : IPhysicalInterface {
            if (game == null)
                throw new ArgumentNullException(nameof(game));

            return scores.ContainsKey(game.Id);
        }

        /*
        public void SetScore(string gameId, int score) {
            if (gameId == null)
                throw new ArgumentNullException(nameof(gameId));

            scores[gameId] = score;
        }
        */

        public int GetTotal() {
            int sum = 0;
            foreach (int score in scores.Values)
                sum += score;
            return sum;
        }

        internal void WriteTXT(string targetFile) {
            using (StreamWriter sw = new StreamWriter(targetFile)) {
                foreach (KeyValuePair<string, int> score in scores)
                    sw.WriteLine(score.Key + ' ' + score.Value);
                sw.WriteLine("Total " + GetTotal());
            }
        }

    }

    public delegate void ScoresUpdatedHandler(ScoringModule sender, ScoresUpdatedEventArgs args);

    public class ScoresUpdatedEventArgs : EventArgs {

        public Scores Scores { get; }

        internal ScoresUpdatedEventArgs(Scores scores) {
            Scores = scores;
        }

    }

    public class ScoringModule : Module<IEtsInterface> {

        #region Scoring rules
        /// <summary>
        /// Bonus for reaching a difficulty level for any game.
        /// </summary>
        private static readonly int[] difficultyBonus = new int[] { 0, 50, 100 };
        /// <summary>
        /// Bonus for completing a game.
        /// </summary>
        private static readonly int completedBonus = 100;

        /// <summary>
        /// Bonus per fan, awarded times the maximum number of running fans in the game run.
        /// </summary>
        private static readonly int pipesRunningFanBonus = 15;
        /// <summary>
        /// Bonus per running subsystem at the end of the game run.
        /// </summary>
        private static readonly int pipesSolvedSubsystemBonus = 30;


        /// <summary>
        /// Bonus per crate in the dropzone at the end of the game run.
        /// </summary>
        private static readonly int craneCrateInDropzoneBonus = 15;
        /// <summary>
        /// Penalty for crate pickups beyond the necessary amount.
        /// </summary>
        private static readonly int craneUnnecessaryPickupPenalty = -15;

        /// <summary>
        /// Bonus per solved instruction at the end of the game.
        /// </summary>
        private static readonly int dynamiteSolvedInstructionBonus = 15;

        /// <summary>
        /// Bonus per button, awarded times the length of the longest correct subsequence
        /// </summary>
        private static readonly int triggerMaxSequenceProgress = 15;
        #endregion Scoring rules

        private static readonly string filename = "scores.txt";

        public Scores Scores { get; private set; }

        public event ScoresUpdatedHandler OnScoresUpdated;

        public override void OnRoomStateChanged(EscapeRoom<IEtsInterface> sender, RoomStateChangedEventArgs e) {
            if (e.NewState == RoomState.Uninitialized)
                // start a new record
                Scores = new Scores();

            if (e.NewState == RoomState.Completed || e.NewState == RoomState.Aborted)
                Scores.WriteTXT(Ets.AnalyticsPath + filename);
        }

        public override void OnGameStateChanged(Game<IEtsInterface> sender, GameStateChangedEventArgs e) {
            if (e.NewState == GameState.Completed) {
                Scores.SetScore(sender, CalculateScore(sender, true));
                OnScoresUpdated?.Invoke(this, new ScoresUpdatedEventArgs(Scores));
            }
            else if (e.NewState == GameState.Aborted) {
                Scores.SetScore(sender, CalculateScore(sender, false));
                OnScoresUpdated?.Invoke(this, new ScoresUpdatedEventArgs(Scores));
            }
        }

        private int CalculateScore(Game<IEtsInterface> sender, bool completed) {
            int score = 0;

            if (completed)
                score += completedBonus;

            score += difficultyBonus[(int) sender.Difficulty];

            Statistics statistics = Room.GetModule<StatisticsModule>().Statistics;

            switch (sender.Id) {
            case PipesGame.GameId:
                score += pipesRunningFanBonus * statistics.pipesMaxRunningFans;
                score += pipesSolvedSubsystemBonus * statistics.pipesSolvedSubsystems;
                break;
            case CratesGame.GameId:
                CratesGame crane = (CratesGame) sender;
                score += craneCrateInDropzoneBonus * crane.CratesInDropzone;

                int unnecessaryPickups = Math.Max(0, statistics.craneCratePickups - crane.CrateCount);
                score += craneUnnecessaryPickupPenalty * unnecessaryPickups;
                break;
            case DynamiteGame.GameId:
                score += dynamiteSolvedInstructionBonus * statistics.dynamiteInstructionsSolved;
                break;
            case TriggersGame.GameId:
                score += triggerMaxSequenceProgress * statistics.triggerMaxSequenceProgress;
                break;
            }

            return score;
        }
    }

}