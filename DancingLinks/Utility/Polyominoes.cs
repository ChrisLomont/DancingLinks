namespace Lomont.Algorithms.Utility;

public static class Polyominoes
{

    public class Piece
    {
        public Piece(int[] p, int rot, int flip, string name)
        {
            s = new int[p.Length];
            Array.Copy(p, s, p.Length);

            this.rot = rot;
            this.flip = flip;
            Name = name;
        }

        readonly int[] s;
        readonly int rot;
        readonly int flip;
        public int Size => s.Length / 2;

        public string Name { get; set; }

        /// <summary>
        /// Get piece under all orientations
        /// Doesn't duplicate shape under symmetries
        /// Aligned to first quadrant, along x and y axes
        /// </summary>
        /// <param name="allowFlips"></param>
        /// <returns></returns>
        public IEnumerable<Piece> GetOrientations(bool allowFlips = true)
        {
            var t = rot * (flip + 1);

            var temp = new Piece(s, rot, flip, Name);
            for (var orientation = 0; orientation < t; ++orientation)
            {
                yield return new Piece(temp.s, rot, flip, Name);
                temp.Rotate(); // rot by 90
                if (flip == 1 && orientation == rot - 1)
                {
                    if (!allowFlips) break; // no more
                    temp.Flip(); // todo - check symmetries!
                }

                temp.Center(); // 
            }
        }

        public IEnumerable<(int x, int y)> Coords()
        {
            for (var i = 0; i < s.Length; i += 2)
                yield return (s[i], s[i + 1]);
        }

        public void Rotate()
        {
            for (var i = 0; i < s.Length; i += 2)
                (s[i], s[i + 1]) = (-s[i + 1], s[i]);
        }
        public void Flip()
        {
            for (var i = 0; i < s.Length; i += 2)
                (s[i], s[i + 1]) = (-s[i], s[i + 1]);
        }
        public void Shift(int dx, int dy)
        {
            for (var i = 0; i < s.Length; i += 2)
                (s[i], s[i + 1]) = (s[i] + dx, s[i + 1] + dy);
        }
        /// <summary>
        /// Center shape in quad 1, along x and y axes
        /// </summary>
        public void Center()
        {
            var minx = s.Chunk(2).Min(p => p[0]);
            var miny = s.Chunk(2).Min(p => p[1]);
            Shift(-minx, -miny);
        }
        // count how many cells match functor
        // good for checking piece is in some region by checking count is 5
        public int CountMatching(Func<(int i, int j), bool> match) => Coords().Count(match);
    }

    // unique rotations and flips for pentominoes
    //                             O  P  Q  R  S  T  U  V  W  X  Y  Z
    static readonly int[] P5Rots = new[] { 2, 4, 4, 4, 4, 4, 4, 4, 4, 1, 4, 2 };
    static readonly int[] P5Flips = new[] { 0, 1, 1, 1, 1, 0, 0, 0, 0, 0, 1, 1 };

    // get piece 0-11, Conway names 'O','P',...,'Y','Z'
    public static Piece GetPentomino(int index)
    {
        index = (index % 12 + 12) % 12; // positive mod
        return new Piece(PentominoDefs.Skip(index * 5 * 2).Take(10).ToArray(), P5Rots[index], P5Flips[index], ((char)('O' + index)).ToString());
    }

    // 12 pentominoes, Conway notation, O,P,..,W,X,Y,Z
    static readonly int[] PentominoDefs = new int[] // 5 x,y cells each
    {
        0,0,1,0,2,0,3,0,4,0, // O, which is just a bar
        0,0,0,1,0,2,1,2,1,1, // P
        0,1,1,1,2,1,3,1,3,0, // Q
        1,0,1,1,0,1,1,2,2,2, // R
        0,0,1,0,2,0,2,1,3,1, // S
        0,2,1,2,2,2,1,1,1,0, // T
        0,1,0,0,1,0,2,0,2,1, // U
        0,0,1,0,2,0,2,1,2,2, // V
        0,0,1,0,1,1,2,1,2,2, // W
        1,0,0,1,1,1,1,2,2,1, // X
        0,0,1,0,2,0,3,0,2,1, // Y
        0,2,1,2,1,1,1,0,2,0, // Z
    };




}