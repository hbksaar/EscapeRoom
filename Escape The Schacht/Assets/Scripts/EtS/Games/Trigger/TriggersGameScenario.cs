using System;
using System.Collections.Generic;
using System.Text;

namespace EscapeTheSchacht.Trigger {

    internal class TriggersGameScenario {

        #region static
        private static readonly Random r = new Random();

        /// <summary>
        /// Creates the trigger sequence for the lowest difficulty level. The sequence is as long as 
        /// there are buttons and every button needs to be pressed exactly once. There are two possible 
        /// solutions for the first button though.
        /// </summary>
        /// <returns></returns>
        public static TriggersGameScenario CreateEasy() {
            int[] sequence = new int[TriggersGame.ButtonCount];
            for (int i = 0; i < sequence.Length; i++)
                sequence[i] = i;
            sequence.Shuffle();

            int[] sequence2 = new int[sequence.Length];
            Array.Copy(sequence, sequence2, sequence.Length);
            int index = r.Next(sequence.Length / 2) + 1;
            int temp = sequence2[0];
            sequence2[0] = sequence2[index];
            sequence2[index] = temp;

            return new TriggersGameScenario(sequence, sequence2);
        }

        /// <summary>
        /// Creates the trigger sequence for the default difficulty level. The sequence as long as there 
        /// are buttons and every button needs to be pressed exactly once.
        /// </summary>
        /// <returns></returns>
        public static TriggersGameScenario CreateDefault() {
            int[] sequence = new int[TriggersGame.ButtonCount];
            for (int i = 0; i < sequence.Length; i++)
                sequence[i] = i;
            sequence.Shuffle();

            return new TriggersGameScenario(sequence);
        }

        /// <summary>
        /// Creates the trigger sequence for the hardest difficulty level. The sequence is as long as 
        /// there are buttons. One button is not included in the solution which means that one of the 
        /// others needs to be pressed twice.
        /// </summary>
        /// <returns></returns>
        public static TriggersGameScenario CreateHard() {
            int[] pool = new int[TriggersGame.ButtonCount];
            for (int i = 0; i < pool.Length; i++)
                pool[i] = i;
            pool.Shuffle();

            int[] sequence = new int[TriggersGame.ButtonCount];
            Array.Copy(pool, sequence, pool.Length - 1);
            int lastNo = r.Next(TriggersGame.ButtonCount);
            if (lastNo == pool[pool.Length - 1])
                lastNo = (lastNo + 1) % TriggersGame.ButtonCount;
            sequence[sequence.Length - 1] = lastNo;
            sequence.Shuffle();

            return new TriggersGameScenario(sequence);
        }
        #endregion static

        /// <summary>
        /// See <see cref="TriggersGame.SequenceLength"/>
        /// </summary>
        public int SequenceLength { get; private set; }

        /// <summary>
        /// See <see cref="TriggersGame.CurrentSequenceIndex"/>
        /// </summary>
        public int CurrentSequenceIndex { get; private set; }

        /// <summary>
        /// See <see cref="TriggersGame.SequenceCompleted"/>
        /// </summary>
        public bool SequenceCompleted => CurrentSequenceIndex >= SequenceLength;

        /// <summary>
        /// <see cref="TriggersGame.Attempt"/>
        /// </summary>
        public int Attempt { get; internal set; }

        private List<int[]> possibleSolutions = new List<int[]>();

        private TriggersGameScenario(params int[][] sequences) {
            SequenceLength = sequences[0].Length;

            // precondition
            foreach (int[] sequence in sequences) {
                if (sequence.Length != SequenceLength)
                    throw new ArgumentException("All sequences must be equally long.");
                possibleSolutions.Add(sequence);
            }

            Attempt = 0;
            Restart();
        }

        /// <summary>
        /// Checks the given button against the current position in the sequence. If the button is 
        /// incorrect, the sequence index will be reset to 0, restarting the sequence. If the button is 
        /// correct, the sequence index will be advanced; also if the current position allows multiple 
        /// solutions, all solutions that don't match the given button at the current sequence will be 
        /// discarded, thereby reducing the possible solution space. (In the end the solution will be 
        /// unambiguous; the solution space is not reset on a wrong button press.)
        /// </summary>
        /// <param name="button">the index of the button that has been pressed</param>
        /// <returns>true iff the pressed button is correct</returns>
        internal bool ProcessButtonPress(int button) {
            if (SequenceCompleted)
                throw new InvalidOperationException("Sequence already completed.");

            // collect the sequences in which the pressed button is incorrect
            List<int[]> differingSequences = new List<int[]>();
            foreach (int[] sequence in possibleSolutions) {
                if (sequence[CurrentSequenceIndex] != button)
                    differingSequences.Add(sequence);
            }

            // if the button is wrong in every sequence, it's a mistake
            if (differingSequences.Count == possibleSolutions.Count)
                return false;

            // otherwise there are possible solutions where the button is correct and thus the button press was correct
            // but first we remove all differing sequences from the pool of possible solutions as they are now deemed incorrect to always appear unambiguous to the player
            foreach (int[] diffSeq in differingSequences)
                possibleSolutions.Remove(diffSeq);
            CurrentSequenceIndex++;
            return true;
        }

        /// <summary>
        /// Restarts the current sequence by resetting the sequence index to 0. Also increases the attempt counter by 1.
        /// </summary>
        internal void Restart() {
            CurrentSequenceIndex = 0;
            Attempt++;
        }

        /// <summary>
        /// <see cref="ITriggerInfo.GetSolutionButtons(int)"/>
        /// </summary>
        internal List<int> GetSolutionButtons(int sequenceIndex) {
            // collect all different button indexes in all possible solutions at the given sequence index
            List<int> result = new List<int>();
            foreach (int[] sequence in possibleSolutions)
                if (!result.Contains(sequence[sequenceIndex]))
                    result.Add(sequence[sequenceIndex]);
            return result;
        }

        /// <summary>
        /// <see cref="ITriggerInfo.IsSequenceButtonCorrect(int)"/>
        /// </summary>
        internal bool IsSequenceButtonCorrect(int sequenceIndex) {
            return CurrentSequenceIndex > sequenceIndex;
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            foreach (int[] sequence in possibleSolutions)
                sb.Append(sequence.ToString1()).Append(" ");
            return sb.ToString();
        }

    }

}