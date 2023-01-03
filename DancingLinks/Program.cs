﻿using System.Diagnostics;
using DancingLinks;
using Lomont.Algorithms;

Console.WriteLine("Dancing links testing and experiments");

// all reuse this - allows nicer control of settings
var dl = new DancingLinksSolver();

// todo - get toy dlx, toy secondary, toy colors, toy mult, toy mult with colors, etc. to quickly catch errors
// test toy problems
Test(1, () => ToyDlxPage68());
Test(1, () => ToyColorPage89(dump: false));
Test(92, () => NQueens(8, useSecondary: true));
Test(1, ()=>ToyMultiplicity());
Test(1, () => ToyMultiplicityWithColor());

// some options
dl.SetOutput(Console.Out);
//dl.Options.OutputFlags = DancingLinksSolver.SolverOptions.ShowFlags.All;
//dl.Options.OutputFlags = DancingLinksSolver.SolverOptions.ShowFlags.None;
//dl.Options.OutputFlags |= DancingLinksSolver.SolverOptions.ShowFlags.AllSolutions;
//dl.Options.MemsDumpStepSize = 100_000;
//dl.Options.MemsDumpStepSize = 10_000_000;
//dl.Options.MinimumRemainingValuesHeuristic = false;

// should have solutions for n = 8
//WainwrightPackingPage92(8); // long, needs tested
//return;
//for (var n = 1; n <=5; ++n)
//{
//    Console.WriteLine("Wain " + n);
//    WainwrightPackingPage92(n);
//}


// requires 5 queens to attack all squares
//NQueenCover(1, 8);
//NQueenCover(2, 8);
//NQueenCover(3, 8);
//NQueenCover(4, 8);
// NQueenCover(5, 8); // 4680 solns, 15 gigamems - TODO  -hits printing bug on progress - fgure out?


return;

//ToyMultiplicityWithColor();

long NQueenCover(int m, int n)
{ // how many queens to cover nxn, uses multiplicities
    dl.Clear();
    for (var i = 0; i < n; ++i)
    for (var j = 0; j < n; ++j)
        dl.AddItem(Cell(i,j), lowerBound:1, upperBound:m);
    dl.AddItem("#",lowerBound:m, upperBound:m); // number tag

    for (var i = 0; i < n; ++i)
    for (var j = 0; j < n; ++j)
    {
        var opt = $"# {Cell(i,j)} "; // start pos
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

        dl.AddOption(opt.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    string Cell(int i, int j) => $"c_{i}_{j}";


            dl.Solve();
    return dl.SolutionCount;
}

long WainwrightPackingPage92(int n)
{
    //n = 2;
    dl.Clear();
    // N=(1+2+..+n)^2=1^3+2^3+...+n^3
    var N = n * (n + 1) / 2; // 1+2+3..+n
    for (var i = 0; i < N; ++i)
    for (var j = 0; j < N; ++j)
        dl.AddItem($"c_{i}_{j}"); // cell i,j
    for (var k = 1; k <= n; ++k)
        dl.AddItem($"k_{k}", lowerBound:k, upperBound:k); // cover each of these k times


    for (var k = 1; k <= n; ++k)
    {
        for (var i = 0; i <= N - k; ++i)
        for (var j = 0; j <= N - k; ++j)
        {
            var opt = $"k_{k} ";
            for (var di = 0; di < k; ++di)
            for (var dj = 0; dj < k; ++dj)
                opt += $"c_{i+di}_{j+dj} ";
           // Console.WriteLine(opt);
            dl.AddOption(opt.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }
    }

    dl.Solve();
    return dl.SolutionCount;

}

return;
//Test(64,Exercise94); // tests colors
//return;

// tests - nothing super long to check
//Test(8, () => PolyominoesRectangle(20, 3));
//Test(14200, () => NQueens(12, organPipe: true, mrv: false));
//Test(500,()=>TestRandomVersusBruteForce(numTests:500));
//Test(520,ScottsPentominoProblem); // 520 solutions

//return;

//WordCube(4);
//Page68(dumpState: false);
//Dudney(); // 16146, ERROR - we do not match
ToyColorPage89(false);
//DoubleWordSquare(6);
//WordCube(3);

return;

//PackYSquare();
//return;

// 12 pieces, 5 cells = 60 size
dl.Options.MemsDumpStepSize = 100_000_000;

// https://isomerdesign.com/Pentomino/4x15/index.html
// PolyominoesRectangle(10, 6); // exercise 271, 2339 without reflections - I get 9356 with, GOOD
//PolyominoesRectangle(12, 5); // exercise 268 - 1010 without reflections - i get 4040 , GOOD
//PolyominoesRectangle(15, 4); // I get 1472 solutions with reflections - correct 368 without reflections, GOOD


//PolyominoesRectangle(20, 3); // 8 solution 
//PolyominoesRectangle(3, 20); // 8 solution 


//PolyominoesRectangle(2, 30); // none, cannot fit
//PolyominoesRectangle(1, 60); // none, cannot fit

ScottsPentominoProblem(); // 520 solutions

//return;

// experiments to match his 16-queens results
//NQueens(5, organPipe: false, mrv: false);
//NQueens(8, organPipe: false, mrv: false, true);

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

//NQueens(8, organPipe: false, mrv: true);
//NQueens(8, organPipe: true, mrv: false);
//NQueens(8, organPipe: true, mrv: true);


//NQueens(12, organPipe: true, mrv: false); //14200

//for (var n = 1; n <= 12; ++n) // 1,0,0,2, 10, 4, 40, 92, 352, 724, 2680, 14200, 73712 solutions
//    NQueens(n);

// 2 solutions
//Sudoku(
//    0,0,3, 0,1,0, 0,0,0,
//    0,0,0, 4,0,0, 1,0,0,
//    0,5,0, 0,0,0, 0,9,0,
//    2,0,0, 0,0,0, 6,0,4,
//    0,0,0, 0,3,5, 0,0,0,
//    1,0,0, 0,0,0, 0,0,0,
//    4,0,0, 6,0,0, 0,0,0,
//    0,0,0, 0,0,0, 0,5,0, 
//    0,9,0, 0,0,0, 0,0,0
//    );

// Polyominoes();

//Page68();
//Page68();
//return;
#if false
// Langford pairs have soln when n is 0 or 3 mod 4
dl.Options.MemsDumpStepSize = 10_000_000;
for (var n = 1; n < 4; ++n)
{

    LangfordPairs(n*4-1, false);
    LangfordPairs(n*4, false);
}
Trace.Assert(LangfordPairs(3, false)==2);
//LangfordPairs(7, false, 5);//1_000_000L);
//Trace.Assert(LangfordPairs(16,true,1_000_000) == 326_721_800);
#endif

long ToyMultiplicity(bool dump=false)
{
    dl.Clear();

    dl.AddItem("A");
    dl.AddItem("B");
    dl.AddItem("C", lowerBound: 2, upperBound: 3);
    dl.AddOption(new[] { "A", "B", "C" });
    dl.AddOption(new[] { "C" });

    if (dump)
        dl.DumpNodes(Console.Out);

    dl.Solve();
    return dl.SolutionCount;
}


long ToyMultiplicityWithColor(bool dump=false)
{
    // simple example with mult and colors
    dl.Clear();

    dl.AddItem("A");
    dl.AddItem("B");
    dl.AddItem("C", lowerBound: 2, upperBound: 3);
    dl.AddItem("X", secondary: true);
    dl.AddItem("Y", secondary: true);

    dl.AddOption(new[] {"A", "B", "X:1", "Y:1"});
    dl.AddOption(new[] {"A", "C", "X:2", "Y:2"});
    dl.AddOption(new[] {"C", "X:1"});
    dl.AddOption(new[] {"B", "X:2"});
    dl.AddOption(new[] {"C", "Y:2"});

    // unique solution { A C X:2 Y:2} { B X:2}  { C Y:2}

    if (dump)
        dl.DumpNodes(Console.Out);

    dl.Solve();
    return dl.SolutionCount;
}


long WordCube(int n)
{
    // word cube
    // todo - make Word(d1,d2,d3,...,dn) version
    dl.Clear();


    var lines = File.ReadAllLines(@"..\..\..\words.txt");
    var words = lines.Where(word => word.ToLower() == word && word.Length == n && AllAlpha(word)).ToList();
    Console.WriteLine($"{words.Count} {n}-letter words");

    bool AllAlpha(string s) => s.ToCharArray().All(Char.IsLetter);

    // items:
    for (var i = 0; i < n; ++i)
    for (var j = 0; j < n; ++j)
    {
        dl.AddItem($"x_{i}_{j}"); // x dir i,j on face
        dl.AddItem($"y_{i}_{j}"); // y dir i
        dl.AddItem($"z_{i}_{j}"); // z dir i
    }

    for (var i = 0; i < n; ++i)
    for (var j = 0; j < n; ++j)
    for (var k = 0; k < n; ++k)
        dl.AddItem(Cell(i, j, k), true); // cells have colors, are secondary
    foreach (var word in words)
        dl.AddItem(word, true);


    // options
    foreach (var w in words)
    {
        // x,y,z dirs, letters, cells:
        for (var i1 = 0; i1 < n; ++i1)
        for (var j1 = 0; j1 < n; ++j1)
        {
            // view i,j as going over a face, dir is perp to this

            var (i, j) = (i1, j1); // for lambda capture

            var opx = Op("x", k => (k, i, j));
            dl.AddOption(opx.Split(' ', StringSplitOptions.RemoveEmptyEntries));

            var opy = Op("y", k => (i, k, j));
            dl.AddOption(opy.Split(' ', StringSplitOptions.RemoveEmptyEntries));

            var opz = Op("z", k => (i, j, k));
            dl.AddOption(opz.Split(' ', StringSplitOptions.RemoveEmptyEntries));

            string Op(string dir, Func<int, (int x, int y, int z)> perm)
            {
                var op = $"{dir}_{i}_{j} ";
                // cell:letter
                for (var k = 0; k < n; ++k)
                {
                    var (x, y, z) = perm(k);
                    op += $"{Cell(x, y, z)}:{w[k]} ";
                }

                // word 
                op += w;
                return op;
            }

        }
    }

    dl.Solve();

    return dl.SolutionCount;

    string Cell(int i, int j, int k) => $"c_{i}_{j}_{k}";

}

long DoubleWordSquare(int n)
{   
    dl.Clear();
    // exercise 87
    // 2n distinct words in n rows and n columns

    var lines = File.ReadAllLines(@"..\..\..\words.txt");
    var words = lines.Where(word => word.ToLower() == word && word.Length == n && AllAlpha(word)).ToList();
    Console.WriteLine($"{words.Count} {n}-letter words");

    bool AllAlpha(string s) => s.ToCharArray().All(Char.IsLetter);

    // items:
    for (var i = 0; i < n; ++i)
    {
        dl.AddItem($"a_{i}"); // across i
        dl.AddItem($"d_{i}"); // down i
    }
    for (var i = 0; i < n; ++i)
    for (var j = 0; j < n; ++j)
        dl.AddItem(Cell(i,j),true); // cells have colors, are secondary
    foreach (var word in words)
        dl.AddItem(word,true);


    // options
    foreach (var w in words)
    {
        // across or down, letters, cells:
        for (var i = 0; i < n; ++i)
        {
            var op = $"a_{i} ";
            // cell:letter
            for (var j = 0; j < n; ++j)
                op += $"{Cell(i,j)}:{w[j]} ";

            // word 
            op += w;
            dl.AddOption(op.Split(' ',StringSplitOptions.RemoveEmptyEntries));
        }
        for (var j = 0; j < n; ++j)
        {
            var op = $"d_{j} ";
            // cell:letter
            for (var i = 0; i < n; ++i)
                op += $"{Cell(i, j)}:{w[i]} ";

            // word 
            op += w;
            dl.AddOption(op.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }
    }

    dl.Options.MemsDumpStepSize = 10_000_000;
    dl.Options.OutputFlags |= DancingLinksSolver.SolverOptions.ShowFlags.AllSolutions;
    dl.Solve();
    dl.Options.OutputFlags &= ~DancingLinksSolver.SolverOptions.ShowFlags.AllSolutions;

    string Cell(int i, int j) => $"c_{i}_{j}";

    return  dl.SolutionCount;
}



void Test(long answer, Func<long> func)
{
    var v = dl.Options.OutputFlags;
    dl.Options.OutputFlags = DancingLinksSolver.SolverOptions.ShowFlags.None;
    var ans = func();
    dl.Options.OutputFlags = v;
    Trace.Assert(answer==ans);
}

long ToyColorPage89(bool dump)
{
    dl.Clear();
    // three primary, two secondary
    dl.AddItem("p");
    dl.AddItem("q");
    dl.AddItem("r");
    dl.AddItem("x", true);
    dl.AddItem("y", true);

    dl.AddOption("p q x y:A".Split(' '));
    dl.AddOption("p r x:A y".Split(' ')); // change to x:C to get no solution
    dl.AddOption("p x:B".Split(' '));
    dl.AddOption("q x:A".Split(' '));
    dl.AddOption("r y:B".Split(' '));

    // solution: 'q x' and 'p r x y' // todo - needs colors chosen also on output
    
    if (dump)
        dl.DumpNodes(Console.Out);
    
    
    dl.Solve();
    return dl.SolutionCount;
}

long Exercise94()
{// tests colors
    // has unique solution, up to cyclic perm, complementation, reflection, so 1 * 16 * 2 * 2 = 64 solutions
    dl.Clear();
    for (var k = 0; k < 16; ++k)
    {
        dl.AddItem(k.ToString());
        dl.AddItem("p"+k);
    }
    for (var k = 0; k < 16; ++k)
        dl.AddItem("x" + k,secondary:true);

    for (var j = 0; j < 16; ++j)
    for (var k = 0; k < 16; ++k)
    {
        // j pk xk:a x(k+1):b x(k+3):c x(k+4):d
        // where indices taken mod 16, j = abcd in binary
        var d = j & 1;
        var c = (j >> 1) & 1;
        var b = (j >> 2) & 1;
        var a = (j >> 3) & 1;

        var opt = $"{j} p{k} x{k}:{a} x{(k + 1) % 16}:{b} x{(k + 3) % 16}:{c} x{(k + 4) % 16}:{d}";
        dl.AddOption(opt.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    dl.Solve();
    return dl.SolutionCount;
}

// test if all pass, else report and throw
long TestRandomVersusBruteForce(bool showResults = false, int numTests = 1000)
{
    int errors = 0, successes = 0;
    for (var testNumber = 0; testNumber < numTests; ++testNumber)
    {
        var dl = new DancingLinksSolver();
        dl.Options.OutputFlags |= DancingLinksSolver.SolverOptions.ShowFlags.AllSolutions;

        var r = new Random(testNumber);
        //r = new Random(19); // fails 1 != 2

        var bits = 5; // number of items in a bitfield
        var options = 16; // number of options to generate

        // generate instance
        List<int> optionNumbers = new();
        for (var optionNumber = 0; optionNumber < options; ++optionNumber)
        {
            var v = r.Next(1,(1 << bits)+1); // disallow 0
            optionNumbers.Add(v);
        }

        var danceCount = SolveDL(dl,bits,optionNumbers);
        var bruteCount = BruteForce(bits, optionNumbers);

        if (danceCount > 0 && showResults)
            Console.WriteLine($"{testNumber}: {danceCount}");
        // test brute force
        if (bruteCount != danceCount)
        {
            if (showResults)
            {
                Console.WriteLine($"ERROR {testNumber}: DL {danceCount} != Brute {bruteCount}");
                Console.Write("Vals: ");
                foreach (var v in optionNumbers)
                    Console.Write($"{v}, ");
                Console.WriteLine();
            }

            throw new Exception("Mismatch in TestBruteForce");
            return successes;
        }
        else successes++;
    }

    return successes;

    // Solve a DL problem, Algorithm X
    static long SolveDL(DancingLinksSolver dl, int bits, List<int> optionNumbers)
    {
        dl.Clear();
        for (var j = 0; j < bits; ++j)
            dl.AddItem(j.ToString());
        foreach (var v1 in optionNumbers)
        {
            {
                var v = v1;
                var op = new List<string>();
                for (var bit = 0; bit < bits; ++bit)
                {
                    if ((v & 1) == 1)
                        op.Add(bit.ToString());
                    v >>= 1;
                }

                dl.AddOption(op);
            }
        }
        var sw = new StringWriter();
        dl.SetOutput(sw);
        dl.Solve();
        dl.SetOutput(Console.Out);
        return dl.SolutionCount;
    }

    // Solve a count by brute force
    static int BruteForce(int bits, List<int> options)
    {
        var n = 1 << options.Count;
        int count = 0;
        for (var optionsSet = 0; optionsSet < n; ++optionsSet)
        {
            bool okCover = true;
            var cover = 0;
            for (var optionIndex = 0; optionIndex < options.Count; ++optionIndex)
            {
                if ((optionsSet & (1 << optionIndex)) != 0)
                {
                    var next = options[optionIndex];
                    okCover &= (cover & next) == 0;
                    cover |= next;
                }
            }

            okCover &= cover + 1 == 1 << bits;
            if (okCover)
            {
                count++;
                //Console.WriteLine($"BF {optionsSet}");
            }

        }
        return count;
    }
}

void Dudney()
{
    // dlx paper
    // 12 pentominoes and 2x2 square tetra to cover chessboard
    //  1,526,279,783 updates to determine that it is exactly 16,146. from paper
    int w = 8, h = 8;
    var cells = new List<(int i, int j)>();
    for (var j = 0; j < h; ++j)
    for (var i = 0; i < w; ++i)
        cells.Add((i, j));

    bool OnBoard(Polyominoes.Piece piece)
        => piece.CountMatching(p => (0 <= p.i && p.i < w && 0 <= p.j && p.j < h)) == piece.size;

    var pieces = Enumerable.Range(0, 12).Select(p => DancingLinks.Polyominoes.GetPentomino(p)).ToList();
    var p = new Polyominoes.Piece(new int[]{0,0,1,0,0,1,1,1},0,1,0,"tetromino");
    pieces.Add(p);
    Polyominoes(cells, OnBoard, pieces);
}

void PackYSquare()
{ // from DLX paper, pack 45 'Y' pentominoes into 15x15 square
    int w = 15, h = 15;
    var cells = new List<(int i, int j)>();
    for (var j = 0; j < h; ++j)
    for (var i = 0; i < w; ++i)
        cells.Add((i, j));

    bool OnBoard(Polyominoes.Piece piece)
        => piece.CountMatching(p => (0 <= p.i && p.i < w && 0 <= p.j && p.j < h)) == 5;

    var pieces = Enumerable.Range(0, 45).Select(p => DancingLinks.Polyominoes.GetPentomino(10/*'Y'-'O'*/)).ToList();
    for (var i = 0; i < pieces.Count; ++i)
        pieces[i].Name += "_"+i;
    Polyominoes(cells, OnBoard, pieces);
}



// pack polyominoes to cover wxh rectangle
long PolyominoesRectangle(int w = 20, int h = 3)
{
    // 20x3 size is
    // piece k gives options 48,220,136,144,136,72,110,72,72,18,136,72 = 1236
    // items: piece names O-Z, each of 60 cells cij , gives 72
    // options: each position of a piece that fits 
    var cells = new List<(int i, int j)>();
    for (var j = 0; j < h; ++j)
    for (var i = 0; i < w; ++i)
        cells.Add((i,j));

    bool OnBoard(Polyominoes.Piece piece)
        => piece.CountMatching(p => (0 <= p.i && p.i < w && 0 <= p.j && p.j < h)) == 5;

    var pieces = Enumerable.Range(0, 12).Select(p => DancingLinks.Polyominoes.GetPentomino(p)).ToList();
    return Polyominoes(cells, OnBoard, pieces);

}


// pack polyominoes to cover cells
// return # solutions
long Polyominoes(
    List<(int i, int j)> cells, 
    Func<Polyominoes.Piece,bool>? LegalPlacement,
    List<Polyominoes.Piece> pieces,
    bool dumpAll = false
)
{
    LegalPlacement ??= p => p.CountMatching(cells.Contains) == 5;

    dl.Clear();

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
                if (LegalPlacement(piece))
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


        var grid = new char[ww,hh];
        foreach (var s in solution)
        {
            var key = s[0];
            for (var k = 1; k < s.Count; ++k)
            {
                var t = s[k];
                var w = t.Split(new[] {'c','_'},StringSplitOptions.RemoveEmptyEntries);
                var (i,j) = (Int32.Parse(w[0]), Int32.Parse(w[1]));
                grid[i-minx,j-miny] = key[0];
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


void Sudoku(params int[] board)
{
    // number items 0-8
    // 4*9*9=324 items pij rik cjk bxk
    // 729 options pij rik cjk bxk for 0 <= i,j < 9, 1 <=k <= 9, x= 3 Floor(i/3) + Floor[j/3]
    // item pij must be one of the 9 options that fill cell cij, item rk must be covered by exacrlty on of the 9 options putting a k in row i, item bxk must be covered by one of the 9 optionsputting k in box x

    // then simply remove pij, rik, cjk, bxk for any given item, and any options with those
    //todo;
}

long NQueens(int n, bool organPipe = false, bool mrv = false, bool useSecondary = false)
{ // page 71
  // items row_i, column_j, a_s (upward diag s), b_d (downward diagonal d)
  // options
  //   - r_ cJ a(i+j), b(i-j) for queen placements
  //   - slack options a_s and b_d
  // symmetric only counted once: https://oeis.org/A002562, 1, 0, 0, 1, 2, 1, 6, 12, 46, 92, 341, 1787, 9233, 45752, 285053, 1846955, 11977939, 83263591, 621012754, 4878666808, 39333324973
  // all, including symmetries: https://oeis.org/A000170,   1, 0, 0, 2, 10, 4, 40, 92, 352, 724, 2680, 14200, 73712, 365596, 2279184, 14772512, 95815104, 666090624, 4968057848, 39029188884
  // remove symmetries with exercise 20,22,23

    dl.Clear();

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
            diags.Add($"a{i+j}"); // many dups here
            diags.Add($"b{i - j}");
        }
    }
    foreach (var d in diags)
        dl.AddItem(d, secondary:useSecondary);

    // options
    for (var i = 1; i <= n; ++i)
    for (var j = 1; j <= n; ++j)
        dl.AddOption($"r{i} c{j} a{i+j} b{i-j}".Split());
    // 4n-2 slack items
    Trace.Assert(diags.Count == 4*n-2);
    foreach (var d in diags)
        dl.AddOption(d);

    //dl.ProgressDelta = 10_000_000;
    dl.Options.MinimumRemainingValuesHeuristic = mrv;
    dl.Solve();
    return dl.SolutionCount;
}

long ScottsPentominoProblem()
{
    // from Dancing Links paper, using new algorithm
    // Dana Scott, 1958
    // Place all 12 pentominoes on a chessboard leaving center 4 squares vacant
    dl.Clear();


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

    var pieces = Enumerable.Range(0, 12).Select(p => DancingLinks.Polyominoes.GetPentomino(p)).ToList();
    return Polyominoes(cells, OnBoard, pieces);
}


long LangfordPairs(int n, bool useExercise15 = false)
{
    // solns for n that exist: https://oeis.org/A014552
    // 0, 0, 1, 1, 0, 0, 26, 150, 0, 0, 17792, 108144, 0, 0, 39809640, 326721800, 0, 0, 256814891280, 2636337861200, 0, 0, 3799455942515488, 46845158056515936, 0, 0, 111683611098764903232, 1607383260609382393152, 0, 0

    Console.WriteLine($"Langford pair size {n}");
    // has solution when n is 0,3 mod 4
    Trace.Assert(useExercise15 == false); // todo - do this and test
    dl.Clear();

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
            dl.AddOption(new[] { i.ToString(), $"s{j}", $"s{k}" });
    }

    dl.Solve();
    Console.WriteLine();
    return dl.SolutionCount;
}

long ToyDlxPage68(bool dumpState=false)
{
    dl.Clear();

    dl.AddItems("abcdefg".Select(c => c.ToString()).ToList());

    // check secondary structure
    //dl.AddItem("h",true);
    //dl.AddItem("i", true);
    //dl.AddItem("j", true);

    var opts = new[] { "ce", "adg", "bcf", "adf", "bg", "deg" };
    dl.AddOptions(opts
        .Select(s => s.ToCharArray().Select(c => c.ToString()).ToList())
    );

    if (dumpState)
        dl.DumpNodes(Console.Out); // check against page 68 TAOCP 4B
    dl.Solve();

    return dl.SolutionCount;
}