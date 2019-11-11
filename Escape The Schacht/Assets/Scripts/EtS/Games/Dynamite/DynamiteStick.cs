namespace EscapeTheSchacht.Dynamite {

    public class DynamiteStick {

        internal static readonly DynamiteStick[] Sticks = new DynamiteStick[] {
            new DynamiteStick(0, 0, 1, 2, 1),
            new DynamiteStick(1, 0, 2, 1, 7),
            new DynamiteStick(2, 0, 0, 1, 4),
            new DynamiteStick(3, 0, 3, 0, 3),
            new DynamiteStick(4, 1, 0, 2, 6),
            new DynamiteStick(5, 1, 2, 1, 2),
            new DynamiteStick(6, 1, 1, 1, 0),
            new DynamiteStick(7, 1, 0, 0, 5)
        };

        public int Index { get; private set; }
        public int Length { get; private set; }
        public int Weight { get; private set; }
        public int Grooves { get; private set; }
        public int Holes { get; private set; }

        internal DynamiteStick(int index, int length, int weight, int grooves, int holes) {
            Index = index;
            Length = length;
            Weight = weight;
            Grooves = grooves;
            Holes = holes;
        }

    }

}
