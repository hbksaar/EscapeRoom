using EscapeRoomFramework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EscapeTheSchacht.Dynamite {

    class DynamiteScenarioInstruction {

        internal string Text { get; }

        private Func<DynamiteWall, bool> checker;

        internal DynamiteScenarioInstruction(string text, Func<DynamiteWall, bool> checker) {
            Text = text;
            this.checker = checker;
        }

        internal bool Check(DynamiteWall dw) {
            return checker(dw);
        }

    }

    public class DynamiteScenario {

        public int Number { get; }
        public GameDifficulty Difficulty { get; }
        public string Name { get; }

        public int InstructionCount => instructions.Length;

        private DynamiteScenarioInstruction[] instructions;

        private DynamiteScenario(int number, GameDifficulty difficulty, string name, params DynamiteScenarioInstruction[] instructions) {
            Number = number;
            Difficulty = difficulty;
            Name = name;
            this.instructions = instructions;
        }

        public string GetInstructionText(int index) {
            return instructions[index].Text;
        }

        public bool[] CheckSolution(DynamiteWall dw) {
            bool[] result = new bool[instructions.Length];

            for (int i = 0; i < instructions.Length; i++)
                result[i] = instructions[i].Check(dw);

            return result;
        }

        #region static
        public static readonly int TopRowIndex = 0;
        public static readonly int MiddleRowIndex = 1;
        public static readonly int BottomRowIndex = 2;

        public static readonly int FirstColumnIndex = 0;
        public static readonly int SecondColumnIndex = 1;
        public static readonly int ThirdColumnIndex = 2;
        public static readonly int LastColumnIndex = 3;

        public static readonly DynamiteScenario[] ALL_SCENARIOS = new DynamiteScenario[] { CreateEasy1(), CreateEasy2(), CreateMedium1(), CreateMedium2(), CreateHard1(), CreateHard2() };

        private static readonly string LAST_SCENARIO_FILE_PATH = "lastscenario.txt";

        private static List<DynamiteScenario> getPossibleScenarios(GameDifficulty difficulty) {
            List<DynamiteScenario> result = new List<DynamiteScenario>();
            foreach (DynamiteScenario s in ALL_SCENARIOS)
                if (s.Difficulty == difficulty)
                    result.Add(s);
            return result;
        }

        private struct LastScenario {
            public int number;
            public string name;
            public GameDifficulty difficulty;
        }

        public static DynamiteScenario ChooseScenario(GameDifficulty difficulty, DynamiteWall wall, int[] currentHoleStates) {
            //LastScenario last;
            //bool lastScenarioAvailable = readLastScenario(out last);

            List<DynamiteScenario> possibleScenarios = getPossibleScenarios(difficulty);

            /*
            if (lastScenarioAvailable && difficulty == last.difficulty) {
                for (int i = 0; i < possibleScenarios.Count; i++)
                    if (possibleScenarios[i].Number == last.number) {
                        Log.Verbose("Last scenario found ({0} for difficulty {1}), skipping number {2}", last.name, last.difficulty, last.number);
                        possibleScenarios.RemoveAt(i);
                        break;
                    }
            }*/

            DynamiteScenario[] shuffled = possibleScenarios.ToArray();
            shuffled.Shuffle();

            for (int i = 0; i < shuffled.Length; i++) {
                bool[] instructionsSolved = wall.CheckSolution(shuffled[i], currentHoleStates);
                if (instructionsSolved.Count(false) > 0)
                    return shuffled[i];
            }

            Log.Warn("Current hole states fulfil every possible scenario for difficulty " + difficulty);
            return shuffled[0];

            /*
            Scenario result = possibleScenarios[Extensions.r.Next(possibleScenarios.Count)];
            writeLastScenario(result);
            return result;
            */
        }

        private static bool readLastScenario(out LastScenario result) {
            if (File.Exists(LAST_SCENARIO_FILE_PATH))
                try {
                    using (StreamReader sr = new StreamReader(LAST_SCENARIO_FILE_PATH, Encoding.UTF8)) {
                        string fileContent = sr.ReadToEnd();
                        result = JsonConvert.DeserializeObject<LastScenario>(fileContent);
                        return true;
                    }
                } catch (Exception e) {
                    Log.Error(e);
                    Log.Warn("Cannot read last scenario data.");
                }

            result = default(LastScenario);
            return false;
        }

        private static bool writeLastScenario(DynamiteScenario scenario) {
            LastScenario last = new LastScenario {
                difficulty = scenario.Difficulty,
                number = scenario.Number,
                name = scenario.Name
            };

            try {
                using (StreamWriter sw = new StreamWriter(LAST_SCENARIO_FILE_PATH, false, Encoding.UTF8)) {
                    string data = JsonConvert.SerializeObject(last);
                    sw.WriteLine(data);
                    return true;
                }
            } catch (Exception e) {
                Log.Error(e);
                Log.Warn("Cannot write last scenario data.");
                return false;
            }
        }
        #endregion static

        #region Scenario definitions
        private static DynamiteScenario CreateEasy1() {
            return new DynamiteScenario(1, GameDifficulty.Easy, "Sprengtafel 1 (leicht)",
                new DynamiteScenarioInstruction(
                    "Alle Stäbe sind verteilt.",
                    (dw) => dw.countInRow(TopRowIndex) + dw.countInRow(MiddleRowIndex) + dw.countInRow(BottomRowIndex) == DynamiteGame.StickCount
                ),
                new DynamiteScenarioInstruction(
                    "Die mittlere Zeile bleibt frei.",
                    (dw) => dw.countInRow(MiddleRowIndex) == 0
                ),
                new DynamiteScenarioInstruction(
                    "Die drei leichtesten Stangen müssen nach unten.",
                    (dw) => dw.countInRow(BottomRowIndex, (s) => s.Weight == 0) == 3
                ),
                new DynamiteScenarioInstruction(
                    "Die schwerste Stange muss nach unten.",
                    (dw) => dw.countInRow(BottomRowIndex, (s) => s.Weight == 3) == 1
                ),
                new DynamiteScenarioInstruction(
                    "Die Stange mit zwei Löchern muss ganz nach links.",
                    (dw) => dw.countInColumn(FirstColumnIndex, (s) => s.Holes == 2) == 1
                ),
                new DynamiteScenarioInstruction(
                    "Die Stange mit sechs Löchern muss ganz nach rechts.",
                    (dw) => dw.countInColumn(LastColumnIndex, (s) => s.Holes == 6) == 1
                )
            );
        }

        private static DynamiteScenario CreateEasy2() {
            return new DynamiteScenario(2, GameDifficulty.Easy, "Sprengtafel 2 (leicht)",
                new DynamiteScenarioInstruction(
                    "Alle Stäbe sind verteilt.",
                    (dw) => dw.countInRow(TopRowIndex) + dw.countInRow(MiddleRowIndex) + dw.countInRow(BottomRowIndex) == DynamiteGame.StickCount
                ),
                new DynamiteScenarioInstruction(
                    "Die untere Zeile bleibt frei.",
                    (dw) => dw.countInRow(BottomRowIndex) == 0
                ),
                new DynamiteScenarioInstruction(
                    "Die drei leichtesten Stangen müssen nach oben.",
                    (dw) => dw.countInRow(TopRowIndex, (s) => s.Weight == 0) == 3
                ),
                new DynamiteScenarioInstruction(
                    "Die schwerste Stange muss nach oben.",
                    (dw) => dw.countInRow(TopRowIndex, (s) => s.Weight == 3) == 1
                ),
                new DynamiteScenarioInstruction(
                    "Die Stange mit vier Löchern muss ganz nach rechts.",
                    (dw) => dw.countInColumn(LastColumnIndex, (s) => s.Holes == 4) == 1
                ),
                new DynamiteScenarioInstruction(
                    "Die Stange mit fünf Löchern muss ganz nach links.",
                    (dw) => dw.countInColumn(FirstColumnIndex, (s) => s.Holes == 5) == 1
                )
            );
        }

        private static DynamiteScenario CreateMedium1() {
            return new DynamiteScenario(3, GameDifficulty.Medium, "Sprengtafel 3 (mittel)",
                new DynamiteScenarioInstruction(
                    "Alle Stäbe sind verteilt.",
                    (dw) => dw.countInRow(TopRowIndex) + dw.countInRow(MiddleRowIndex) + dw.countInRow(BottomRowIndex) == DynamiteGame.StickCount
                ),
                new DynamiteScenarioInstruction(
                    "In die obere Zeile dürfen nur Stangen mit einer Rille.",
                    (dw) => dw.checkAllInRow(TopRowIndex, (s) => s.Grooves == 1)
                ),
                new DynamiteScenarioInstruction(
                    "In die mittlere Zeile dürfen nur Stangen mit zwei Rillen.",
                    (dw) => dw.checkAllInRow(MiddleRowIndex, (s) => s.Grooves == 2)
                ),
                new DynamiteScenarioInstruction(
                    "Jede Stange hat mehr Löcher als die Stange links von ihr.",
                    //"In jeder Zeile muss die Anzahl der Löcher pro Stab von links nach rechts zunehmen.",
                    (dw) => dw.checkSequenceInRow(TopRowIndex, (s1, s2) => s1.Holes < s2.Holes)
                          && dw.checkSequenceInRow(MiddleRowIndex, (s1, s2) => s1.Holes < s2.Holes)
                          && dw.checkSequenceInRow(BottomRowIndex, (s1, s2) => s1.Holes < s2.Holes)
                ),
                new DynamiteScenarioInstruction(
                    "In der oberen Zeile müssen die zwei Stangen mit dem gleichen Gewicht nebeneinander liegen.",
                    (dw) => dw.compareAllInRow(TopRowIndex, (s1, s2) => s1.Weight != s2.Weight || dw.neighboursInRow(s1, s2))
                ),
                new DynamiteScenarioInstruction(
                    "In der dritten Spalte von links müssen alle Bohrlöcher belegt sein.",
                    (dw) => dw.countInColumn(ThirdColumnIndex) == DynamiteGame.RowCount
                ),
                new DynamiteScenarioInstruction(
                    "In der dritten Spalte von links sind alle Stangen lang.",
                    (dw) => dw.checkSequenceInColumn(ThirdColumnIndex, (s1, s2) => s1.Length == 1)
                )
            );
        }

        private static DynamiteScenario CreateMedium2() {
            return new DynamiteScenario(4, GameDifficulty.Medium, "Sprengtafel 4 (mittel)",
                new DynamiteScenarioInstruction(
                    "Alle Stäbe sind verteilt.",
                    (dw) => dw.countInRow(TopRowIndex) + dw.countInRow(MiddleRowIndex) + dw.countInRow(BottomRowIndex) == DynamiteGame.StickCount
                ),
                new DynamiteScenarioInstruction(
                    "In die untere Zeile dürfen nur Stangen mit einer Rille.",
                    (dw) => dw.checkAllInRow(BottomRowIndex, (s) => s.Grooves == 1)
                ),
                new DynamiteScenarioInstruction(
                    "In die mittlere Zeile dürfen nur Stangen mit zwei Rillen.",
                    (dw) => dw.checkAllInRow(MiddleRowIndex, (s) => s.Grooves == 2)
                ),
                new DynamiteScenarioInstruction(
                    "Jede Stange hat weniger Löcher als die Stange links von ihr.",
                    //"In jeder Zeile muss die Anzahl der Löcher pro Stab von links nach rechts abnehmen.",
                    (dw) => dw.checkSequenceInRow(TopRowIndex, (s1, s2) => s1.Holes > s2.Holes)
                          && dw.checkSequenceInRow(MiddleRowIndex, (s1, s2) => s1.Holes > s2.Holes)
                          && dw.checkSequenceInRow(BottomRowIndex, (s1, s2) => s1.Holes > s2.Holes)
                ),
                new DynamiteScenarioInstruction(
                    "In der unteren Zeile müssen die zwei Stangen mit dem gleichen Gewicht nebeneinander liegen.",
                    (dw) => dw.compareAllInRow(BottomRowIndex, (s1, s2) => s1.Weight != s2.Weight || dw.neighboursInRow(s1, s2))
                ),
                new DynamiteScenarioInstruction(
                    "In der zweiten Spalte von links müssen alle Bohrlöcher belegt sein.",
                    (dw) => dw.countInColumn(SecondColumnIndex) == DynamiteGame.RowCount 
                ),
                new DynamiteScenarioInstruction(
                    "In der zweiten Spalte von links sind alle Stangen lang.",
                    (dw) => dw.checkSequenceInColumn(SecondColumnIndex, (s1, s2) => s1.Length == 1)
                )
            );
        }

        private static DynamiteScenario CreateHard1() {
            return new DynamiteScenario(5, GameDifficulty.Hard, "Sprengtafel 5 (schwer)",
                new DynamiteScenarioInstruction(
                    "Alle Stäbe sind verteilt.",
                    (dw) => dw.countInRow(TopRowIndex) + dw.countInRow(MiddleRowIndex) + dw.countInRow(BottomRowIndex) == DynamiteGame.StickCount
                ),
                new DynamiteScenarioInstruction(
                    "Die Stange mit sechs Löchern muss nach rechts unten.",
                    (dw) => dw.checkPosition(BottomRowIndex, LastColumnIndex, (s) => s != null && s.Holes == 6)
                ),
                new DynamiteScenarioInstruction(
                    "Die schwerste Stange und die Stange ohne Löcher müssen in die mittlere Zeile.",
                    (dw) => dw.countInRow(MiddleRowIndex, (s) => s.Weight == 3) == 1
                          && dw.countInRow(MiddleRowIndex, (s) => s.Holes == 0) == 1
                ),
                new DynamiteScenarioInstruction(
                    "In die linke Spalte dürfen keine Stangen mit Rillen.",
                    (dw) => dw.checkAllInColumn(FirstColumnIndex, (s) => s.Grooves == 0)
                ),
                new DynamiteScenarioInstruction(
                    "Die Bohrlöcher direkt über Stangen mit zwei Rillen müssen leer bleiben.",
                    (dw) => dw.checkSequenceInAllColumns((s1, s2) => s2.Grooves != 2 || !dw.neighboursInColumn(s1, s2))
                ),
                new DynamiteScenarioInstruction(
                    "Stangen mit gleicher Rillenzahl müssen immer direkt nebeneinander oder direkt übereinander stecken.",
                    (dw) => dw.compareAll((s1, s2) => s1.Grooves != s2.Grooves || dw.neighboursInRow(s1, s2) || dw.neighboursInColumn(s1, s2))
                ),
                new DynamiteScenarioInstruction(
                    "Stangen mit gleicher Länge dürfen nicht direkt nebeneinander oder direkt übereinander stecken.",
                    (dw) => dw.compareAll((s1, s2) => s1.Length != s2.Length || !(dw.neighboursInRow(s1, s2) || dw.neighboursInColumn(s1, s2)))
                ),
                new DynamiteScenarioInstruction(
                    "Jede Stange ist leichter als die Stange über ihr.",
                    (dw) => dw.checkSequenceInAllColumns((s1, s2) => s1.Weight > s2.Weight)
                )
            );
        }

        private static DynamiteScenario CreateHard2() {
            return new DynamiteScenario(6, GameDifficulty.Hard, "Sprengtafel 6 (schwer)",
                new DynamiteScenarioInstruction(
                    "Alle Stäbe sind verteilt.",
                    (dw) => dw.countInRow(TopRowIndex) + dw.countInRow(MiddleRowIndex) + dw.countInRow(BottomRowIndex) == DynamiteGame.StickCount
                ),
                new DynamiteScenarioInstruction(
                    "Die Stange mit einem Loch muss nach links oben.",
                    (dw) => dw.checkPosition(TopRowIndex, FirstColumnIndex, (s) => s != null && s.Holes == 1)
                ),
                new DynamiteScenarioInstruction(
                    "Die schwerste Stange und die Stange ohne Löcher müssen in die untere Zeile.",
                    (dw) => dw.countInRow(BottomRowIndex, (s) => s.Weight == 3) == 1
                          && dw.countInRow(BottomRowIndex, (s) => s.Holes == 0) == 1
                ),
                new DynamiteScenarioInstruction(
                    "In die rechte Spalte dürfen keine Stangen mit Rillen.",
                    (dw) => dw.checkAllInColumn(LastColumnIndex, (s) => s.Grooves == 0)
                ),
                new DynamiteScenarioInstruction(
                    "Die Bohrlöcher direkt unter Stangen mit zwei Rillen müssen leer bleiben.",
                    (dw) => dw.checkSequenceInAllColumns((s1, s2) => s1.Grooves != 2 || !dw.neighboursInColumn(s1, s2))
                ),

                new DynamiteScenarioInstruction(
                    "Stangen mit gleicher Rillenzahl müssen immer direkt nebeneinander oder direkt übereinander stecken.",
                    (dw) => dw.compareAll((s1, s2) => s1.Grooves != s2.Grooves || dw.neighboursInRow(s1, s2) || dw.neighboursInColumn(s1, s2))
                ),
                new DynamiteScenarioInstruction(
                    "Stangen mit gleicher Länge dürfen nicht direkt nebeneinander oder direkt übereinander stecken.",
                    (dw) => dw.compareAll((s1, s2) => s1.Length != s2.Length || !(dw.neighboursInRow(s1, s2) || dw.neighboursInColumn(s1, s2)))
                ),
                new DynamiteScenarioInstruction(
                    "Jede Stange ist schwerer als die Stange über ihr.",
                    (dw) => dw.checkSequenceInAllColumns((s1, s2) => s1.Weight < s2.Weight)
                )
            );
        }
        #endregion Scenario definitions
    }

}
