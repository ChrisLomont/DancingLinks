using System.Diagnostics;
using Lomont.Algorithms.Utility;

namespace Lomont.Algorithms.Examples;

public static class NumberPuzzles
{
    public static void RunAll(DancingLinksSolver.SolverOptions opts)
    {
        // Langford pairs have soln when n is 0 or 3 mod 4
        // solutions by size: 3=>2,4=>3, 7=>52,8=>300,11=>35584
        for (var n = 1; n < 4; ++n)
        {
            LangfordPairs(opts, n * 4 - 1, false);
            LangfordPairs(opts, n * 4, false);
        }

        Trace.Assert(LangfordPairs(opts, 16, false) == 326_721_800);

        for (var n = 1; n <= 5; ++n)
        {
            Console.WriteLine("Wainwright " + n);
            WainwrightPackingPage92(opts, n); // no solutions for so small
        }
        // should have solutions for n = 8
        WainwrightPackingPage92(opts, 8); // long, needs tested


        // exercise 284 in TAOCP 
        PrimeSquares(opts, 1, 3, 3); // answer: 997 787 733
        PrimeSquares(opts, -1, 3, 3); // answer: 113 307 139
        PrimeSquares(opts, 1, 3, 4); // answer:  8999 8699 7717
        PrimeSquares(opts, -1, 3, 4); // answer: 2111 1031 1193
        PrimeSquares(opts, -1, 3, 5); // answer: 21211 10301 11393
        PrimeSquares(opts, -1, 3, 6); // answer: 111211 100103 331171
        PrimeSquares(opts, -1, 3, 7); // answer: 1111211 1000403 3193171


    }

    [Puzzle("Compute Langford pairs, as described in Knuth's TAOCP")]
    public static long LangfordPairs(DancingLinksSolver.SolverOptions opts, int n, bool useExercise15 = false)
    {
        // solns for n that exist: https://oeis.org/A014552
        // 0, 0, 1, 1, 0, 0, 26, 150, 0, 0, 17792, 108144, 0, 0, 39809640, 326721800, 0, 0, 256814891280, 2636337861200, 0, 0, 3799455942515488, 46845158056515936, 0, 0, 111683611098764903232, 1607383260609382393152, 0, 0

        Console.WriteLine($"Langford pair size {n}");
        // has solution when n is 0,3 mod 4
        Trace.Assert(useExercise15 == false); // todo - do this and test
        var dl = new DancingLinksSolver {Options = opts};

        // Langford pairs : insert 1,1,2,2,3,3,...,n,n into slots 
        // s1,s2,...,s_{2n} so that there are i numbers between
        // occurrences of i
        // items: n values of i, and sj slots between
        // options: i sj sk for 1<=j<k<2n, k = i+j-1, 1<= i <= n
        for (var i = 1; i <= n; ++i)
            dl.AddItem(i.ToString());
        for (var j = 1; j <= 2 * n; ++j)
            dl.AddItem($"s{j}");
        for (var i = 1; i <= n; ++i)
        for (var j = 1; j <= 2 * n; ++j)
        {
            var k = i + j + 1;
            if (j < k && k <= 2 * n)
                dl.AddOption(new[] {i.ToString(), $"s{j}", $"s{k}"});
        }

        dl.Solve();
        Console.WriteLine();
        return dl.SolutionCount;
    }


    /// <summary>
    ///     |topN| is number to find, topN > 0 get largest, topN < 0 gets smallest
    /// </summary>
    /// <param name="opts"></param>
    /// <param name="topN"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    [Puzzle("Compute rectangles filled with primes, obtaining the top N by max or min cost")]
    public static long PrimeSquares(DancingLinksSolver.SolverOptions opts, int topN, int width = 5, int height = 5)
    {
        // 5x5 squares of primes made of 10 different primes with smallest or largest possible product
        // largest 5 can be found in 22 Gu, so reasonable
        // shown in (111) and (112)
        var dl = new DancingLinksSolver {Options = opts };
        var primesW = Utils.GetWords("primes.txt", width, allLetters: false).Select(Int32.Parse).ToList();
        var primesH = primesW;
        Console.WriteLine($"{primesW.Count} {width}-digit primes loaded from {primesW[0]} to {primesW.Last()}");
        if (width != height)
        {
            primesH = Utils.GetWords("primes.txt", height, allLetters: false).Select(Int32.Parse).ToList();
            Console.WriteLine($"{primesH.Count} {height}-digit primes loaded from {primesH[0]} to {primesH.Last()}");
        }

        // items:
        for (var i = 0; i < width; ++i)
            dl.AddItem($"d_{i}"); // down i
        for (var j = 0; j < height; ++j)
            dl.AddItem($"a_{j}"); // across j

        // cells have 'colors' (which are the digit placed there), are secondary
        for (var i = 0; i < width; ++i)
        for (var j = 0; j < height; ++j)
            dl.AddItem(Cell(i, j), true);

        // primes are secondary, this disallows duplicate prime usage
        foreach (var prime in primesW)
            dl.AddItem(prime.ToString(), true);
        if (width != height)
            foreach (var prime in primesH)
                dl.AddItem(prime.ToString(), true);

        // we want to maximize the product of primes chosen
        // costs are added per option, and are added in the algorithm, so
        // we assign log(p) as the cost. Costs are integers, so we then
        // choose Floor[C*log(p)] as the cost where C is chosen large enough
        // to make values distinct enough we do not get errors, yet small
        // enough that the sum does not overflow an int64 (the type of the cost)

        // if p_m is largest prime under consideration, can show that
        // C > 2/log(pm/(pm-2)) suffices. We'll take 5x that

        var pm = Math.Max(primesH.Max(), primesW.Max());
        var C = 10 / Math.Log(pm / (pm - 2.0));

        // product of primes. By default, alg gets smallest solution,
        // if we want largest solution, we make these of form K-cost where K-cost > 0
        // and sum still fits in a int64
        var K = 4 * (width + height) * C * Math.Log(pm);

        // cost function, 
        long Cost(long val)
        {
            var cost = Math.Floor(C * Math.Log(val));
            if (topN > 0)
                cost = K - cost;
            var icost = (long)cost;
            Trace.Assert(0 < icost && icost < Int64.MaxValue / (2 * (width + height)));
            return icost;
        }

        // options with cost
        foreach (var prime in primesH)
        {
            var w = prime.ToString();

            var cost = Cost(prime);

            // across or down, letters, cells:
            for (var i = 0; i < width; ++i)
            {
                var op = $"d_{i}";
                op += $"${cost}";
                op += " ";
                // cell:letter
                for (var j = 0; j < height; ++j)
                    op += $"{Cell(i, j)}:{w[j]} ";

                // prime used 
                op += w;
                dl.ParseOption(op);
            }
        }

        foreach (var prime in primesW)
        {
            var w = prime.ToString();

            var cost = Cost(prime);

            for (var j = 0; j < height; ++j)
            {
                var op = $"a_{j}";
                op += $"${cost}";
                op += " ";
                // cell:letter
                for (var i = 0; i < width; ++i)
                    op += $"{Cell(i, j)}:{w[i]} ";

                // prime used 
                op += w;
                dl.ParseOption(op);
            }
        }

        // assign 9 to 0,2
        // assign 9 to 1,2
        //dl.ParseOption($"{Cell(0, 2)}:9");
        //dl.ParseOption($"{Cell(1, 2)}:9");


        //dl.SolutionListener += DumpCellSolution;
        dl.Solve(topKbyCosts: Math.Abs(topN));
        //dl.SolutionListener -= DumpCellSolution;

        var s = 0;
        foreach (var (cost, solution) in dl.LowestCostSolutions)
        {
            Console.WriteLine($"Cost {cost}:");
            Utils.DumpCellSolution(++s, cost, solution);
        }

        return dl.SolutionCount;

        string Cell(int i, int j) => $"c_{i}_{j}";
    }

    [Puzzle("Compute Wainwright packings as in TAOCP pg 92")]

    public static long WainwrightPackingPage92(DancingLinksSolver.SolverOptions opts, int n)
    {
        //n = 2;
        var dl = new DancingLinksSolver {Options = opts };
        // N=(1+2+..+n)^2=1^3+2^3+...+n^3
        var N = n * (n + 1) / 2; // 1+2+3..+n
        for (var i = 0; i < N; ++i)
        for (var j = 0; j < N; ++j)
            dl.AddItem($"c_{i}_{j}"); // cell i,j
        for (var k = 1; k <= n; ++k)
            dl.AddItem($"k_{k}", lowerBound: k, upperBound: k); // cover each of these k times


        for (var k = 1; k <= n; ++k)
        {
            for (var i = 0; i <= N - k; ++i)
            for (var j = 0; j <= N - k; ++j)
            {
                var opt = $"k_{k} ";
                for (var di = 0; di < k; ++di)
                for (var dj = 0; dj < k; ++dj)
                    opt += $"c_{i + di}_{j + dj} ";
                // Console.WriteLine(opt);
                dl.ParseOption(opt);
            }
        }

        dl.Solve();
        return dl.SolutionCount;

    }


}