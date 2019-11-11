using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EscapeTheSchacht;
using EscapeTheSchacht.Dynamite;
using System;
using EscapeRoomFramework;

namespace EscapeTheSchacht.UI {

    public class DynamiteView : MonoBehaviour {

        private DynamiteGame game;

        private static readonly Color ColorSolvedInstruction = Color.green;
        private static readonly Color ColorUnsolvedInstruction = Color.red;

        public GameObject prefabInstructionText;

        private Transform pnlInstructions;
        private Text[] lblHoles = new Text[DynamiteGame.HoleCount];
        private Text[] lblInstructions = new Text[0];

        // Use this for initialization
        void Awake() {
            pnlInstructions = transform.Find("pnlInstructions");

            Transform pnlHoles = transform.Find("pnlHoles");
            //print(pnlHoles != null);

            for (int i = 0; i < lblHoles.Length; i++) {
                //print(pnlHoles.GetChild(i));
                //print(pnlHoles.Find(i.ToString()));
                lblHoles[i] = pnlHoles.Find(i.ToString()).GetComponentInChildren<Text>();
            }

            game = Ets.Room.GetGame<DynamiteGame>();
            game.OnGameStateChanged += OnGameStateChanged;
            game.OnInstructionSolvedStateChanged += OnInstructionSolvedStateChanged;
            game.OnStickInserted += OnStickInserted;
            game.OnStickRemoved += OnStickRemoved;
            //OnGameStateChanged(game, game.State, game.State);
        }

        public void OnGameStateChanged(Game<IEtsInterface> sender, GameStateChangedEventArgs e) {
            if (e.NewState == GameState.Initialized) {
                for (int i = 0; i < lblHoles.Length; i++)
                    UpdateHoleLabel(i, -1);

                for (int i = 0; i < lblInstructions.Length; i++)
                    Destroy(lblInstructions[i].gameObject);

                lblInstructions = new Text[game.InstructionCount];
                for (int i = 0; i < lblInstructions.Length; i++) {
                    GameObject textGO = Instantiate(prefabInstructionText, pnlInstructions);
                    lblInstructions[i] = textGO.GetComponent<Text>();
                    lblInstructions[i].text = string.Format("({0}) {1}", i, game.GetInstructionText(i));
                    lblInstructions[i].color = ColorUnsolvedInstruction;
                }
            }
        }

        public void OnStickInserted(DynamiteGame sender, StickEventArgs e) {
            UpdateHoleLabel(e.HoleIndex, e.Stick.Index);
        }

        public void OnStickRemoved(DynamiteGame sender, StickEventArgs e) {
            UpdateHoleLabel(e.HoleIndex, -1);
        }

        public void OnInstructionSolvedStateChanged(DynamiteGame sender, InstructionSolvedStateChangedArgs e) {
            lblInstructions[e.InstructionIndex].color = e.Solved ? ColorSolvedInstruction : ColorUnsolvedInstruction;
        }

        private void UpdateHoleLabel(int holeIndex, int stickIndex) {
            if (stickIndex == -1)
                lblHoles[holeIndex].text = "";
            else
                lblHoles[holeIndex].text = StickLabel(stickIndex);
        }

        private string StickLabel(int stickIndex) {
            if (stickIndex == -1)
                return "";
            int i = 'A' + stickIndex;
            char result = (char) i;
            return result.ToString();
        }

    }

}
 