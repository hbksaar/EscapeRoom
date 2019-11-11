using System;
using UnityEngine;

namespace EscapeTheSchacht.Dynamite {

    public class DynamiteWall {

        private int[] holeStates;

        public bool[] CheckSolution(DynamiteScenario scenario, int[] holeStates) {
            this.holeStates = holeStates;
            return scenario.CheckSolution(this);
        }

        #region Scenario Checker Toolkit
        private DynamiteStick stick(int rowIndex, int columnIndex) {
            int stickIndex = holeStates[rowIndex * DynamiteGame.ColumnCount + columnIndex];
            return stickIndex != -1 ? DynamiteStick.Sticks[stickIndex] : null;
        }

        private int row(int holeIndex) {
            // 0 1 2 3
            // 4 5 6 7
            // 8 9 10 11
            return holeIndex / DynamiteGame.ColumnCount;
        }

        private int column(int holeIndex) {
            return holeIndex % DynamiteGame.ColumnCount;
        }

        internal bool checkAllInRow(int rowIndex, Func<DynamiteStick, bool> predicate) {
            for (int i = 0; i < DynamiteGame.ColumnCount; i++) {
                DynamiteStick s = stick(rowIndex, i);
                if (s != null && !predicate(s))
                    return false;
            }
            return true;
        }

        internal bool checkAllInColumn(int columnIndex, Func<DynamiteStick, bool> predicate) {
            for (int i = 0; i < DynamiteGame.RowCount; i++) {
                DynamiteStick s = stick(i, columnIndex);
                if (s != null && !predicate(s))
                    return false;
            }
            return true;
        }


        internal bool compareAll(Func<DynamiteStick, DynamiteStick, bool> compare) {
            for (int i = 0; i < holeStates.Length; i++) {
                if (holeStates[i] == -1)
                    continue;

                for (int j = i + 1; j < holeStates.Length; j++) {
                    if (holeStates[j] == -1)
                        continue;

                    DynamiteStick s1 = DynamiteStick.Sticks[holeStates[i]];
                    DynamiteStick s2 = DynamiteStick.Sticks[holeStates[j]];
                    if (!compare(s1, s2))
                        return false;
                }
            }

            return true;
        }


        internal bool compareAllInRow(int rowIndex, Func<DynamiteStick, DynamiteStick, bool> compare) {
            for (int i = 0; i < DynamiteGame.ColumnCount; i++) {
                DynamiteStick s1 = stick(rowIndex, i);
                if (s1 == null)
                    continue;

                for (int j = i + 1; j < DynamiteGame.ColumnCount; j++) {
                    DynamiteStick s2 = stick(rowIndex, j);
                    if (s2 == null)
                        continue;

                    if (!compare(s1, s2))
                        return false;
                }
            }

            return true;
        }


        /// <summary>
        /// Checks whether all sticks in a row fulfil a sequence property by pairwise application of the comparison function to the sticks in left-to-right order. Empty holes are skipped.
        /// </summary>
        /// <param name="rowIndex">the row to check</param>
        /// <param name="compare">the comparison function to use</param>
        /// <returns>true iff the given function returns true for all comparisons between sticks in the given row</returns>
        internal bool checkSequenceInRow(int rowIndex, Func<DynamiteStick, DynamiteStick, bool> compare) {
            DynamiteStick previous = null;
            for (int i = 0; i < DynamiteGame.ColumnCount; i++) {
                DynamiteStick s = stick(rowIndex, i);
                if (s == null)
                    continue;
                if (previous != null)
                    if (!compare(previous, s))
                        return false;
                previous = s;
            }
            return true;
        }

        internal bool checkSequenceInColumn(int columnIndex, Func<DynamiteStick, DynamiteStick, bool> compare) {
            DynamiteStick previous = null;
            for (int i = 0; i < DynamiteGame.RowCount; i++) {
                DynamiteStick s = stick(i, columnIndex);
                if (s == null)
                    continue;
                if (previous != null)
                    if (!compare(previous, s))
                        return false;
                previous = s;
            }
            return true;
        }

        internal bool checkSequenceInAllRows(Func<DynamiteStick, DynamiteStick, bool> predicate) {
            for (int i = 0; i < DynamiteGame.RowCount; i++)
                if (!checkSequenceInRow(i, predicate))
                    return false;
            return true;
        }

        internal bool checkSequenceInAllColumns(Func<DynamiteStick, DynamiteStick, bool> predicate) {
            for (int i = 0; i < DynamiteGame.ColumnCount; i++)
                if (!checkSequenceInColumn(i, predicate))
                    return false;
            return true;
        }

        internal int countInRow(int rowIndex) {
            return countInRow(rowIndex, (s) => s != null);
        }

        internal int countInRow(int rowIndex, Func<DynamiteStick, bool> predicate) {
            int count = 0;
            for (int i = 0; i < DynamiteGame.ColumnCount; i++) {
                DynamiteStick s = stick(rowIndex, i);
                if (s != null && predicate(s))
                    count++;
            }
            return count;
        }

        internal int countInColumn(int columnIndex) {
            return countInColumn(columnIndex, (s) => s != null);
        }

        internal int countInColumn(int columnIndex, Func<DynamiteStick, bool> predicate) {
            int count = 0;
            for (int i = 0; i < DynamiteGame.RowCount; i++) {
                DynamiteStick s = stick(i, columnIndex);
                if (s != null && predicate(s))
                    count++;
            }
            return count;
        }

        internal bool neighboursInRow(DynamiteStick s1, DynamiteStick s2) {
            int hole1 = Array.IndexOf(holeStates, s1);
            if (hole1 == -1)
                return false;

            int hole2 = Array.IndexOf(holeStates, s2);
            if (hole2 == -1)
                return false;

            if (Mathf.Abs(hole1 - hole2) != 1)
                return false;

            return row(hole1) == row(hole2);
        }

        internal bool neighboursInColumn(DynamiteStick s1, DynamiteStick s2) {
            int hole1 = Array.IndexOf(holeStates, s1);
            if (hole1 == -1)
                return false;

            int hole2 = Array.IndexOf(holeStates, s2);
            if (hole2 == -1)
                return false;

            if (Mathf.Abs(hole1 - hole2) != DynamiteGame.ColumnCount)
                return false;

            return column(hole1) == column(hole2);
        }

        internal bool checkPosition(int rowIndex, int columnIndex, Func<DynamiteStick, bool> predicate) {
            DynamiteStick s = stick(rowIndex, columnIndex);
            return predicate(s);
        }

        internal bool stickAt(int rowIndex, int columnIndex) {
            return stick(rowIndex, columnIndex) != null;
        }

        internal bool compareTwo(int rowIndex1, int columnIndex1, int rowIndex2, int columnIndex2, Func<DynamiteStick, DynamiteStick, bool> compare) {
            DynamiteStick s1 = stick(rowIndex1, columnIndex1);
            DynamiteStick s2 = stick(rowIndex2, columnIndex2);
            
            if (s1 == null)
                return false;
            if (s2 == null)
                return false;
            //if (s1 == null && s2 != null)
            //    return false;
            //if (s2 == null && s1 != null)
            //    return false;

            return compare(s1, s2);
        }
        #endregion Scenario Checker Toolkit

    }

}
