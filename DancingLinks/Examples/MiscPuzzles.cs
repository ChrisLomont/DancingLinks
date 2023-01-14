using Lomont.Algorithms.Utility;

namespace Lomont.Algorithms.Examples;

public static class MiscPuzzles
{
    public static void RunAll(DancingLinksSolver.SolverOptions opts)
    {
        TestRandomVersusBruteForce();
        ToyDlxPage68(opts, dumpState: false);
        ToyColorPage89(opts, dump: true);
        //ToyColorPage89(opts, dump:false);
        ToyMultiplicity(opts);
        ToyMultiplicityWithColor(opts);
        Exercise94(opts);
    }

    [Puzzle("Toy problem from TAOCP with multiplicity")]

    public static long ToyMultiplicity(DancingLinksSolver.SolverOptions opts, bool dump = false)
    {
        var dl = new DancingLinksSolver {Options = opts };

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


    [Puzzle("Toy problem from TAOCP with multiplicity and color")]
    public static long ToyMultiplicityWithColor(DancingLinksSolver.SolverOptions opts, bool dump = false)
    {
        // simple example with mult and colors
        var dl = new DancingLinksSolver { Options = opts };


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


    [Puzzle("Toy problem from TAOCP with colors")]
    public static long ToyColorPage89(DancingLinksSolver.SolverOptions opts, bool dump)
    {
        var dl = new DancingLinksSolver { Options = opts };
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

    [Puzzle("Exercise problem #94 from TAOCP")]
    public static long Exercise94(DancingLinksSolver.SolverOptions opts)
    {// tests colors
        // has unique solution, up to cyclic perm, complementation, reflection, so 1 * 16 * 2 * 2 = 64 solutions
        var dl = new DancingLinksSolver { Options = opts };
        dl.Clear();
        for (var k = 0; k < 16; ++k)
        {
            dl.AddItem(k.ToString());
            dl.AddItem("p" + k);
        }
        for (var k = 0; k < 16; ++k)
            dl.AddItem("x" + k, secondary: true);

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

    [Puzzle("Toy Dancing Links problem from TAOCP, page 68")]
    public static long ToyDlxPage68(DancingLinksSolver.SolverOptions opts, bool dumpState = false)
    {
        var dl = new DancingLinksSolver { Options = opts };

        dl.AddItems("abcdefg".Select(c => c.ToString()).ToList());

        // check secondary structure
        //dl.AddItem("h",true);
        //dl.AddItem("i", true);
        //dl.AddItem("j", true);

        var options = new[] { "ce", "adg", "bcf", "adf", "bg", "deg" };
        dl.AddOptions(options
            .Select(s => s.ToCharArray().Select(c => c.ToString()).ToList())
        );

        if (dumpState)
            dl.DumpNodes(Console.Out); // check against page 68 TAOCP 4B
        dl.Solve();

        return dl.SolutionCount;
    }


    [Puzzle("Generate random DLX problems, and count solutions versus brute force, for testing")]
    public static long TestRandomVersusBruteForce(bool showResults = false, int numTests = 1000)
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
                var v = r.Next(1, (1 << bits) + 1); // disallow 0
                optionNumbers.Add(v);
            }

            var danceCount = SolveDl(dl, bits, optionNumbers);
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
        static long SolveDl(DancingLinksSolver dl, int bits, List<int> optionNumbers)
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
            dl.Options.Output = sw;
            dl.Solve();
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



}