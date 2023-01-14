using System.Diagnostics;
using System.Text.RegularExpressions;
using Lomont.Algorithms.Examples;

namespace Lomont.Algorithms.Utility;

public static class Utils
{
    /// <summary>
    /// Get items from a file, like words or primes
    /// </summary>
    /// <param name="filename"></param>
    /// <param name="len"></param>
    /// <param name="toUppercase"></param>
    /// <param name="toLower"></param>
    /// <param name="allLetters"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public static List<string> GetWords(
        string filename,
        int len,
        bool toUppercase = false,
        bool toLower = false,
        bool allLetters = true,
        int count = -1)
    {
        var path = @"..\..\..\data\"; // todo - make smarter - search up and down current position for them
        var words = File.ReadAllLines(path + filename).ToList();
        words = words.Where(w => w.Length == len).ToList();
        if (allLetters)
            words = words.Where(AllAlpha).ToList();
        if (toUppercase)
            words = words.Select(w => w.ToUpper()).ToList();
        if (count > 0)
            words = words.Take(count).ToList();
        return words;

        bool AllAlpha(string s) => s.ToCharArray().All(char.IsLetter);
    }

    // reusable solution formatter for char grid like solutions
    // items must be formatted using the following conventions:
    // cells as c_i_j:char
    public static bool DumpCellSolution(long solutions, long moves, List<List<string>> options)
    {
        var reg = new Regex(@"^c_?(?<i>\d+)_(?<j>\d+):(?<tag>[^\$]+)(\$\d+)?$");
        // parse all items into (i,j,char) tuples
        var tups = new List<(int i, int j, string tag)>();
        foreach (var items in options)
        foreach (var item in items)
        {
            var m = reg.Match(item);
            if (m.Success)
            {
                var i = int.Parse(m.Groups["i"].Value);
                var j = int.Parse(m.Groups["j"].Value);
                var tag = m.Groups["tag"].Value;
                tups.Add(new(i, j, tag));
            }
        }

        // get bounds
        var minx = tups.Min(t => t.i);
        var maxx = tups.Max(t => t.i);
        var miny = tups.Min(t => t.j);
        var maxy = tups.Max(t => t.j);

        var c = new char[maxx - minx + 1, maxy - miny + 1];
        foreach (var (i, j, t) in tups)
        {
            if (t.Length != 1)
                Console.WriteLine($"Error! - {t} more than one char");
            else
            {
                if (c[i, j] != 0 && c[i, j] != t[0])
                    Console.WriteLine($"Error! - ({i},{j}) over-specified with {c[i, j]} and {t}");
                c[i, j] = t[0];
            }
        }


        Console.WriteLine("Solution : ");
        for (var x = 0; x < c.GetLength(0); ++x)
        {
            for (var y = 0; y < c.GetLength(1); ++y)
            {
                var ch = c[x, y];
                Console.Write(ch == 0 ? ' ' : ch);
            }

            Console.WriteLine();
        }

        Console.WriteLine();
        return true;
    }


    public static void Test(long answer, Func<long> func)
    {
        var ans = func();
        Trace.Assert(answer == ans);
    }

    public static void RegressionTesting()
    {
        var opts = new DancingLinksSolver.SolverOptions
        {
            OutputFlags = DancingLinksSolver.SolverOptions.ShowFlags.None
        };
        // test toy problems
        Test(4, () => NumberPuzzles.PrimeSquares(opts, 1, 3, 3)); // answer: 997 787 733
        Test(1, () => MiscPuzzles.ToyDlxPage68(opts));
        Test(1, () => MiscPuzzles.ToyColorPage89(opts, dump: false));
        Test(92, () => ChessPuzzles.NQueens(opts, 8, useSecondary: true));
        Test(1, () => MiscPuzzles.ToyMultiplicity(opts));
        Test(1, () => MiscPuzzles.ToyMultiplicityWithColor(opts));
        Test(64, () => MiscPuzzles.Exercise94(opts)); // tests colors
        Test(11520, () => GeometricPuzzles.Soma(opts, false));

        // tests - nothing super long to check
        //Test(8, () => PolyominoesRectangle(20, 3));
        //Test(14200, () => ChessPuzzles.NQueens(opts,12, organPipe: true, mrv: false));
        //Test(500,()=>TestRandomVersusBruteForce(numTests:500));
        //Test(520,ScottsPentominoProblem); // 520 solutions
    }
}