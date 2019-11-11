using EscapeTheSchacht.Crates;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using EscapeRoomFramework;

namespace EscapeTheSchacht.UI {

    public class CraneView : MonoBehaviour {

        private static readonly string craneCountFormatStr = "{0} / {1}";
        private static readonly string distanceFormatStr = "{0:0.0}";

        private CratesGame game;

        private Text lblCrates, lblDistance;

        private void Awake() {
            lblCrates = transform.Find("lblCrates").GetComponent<Text>();
            lblDistance = transform.Find("lblDistance").GetComponent<Text>();

            game = Ets.Room.GetGame<CratesGame>();
            game.OnGameStateChanged += OnGameStateChanged;
            game.OnCraneDistanceMoved += OnCraneDistanceMoved;
            game.OnCrateDropped += OnCrateDropped;
            game.OnCratePickedUp += OnCratePickedUp;
            //OnGameStateChanged(game, game.State, game.State);
        }

        public void OnGameStateChanged(Game<IEtsInterface> sender, GameStateChangedEventArgs e) {
            UpdateCrates(game);
            UpdateDistance(game);
        }

        public void OnCraneDistanceMoved(CratesGame sender, CraneDistanceMovedEventArgs e) {
            UpdateDistance(sender);
        }

        public void OnCratePickedUp(CratesGame sender, CrateEventArgs e) {
            UpdateCrates(sender);
        }

        public void OnCrateDropped(CratesGame sender, CrateEventArgs e) {
            UpdateCrates(sender);
        }

        private void UpdateDistance(CratesGame sender) {
            lblDistance.text = string.Format(distanceFormatStr, sender.TotalMoveDistance);
        }

        private void UpdateCrates(CratesGame sender) {
            lblCrates.text = string.Format(craneCountFormatStr, sender.CratesInDropzone, sender.CrateCount);
        }

    }

}