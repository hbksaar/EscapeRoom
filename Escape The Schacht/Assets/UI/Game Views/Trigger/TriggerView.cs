using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EscapeTheSchacht;
using EscapeTheSchacht.Trigger;
using UnityEngine.UI;
using System;
using System.Text;
using EscapeRoomFramework;

namespace EscapeTheSchacht.UI {

    public class TriggerView : MonoBehaviour {

        private TriggersGame game;

        private static readonly string buttonStrF = "<color=#{0}>{1}</color>";

        private static readonly Color rightPosition = Color.green;
        private static readonly Color wrongPosition = Color.red;
        private static readonly Color currentPosition = Color.yellow;
        private static readonly Color neutralPosition = new Color(.125f, .125f, .125f);

        private Text[] lblCurrent;
        private Text[] lblPrevious;

        // Use this for initialization
        void Awake() {
            Transform pnlCurrent = transform.Find("pnlCurrent");
            lblCurrent = new Text[pnlCurrent.childCount];
            for (int i = 0; i < lblCurrent.Length; i++)
                lblCurrent[i] = pnlCurrent.Find(i.ToString()).GetComponent<Text>();

            Transform pnlPrevious = transform.Find("pnlPrevious");
            lblPrevious = new Text[pnlPrevious.childCount];
            for (int i = 0; i < lblPrevious.Length; i++)
                lblPrevious[i] = pnlPrevious.Find(i.ToString()).GetComponent<Text>();

            game = Ets.Room.GetGame<TriggersGame>();
            game.OnGameStateChanged += OnGameStateChanged;
            game.OnButtonPressed += OnButtonPressed;
            //OnGameStateChanged(game, game.State, game.State);
        }

        private void UpdateLabel(TriggersGame info, Text[] labels, int buttonIndex, Color color) {
            StringBuilder label = new StringBuilder();
            List<int> buttons = info.GetSolutionButtons(buttonIndex);
            label.Append(buttons[0]);
            for (int i = 1; i < buttons.Count; i++)
                label.Append('\n').Append(buttons[i]);
            string sColor = ColorUtility.ToHtmlStringRGB(color);
            labels[buttonIndex].text = string.Format(buttonStrF, sColor, label);
        }

        public void OnGameStateChanged(Game<IEtsInterface> sender, GameStateChangedEventArgs e) {
            //Debug.Log("TriggerView.OnGameStateChanged: " + state);
            if (e.NewState == GameState.Initialized) {
                for (int i = 0; i < game.SequenceLength; i++) {
                    UpdateLabel(game, lblCurrent, i, neutralPosition);
                    lblCurrent[i].gameObject.SetActive(true);
                    lblPrevious[i].gameObject.SetActive(true);
                    lblPrevious[i].text = "";
                }

                for (int i = game.SequenceLength; i < lblCurrent.Length; i++) {
                    lblCurrent[i].gameObject.SetActive(false);
                    lblPrevious[i].gameObject.SetActive(false);
                }
            }

            if (e.NewState == GameState.Running)
                UpdateLabel(game, lblCurrent, game.CurrentSequenceIndex, currentPosition);

            if (e.NewState == GameState.Aborted)
                UpdateLabel(game, lblCurrent, game.CurrentSequenceIndex, neutralPosition);
        }

        public void OnButtonPressed(TriggersGame sender, ButtonPressedEventArgs e) {
            //Debug.Log("TriggerView.OnButtonPressed: for sequence index " + sequenceIndex + " " + correct);
            UpdateLabel(sender, lblCurrent, e.SequenceIndex, e.Correct ? rightPosition : wrongPosition);
            if (e.Correct && e.SequenceIndex < sender.SequenceLength - 1)
                UpdateLabel(sender, lblCurrent, e.SequenceIndex + 1, currentPosition);

            if (!e.Correct) {
                for (int i = 0; i < sender.SequenceLength; i++) {
                    lblPrevious[i].text = lblCurrent[i].text;
                    UpdateLabel(sender, lblCurrent, i, neutralPosition);
                }
                UpdateLabel(sender, lblCurrent, 0, currentPosition);
            }
        }

    }

}