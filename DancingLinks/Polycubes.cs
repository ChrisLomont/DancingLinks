using System.Diagnostics;

namespace DancingLinks;

public static class Polycubes
{

    public class Piece
    {
        public Piece(int[] p, string name)
        {
            s = new int[p.Length];
            Array.Copy(p, s, p.Length);
            Name = name;
        }



        readonly int[] s;
        public int Size => s.Length / 3;

        public string Name { get; set; }


        ulong Hash()
        {
            var minx = s.Chunk(3).Min(p => p[0]);
            var miny = s.Chunk(3).Min(p => p[1]);
            var minz = s.Chunk(3).Min(p => p[2]);
            var maxx = s.Chunk(3).Max(p => p[0]);
            var maxy = s.Chunk(3).Max(p => p[1]);
            var maxz = s.Chunk(3).Max(p => p[2]);
            var (dx, dy, dz) = (maxx - minx, maxy - miny, maxz - minz);
            // must fit in 4x4x4 to work, else needs new hash
            Trace.Assert(dx < 4 && dy < 4 && dz < 4);

            ulong hash = 0; // cells set
            for (var i = minx; i <= maxx; ++i)
            for (var j = miny; j <= maxy; ++j)
            for (var k = minz; k <= maxz; ++k)
            {
                var (i1, j1, k1) = (i, j, k); // lambda capture
                var c = CountMatching(p => p.i == i1 && p.j == j1 && p.k == k1);
                if (c == 1)
                {
                    var ind = (i - minx) + 4 * (j - miny) + 16 * (k - minz);
                    hash |= 1UL << ind;
                }
            }

            return hash;
        }


        /// <summary>
        /// Get piece under all orientations
        /// Doesn't duplicate shape under symmetries
        /// Aligned to nonnegative octant, along x, y, z axes
        /// </summary>
        /// <param name="allowFlips"></param>
        /// <returns></returns>
        public IEnumerable<Piece> GetOrientations(bool allowFlips = true)
        {
            var temp = new Piece(this.s, Name);
            var seen = new HashSet<ulong>(); // discard views already seen
            for (var top = 0; top < 6; ++top)
            {
                // rotate face to top
                // x,y rot counts
                int[] rots = new[]
                {
                    0, 0,
                    0, 1,
                    3, 0,
                    0, 3,
                    1, 0,
                    0, 2 // or 0,-2, or 2,0, or -2,0, etc.
                };
                temp.RotX(rots[top * 2]);
                temp.RotY(rots[top * 2 + 1]);

                for (var orientation = 0; orientation < 8; ++orientation)
                {
                    temp.Center();

                    var hash = temp.Hash();
                    if (!seen.Contains(hash))
                    {
                        seen.Add(hash);
                        yield return temp;
                    }

                    temp.RotZ(1);
                    if (orientation == 3 || orientation == 7)
                    {
                        if (!allowFlips)
                            break;
                        temp.FlipX();
                    }
                }
                // invert
                temp.RotY(-rots[top * 2 + 1]);
                temp.RotX(-rots[top * 2]);
            }
        }

        public void Dump(TextWriter output)
        {
            for (var i = 0; i < Size; i++)
                output.Write($"({s[i * 3]},{s[i * 3 + 1]},{s[i * 3 + 2]}) ");
            output.WriteLine();
        }

        public IEnumerable<(int x, int y, int z)> Coords()
        {
            for (var i = 0; i < s.Length; i += 3)
                yield return (s[i], s[i + 1], s[i + 2]);
        }

        public void RotX(int v) => Map(v, (i, j, k) => (i, -k, j));
        public void RotY(int v) => Map(v, (i, j, k) => (k, j, -i));
        public void RotZ(int v) => Map(v, (i, j, k) => (-j, i, k));
        public void FlipX() => Map(1, (i, j, k) => (-i, j, k));
        public void Shift(int dx = 0, int dy = 0, int dz = 0) => Map(1, (i, j, k) => (i + dx, j + dy, k + dz));

        void Map(int v, Func<int, int, int, (int, int, int)> perm)
        {
            v = ((v % 4) + 4) % 4; // positive mod
            while (v-- > 0)
            {
                for (var i = 0; i < s.Length; i += 3)
                    (s[i], s[i + 1], s[i + 2]) = perm(s[i], s[i + 1], s[i + 2]);
            }
        }



        /// <summary>
        /// Center shape in quad 1, along x and y axes
        /// </summary>
        public void Center()
        {
            var minx = s.Chunk(3).Min(p => p[0]);
            var miny = s.Chunk(3).Min(p => p[1]);
            var minz = s.Chunk(3).Min(p => p[2]);
            Shift(-minx, -miny, -minz);
        }

        // count how many cells match functor
        // good for checking piece is in some region by checking count is 5
        public int CountMatching(Func<(int i, int j, int k), bool> match) => Coords().Count(match);
    }

    // get soma cube piece 0-6
    public static Piece GetSoma(int index)
    {
        index = ((index % 7) + 7) % 7; // positive mod
        var len = index == 0 ? 3 : 4;
        var start = index == 0 ? 0 : 3 + 4*(index - 1);
        return new Piece(SomaDefs.Skip(start * 3).Take(len * 3).ToArray(), SomaNames[index]);
    }

    // 7 soma cubes, naming from Knuth TAOCP 4B 7.2.2.1
    static string[] SomaNames = { "bent","ell","tee","skew","L-twist","R-twist","claw"};
    static readonly int[] SomaDefs = new int[] // 4 xyz cells except 0 is 3 xyz cells
    {
        0,0,0, 0,1,0, 1,0,0,

        0,0,0, 0,1,0, 1,0,0, 0,2,0,

        0,0,0, 1,0,0, 2,0,0, 1,1,0, 

        0,0,0, 1,0,0, 1,1,0, 2,1,0,

        0,0,0, 0,1,0, 1,0,0, 1,0,1,

        0,0,0, 0,1,0, 1,0,0, 0,1,1,

        0,0,0, 0,1,0, 1,0,0, 0,0,1
    };




}