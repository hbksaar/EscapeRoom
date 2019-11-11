using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EscapeTheSchacht;
using UnityEngine.UI;
using System;
using EscapeTheSchacht.GameMaster;
using EscapeRoomFramework;
using EscapeTheSchacht.Pipes;
using EscapeTheSchacht.Crates;
using EscapeTheSchacht.Dynamite;
using EscapeTheSchacht.Trigger;

namespace EscapeTheSchacht.UI {

    public class RoomInfoDisplay : MonoBehaviour {

        public static readonly int[] betweenGamesVoiceNumbers = new int[] {
            // collected voice numbers of speech between games, roughly estimated
            35, 36, 57,
            84, 85, 122,
            131, 136, 160,
            163, 164, 166, 170, 190
        };

        public new AudioSystem audio;
        
        public GameObject lblDoNotEnter, lblGameInProgress, lblPleaseWait, lblComing, lblFailures;
        public Text lblTime;
        public GameObject lblScoreCaptionPrevious, lblScoreCaptionCurrent;
        public Text lblScorePipes, lblScoreCrane, lblScoreDynamite, lblScoreTrigger;


        private readonly string timeFormatStr = "~ {0}:{1:00}";
        private float timeLeft;


        public void OnRoomStateChanged(EscapeRoom<IEtsInterface> sender, RoomStateChangedEventArgs e) {
            if (e.NewState == RoomState.Initialized) {
                float maxTimeInGames = RoomMaster.MaxTimePipes + RoomMaster.MaxTimeCrates + RoomMaster.MaxTimeDynamite + RoomMaster.MaxTimeTriggers;

                float totalAudioTime = 0;
                foreach (int voiceNumber in betweenGamesVoiceNumbers)
                    totalAudioTime += audio.GetVoiceLength(voiceNumber);

                timeLeft = maxTimeInGames + totalAudioTime;
                failuresDetected = e.DiagnosticsReport.FailuresDetected;
            }

            if (e.NewState == RoomState.Running) {
                //lblTime.gameObject.SetActive(true);
                lblDoNotEnter.SetActive(true);
                lblGameInProgress.SetActive(true);
                lblPleaseWait.SetActive(false);
                lblComing.SetActive(false);
                lblScoreCaptionPrevious.SetActive(false);
                lblScoreCaptionCurrent.SetActive(true);
                lblScorePipes.text = "-";
                lblScoreCrane.text = "-";
                lblScoreDynamite.text = "-";
                lblScoreTrigger.text = "-";
            }

            if (e.NewState != RoomState.Running) {
                lblScoreCaptionPrevious.SetActive(true);
                lblScoreCaptionCurrent.SetActive(false);
            }

            if (e.NewState == RoomState.Initialized) {
                //lblTime.gameObject.SetActive(false);
                lblTime.text = "00:00";
                lblDoNotEnter.SetActive(false);
                lblGameInProgress.SetActive(false);
                lblPleaseWait.SetActive(true);
                lblComing.SetActive(true);
            }            
        }

        private void Awake() {
            Ets.Room.OnRoomStateChanged += OnRoomStateChanged;
            Ets.Room.GetModule<ScoringModule>().OnScoresUpdated += OnScoresUpdated;
        }

        private void OnScoresUpdated(ScoringModule sender, ScoresUpdatedEventArgs e) {
            PipesGame pipes = Ets.Room.GetGame<PipesGame>();
            lblScorePipes.text = e.Scores.ContainsScore(pipes) ? e.Scores.GetScore(pipes).ToString() : "-";
            CratesGame crates = Ets.Room.GetGame<CratesGame>();
            lblScoreCrane.text = e.Scores.ContainsScore(crates) ? e.Scores.GetScore(crates).ToString() : "-";
            DynamiteGame dynamite = Ets.Room.GetGame<DynamiteGame>();
            lblScoreDynamite.text = e.Scores.ContainsScore(dynamite) ? e.Scores.GetScore(dynamite).ToString() : "-";
            TriggersGame trigger = Ets.Room.GetGame<TriggersGame>();
            lblScoreTrigger.text = e.Scores.ContainsScore(trigger) ? e.Scores.GetScore(trigger).ToString() : "-";
        }

        void Update() {
            if (Ets.Room.State == RoomState.Running) {
                timeLeft -= Time.deltaTime;
                int ms = Mathf.RoundToInt(timeLeft * 1000);
                TimeSpan time = new TimeSpan(0, 0, 0, 0, ms);
                string timeStr = string.Format(timeFormatStr, time.Minutes, time.Seconds);
                lblTime.text = timeStr;
            }

            failureLabelBlinkingCountdown -= Time.deltaTime;
            if (failuresDetected && failureLabelBlinkingCountdown <= 0f) {
                lblFailures.SetActive(!lblFailures.activeInHierarchy);
                failureLabelBlinkingCountdown = failureLabelBlinkingInterval;
            }
        }

        private bool failuresDetected;
        public float failureLabelBlinkingInterval = 1f;
        private float failureLabelBlinkingCountdown;

    }

}