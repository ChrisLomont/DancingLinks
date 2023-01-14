using System.Diagnostics;
using Lomont.Algorithms.Utility;

namespace Lomont.Algorithms.Examples;

public static class ChessPuzzles
{
    public static void RunAll(DancingLinksSolver.SolverOptions opts)
    {
        NQueens(opts, 8, mrv: true, topK: 0); // 92 solutions
        

        // experiments to match his 16-queens results
        NQueens(opts, 5, organPipe: false, mrv: false);  // 10 solutions
        NQueens(opts, 8, organPipe: false, mrv: false, true); // 92 solutions

        // use slack vars
        //Dancing links work
        //    after 0 mems: 0 sols, 0.50000
        //92 solutions in 00:00:00.0166750, 821268 mems, 8087 bytes memory
        //Altogether 92 solutions, 0+821268 mems
        //38992 updates, 8087 bytes, 3815 nodes
        // use secondary
        // Dancing links work
        //     after 0 mems: 0 sols, 0.50000
        // 92 solutions in 00:00:00.0167821, 671823 mems, 8087 bytes memory
        // Altogether 92 solutions, 0+671823 mems
        // 37704 updates, 8087 bytes, 2527 nodes

        NQueens(opts, 8, organPipe: false, mrv: true); // 92 solutions
        NQueens(opts, 8, organPipe: true, mrv: false); // 92 solutions
        NQueens(opts, 8, organPipe: true, mrv: true);  // 92 solutions


        NQueens(opts, 12, organPipe: true, mrv: false); //14200

        for (var n = 1; n <= 12; ++n) // 1,0,0,2, 10, 4, 40, 92, 352, 724, 2680, 14200, 73712 solutions
            NQueens(opts, n);

        NQueenCover(opts, 3, 8); // 0 solns - todo - lots of print errors - figure out and fix, set memstep to 1 to see immediately

        NQueenCover(opts, 1, 1); // 1 soln
        NQueenCover(opts, 1, 2); // 4 solns
        NQueenCover(opts, 1, 3); // 1 soln
        NQueenCover(opts, 1, 4); // 0 soln
        NQueenCover(opts, 2, 4); // 12 solns
        NQueenCover(opts, 3, 4); // 320 solns
        NQueenCover(opts, 4, 4); // 1636 solns
        NQueenCover(opts, 2, 5); // 0 solns
        NQueenCover(opts, 3, 5); // 186 solns
        NQueenCover(opts, 2, 6); // 0 solns
        NQueenCover(opts, 3, 6); // 4 solns
        NQueenCover(opts, 3, 7); // 0 solns
        NQueenCover(opts, 4, 7); // 86 solns
        NQueenCover(opts, 3, 8); // 0 solns - todo - lots of print errors - figure out and fix, set memstep to 1 to see immediately

        // requires 5 queens to attack all squares
        NQueenCover(opts, 1, 8);
        NQueenCover(opts, 2, 8);
        NQueenCover(opts, 3, 8);
        NQueenCover(opts, 4, 8);
        NQueenCover(opts, 5, 8); // 4680 solns, 15 gigamems - TODO  -hits printing bug on progress - fgure out?

        NQueenCover(opts, 5, 8, true); // 284 solns (36 w/o symmetries?)

        
    }

    [Puzzle("Now many queens needed to cover all squares on a m x n chess board")]
    public static long NQueenCover(DancingLinksSolver.SolverOptions opts, int m, int n, bool noEdge = false)
    { // how many queens to cover nxn, uses multiplicities
        var dl = new DancingLinksSolver {Options = opts};
        for (var i = 0; i < n; ++i)
        for (var j = 0; j < n; ++j)
            dl.AddItem(Cell(i, j), lowerBound: 1, upperBound: m);
        dl.AddItem("#", lowerBound: m, upperBound: m); // number tag

        for (var i = 0; i < n; ++i)
        for (var j = 0; j < n; ++j)
        {
            if (noEdge && (i == 0 || j == 0 || i == n - 1 || j == n - 1)) continue;
            var opt = $"# {Cell(i, j)} "; // start pos
            // attacked squares
            for (var di = -1; di <= 1; ++di)
            for (var dj = -1; dj <= 1; ++dj)
            {
                if (di == 0 && dj == 0) continue;
                var (x, y) = (i + di, j + dj);
                while (0 <= x && 0 <= y && x < n && y < n)
                {
                    opt += Cell(x, y) + " ";
                    x += di;
                    y += dj;
                }
            }

            dl.ParseOption(opt);
        }

        string Cell(int i, int j) => $"c_{i}_{j}";


        dl.Solve();
        return dl.SolutionCount;
    }


    [Puzzle("Now many ways to place N on-attacking queens on a NxN chessboard")]
    public static long NQueens(
        DancingLinksSolver.SolverOptions opts,
        int n,
        bool organPipe = false,
        bool mrv = false,
        bool useSecondary = false,
        int topK = 0 // >0 for max cost, < 0 for minCost
    )
    { // page 71
        // items row_i, column_j, a_s (upward diag s), b_d (downward diagonal d)
        // options
        //   - r_ cJ a(i+j), b(i-j) for queen placements
        //   - slack options a_s and b_d
        // symmetric only counted once: https://oeis.org/A002562, 1, 0, 0, 1, 2, 1, 6, 12, 46, 92, 341, 1787, 9233, 45752, 285053, 1846955, 11977939, 83263591, 621012754, 4878666808, 39333324973
        // all, including symmetries: https://oeis.org/A000170,   1, 0, 0, 2, 10, 4, 40, 92, 352, 724, 2680, 14200, 73712, 365596, 2279184, 14772512, 95815104, 666090624, 4968057848, 39029188884
        // remove symmetries with exercise 20,22,23

        var dl = new DancingLinksSolver
        {
            Options = opts
        };

        // cost for max or min
        long Cost(int i, int j)
        {
            var dx = i - (n + 1) / 2.0;
            var dy = j - (n + 1) / 2.0;
            var dist = Math.Sqrt(dx * dx + dy * dy);
            // dist ok for mins, i.e., when topK < 0
            if (topK > 0)
                dist = 2 * n - dist; // invert to get maximum

            dist *= 100; // scale for rounding

            return (long)dist;
        }

        // items
        var diags = new HashSet<string>();
        for (var i = 1; i <= n; ++i)
        {
            var ind = i; // index
            if (organPipe)
            { // floor[n/2] + offset, where
                // offset +1,0,+2,-1,+3,-2,... till all covered
                ind = n / 2; // floor
                var odd = (i & 1) == 1;
                if (odd)
                    ind += (i + 1) / 2;
                else
                    ind -= (i / 2 - 1);
                Trace.Assert(0 <= ind && ind <= n);
            }
            dl.AddItem($"r{ind}");
            dl.AddItem($"c{ind}");

            // todo if organPipe
            // slack vars
            for (var j = 1; j <= n; ++j)
            {
                diags.Add($"a{i + j}"); // many dups here
                diags.Add($"b{i - j}");
            }
        }
        foreach (var d in diags)
            dl.AddItem(d, secondary: useSecondary);

        // options
        for (var i = 1; i <= n; ++i)
        for (var j = 1; j <= n; ++j)
        {
            var opt = $"r{i} c{j} a{i + j} b{i - j}";
            if (topK != 0)
                opt += $"${Cost(i, j)}"; // attach to last item, affects entire option
            dl.AddOption(opt);
        }

        // 4n-2 slack items
        Trace.Assert(diags.Count == 4 * n - 2);
        foreach (var d in diags)
            dl.AddOption(d);

        //dl.ProgressDelta = 10_000_000;
        dl.Options.MinimumRemainingValuesHeuristic = mrv;
        dl.Solve();
        return dl.SolutionCount;
    }

}