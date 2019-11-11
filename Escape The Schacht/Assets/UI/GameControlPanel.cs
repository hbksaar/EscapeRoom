using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EscapeTheSchacht;
using System;
using EscapeRoomFramework;

namespace EscapeTheSchacht.UI {

    public class GameControlPanel : MonoBehaviour {

        public string gameId;

        private ScoringModule scoring;
        private Game<IEtsInterface> game;
        private GameDifficulty selectedDifficulty = GameDifficulty.Medium;

        private Button btnInitialize, btnStart, btnAbort, btnComplete;
        private readonly Button[] btnDifficulty = new Button[3];
        private Text lblState, lblStats;

        private bool roomRunning;


        //private string statsCaptionStr = "Difficulty:\nAttempt:\nTime:\nActions (par):\nRating T/A:\nRating / Score:";
        private readonly string statsCaptionStr = "Difficulty:\nTime:\nScore:";
        //private string statsFormatStr = "{0}\n{1}\n{2}\n{3} ({4})\n{5:F2} / {6:F2}\n{7:F2} / {8}";
        private readonly string statsFormatStr = "{0}\n{1}\n{2}";
        // lblStats.text = string.Format(statsFormatStr, game.Difficulty, game.RunNumber, timeStr, game.PerformedActions);
        private readonly string timeFormatStr = "{0}:{1:00}";

        public float statsUpdateInterval = 1f;
        private float timeToUpdate;

        // Use this for initialization
        protected virtual void Awake() {
            btnDifficulty[0] = transform.Find("pnlDifficulties").Find("btnEasy").GetComponent<Button>();
            btnDifficulty[1] = transform.Find("pnlDifficulties").Find("btnDefault").GetComponent<Button>();
            btnDifficulty[2] = transform.Find("pnlDifficulties").Find("btnHard").GetComponent<Button>();

            btnInitialize = transform.Find("btnInitialize").GetComponent<Button>();
            btnStart = transform.Find("btnStart").GetComponent<Button>();
            btnAbort = transform.Find("btnAbort").GetComponent<Button>();
            btnComplete = transform.Find("btnComplete").GetComponent<Button>();

            transform.Find("pnlGameMonitor").Find("lblStatsCaptions").GetComponent<Text>().text = statsCaptionStr;
            lblState = transform.Find("lblState").GetComponent<Text>();
            lblStats = transform.Find("pnlGameMonitor").Find("lblStats").GetComponent<Text>();

            timeToUpdate = statsUpdateInterval;

            EscapeRoom<IEtsInterface> room = Ets.Room;
            scoring = room.GetModule<ScoringModule>();
            if (room == null || room.GetGame(gameId) == null)
                DisableButtons();
            else {
                game = room.GetGame(gameId);
                //print("Game found: " + game);
                Text lblTitle = transform.Find("lblTitle").GetComponent<Text>();
                lblTitle.text = game.Id.ToUpper();

                room.OnRoomStateChanged += OnRoomStateChanged;
                game.OnGameStateChanged += OnGameStateChanged;
                //OnRoomStateChanged(Room, Room.State, Room.State);
                //OnGameStateChanged(Game, Game.State, Game.State);
            }
        }

        private void DisableButtons() {
            foreach (Button btn in btnDifficulty)
                btn.interactable = false;
            btnInitialize.interactable = btnStart.interactable = btnAbort.interactable = btnComplete.interactable = false;
        }

        public void OnRoomStateChanged(EscapeRoom<IEtsInterface> sender, RoomStateChangedEventArgs e) {
            roomRunning = e.NewState == RoomState.Running;
            //Debug.Log("GCP.OnRoomStateChanged: " + state);

            if (!roomRunning) {
                lblState.text = "Room not running";
                lblState.color = RoomControlPanel.TextUninitialized;
                DisableButtons();
            }
            else
                UpdateUI(game.State);
        }

        private void OnGameStateChanged(Game<IEtsInterface> sender, GameStateChangedEventArgs e) {
            //Debug.Log("GCP.OnGameStateChanged: " + state + " / room active: " + roomRunning);

            if (!roomRunning)
                return;

            UpdateUI(e.NewState);
        }
        
        private void UpdateUI(GameState state) {
            lblState.text = game.State.ToString();
            lblState.color = ColorCode(game.State);

            //print("GCP.updateUI: state = " + state);
            //print(string.Format("  CanInitialize = {0} / CanStart = {1} / CanStop = {2} / CanComplete = {3}", game.CanInitialize, game.CanStart, game.CanStop, game.CanComplete));

            foreach (Button btn in btnDifficulty)
                btn.interactable = game.State.CanTransition(GameState.Initialized) || game.State.CanTransition(GameState.Uninitialized);
            btnInitialize.interactable = game.State.CanTransition(GameState.Initialized) || game.State.CanTransition(GameState.Uninitialized);
            btnStart.interactable = game.State.CanTransition(GameState.Running);
            btnAbort.interactable = game.State.CanTransition(GameState.Aborted);
            btnComplete.interactable = game.State.CanTransition(GameState.Completed);

            if (state != GameState.Uninitialized && state != GameState.Error)
                UpdateStats();
        }

        // Update is called once per frame
        void Update() {
            if (game.State != GameState.Running)
                return;

            timeToUpdate -= Time.deltaTime;
            if (timeToUpdate < 0) {
                UpdateStats();
                timeToUpdate = statsUpdateInterval;
            }
        }

        protected void UpdateStats() {
            TimeSpan gameTime = new TimeSpan(0, 0, 0, 0, (int) (game.ElapsedTime * 1000));
            string timeStr = string.Format(timeFormatStr, gameTime.Minutes, gameTime.Seconds);

            //GameOutcome outcome = game.Outcome != null ? game.Outcome : GameOutcome.Compute(game);

            //lblStats.text = string.Format(statsFormatStr, game.Difficulty, game.RunNumber, timeStr, game.PerformedActions, game.ParActions, outcome.ratingTotal, outcome.score);
            //lblStats.text = string.Format(statsFormatStr, game.Difficulty, game.RunNumber, timeStr, game.PerformedActions, game.ParActions, outcome.RatingTime, outcome.RatingActions, outcome.Rating, outcome.Score);

            int score = scoring.Scores.ContainsScore(game) ? scoring.Scores.GetScore(game) : 0;
            lblStats.text = string.Format(statsFormatStr, game.Difficulty, timeStr, score);
        }

        private Color ColorCode(GameState state) {
            switch (state) {
            case GameState.Uninitialized:
                return RoomControlPanel.TextUninitialized;
            case GameState.Initialized:
                return RoomControlPanel.TextReady;
            case GameState.Running:
                return RoomControlPanel.TextRunning;
            case GameState.Aborted:
                return RoomControlPanel.TextAborted;
            case GameState.Completed:
                return RoomControlPanel.TextCompleted;
            case GameState.Error:
                return RoomControlPanel.TextError;
            default:
                throw new NotImplementedException();
            }
        }
        
        /*
        private Color colorCode(GameOutcome state) {
            return state.Finished ? CW_SUCCESS : CW_FAILURE;
        }
        */

        public void OnDifficultyButtonClick(Button sender) {
            foreach (Button btn in btnDifficulty)
                btn.GetComponentInChildren<Text>().fontStyle = btn == sender ? FontStyle.BoldAndItalic : FontStyle.Italic;

            if (sender == btnDifficulty[0])
                selectedDifficulty = GameDifficulty.Easy;
            if (sender == btnDifficulty[1])
                selectedDifficulty = GameDifficulty.Medium;
            if (sender == btnDifficulty[2])
                selectedDifficulty = GameDifficulty.Hard;
        }

        public void OnButtonClick(Button sender) {
            print("On click: " + sender.name);

            if (sender == btnInitialize) {
                if (game.State != GameState.Uninitialized)
                    game.Reset();
                game.Initialize(selectedDifficulty);
            }
            else if (sender == btnStart)
                game.Start();
            else if (sender == btnAbort)
                game.Abort();
            else if (sender == btnComplete)
                game.Complete();
            else
                throw new NotImplementedException();
        }

        private GameDifficulty Next(GameDifficulty current) {
            switch (current) {
            case GameDifficulty.Easy:
                return GameDifficulty.Medium;
            case GameDifficulty.Medium:
                return GameDifficulty.Hard;
            case GameDifficulty.Hard:
                return GameDifficulty.Easy;
            default:
                throw new NotImplementedException();
            }
        }

    }

}