using Lomont.Algorithms.Utility;

namespace Lomont.Algorithms.Examples;

/// <summary>
/// Word and letter related problems to be solved by Dancing Links
/// </summary>
public static class WordPuzzles
{
    public static void RunAll(DancingLinksSolver.SolverOptions opts)
    {
        DoubleWordSquare(opts, 5, "sgb_words.txt", specialWord: "chris", showSolutions: true);

        DoubleWordSquare(opts, 3, specialWord: "the", specialIndex: 0);
        DoubleWordSquare(opts, 3, "most_common_words.txt", numWords: 100);
        DoubleWordSquare(opts, 5, "sgb_words.txt", specialWord: "chris", showSolutions: true);
        DoubleWordSquare(opts, 4, specialWord: "abbe", specialIndex: 0);
        DoubleWordSquare(opts, 6);
        DoubleWordSquare(opts, 12); // specialWord:"the",specialIndex:0); // none


        WordP94(true); // todo - bad fields in print progress

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

        WordCube(opts, 3);
        WordCube(opts, 4);
        WordShape(opts, ShapeFiveTorus, "most_common_words.txt"); //,numWords:130);
        WordCube(opts, 2, "most_common_words.txt", numWords: 62); // 12 solutions
        WordCube(opts, 3, "most_common_words.txt", numWords: 470); //, "sgb_words.txt",2500); // none at 350, 425, some at 500, some at 470
        WordCube(opts, 3, wordfile: "primes.txt", allLetters: false); // finds one, crashes
        WordCube(opts, 4, wordfile: "primes.txt", allLetters: false, numWords: 1000); // has formatting bug, finds 
        WordCube(opts, 5, wordfile: "primes.txt", allLetters: false); // has formatting bug, 8363 primes, finds 
        WordCube(opts, 5, wordfile: "primes.txt", allLetters: false); // crashes


    }

    [Puzzle("Make a n x n x n cube of words or numbers")]
    public static long WordCube(DancingLinksSolver.SolverOptions opts, int n, string wordfile = "", int numWords = -1, bool allLetters = false)
    {
        // word cube
        // todo - make Word(d1,d2,d3,...,dn) version
        var dl = new DancingLinksSolver {Options = opts};

        if (wordfile == "")
            wordfile = "words.txt";
        var words = Utils.GetWords(wordfile, n, toLower: true, allLetters: allLetters);
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

        bool Dump(long solutions, long moves, List<List<string>> options)
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


    #region Shapes for word puzzles
    // 7 wide disk
    public static List<(int i, int j)> ShapeSevenDisk = new[]
    {
        6, 2, 6, 3, 6, 4, // top
        5, 1, 5, 2, 5, 3, 5, 4, 5, 5,
        4, 0, 4, 1, 4, 2, 4, 3, 4, 4, 4, 5, 4, 6,
        3, 0, 3, 1, 3, 2, 3, 3, 3, 4, 3, 5, 3, 6,
        2, 0, 2, 1, 2, 2, 2, 3, 2, 4, 2, 5, 2, 6,
        1, 1, 1, 2, 1, 3, 1, 4, 1, 5,
        0, 2, 0, 3, 0, 4,
    }.Chunk(2).Select(p => (i: p[0], j: p[1])).ToList();
    // 7 torus
    public static List<(int i, int j)> ShapeSevenTorus = new[]
    {
                    6, 2, 6, 3, 6, 4, // top
              5, 1, 5, 2, 5, 3, 5, 4, 5, 5,
        4, 0, 4, 1, 4, 2,       4, 4, 4, 5, 4, 6,
        3, 0, 3, 1,                   3, 5, 3, 6,
        2, 0, 2, 1, 2, 2,       2, 4, 2, 5, 2, 6,
              1, 1, 1, 2, 1, 3, 1, 4, 1, 5,
                    0, 2, 0, 3, 0, 4, 
    }.Chunk(2).Select(p => (i: p[0], j: p[1])).ToList();
    // 5 disk
    public static List<(int i, int j)> ShapeFiveDisk = new[]
    {
              4, 1, 4, 2, 4, 3, 
        3, 0, 3, 1, 3, 2, 3, 3, 3, 4, 
        2, 0, 2, 1, 2, 2, 2, 3, 2, 4, 
        1, 0, 1, 1, 1, 2, 1, 3, 1, 4, 
              0, 1, 0, 2, 0, 3, 
    }.Chunk(2).Select(p => (i: p[0], j: p[1])).ToList();
    // 5 torus
    public static List<(int i, int j)> ShapeFiveTorus = new[]
    {
              4, 1, 4, 2, 4, 3,
        3, 0, 3, 1, 3, 2, 3, 3, 3, 4,
        2, 0, 2, 1,       2, 3, 2, 4,
        1, 0, 1, 1, 1, 2, 1, 3, 1, 4,
              0, 1, 0, 2, 0, 3,
    }.Chunk(2).Select(p => (i: p[0], j: p[1])).ToList();
    // 3 sq
    public static List<(int i, int j)> ShapeThreeSquare = new[]
    {
        2, 0, 2, 1, 2, 2,
        1, 0, 1, 1, 1, 2,
        0, 0, 0, 1, 0, 2,
    }.Chunk(2).Select(p => (i: p[0], j: p[1])).ToList();
    #endregion

    [Puzzle("Make words or numbers in a given shape")]
    public static long WordShape(
        DancingLinksSolver.SolverOptions opts, 
        List<(int i, int j)> cells,
        string wordfile = "", int numWords = -1, bool allLetters = false
        )
    {

        var dl = new DancingLinksSolver { Options = opts};

        var runs = new List<((int i, int j), (int di, int dj), int len)>();

        // get list of rows and columns by lengths
        var minx = cells.Min(p => p.i);
        var maxx = cells.Max(p => p.i);
        var miny = cells.Min(p => p.j);
        var maxy = cells.Max(p => p.j);
        for (var i = minx; i <= maxx; ++i)
        for (var j = miny; j <= maxy; ++j)
        {
            if (cells.Contains((i, j)))
            {
                // go up and down to edges
                var d1 = 0;
                while (cells.Contains((i + d1, j)))
                    d1--;
                d1++;
                var d2 = 0;
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
                var words = Utils.GetWords(wordfile, r.len, toLower: true, allLetters: allLetters);
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
            op = di == 1 
                ? $"a_{i}_{j} " // across
                : $"d_{i}_{j} "; // down

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

    [Puzzle("Make n x n square of 2n distinct words")]
    public static long DoubleWordSquare(
        DancingLinksSolver.SolverOptions opts, int n, string wordfile = "", int numWords = -1, string specialWord = "", int specialIndex = -1,
        bool showSolutions = false
        )
    {
        var dl = new DancingLinksSolver { Options = opts};

        // exercise 87
        // 2n distinct words in n rows and n columns

        if (wordfile == "")
            wordfile = "words.txt";
        var words = Utils.GetWords(wordfile, n, toLower: true);
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
            dl.AddItem(Cell(i, j), true); // cells have colors, are secondary
        foreach (var word in words)
            if (word != specialWord)
                dl.AddItem(word, true);

        if (specialWord != "")
        {
            var index = specialIndex >= 0 ? specialIndex : n / 2; // where to place row and column
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
                    op += $"{Cell(i, j)}:{w[j]} ";

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
        if (showSolutions)
            dl.SolutionListener += Utils.DumpCellSolution;
        dl.Solve();
        if (showSolutions)
            dl.SolutionListener -= Utils.DumpCellSolution;
        //dl.Options.OutputFlags &= ~DancingLinksSolver.SolverOptions.ShowFlags.AllSolutions;

        string Cell(int i, int j) => $"c_{i}_{j}";

        return dl.SolutionCount;
    }




    [Puzzle("Solve word problem in TAOCP p94, 4x5 word rectangles from top 1000 words")]
    public static long WordP94(bool showSolutions)
    {
        // colors and multiplicity
        // 4x5 word rectangles from top 1000 words in WORDS(1000);
        // at most 8 letters
        // Knuth finds 8 such answers, but not sure if our 4 word list is same as his
        var dl = new DancingLinksSolver();
        for (var i = 0; i < 4; ++i)
            dl.AddItem($"A{i}");
        for (var i = 0; i < 5; ++i)
            dl.AddItem($"D{i}");
        for (var i = 0; i < 26; ++i)
            dl.AddItem($"#{(char)(i + 'A')}");
        dl.AddItem("#", lowerBound: 8, upperBound: 8);

        for (var i = 0; i < 26; ++i)
            dl.AddItem($"{(char)(i + 'A')}", secondary: true);
        for (var i = 0; i < 4; ++i)
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
        var (num4, num5) = (1000, 2000);
        var len4 = Utils.GetWords("most_common_words.txt", 4, toUppercase: true, count: num4);
        var len5 = Utils.GetWords("sgb_words.txt", 5, toUppercase: true, count: num5);

        Func(dl, len5, 4, 'A', (i, j) => $"{i}_{j}");
        Func(dl, len4, 5, 'D', (i, j) => $"{j}_{i}");

        static void Func(DancingLinksSolver dl, IList<string> words, int wid, char dir, Func<int, int, string> toCell)
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
                            var c = (char)(k + 'A');
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

    [Puzzle("Count solutions to the given Sudoku problem")]
    public static void Sudoku(params int[] board)
    {
        // number items 0-8
        // 4*9*9=324 items pij rik cjk bxk
        // 729 options pij rik cjk bxk for 0 <= i,j < 9, 1 <=k <= 9, x= 3 Floor(i/3) + Floor[j/3]
        // item pij must be one of the 9 options that fill cell cij,
        // item rk must be covered by exactly on of the 9 options putting a k in row i,
        // item bxk must be covered by one of the 9 options putting k in box x
        // then simply remove pij, rik, cjk, bxk for any given item, and any options with those
        // todo;
    }





}