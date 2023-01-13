// Chris Lomont 2023
// various problems solved from Knuth books and papers to
// illustrate use of Dancing Links variants

using System.Diagnostics;
using System.Text.RegularExpressions;
using DancingLinks;
using Lomont.Algorithms;

// testing to make insert works well
//DancingLinksSolver.MaxHeap.Test();

Console.WriteLine("Dancing links testing and experiments");

// all reuse this - allows nicer control of settings
var dl = new DancingLinksSolver();


// todo - get toy dlx, toy secondary, toy colors, toy mult, toy mult with colors, etc. to quickly catch errors

// test toy problems
if (true)
{
    Test(4,()=>PrimeSquares(1, 3, 3)); // answer: 997 787 733

    Test(1, () => ToyDlxPage68());
    Test(1, () => ToyColorPage89(dump: false));
    Test(92, () => NQueens(8, useSecondary: true));
    Test(1, () => ToyMultiplicity());
    Test(1, () => ToyMultiplicityWithColor());
    Test(64, Exercise94); // tests colors
    Test(11520, () => Soma(false));
}

return;


//return;

// set some useful options
dl.SetOutput(Console.Out);
// dl.Options.MinimumRemainingValuesHeuristic = false;
//dl.Options.OutputFlags = DancingLinksSolver.SolverOptions.ShowFlags.All;
// dl.Options.OutputFlags = DancingLinksSolver.SolverOptions.ShowFlags.None;
//dl.Options.OutputFlags |= DancingLinksSolver.SolverOptions.ShowFlags.AllSolutions;
dl.Options.MemsDumpStepSize = 1;
//dl.Options.MemsDumpStepSize = 100_000;
//dl.Options.MemsDumpStepSize = 1_000_000;
//dl.Options.MemsDumpStepSize = 10_000_000;
dl.Options.MemsDumpStepSize = 100_000_000;
//dl.Options.MemsDumpStepSize = 1_000_000_000;

//NQueens(12, mrv: true, topK:0); // 92 solutions

//PrimeSquares(1, 3, 3); // answer: 997 787 733
//PrimeSquares(1, 3, 4); // answer:  8999 8699 7717
//PrimeSquares(-1, 3, 3); // answer: 113 307 139
//PrimeSquares(-1, 3, 4); // answer: 2111 1031 1193
//PrimeSquares(-1, 3, 5); // answer: 21211 10301 11393
//PrimeSquares(-1, 3, 6); // answer: 111211 100103 331171
//PrimeSquares(-1, 3, 7); // answer: 1111211 1000403 3193171
return;

// not working well yet?!
// PrimeSquares(10); // 10 largest solutions


WordShape("most_common_words.txt");//,numWords:130);
//DoubleWordSquare(3, "most_common_words.txt", numWords: 100);

//WordCube(2, "most_common_words.txt", numWords: 62); // 12 solutions
//WordCube(3, "most_common_words.txt",numWords:470);//, "sgb_words.txt",2500); // none at 350, 425, some at 500, some at 470
//WordCube(3, wordfile:"primes.txt", allLetters:false); // finds one, crashes
//WordCube(4, wordfile: "primes.txt", allLetters: false, numWords:1000); // has formatting bug, finds 
//WordCube(5, wordfile: "primes.txt", allLetters: false); // has formatting bug, 8363 primes, finds 
//WordCube(5, wordfile: "primes.txt", allLetters: false); // crashes

//DoubleWordSquare(5, "sgb_words.txt", specialWord:"chris");
//DoubleWordSquare(4, specialWord: "abbe", specialIndex:0);
//DoubleWordSquare(6, specialWord: "stacie", specialIndex: 5);
//DoubleWordSquare(5, specialWord: "chris", specialIndex: 3);
//DoubleWordSquare(3,specialWord:"the",specialIndex:0);
//DoubleWordSquare(12); // specialWord:"the",specialIndex:0); // none

//ToyColorPage89(dump:true);

//WordP94(true); // todo - bad fields in print progress

// should have solutions for n = 8
//WainwrightPackingPage92(8); // long, needs tested
//return;
//for (var n = 1; n <=5; ++n)
//{
//    Console.WriteLine("Wain " + n);
//    WainwrightPackingPage92(n);
//}

//NQueenCover(3, 8); // 0 solns - todo - lots of print errors - figure out and fix, set memstep to 1 to see immediately


//NQueenCover(1, 1); // 1 soln
//NQueenCover(1, 2); // 4 solns
//NQueenCover(1, 3); // 1 soln
//NQueenCover(1, 4); // 0 soln
//NQueenCover(2, 4); // 12 solns
//NQueenCover(3, 4); // 320 solns
//NQueenCover(4, 4); // 1636 solns
//NQueenCover(2, 5); // 0 solns
//NQueenCover(3, 5); // 186 solns
//NQueenCover(2, 6); // 0 solns
//NQueenCover(3, 6); // 4 solns
//NQueenCover(3, 7); // 0 solns
//NQueenCover(4, 7); // 86 solns
//NQueenCover(3, 8); // 0 solns - todo - lots of print errors - figure out and fix, set memstep to 1 to see immediately
// requires 5 queens to attack all squares
//NQueenCover(1, 8);
//NQueenCover(2, 8);
//NQueenCover(3, 8);
//NQueenCover(4, 8);
// NQueenCover(5, 8); // 4680 solns, 15 gigamems - TODO  -hits printing bug on progress - fgure out?

// NQueenCover(5, 8, true); // 284 solns (36 w/o symmetries?)

//WordCube(4);

//return;

//ToyMultiplicityWithColor();
//return;

// tests - nothing super long to check
//Test(8, () => PolyominoesRectangle(20, 3));
//Test(14200, () => NQueens(12, organPipe: true, mrv: false));
//Test(500,()=>TestRandomVersusBruteForce(numTests:500));
//Test(520,ScottsPentominoProblem); // 520 solutions

//return;

WordCube(4);
//Page68(dumpState: false);
//Dudney(); // 16146, ERROR - we do not match
ToyColorPage89(false);
//DoubleWordSquare(6);
//WordCube(3);

//return;

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

// |topN| is number to find, topN > 0 get largest, topN < 0 gets smallest
    long PrimeSquares(int topN, int width = 5, int height = 5)
{
    // 5x5 squares of primes made of 10 different primes with smallest or largest possible product
    // largest 5 can be found in 22 Gu, so reasonable
    // shown in (111) and (112)
    dl.Clear();
    var primesW = GetWords("primes.txt", width, allLetters: false).Select(Int32.Parse).ToList();
    var primesH = primesW;
    Console.WriteLine($"{primesW.Count} {width}-digit primes loaded from {primesW[0]} to {primesW.Last()}");
    if (width != height)
    {
        primesH = GetWords("primes.txt", height, allLetters: false).Select(Int32.Parse).ToList();
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
    if (width!=height)
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

    var pm = Math.Max(primesH.Max(),primesW.Max());
    var C = 10 / Math.Log(pm / (pm - 2.0));

    // product of primes. By default, alg gets smallest solution,
    // if we want largest solution, we make these of form K-cost where K-cost > 0
    // and sum still fits in a int64
    var K = 4*(width + height) * C*Math.Log(pm);

    // cost function, 
    long Cost(long val)
    {
        var cost = Math.Floor(C * Math.Log(val));
        if (topN > 0)
            cost = K - cost;
        var icost = (long) cost;
        Trace.Assert(0 < icost && icost < Int64.MaxValue/(2*(width+height)));
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
        DumpCellSolution(++s, cost, solution);
    }

    return dl.SolutionCount;

    string Cell(int i, int j) => $"c_{i}_{j}";
}

// reusable solution formatter for char grid like solutions
// items must be formatted using the following conventions:
// cells as c_i_j:char
bool DumpCellSolution(long solutions, long moves, List<List<string>> options)
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
            var i = Int32.Parse(m.Groups["i"].Value);
            var j = Int32.Parse(m.Groups["j"].Value);
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
    foreach (var (i,j,t) in tups)
    {
        if (t.Length != 1)
            Console.WriteLine($"Error! - {t} more than one char");
        else
        {
            if (c[i, j] != 0 && c[i,j] != t[0])
                Console.WriteLine($"Error! - ({i},{j}) over-specified with {c[i,j]} and {t}");
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


long Soma(bool dump)
{ // 11520 solutions, 480 if "ell" piece not rotated, only shifted, form 240 mirror image pairs
    var sz = 3; // 0,1,2 cells
    dl.Clear();

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
        var p = new Polycubes.Piece(new[] {0, 0, 0}, "single");
        Console.WriteLine(
            $"{p.Name} has {p.GetOrientations(false).Count()} orientations w/o flips, {p.GetOrientations(true).Count()} with flips");

        p = new Polycubes.Piece(new[] {0, 0, 0, 1, 2, 3, 1, 1, 2, 2, 3, 1, 0, 2, 2, 3, 0, 2, 1, 3, 3}, "chiral");
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
    for (var p =0; p < 7; ++p)
        dl.AddItem($"{Polycubes.GetSoma(p).Name}");

    // piece p covers cell i
    var optCount = 0; // todo - get from dl
    for (var p = 0; p < 7; ++p)
    {
        int localOptCount = 0;
        var piece = Polycubes.GetSoma(p);
        foreach (var or in piece.GetOrientations(allowFlips: false))
            for (var i = 0 - 2*sz; i < 2*sz; ++i)
            for (var j = 0 - 2*sz; j < 2*sz; ++j)
            for (var k = 0 - 2*sz; k < 2*sz; ++k)
            {
                or.Shift(i, j, k);
                {
                    if (or.CountMatching(p=> Islegal(p.i,p.j,p.k)) == or.Size)
                    {
                        // piece in place, add option
                        var opt = $"{piece.Name} ";
                        foreach (var (x,y,z) in or.Coords())
                        {
                            opt += $"{Cell(x,y,z)} ";
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


List<string> GetWords(
    string filename, 
    int len, 
    bool toUppercase = false, 
    bool toLower = false, 
    bool allLetters = true, 
    int count = -1)
{
    var path = @"..\..\..\data\"; // todo - make smarter - search up and down current position for them
    var words = File.ReadAllLines( path + filename).ToList();
    words = words.Where(w => w.Length == len).ToList();
    if (allLetters)
        words = words.Where(AllAlpha).ToList();
    if (toUppercase)
        words = words.Select(w=>w.ToUpper()).ToList();
    if (count > 0)
        words = words.Take(count).ToList();
    return words;

    bool AllAlpha(string s) => s.ToCharArray().All(Char.IsLetter);
}


long WordP94(bool showSolutions)
{ 
    // colors and multiplicity
    // 4x5 word rectangles from top 1000 words in WORDS(1000);
    // at most 8 letters
    // Knuth finds 8 such answers, but not sure if our 4 word list is same as his

    dl.Clear();
    for (var i =0; i < 4; ++i)
        dl.AddItem($"A{i}");
    for (var i = 0; i < 5; ++i)
        dl.AddItem($"D{i}");
    for (var i =0; i < 26; ++i)
        dl.AddItem($"#{(char)(i+'A')}");
    dl.AddItem("#",lowerBound:8, upperBound:8);

    for (var i = 0; i < 26; ++i)
        dl.AddItem($"{(char)(i+'A')}", secondary:true);
    for (var i = 0 ; i < 4; ++i)
    for (var j = 0; j < 5; ++j)
        dl.AddItem($"{i}_{j}", secondary: true);

    // letter counting by these 2*26 options
    for (var i = 0; i < 26; ++i)
    {
        var c = (char)('A' + i);
        dl.ParseOption($"#{c} {c}:0");
        dl.ParseOption($"#{c} {c}:1 #");
    }

    // now add words
    var (num4,num5) = (1000,2000);
    var len4 = GetWords("most_common_words.txt", 4, toUppercase: true, count:num4);
    var len5 = GetWords("sgb_words.txt",5, toUppercase:true, count:num5);

    Func(dl, len5, 4, 'A', (i, j) => $"{i}_{j}");
    Func(dl, len4, 5, 'D', (i, j) => $"{j}_{i}");

    static void Func(DancingLinksSolver dl, IList<string> words, int wid, char dir, Func<int,int,string> toCell)
    {
        foreach (var word in words)
        {
            var letters = new int[26];
            foreach (var w in word)
                letters[w - 'A']++;
            for (var i = 0; i < wid; ++i)
            {
                var opt = $"{dir}{i} ";
                var j = 0;
                foreach (var c in word)
                    opt += $"{toCell(i, j++)}:{c} ";

                j = 0;
                for (var k = 0; k < 26; ++k)
                {
                    if (letters[k] != 0)
                    {
                        var c = (char) (k + 'A');
                        opt += $"{c}:1 ";
                        ++j;
                    }
                }
                dl.ParseOption(opt);
            }
        }
    }


    if (showSolutions)
        dl.SolutionListener += Dump;

    dl.Solve();

    if (showSolutions)
        dl.SolutionListener -= Dump;

    bool Dump(long solutionNumber,
        long dequeueNumber,
        List<List<string>> solution
    )
    {
        Console.WriteLine($"Solution {solutionNumber} :");
        foreach (var option in solution)
        {
            Console.Write("   ");
            foreach (var item in option)
                Console.Write($"{item} ");

            Console.WriteLine();
        }

        return true;
    }

    return dl.SolutionCount;
}

long NQueenCover(int m, int n, bool noEdge = false)
{ // how many queens to cover nxn, uses multiplicities
    dl.Clear();
    for (var i = 0; i < n; ++i)
    for (var j = 0; j < n; ++j)
        dl.AddItem(Cell(i, j), lowerBound: 1, upperBound: m);
    dl.AddItem("#", lowerBound: m, upperBound: m); // number tag

    for (var i = 0; i < n; ++i)
    for (var j = 0; j < n; ++j)
    {
        if (noEdge && (i ==0 || j == 0 || i == n-1 || j == n-1)) continue;
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


long ToyMultiplicity(bool dump=false)
{
    dl.Clear();

    dl.AddItem("A");
    dl.AddItem("B");
    dl.AddItem("C", lowerBound: 2, upperBound: 3);
    dl.AddOption("A B C");
    dl.AddOption("C");

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

    dl.AddOption("A B X:1 Y:1");
    dl.AddOption("A C X:2 Y:2");
    dl.AddOption("C X:1");
    dl.AddOption("B X:2");
    dl.AddOption("C Y:2");

    // unique solution { A C X:2 Y:2} { B X:2}  { C Y:2}

    if (dump)
        dl.DumpNodes(Console.Out);

    dl.Solve();
    return dl.SolutionCount;
}


long WordCube(int n, string wordfile="",int numWords = -1, bool allLetters = false)
{
    // word cube
    // todo - make Word(d1,d2,d3,...,dn) version
    dl.Clear();

    if (wordfile == "")
        wordfile = "words.txt";
    var words = GetWords(wordfile, n, toLower: true, allLetters:allLetters);
    if (numWords > 0)
        words = words.Take(numWords).ToList();

    Console.WriteLine($"{words.Count} {n}-letter words");

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
            dl.ParseOption(opx);

            var opy = Op("y", k => (i, k, j));
            dl.ParseOption(opy);

            var opz = Op("z", k => (i, j, k));
            dl.ParseOption(opz);

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

    dl.SolutionListener += Dump;
    dl.Solve();
    dl.SolutionListener -= Dump;

    return dl.SolutionCount;

    string Cell(int i, int j, int k) => $"c_{i}_{j}_{k}";

    bool Dump(long solutions,long moves,List<List<string>> options)
    {
        var c = new char[n, n, n];
        foreach (var items in options)
        {
            var loc = items[0]; // x_3_2
            var word = items.Last(); 
            var w = loc.Split('_').Skip(1).ToList();
            var c1 = Int32.Parse(w[0]);
            var c2 = Int32.Parse(w[1]);
            var (x, y, z, dx, dy, dz) = loc[0] switch
            {
                'x' => (0, c1, c2, 1, 0, 0),
                'y' => (c1, 0, c2, 0, 1, 0),
                'z' => (c1, c2, 0, 0, 0, 1),
                _ => throw new NotImplementedException()
            };
            foreach (var ch in word)
            {
                c[x, y, z] = ch;
                x += dx;
                y += dy;
                z += dz;
            }
        }

        Console.WriteLine("Solution : top layer on down");
        for (var z = 0; z < n; ++z)
        {
            for (var x = 0; x < n; ++x)
            {
                for (var y = 0; y < n; ++y)
                    Console.Write(c[x, y, z]);
                Console.WriteLine();
            }
            Console.WriteLine();
        }

        return true;
    }
}

long WordShape(string wordfile = "", int numWords = -1, bool allLetters = false)
{
    // word shape - fill cells

#if true
    // 7 wide disk
    var cells = new int[]
    {
                    6, 2, 6, 3, 6, 4, // top
              5, 1, 5, 2, 5, 3, 5, 4, 5, 5,
        4, 0, 4, 1, 4, 2, 4, 3, 4, 4, 4, 5, 4, 6,
        3, 0, 3, 1, 3, 2, 3, 3, 3, 4, 3, 5, 3, 6,
        2, 0, 2, 1, 2, 2, 2, 3, 2, 4, 2, 5, 2, 6,
              1, 1, 1, 2, 1, 3, 1, 4, 1, 5,
                    0, 2, 0, 3, 0, 4, 
    }.Chunk(2).Select(p => (i: p[0], j: p[1])).ToList();
#endif
#if false
    // 7 torus
    var cells = new int[]
    {
                    6, 2, 6, 3, 6, 4, // top
              5, 1, 5, 2, 5, 3, 5, 4, 5, 5,
        4, 0, 4, 1, 4, 2,       4, 4, 4, 5, 4, 6,
        3, 0, 3, 1,                   3, 5, 3, 6,
        2, 0, 2, 1, 2, 2,       2, 4, 2, 5, 2, 6,
              1, 1, 1, 2, 1, 3, 1, 4, 1, 5,
                    0, 2, 0, 3, 0, 4, 
    }.Chunk(2).Select(p => (i: p[0], j: p[1])).ToList();
#endif
#if false
    // 5 disk
    var cells = new int[]
    {
              4, 1, 4, 2, 4, 3, 
        3, 0, 3, 1, 3, 2, 3, 3, 3, 4, 
        2, 0, 2, 1, 2, 2, 2, 3, 2, 4, 
        1, 0, 1, 1, 1, 2, 1, 3, 1, 4, 
              0, 1, 0, 2, 0, 3, 
    }.Chunk(2).Select(p => (i: p[0], j: p[1])).ToList();
#endif
#if false
    // 5 torus
    var cells = new int[]
    {
              4, 1, 4, 2, 4, 3,
        3, 0, 3, 1, 3, 2, 3, 3, 3, 4,
        2, 0, 2, 1,       2, 3, 2, 4,
        1, 0, 1, 1, 1, 2, 1, 3, 1, 4,
              0, 1, 0, 2, 0, 3,
    }.Chunk(2).Select(p => (i: p[0], j: p[1])).ToList();
#endif
#if false
    // 3 sq
    var cells = new int[]
    {
        2, 0, 2, 1, 2, 2,
        1, 0, 1, 1, 1, 2,
        0, 0, 0, 1, 0, 2,
    }.Chunk(2).Select(p => (i: p[0], j: p[1])).ToList();
#endif

    dl.Clear();

    var runs = new List<((int i, int j), (int di, int dj), int len)>();

    // get list of rows and columns by lengths
    var minx = cells.Min(p => p.i);
    var maxx = cells.Max(p => p.i);
    var miny = cells.Min(p => p.j);
    var maxy = cells.Max(p => p.j);
    for (var i = minx; i <= maxx; ++i)
    for (var j = miny; j <= maxy; ++j)
    {
        int d1, d2;
        if (cells.Contains((i, j)))
        {
            // go up and down to edges
            d1 = 0;
            while (cells.Contains((i + d1, j)))
                d1--;
            d1++;
            d2 = 0;
            while (cells.Contains((i + d2, j)))
                d2++;
            d2--;
            var run1 = ((i + d1, j), (1, 0), d2 - d1 + 1);
            if (!runs.Contains(run1))
                runs.Add(run1);

            // go left and right to edges
            d1 = 0;
            while (cells.Contains((i, j + d1)))
                d1--;
            d1++;
            d2 = 0;
            while (cells.Contains((i, j + d2)))
                d2++;
            d2--;
            var run2 = ((i, j + d1), (0, 1), d2 - d1 + 1);
            if (!runs.Contains(run2))
                runs.Add(run2);
        }
    }

    Console.WriteLine($"{runs.Count} slots to fill");

    // words by run length
    var wordLists = new Dictionary<int, List<string>>();
    foreach (var r in runs)
        if (!wordLists.ContainsKey(r.len))
        {
            var words = GetWords(wordfile, r.len, toLower: true, allLetters: allLetters);
            if (numWords > 0) words = words.Take(numWords).ToList();
            wordLists.Add(r.len, words);
            Console.WriteLine($"{words.Count} words of length {r.len} added");
        }


    // items:
    // across and down items
    foreach (var ((i, j), (di, dj), _) in runs)
    {
        if (di == 1)
            dl.AddItem($"a_{i}_{j}"); // across direction
        if (dj == 1)
            dl.AddItem($"d_{i}_{j}"); // down direction
    }

    foreach (var (i, j) in cells)
        dl.AddItem(Cell(i, j), true); // cells have colors, are secondary

    // all words
    foreach (var list in wordLists.Values)
    foreach (var word in list)
        dl.AddItem(word, true);

    // options
    foreach (var ((i, j), (di, dj), len) in runs)
    foreach (var word in wordLists[len])
    {
        string op = "";
        if (di == 1)
            op = $"a_{i}_{j} "; // across
        else
            op = $"d_{i}_{j} "; // down

        var (i1, j1) = (i, j);
        foreach (var ch in word)
        {
            op += $"{Cell(i1, j1)}:{ch} ";
            i1 += di;
            j1 += dj;
        }

        op += word;
        dl.ParseOption(op);
    }

    Console.WriteLine($"{dl.OptionCount} options");


    dl.SolutionListener += Dump;
    dl.Solve();
    dl.SolutionListener -= Dump;

    return dl.SolutionCount;

    string Cell(int i, int j) => $"c_{i}_{j}";

    bool Dump(long solutions, long moves, List<List<string>> options)
    {
        var c = new char[maxx - minx + 1, maxy - miny + 1];
        foreach (var items in options)
        {
            var loc = items[0]; // a/d_3_2
            var word = items.Last();
            var w = loc.Split('_').Skip(1).ToList();
            var i = Int32.Parse(w[0]);
            var j = Int32.Parse(w[1]);
            var di = loc[0] == 'a' ? 1 : 0;
            var dj = loc[0] == 'd' ? 1 : 0;

            foreach (var ch in word)
            {
                c[i, j] = ch;
                i += di;
                j += dj;
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
}

long DoubleWordSquare(int n, string wordfile = "", int numWords = -1, string specialWord = "", int specialIndex = -1)
{

        dl.Clear();
    // exercise 87
    // 2n distinct words in n rows and n columns

    if (wordfile == "")
        wordfile = "words.txt";
    var words = GetWords(wordfile, n, toLower: true);
    if (numWords > 0)
        words = words.Take(numWords).ToList();


    Console.WriteLine($"{words.Count} {n}-letter words");

    // items:
    for (var i = 0; i < n; ++i)
    {
        dl.AddItem($"a_{i}"); // across i
        dl.AddItem($"d_{i}"); // down i
    }

    // special case: add a word, mandatory use 2 times
    if (specialWord != "")
        dl.AddItem(specialWord, lowerBound: 1, upperBound: 1);

    for (var i = 0; i < n; ++i)
    for (var j = 0; j < n; ++j)
        dl.AddItem(Cell(i,j),true); // cells have colors, are secondary
    foreach (var word in words)
        if (word != specialWord)
            dl.AddItem(word,true);

    if (specialWord != "")
    {
        var index = specialIndex >=0 ? specialIndex : n /2; // where to place row and column
        // special case: add a word, mandatory use 2 times
        var w = specialWord;

        // special option: use down and across in middle
        var a = $"a_{index} ";
        var d = $"d_{index} ";
        for (var k = 0; k < w.Length; ++k)
        {
            a += $"{Cell(index, k)}:{w[k]} ";
            d += $"{Cell(k, index)}:{w[k]} ";
        }

        a += w;
        d += w;
        dl.ParseOption(a);
        //dl.ParseOption(d);
    }


    // options
    foreach (var w in words)
    {
        if (w == specialWord) continue;
        // across or down, letters, cells:
        for (var i = 0; i < n; ++i)
        {
            var op = $"a_{i} ";
            // cell:letter
            for (var j = 0; j < n; ++j)
                op += $"{Cell(i,j)}:{w[j]} ";

            // word 
            op += w;
            dl.ParseOption(op);
        }
        for (var j = 0; j < n; ++j)
        {
            var op = $"d_{j} ";
            // cell:letter
            for (var i = 0; i < n; ++i)
                op += $"{Cell(i, j)}:{w[i]} ";

            // word 
            op += w;
            dl.ParseOption(op);
        }
    }
    Console.WriteLine($"{dl.OptionCount} options");


    //dl.Options.MemsDumpStepSize = 10_000_000;
    //dl.Options.OutputFlags |= DancingLinksSolver.SolverOptions.ShowFlags.AllSolutions;
    dl.Solve();
    //dl.Options.OutputFlags &= ~DancingLinksSolver.SolverOptions.ShowFlags.AllSolutions;

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

    dl.AddOption("p q x y:A");
    dl.AddOption("p r x:A y"); // change to x:C to get no solution
    dl.AddOption("p x:B");
    dl.AddOption("q x:A");
    dl.AddOption("r y:B");

    // solution: 
    // q x:B   
    // p r x:B y    

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
        dl.ParseOption(opt);
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
        => piece.CountMatching(p => (0 <= p.i && p.i < w && 0 <= p.j && p.j < h)) == piece.Size;

    var pieces = Enumerable.Range(0, 12).Select(p => DancingLinks.Polyominoes.GetPentomino(p)).ToList();
    var p = new Polyominoes.Piece(new int[]{0,0,1,0,0,1,1,1},1,0,"tetromino");
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

long NQueens(int n, 
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

    dl.Clear();

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

        return (long) dist;
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
        dl.AddItem(d, secondary:useSecondary);

    // options
    for (var i = 1; i <= n; ++i)
    for (var j = 1; j <= n; ++j)
    {
        var opt = $"r{i} c{j} a{i + j} b{i - j}";
        if (topK != 0)
            opt += $"${Cost(i,j)}"; // attach to last item, affects entire option
        dl.AddOption(opt);
    }

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
