using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EscapeTheSchacht.Trigger;
using EscapeRoomFramework;

namespace EscapeTheSchacht.Trigger {

    public delegate void ButtonPressedHandler(TriggersGame sender, ButtonPressedEventArgs args);

    public class ButtonPressedEventArgs : EventArgs {
        /// <summary>
        /// The button that has been pressed (this can be -1, if multiple buttons are pressed at once).
        /// </summary>
        public int ButtonIndex { get; }
        /// <summary>
        /// The position in the sequence before pressing the button.
        /// </summary>
        public int SequenceIndex { get; }
        /// <summary>
        /// True iff pressing the button was correct at the position in the sequence.
        /// </summary>
        public bool Correct { get; }

        internal ButtonPressedEventArgs(int buttonIndex, int sequenceIndex, bool correct) {
            ButtonIndex = buttonIndex;
            SequenceIndex = sequenceIndex;
            Correct = correct;
        }
    }

    public class TriggersGame : Game<IEtsInterface> {

        public const string GameId = "Trigger";
        public static readonly int ButtonCount = 6;
        public static readonly int LedCount = ButtonCount;

        private TriggersGameScenario scenario;

        public override string Id => GameId;

        /// <summary>
        /// Is raised when a button has been pressed.
        /// </summary>
        public event ButtonPressedHandler OnButtonPressed;

        public TriggersGame() {
            Log.Info("Trigger game set up.");
        }

        /// <summary>
        /// The length of the solution sequence.
        /// </summary>
        public int SequenceLength => scenario.SequenceLength;

        /// <summary>
        /// The number of the current attempt. It is increased every time the sequence is reset after a wrong button has been pressed.
        /// </summary>
        public int Attempt => scenario.Attempt;

        /// <summary>
        /// A cursor that points to the current index in the sequence and denotes the next button to press.
        /// </summary>
        public int CurrentSequenceIndex => scenario.CurrentSequenceIndex;

        /// <summary>
        /// Returns true iff the sequence has been completed.
        /// </summary>
        public bool SequenceCompleted => scenario.SequenceCompleted;

        /// <summary>
        /// Returns the index of the button to press at the given position in the sequence. In case there are more than one possibilities to continue the sequence, the list contains more than one element.
        /// </summary>
        /// <param name="sequenceIndex"></param>
        /// <returns></returns>
        public List<int> GetSolutionButtons(int sequenceIndex) {
            return scenario.GetSolutionButtons(sequenceIndex);
        }

        /// <summary>
        /// Returns true iff the/a correct button has been pressed at the given position in the sequence.
        /// </summary>
        /// <param name="sequenceIndex"></param>
        /// <returns></returns>
        public bool IsSequenceButtonCorrect(int sequenceIndex) {
            return scenario.IsSequenceButtonCorrect(sequenceIndex);
        }

        public override bool CompensateTechnicalFailures(DiagnosticsReport report, List<string> affectedComponents) {
            return false;
        }

        /// <summary>
        /// See <see cref="Game.Initialize0(GameDifficulty)"/>
        /// </summary>
        protected override bool Initialize0(GameDifficulty difficulty, DiagnosticsReport diagnosticsReport) {
            // set up game configuration for specified difficulty
            switch (difficulty) {
            case GameDifficulty.Medium:
                scenario = TriggersGameScenario.CreateDefault();
                break;
            case GameDifficulty.Easy:
                scenario = TriggersGameScenario.CreateEasy();
                break;
            case GameDifficulty.Hard:
                scenario = TriggersGameScenario.CreateHard();
                break;
            default:
                throw new NotImplementedException(difficulty.ToString());
            }

            Log.Verbose("Created trigger game with solution " + scenario);
            return true;
        }

        /// <summary>
        /// See <see cref="Game.Start0"/>
        /// </summary>
        protected override void Start0() {
            // query all relevant input states from the physical interface to reset any changes in the pipe
            // (i.e. prevent processing button presses that occurred before the game started)
            for (int i = 0; i < ButtonCount; i++)
                Physical.WasSequenceButtonPressed(i);
        }

        /// <summary>
        /// See <see cref="Game.Update0(int)"/>
        /// </summary>
        protected override GameState Update0(float deltaTime) {
            // query button states from physical interface
            bool[] buttonsPressed = new bool[ButtonCount];
            for (int i = 0; i < buttonsPressed.Length; i++)
                buttonsPressed[i] = Physical.WasSequenceButtonPressed(i);

            processButtons(buttonsPressed);

            if (scenario.SequenceCompleted)
                return GameState.Completed;

            return GameState.Running;
        }

        private void processButtons(bool[] buttonsPressed) {
            // count the buttons that have are pressed in this frame
            int buttonCount = buttonsPressed.Count(true);

            // if no button is pressed, do nothing and return
            if (buttonCount == 0)
                return;

            // if there are more than one buttons pressed: automatic failure and return
            if (buttonCount > 1) { 
                Log.Verbose("Trigger: {0} buttons pressed at once, resetting sequence.", buttonCount);
                OnButtonPressed?.Invoke(this, new ButtonPressedEventArgs(-1, scenario.CurrentSequenceIndex, false));
                return;
            }

            // exactly one button pressed: process the pressed button
            int sequenceIndex = scenario.CurrentSequenceIndex;
            int buttonIndex = Array.IndexOf(buttonsPressed, true);
            bool correct = scenario.ProcessButtonPress(buttonIndex);

            // output for correct button
            if (correct) {
                if (scenario.SequenceCompleted) // game finished
                    Log.Verbose("Trigger: Correct button pressed, sequence completed.");
                else
                    Log.Verbose("Trigger: Correct button pressed, sequence index advanced to {0}", scenario.CurrentSequenceIndex);
            }

            // output for incorrect button and restart sequence
            if (!correct) {
                Log.Verbose("Trigger: Wrong button pressed, resetting sequence.");
                scenario.Restart();
            }

            OnButtonPressed?.Invoke(this, new ButtonPressedEventArgs(buttonIndex, sequenceIndex, correct));
        }

    }

}