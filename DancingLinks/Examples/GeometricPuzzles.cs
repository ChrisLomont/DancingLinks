using Lomont.Algorithms.Utility;

namespace Lomont.Algorithms.Examples;

public static class GeometricPuzzles
{
    public static void RunAll(DancingLinksSolver.SolverOptions opts)
    {
        // https://isomerdesign.com/Pentomino/4x15/index.html
        PolyominoesRectangle(opts, 10, 6); // exercise 271, 2339 without reflections - I get 9356 with, GOOD
        PolyominoesRectangle(opts, 12, 5); // exercise 268 - 1010 without reflections - i get 4040 , GOOD
        PolyominoesRectangle(opts, 15, 4); // I get 1472 solutions with reflections - correct 368 without reflections, GOOD
        PolyominoesRectangle(opts, 20, 3); // 8 solutions
        PolyominoesRectangle(opts, 3, 20); // 8 solutions 
        PolyominoesRectangle(opts, 2, 30); // none, cannot fit
        PolyominoesRectangle(opts, 1, 60); // none, cannot fit

        ScottsPentominoProblem(opts); // 520 solutions

        Dudeney(opts); // 16146 * 8 = 129168

        PackYSquare(opts);
    }

    [Puzzle("Number of solutions to a Soma Cube")]

    public static long Soma(DancingLinksSolver.SolverOptions opts, bool dump)

    { // 11520 solutions, 480 if "ell" piece not rotated, only shifted, form 240 mirror image pairs
        var sz = 3; // 0,1,2 cells
        var dl = new DancingLinksSolver {Options = opts };

        if (false)
        { // some polycube orientation testing

            //var p1 = Polycubes.GetSoma(0);
            //foreach (var q in p1.GetOrientations(allowFlips: false))
            //{
            //    q.Dump(Console.Out);
            //}
            //return 0;

            for (var i = 0; i < 7; ++i)
            {
                var s = Polycubes.GetSoma(i);
                var or = s.GetOrientations(allowFlips: false).ToList();
                var orf = s.GetOrientations(allowFlips: true).ToList();
                Console.WriteLine($"{s.Name} has {or.Count} orientations w/o flips, {orf.Count} with flips");
            }

            // test one
            var p = new Polycubes.Piece(new[] { 0, 0, 0 }, "single");
            Console.WriteLine(
                $"{p.Name} has {p.GetOrientations(false).Count()} orientations w/o flips, {p.GetOrientations(true).Count()} with flips");

            p = new Polycubes.Piece(new[] { 0, 0, 0, 1, 2, 3, 1, 1, 2, 2, 3, 1, 0, 2, 2, 3, 0, 2, 1, 3, 3 }, "chiral");
            Console.WriteLine(
                $"{p.Name} has {p.GetOrientations(false).Count()} orientations w/o flips, {p.GetOrientations(true).Count()} with flips");

            foreach (var q in p.GetOrientations(true))
            {
                //q.Dump(Console.Out);
            }

            return 0;
        }
        // cover each cell
        for (var i = 0; i < sz; ++i)
        for (var j = 0; j < sz; ++j)
        for (var k = 0; k < sz; ++k)
            dl.AddItem(Cell(i, j, k));
        // use each piece
        for (var p = 0; p < 7; ++p)
            dl.AddItem($"{Polycubes.GetSoma(p).Name}");

        // piece p covers cell i
        var optCount = 0; // todo - get from dl
        for (var p = 0; p < 7; ++p)
        {
            int localOptCount = 0;
            var piece = Polycubes.GetSoma(p);
            foreach (var or in piece.GetOrientations(allowFlips: false))
                for (var i = 0 - 2 * sz; i < 2 * sz; ++i)
                for (var j = 0 - 2 * sz; j < 2 * sz; ++j)
                for (var k = 0 - 2 * sz; k < 2 * sz; ++k)
                {
                    or.Shift(i, j, k);
                    {
                        if (or.CountMatching(p => Islegal(p.i, p.j, p.k)) == or.Size)
                        {
                            // piece in place, add option
                            var opt = $"{piece.Name} ";
                            foreach (var (x, y, z) in or.Coords())
                            {
                                opt += $"{Cell(x, y, z)} ";
                            }
                            dl.ParseOption(opt);
                            ++localOptCount;
                        }
                    }
                    or.Shift(-i, -j, -k);
                }

            optCount += localOptCount;
            if (dump)
                Console.WriteLine($"local opts {localOptCount}");
        }


        // pieces 0,1,..,6 have 12,24,12,12,12,12,8 base placements
        // leading to 144,144,72,72,96,96,64 = 688 options for the 3x3 case
        if (dump)
            Console.WriteLine($"Soma opts {optCount} = 688");

        bool Islegal(int i, int j, int k) => 0 <= i && 0 <= j && 0 <= k && i < sz && j < sz && k < sz;


        dl.Solve();

        return dl.SolutionCount;
        string Cell(int i, int j, int k) => $"{i}_{j}_{k}";
    }


    [Puzzle("A problem of Dudeney to cover a chessboard with 12 pentominoes and a 2x2 tetromino, in DLX paper")]
    public static void Dudeney(DancingLinksSolver.SolverOptions opts)
    {
        // dlx paper
        // 12 pentominoes and 2x2 square tetra to cover chessboard
        //  1,526,279,783 updates to determine that it is exactly 16,146. from paper
        // mine gets 129168 = 8*16,146 since I have not removed symmetries
        int w = 8, h = 8;
        var cells = new List<(int i, int j)>();
        for (var j = 0; j < h; ++j)
        for (var i = 0; i < w; ++i)
            cells.Add((i, j));

        bool OnBoard(Polyominoes.Piece piece)
            => piece.CountMatching(p => (0 <= p.i && p.i < w && 0 <= p.j && p.j < h)) == piece.Size;

        var pieces = Enumerable.Range(0, 12).Select(Utility.Polyominoes.GetPentomino).ToList();
        var p = new Polyominoes.Piece(new int[] { 0, 0, 1, 0, 0, 1, 1, 1 }, 1, 0, "tetromino");
        pieces.Add(p);
        Polyominoes(opts,cells, OnBoard, pieces);
    }

    [Puzzle("A problem of from the DLX paper on packing 45 'Y' pentominoes into a 15x15 square")]
    public static void PackYSquare(DancingLinksSolver.SolverOptions opts)
    { // from DLX paper, pack 45 'Y' pentominoes into 15x15 square
        int w = 15, h = 15;
        var cells = new List<(int i, int j)>();
        for (var j = 0; j < h; ++j)
        for (var i = 0; i < w; ++i)
            cells.Add((i, j));

        bool OnBoard(Polyominoes.Piece piece)
            => piece.CountMatching(p => (0 <= p.i && p.i < w && 0 <= p.j && p.j < h)) == 5;

        var pieces = Enumerable.Range(0, 45).Select(p => Utility.Polyominoes.GetPentomino(10/*'Y'-'O'*/)).ToList();
        for (var i = 0; i < pieces.Count; ++i)
            pieces[i].Name += "_" + i;
        Polyominoes(opts,cells, OnBoard, pieces);
    }



    [Puzzle("How many ways to pack polyominoes to cover w x h rectangle")]
    public static long PolyominoesRectangle(DancingLinksSolver.SolverOptions opts, int w = 20, int h = 3)
    {
        // 20x3 size is
        // piece k gives options 48,220,136,144,136,72,110,72,72,18,136,72 = 1236
        // items: piece names O-Z, each of 60 cells cij , gives 72
        // options: each position of a piece that fits 
        var cells = new List<(int i, int j)>();
        for (var j = 0; j < h; ++j)
        for (var i = 0; i < w; ++i)
            cells.Add((i, j));

        bool OnBoard(Polyominoes.Piece piece)
            => piece.CountMatching(p => (0 <= p.i && p.i < w && 0 <= p.j && p.j < h)) == 5;

        var pieces = Enumerable.Range(0, 12).Select(Utility.Polyominoes.GetPentomino).ToList();
        return Polyominoes(opts,cells, OnBoard, pieces);

    }


    [Puzzle("How many ways to pack polyominoes to cover the given set of cells")]
    public static long Polyominoes(
        DancingLinksSolver.SolverOptions opts,
        List<(int i, int j)> cells,
        Func<Polyominoes.Piece, bool>? legalPlacement,
        List<Polyominoes.Piece> pieces,
        bool dumpAll = false
    )
    {
        legalPlacement ??= p => p.CountMatching(cells.Contains) == 5;
        var dl = new DancingLinksSolver { Options = opts};

        // items
        // one per piece
        foreach (var p in pieces)
            dl.AddItem(p.Name);
        // one per cell
        foreach (var cell in cells)
            dl.AddItem($"c{cell.i}_{cell.j}");

        var minx = cells.Min(c => c.i);
        var maxx = cells.Max(c => c.i);
        var miny = cells.Min(c => c.j);
        var maxy = cells.Max(c => c.j);

        if (dumpAll)
        {
            //dl.Options.Verbosity |= AlgorithmX.SolverOptions.ShowFlags.AllSolutions;
            dl.SolutionListener += Dump;
        }

        // now all pieces in each spot:
        if (dumpAll)
            Console.Write("Piece option counts: \n   ");
        foreach (var piece1 in pieces)
        {
            int pieceOptionCount = 0;

#if false
        minx = -5;
        maxx = 5;
        miny = -5;
        maxy = 5;
        foreach (var piece in piece1.GetOrientations())
        {
            var op = MakeOption(piece);
            Dump(0, 0, new List<List<string>>{op.ToList()});
            Console.WriteLine("----");
        }
#else
            foreach (var piece in piece1.GetOrientations())
            {
                // shift over large region to ensure fits
                for (var i = minx; i <= maxx; ++i)
                for (var j = miny; j <= maxy; ++j)
                {
                    piece.Shift(i, j); // shift piece
                    if (legalPlacement(piece))
                    {
                        // add option, count them
                        dl.AddOption(MakeOption(piece));
                        pieceOptionCount++;
                    }

                    piece.Shift(-i, -j); // restore
                }
            }
            if (dumpAll)
                Console.Write($"{pieceOptionCount}, ");
#endif
        }


        if (dumpAll)
            Console.WriteLine();

        dl.Solve();
        if (dumpAll)
            dl.SolutionListener -= Dump;

        return dl.SolutionCount;

        // turn piece position into option
        string[] MakeOption(Polyominoes.Piece piece)
        {
            var option = piece.Name + " ";
            option += piece.Coords()
                .Select(p => $"c{p.x}_{p.y}")
                .Aggregate("", (a, b) => a + " " + b);
            return option.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        }


        bool Dump(long solutionNumber,
            long dequeueNumber,
            List<List<string>> solution
        )
        {
            var (ww, hh) = (maxx - minx + 1, maxy - miny + 1);


            var grid = new char[ww, hh];
            foreach (var s in solution)
            {
                var key = s[0];
                for (var k = 1; k < s.Count; ++k)
                {
                    var t = s[k];
                    var w = t.Split(new[] { 'c', '_' }, StringSplitOptions.RemoveEmptyEntries);
                    var (i, j) = (Int32.Parse(w[0]), Int32.Parse(w[1]));
                    grid[i - minx, j - miny] = key[0];
                }
            }

            for (var j = 0; j < hh; ++j)
            {
                for (var i = 0; i < ww; ++i)
                {
                    var ch = grid[i, j];
                    if (ch == 0) ch = '.';
                    Console.Write(ch);
                }
                Console.WriteLine();
            }

            return true;
        }

    }

    [Puzzle("Dana Scott's 1958 packing problem: place 12 pentominoes on a chessboard, leaving 4 squares vacant")]
    public static long ScottsPentominoProblem(DancingLinksSolver.SolverOptions opts)
    {
        // from Dancing Links paper, using new algorithm
        // Dana Scott, 1958
        // Place all 12 pentominoes on a chessboard leaving center 4 squares vacant

        // items 12 pentominoes, and 60 square indices
        // options: 1 identifying piece, five 1's in appropriate columns

        // 1568 options: 48, 248, 184, 192, 184, 96, 120, 96, 96, 24, 184, 96
        // 520 solutions

        var cells = new List<(int i, int j)>();
        for (var i = 0; i < 8; ++i)
        for (var j = 0; j < 8; ++j)
        {
            // avoid if all coords 3 or 4
            if ((i + 1) / 2 != 2 || (j + 1) / 2 != 2)
                cells.Add((i, j));
        }

        bool OnBoard(Polyominoes.Piece piece)
            => piece.CountMatching(cells.Contains) == 5;

        var pieces = Enumerable.Range(0, 12).Select(Utility.Polyominoes.GetPentomino).ToList();
        return Polyominoes(opts, cells, OnBoard, pieces);
    }


}