using Microsoft.Win32;
using System.Diagnostics;
using System.IO;

namespace Lomont.Algorithms;

// copyright Chris Lomont, www.lomont.org
// Replaces my earlier DLX solvers from the past 20-ish years
//
// Chris Lomont, 2023
//     - rewrite to handle all the new variants in Knuth's
//       "The Art of Computer Programming" (TAOCP) volume 4B, "Combinatorial Algorithms, Part II"
//       Also references his 2002 paper Dancing Links
// Some of my older version comments :
// Chris Lomont, 2015, 
//     - added dumping the state, lineEnd, and solutionEnd, UseGolumbHeuristic
//     - added SolutionRecorderDelegate for nicer solution handling
// Chris Lomont, 2013, C# ported from earlier Lomont C++ version


/// <summary>
/// code for Knuth's Dancing Links algorithms from his Dancing Links
/// paper, as extended by his book TAOCP vol 4B.
///
/// This solves basic dancing links, adds support for secondary items,
/// colors, multiplicities. Planned to handle costs, preprocessing, and ZDDs
/// 
/// To use:
/// 0. (Optional) set options
///    TODO - explain
/// 1. Add each item. There is usually an item for each cell to cover and for
///    each piece when solving puzzles. Items can be primary (required to be covered)
///    or secondary (optionally covered). Secondary items can have 'colors' attached which
///    require any solution to have all options picking the same color for that item. Primary
///    items can have lower and upper bounds (default 1,1) for how many times it needs covered.
/// 2. Add options which consist of a set of items to cover. An option can have a cost associated
///    when computing the K lowest cost solutions.
/// 3. Set output as desired. The Options can set things, SetOutput sets a stream writer, and
///    event SolutionListener can be attached for getting all solutions, providing a way to stop
///    enumeration during a solve.
/// 4. Call Solve, which automatically calls the correct internal solver.
/// 5. Inspect Stats for solution count and other statistics.
///
/// 
/// The solver picks all subsets of rows that match the solver criteria. Basic is exactly one entry
/// per item. Colors require a given item to have all options agreeing on that color. Secondary items
/// default to 0 or 1 covering. Multiplicity allows setting a lower and upper bound on how many
/// covers are for an item. Costs allow finding the K lowest cost solutions.
/// </summary>
public class DancingLinksSolver
{
    // Knuth Algorithm X
    // Exact cover via Dancing Links


    /* TODO
    - MIT license
    - extend to have a name for each row, and return row names in solution 
    - DONE: move namespace to match other Lomont DL
    - DONE: add color codes algo as generalization XC = exact cover, XCC = with colors
    - MCC adds multiplicities to color problem
    - explain colors, secondary items, slack versus secondary, examples, etc.
    - track updates per depth, nodes per depth, like DL paper, show updates/node, etc.
    - add nice description of how to use: mention slack vars and secondary items
    - exercise 19 handles options with no primary items
    - C$ handles with costs per options, return slowest cost
    - Z produces XCC solns as ZDDS which can be handles in other ways
    - make as drop in replacement for my old DancingLinks code
    - make nicer parser for file, command line, other uses
    - output colors in solutions (not currently working?)
    - output costs in solutions (not currently working?)
    - error if color assigned to non-secondary item
    - output for bound and slack
    - some sanity checking to ensure structure is valid, useful for debugging, extending
    - extend cost C$ and X$ algos to an M$ algo, send to Knuth
    - better M3 step choices - see book and exercises for ideas

    items strings, spaced out, '|' splits primary from secondary
    - 

     -  Command line: tool - sets options, runs file or console or....
      -v[bcdpftwm] = things to show
      -m = spacing
      -s = random seed -> randomizing
      -d = delta - memory step
      -c = show_choices_max
      -C = show_levels_max
      -l = show_choices_gap
      -t = maxcount
      -T = timeout
      -S = shapefile....
       



    Knuth format looks like this: 
    '|' starts comment line
    items have 
    num:num|name for bounds
    num|name if bounds are num...num
    default to 1
    " | " separates primary from secondary items
    then lines of options
    option has  ':' for color after oni secondary items only

| A simple example of color controls
A B 2:3|C | X Y
A B X:0 Y:0
A C X:1 Y:1
C X:0
B X:1
C Y:1

has unique solution of options {A C X:1 Y:1}  {B X:1} {C Y:1}

    Costs:
    Append things like "|$n" where n is nonnegative integer
    to add cost to any option. If multiple such entries in same option, 
    option cost is their sum
     */


    public DancingLinksSolver()
    {
        formatter = new Formatter(this);
        Stats = new LogStats(this);
        Clear();
        output = TextWriter.Null;
    }

    /// <summary>
    /// Reset all internals, allows reusing the object
    /// </summary>
    public void Clear()
    {
        ClearData();

        Stats.ResetStats(1);
        formatter.Reset();

        // initial spacer items
        AddSpacer(true); // regular nodes all 0s
        AddSpacer(false); // name nodes all 0s
        AddName("_"); // not in map, so not usable or discoverable by outside
    }

    #region Solver and output options

    public SolverOptions Options { get; set; } = new();

    public class SolverOptions
    {

        [Flags]
        public enum ShowFlags
        {
            None = 0,
            Basics = 0x0001,
            Choices = 0x0002,
            Details = 0x0004,
            Profile = 0x0008,
            FullState = 0x0010,
            Totals = 0x0020,
            Warnings = 0x0040,
            MaxDegree = 0x0080,
            AllSolutions = 0x0100,
            All = Basics | Choices | Details | Profile | FullState | Totals | Warnings | MaxDegree | AllSolutions,
        }

        /// <summary>
        /// Things to show
        /// </summary>
        public ShowFlags OutputFlags { get; set; } = ShowFlags.Basics | ShowFlags.Warnings;

        /// <summary>
        /// Use smallest next choice heuristic, sometimes called
        /// Golomb Heuristic, or MRV in Knuth TAOCP
        /// </summary>
        public bool MinimumRemainingValuesHeuristic { get; set; } = true; // exercise 9

        /// <summary>
        /// Show progress every this many mem accesses
        /// </summary>
        public long MemsDumpStepSize { get; set; } = 10_000_000_000;

        /// <summary>
        /// max mems till 'timeout'
        /// </summary>
        public long MemsStopThreshold { get; set; } = Int64.MaxValue; // max mems, else timeout


        // todo - describe and use these
        internal int Spacing = 0; // solution count spacing, 0 for show none
        internal long MaxSolutionCount = Int64.MaxValue; // max solutions to show
        internal int ShowLevelMax = 1000000; // show this many levels max, else show ...
        internal bool Randomize = false;
        internal int ShowChoicesMax = 1000000;
        internal int ShowChoicesGap = 1000000;
        internal int RandomSeed = 0;
        internal TextWriter? ShapeOutput = null; // set to file, or output, or...

    }

    #endregion

    #region Items Input

    /// <summary>
    /// Add items. See AddItem for format
    /// </summary>
    /// <param name="items"></param>
    public void AddItems(params string[] items) => AddItems((IEnumerable<string>)items);

    /// <summary>
    /// Add items. 
    /// </summary>
    /// <param name="items"></param>
    public void AddItems(IEnumerable<string> items)
    {
        foreach (var item in items)
            AddItem(item);
    }

    /// <summary>
    /// Add a new item by unique name
    /// Secondary items must come after all primary items
    /// TODO - detail how these work
    /// </summary>
    /// <param name="name"></param>
    /// <param name="secondary"></param>
    /// <param name="lowerBound"></param>
    /// <param name="upperBound"></param>
    public void AddItem(string name, bool secondary = false, int lowerBound = 1, int upperBound = 1)
    {
        // track N = # items, N1 = primary items

        // TODO - rewrite to be cleaner: track list of items, then gen all once all are added
        // as in exercise 8

        // handle secondary items
        if (secondary)
            N2++;
        else
            Trace.Assert(N2 == 0); // all must be primary before first secondary, then all secondary


        var i = AddNode(false) + 1;
        AddName(name, i);

        nameNodes[i].SetA(i); // name index


        // set node links
        var len = NameNodeCount;
        RLINK(i, 0);
        LLINK(i, (i - 1 + len) % len);

        // attach prev and next
        LLINK(0, i);
        RLINK((i - 1 + len) % len, i);

        // update general nodes
        var x = AddNode(true);
        LEN(x, 0);
        ULINK(x, x); // point to self
        DLINK(x, x);

        if (N2>0)
        {
            // (link one less than second to 0)
            LLINK(0, N1);
            RLINK(N1, 0);

            // (link second to end)
            LLINK(N1+1, N);
            RLINK(N, N1+1);
        }

        // only primary items can have these)
        Trace.Assert(!secondary || (lowerBound == 1 && upperBound == 1),
            "Secondary items cannot have lower or upper bounds that are not 1");

        // todo - make all probs use algo m?
        if (lowerBound != 1 || upperBound != 1)
            //if (!secondary)
        {
            Trace.Assert(0 <= lowerBound);
            Trace.Assert(lowerBound <= upperBound);
            hasMultiplicities = true;
            while (slack.Count <= x)
            {
                // upper bound and lower bound default 1
                bound.Add(1); // upper bound
                slack.Add(0); // slack defaults to upper-lower
            }

            bound[x] = upperBound;
            slack[x] = upperBound - lowerBound;
        }

        //  DumpNodes(Console.Out);
    }

    #endregion

    #region Options Input

    public void ParseOption(string option) => AddOption(option.Split(' ', StringSplitOptions.RemoveEmptyEntries));

    public void AddOption(params string[] names) => AddOption((IEnumerable<string>)names);

    public void AddOptions(IEnumerable<IList<string>> items)
    {
        foreach (var item in items)
            AddOption(item);
    }

    // Every option must include at least one primary item otherwise the option will not be included in the solver
    // todo - do exercise 19 which allows using options with only secondary items
    public void AddOption(IEnumerable<string> names)
    {
        if (NodeCount == NamesCount)
        {
            // first option after items added
            AddSpacer(true, true); // initial spacer
        }


        foreach (var name1 in names)
        {
            var name = name1;

            string color = ""; // empty for none
            if (name.Contains(':'))
            {
                hasColor = true;
                var w = name.Split(':', StringSplitOptions.RemoveEmptyEntries);
                Trace.Assert(w.Length == 2);
                color = w[1];
                name = w[0];
            }

            if (name.Contains('$'))
            {
                //todo.
                // lots to do to make it work
                throw new NotImplementedException("Option costs not implemented");
                //name = ;
            }

            var i = NameIndex(name);

            var x = AddNode(true); // node index

            if (!String.IsNullOrEmpty(color))
                COLOR(x, ColorIndex(color));

            // new node settings
            TOP(x, i); // header and name items

            // link to top nodes lists
            var prev = ULINK(i);
            ULINK(x, prev);
            DLINK(prev, x);

            ULINK(i, x);
            DLINK(x, i);

            LEN(i, LEN(i) + 1); // one more in list
        }

        AddSpacer(true, true);
        ++optionCount;

    }

    #endregion

    #region Output listener, formatting solutions

    /// <summary>
    /// Each solution can be captured using this delegate.
    /// NOTE: we avoid using the standard event mechanism so we can return
    /// a value to continue or stop enumeration
    /// </summary>
    /// <param name="solutionNumber">The number of solutions found</param>
    /// <param name="dequeueNumber">How many total cover/hide have been done so far</param>
    /// <param name="solution">A list of solution combinations, each combination
    /// is a list of column headers for the given selection</param>
    /// <returns>Return true to continue enumeration, or false to stop</returns>
    public delegate bool SolutionRecorderDelegate(
        long solutionNumber,
        long dequeueNumber,
        List<List<string>> solution
    );

    /// <summary>
    /// Set this to listen to solutions. See SolutionRecorderDelegate
    /// for the meanings of the parameters
    /// </summary>
    public SolutionRecorderDelegate? SolutionListener;

    /// <summary>
    /// Dump node definitions, useful for debugging
    /// </summary>
    /// <param name="dumpOutput"></param>
    public void DumpNodes(TextWriter dumpOutput)
    {
        dumpOutput.WriteLine($"------------------");
        dumpOutput.WriteLine($"i NAME LLINK RLINK");
        for (var i = 0; i < NameNodeCount; ++i)
            dumpOutput.WriteLine($"{i}: {NAME(i)} {LLINK(i)} {RLINK(i)}");

        var cs = "";
        if (hasColor)
        {
            cs = "COLOR";
        }

        dumpOutput.WriteLine($"------------------");
        dumpOutput.WriteLine($"x LEN ULINK DLINK {cs}");
        for (var x = 0; x < NameNodeCount; ++x)
        {
            var cc = COLOR(x);
            var ce = (hasColor && cc > 0) ? Colors[cc - 1] : cc.ToString();
            dumpOutput.WriteLine($"{x}: {LEN(x)} {ULINK(x)} {DLINK(x)} {ce}");
        }

        dumpOutput.WriteLine($"------------------");
        dumpOutput.WriteLine($"x TOP ULINK DLINK {cs}");
        for (var x = NameNodeCount; x < NodeCount; ++x)
        {
            var cc = COLOR(x);
            var ce = (hasColor && cc > 0) ? Colors[cc - 1] : cc.ToString();
            dumpOutput.WriteLine($"{x}: {TOP(x)} {ULINK(x)} {DLINK(x)} {ce}");
        }
    }

    #endregion

    #region Stats

    public long SolutionCount => Stats.SolutionCount;
    public LogStats Stats { get; private set; }

    public class LogStats
    {

        public LogStats(DancingLinksSolver dl) => this.dl = dl;

        public long SolutionCount { get; set; }
        public long Updates { get; private set; }

        public void Update() => Updates++;

        // max possible level 
        public int maxAllowedLevel = 0;

        /// <summary>
        /// Increment mems
        /// </summary>
        public void Mem() => memAccesses++;

        public void ResetStats(int optionCount)
        {
            // tracking of mem and other usage
            SolutionCount = 0;
            Updates = 0;
            nodeCount = 0;
            maxDegree = 0;
            maxLevelSeen = 0;
            memAccesses = 0; 
            inputMemAccesses = 0;
            nextMemoryDump = -1; // memory threshold
            
            // max allowed levels is (optionCount+1) * max of bounds
            maxAllowedLevel = optionCount + 1;
            if (dl.hasMultiplicities)
                maxAllowedLevel *= dl.bound.Max();

            profile = new long[maxAllowedLevel];
            choice = new int[optionCount + 1];
        }

        // timer for timing entire solution search
        Stopwatch timer = new();

        public void StartTimer() => timer.Restart();

        public void StopTimer() => timer.Stop();
        public bool TrackMemory(int level, int[] x)
        {
            if (dl.Options.OutputFlags.HasFlag(SolverOptions.ShowFlags.Profile))
                profile[level]++;

            nodeCount++; // nodes explored

            if (nextMemoryDump == -1) nextMemoryDump = dl.Options.MemsDumpStepSize;
            if (dl.Options.MemsDumpStepSize > 0 && (memAccesses >= nextMemoryDump) && 
                dl.Options.OutputFlags.HasFlag(SolverOptions.ShowFlags.Basics))
            {
                nextMemoryDump += dl.Options.MemsDumpStepSize;
                if (dl.Options.OutputFlags.HasFlag(SolverOptions.ShowFlags.FullState)) 
                    dl.formatter.PrintState(level, x);
                else
                    dl.formatter.PrintProgress(level, x);
            }

            if (memAccesses >= dl.Options.MemsStopThreshold)
            {
                dl.output.WriteLine("TIMEOUT!");
                return true; // done!
            }

            return false;
        }

        public bool TrackLevels(int l)
        {
            if (l > maxLevelSeen)
            {
                if (l >= maxAllowedLevel)
                {
                    dl.output.WriteLine("Too many levels!");
                    return true; // done
                }

                maxLevelSeen = l;
            }

            return false;
        }

        public bool TrackShape(int l)
        {
            if (!TrackLevels(l))
                return false;

            if (dl.Options.OutputFlags.HasFlag(SolverOptions.ShowFlags.Profile)) profile[l]++;
            if (dl.Options.ShapeOutput != null)
            {
                dl.Options.ShapeOutput.WriteLine("sol");
                dl.Options.ShapeOutput.Flush();
            }

            return false;
        }

        public bool TrackSolutions(int l)
        {

            if (dl.Options.Spacing > 0 && (SolutionCount % dl.Options.Spacing == 0))
            {
                dl.output.WriteLine($"{SolutionCount}:");
                for (var k = 0; k < l; k++)
                    dl.formatter.PrintOption(choice[k]);
                dl.output.Flush();
            }

            nodeCount++; 

            if (SolutionCount >= dl.Options.MaxSolutionCount)
                return true; // done
            return false;
        }

        public void TrackLevels(int type, int l, int p = -1, int best_item = -1, int tmax = -1)
        {
            var good = dl.Options.OutputFlags.HasFlag(SolverOptions.ShowFlags.Details) &&
                       l < dl.Options.ShowChoicesMax && l >= maxLevelSeen - dl.Options.ShowChoicesGap;
            if (!good) return;

            if (type == 0)
                dl.output.Write($"Level {l}");
            if (type == 1)
                dl.output.Write($" {dl.NAME(p)} {dl.LEN(p)}");

            if (type == 2)
            {
                dl.output.WriteLine($" branching on {dl.NAME(best_item)} {tmax}");
                if (dl.Options.ShapeOutput != null)
                {
                    dl.Options.ShapeOutput.WriteLine($"{tmax} {dl.NAME(best_item)}");
                    dl.Options.ShapeOutput.Flush();
                }

                dl.Stats.maxDegree = Math.Max(dl.Stats.maxDegree, tmax);
            }
        }

        public void TrackChoices(int l, int xl)
        {
            if (dl.Options.OutputFlags.HasFlag(SolverOptions.ShowFlags.Choices) && l < dl.Options.ShowChoicesMax)
            {
                dl.output.Write($"L{l}:");
                dl.formatter.PrintOption(xl);
            }
        }

        public void ShowFinalStats(int[] x)
        {
            var (imems, mems) = (imems: this.inputMemAccesses, mems: this.memAccesses); // store updates here before reporting
            if (dl.Options.OutputFlags.HasFlag(SolverOptions.ShowFlags.Totals))
            {
                dl.output.Write("Item totals:");
                for (var k = 1; k < dl.NameNodeCount; k++)
                {
                    if (k == dl.N1)
                        dl.output.Write(" |");
                    dl.output.Write($" {dl.LEN(k)}");
                }

                dl.output.WriteLine("");
            }

            if (dl.Options.OutputFlags.HasFlag(SolverOptions.ShowFlags.Profile))
            {
                dl.output.WriteLine("Profile:");
                for (var level = 0; level <= maxLevelSeen; level++)
                    dl.output.WriteLine($"   {level} {profile[level]}");
            }

            if (dl.Options.OutputFlags.HasFlag(SolverOptions.ShowFlags.MaxDegree))
                dl.output.WriteLine($"The maximum branching degree was {maxDegree}");

            if (dl.Options.OutputFlags.HasFlag(SolverOptions.ShowFlags.Basics))
            {

                var nodeSize = 4 * sizeof(int);
                var bytes = dl.NodeCount * nodeSize + dl.NameNodeCount * nodeSize + x.Length * sizeof(int) +
                            dl.Names.Sum(n => n.Length); // treat as ASCII or utf8


                var plural = SolutionCount == 1 ? "" : "s";
                dl.output.WriteLine($"{SolutionCount} solution{plural} in {timer.Elapsed}");
                dl.output.WriteLine($"{imems}+{mems} mems, {Updates} updates, {bytes} bytes memory, {nodeCount} nodes");
                // todo - mems/soln? Nodes/sec? solns/sec?
            }

            if (dl.Options.ShapeOutput != null)
                dl.Options.ShapeOutput.Close(); // todo -  dont; close console.out - how to cloe file but not this?
        }

        #region Implementation

        DancingLinksSolver dl;
        public int maxLevelSeen = 0; // max level seen while searching
        int maxDegree = 0;

        long nodeCount = 0; // nodes walked
        // (one up and down is one mem, basically # of 64 bit line accesses
        // nodes padded with _reserve_ field to pad to 128 bits to match Knuth

        public long memAccesses = 0; // each mem access 
        long inputMemAccesses = 0; // mems to initialize, set in solver
        long[] profile = new long[1];
        long nextMemoryDump = -1; // next memory threshold for output, or -1 when eeding set
        int[] choice = new int[1]; // current choice index



        #endregion
    } // stats class

    #endregion

    class Formatter
    {
        DancingLinksSolver dl;
        
        public Formatter(DancingLinksSolver dl) => this.dl = dl;

        long ItemCost(int p)
        {
            if (p < dl.NameNodeCount || p >= dl.NodeCount || dl.TOP(p) <= 0)
            {
                dl.output.WriteLine($"Illegal option {p}!");
                return 0;
            }

            var q = p;
            var cost = 0L;
            while (true)
            {
                cost += dl.COST(dl.TOP(q));
                q++;
                if (dl.TOP(q) <= 0) q = dl.ULINK(q);
                if (q == p) break;
            }

            return cost;
        }

        public IEnumerable<string> GetOptionItems(int p, bool normalizeOrder)
        {
            //todo
            if (p < dl.NameNodeCount || p >= dl.NodeCount || dl.TOP(p) <= 0)
            {
                dl.output.WriteLine($"Illegal option {p}!");
                yield break;
            }

            var q = p;
            if (normalizeOrder)
            {
                // wrap q to start to normalize item order
                while (true)
                {
                    q++;
                    if (dl.TOP(q) <= 0)
                    {
                        q = dl.ULINK(q);
                        break;
                    }
                }
            }

            p = q; // set this as end point
            while (true)
            {
                var name = dl.NAME(dl.TOP(q));
                if (dl.hasColor && dl.COLOR(q) != 0)
                {
                    var color = dl.Colors[dl.COLOR(q) > 0 ? dl.COLOR(q) : dl.COLOR(dl.TOP(q))];
                    name += $":{color}";
                }
                yield return name;

                q++;
                if (dl.TOP(q) <= 0) q = dl.ULINK(q);
                if (q == p) break;
            }
        }

        public void PrintOption(int p, long costThreshold = Int64.MaxValue)
        {
            if (p < dl.NameNodeCount || p >= dl.NodeCount || dl.TOP(p) <= 0)
            {
                dl.output.WriteLine($"Illegal option {p}!");
                return;
            }
            foreach (var itemText in GetOptionItems(p, normalizeOrder: false))
                dl.output.WriteLine(itemText);
            var s = ItemCost(p);

            int k, j, q;

            for (q = dl.DLINK(dl.TOP(p)), k = 1; q != p; k++)
            {
                if (q == dl.TOP(p))
                {
                    dl.output.Write(" (?)");
                    goto finish;
                }
                else
                    q = dl.DLINK(q);
            }

            for (q = dl.DLINK(dl.TOP(p)), j = 0; q >= dl.NameNodeCount; q = dl.DLINK(q), j++)
                if (dl.COST(q) >= costThreshold)
                    break;
            dl.output.Write($" ({k} of {j})");

        finish:

            if (s + dl.COST(p) != 0)
                dl.output.Write($" {s + dl.COST(p)} [{dl.COST(p)}]");
            dl.output.WriteLine();
        }

        public void PrintState(int level, int[] choice)
        {
            // based on Exercise #12, as noted on page 73

            dl.output.WriteLine($"Current state (level {level})");
            for (var l = 0; l < level; l++)
            {
                PrintOption(choice[l]);
                if (l >= dl.Options.ShowLevelMax)
                {
                    dl.output.WriteLine(" ...");
                    break;
                }
            }

            dl.output.WriteLine($"{dl.SolutionCount} solutions, {dl.Stats.memAccesses} mems, and max level {dl.Stats.maxLevelSeen} so far.");
        }

        public void PrintProgress(int level, int[] choice)
        {
            // based on Exercise #12, as noted on page 73

            dl.output.Write($" after {dl.Stats.memAccesses} mems: {dl.SolutionCount} sols,");
            double f = 0, fd = 1;

            for (var l = 0; l < level; l++)
            {
                var c = dl.TOP(choice[l]);
                var d = dl.LEN(c);

                int k, p;
                for (k = 1, p = dl.DLINK(c); p != choice[l]; k++)
                {
                    if (k > dl.N)
                    {
                        dl.output.WriteLine("ERROR - bad fields in Print Progress!");
                        break;
                    }
                    p = dl.DLINK(p);
                }

                fd *= d;
                f += (k - 1) / fd;
                dl.output.Write($"{Enc(k)}{Enc(d)} ");

                if (l >= dl.Options.ShowLevelMax)
                {
                    dl.output.Write("...");
                    break;
                }
            }

            char
                Enc(int num) => (char)(
                num switch
                {
                    < 10 => '0' + num,
                    < 36 => 'a' + num - 10,
                    < 62 => 'A' + num - 36,
                    _ => '*'
                }
            );

            dl.output.WriteLine($" {(f + 0.5 / fd):F5}");
        }

        /// <summary>
        /// Write out a solution to the current output
        /// </summary>
        public void PrintSolution(IEnumerable<List<string>> solutionNodes)
        {
            var os = dl.output;
            os.WriteLine($"Solution {dl.SolutionCount} (found after {dl.Stats.Updates} deque removals):");
            foreach (var line in solutionNodes)
            {
                foreach (var s in line)
                    os.Write($"{s} ");
                os.WriteLine(lineEnd);
            }

            os.WriteLine(solutionEnd);
        }

        // set from outside, useful for separating for parsers
        public string lineEnd = "";
        public string solutionEnd = "";

      
        public void Reset()
        {
            lineEnd = "";
            solutionEnd = "";
        }
    }

    private Formatter formatter;

    /// <summary>
    /// Set where the output text goes
    /// Can be used like SetOutput(Console.Out);
    /// </summary>
    /// <param name="outputWriter">the text writer where output goes</param>
    /// <param name="newLineEnd">An optional marker to denote line ends in each solution</param>
    /// <param name="newSolutionEnd">An optional marker to denote a solution end</param>
    public void SetOutput(TextWriter outputWriter, string newLineEnd = "", string newSolutionEnd = "")
    {
        // todo - merge into options?
        output = outputWriter;
        formatter.lineEnd = newLineEnd;
        formatter.solutionEnd = newSolutionEnd;
    }

    /// <summary>
    /// Solve the problem instance.
    /// Answers are written to wherever output was sent.
    /// </summary>
    public void Solve()
    {
        if (hasMultiplicities)
        { // can make all use SolveM if bounds added for each item.
            SolveM(); // todo - unify all solutions into one solver?
            return;
        }

        // todo - on randomizing, shuffle items? shuffle options? keep 
        // todo - print options count, item count, node count, secondary item count, etc. here
        // if show totals selected
        // print totals of item covers...?

        Stats.ResetStats(optionCount);
        Stats.StartTimer();
        if (Options.Randomize)
            rand = new Random(Options.RandomSeed);

        var step = 1; // represent algo step X1 through X8
        int Z = 0, l = 0, i = 0; //, xl = 0;

        // for local work
        int j = 0, p = 0;

        // solution
        var x = new int[Stats.maxAllowedLevel]; // overkill in space

        var done = false;
        // this follows Knuth TAOCP notation and format
        while (!done)
        {
            switch (step)
            {
                case 1: // Initialize: load data as above
                    Z = lastSpacerIndex;


                    l = 0; // level
                    step = 2;
                    break;
                case 2: // Enter level l
                    done |= Stats.TrackMemory(l, x); // progress updates
                    if (done)
                        break;

                    if (RLINK(0) == 0) // all items covered
                    {
                        done |= Stats.TrackShape(l);
                        if (done)
                            break;

                        done |= !VisitSolution(x, l); // solution in x0,x1,..,x_{l-1}

                        done |= Stats.TrackSolutions(l);
                        if (done)
                            break;

                        step = 8;
                    }
                    else
                        step = 3;

                    break;
                case 3: // Choose i
                    // todo - abstract out to func calls?
                    // need one of i1, i2, .., it, where i1= RLINK(0), i_{j+1}=RLINK(i_j), RLINK(it)=0
                    // choose one, here can use MRV (Exercise 9 - todo)

                    // will always walk to get stats


                    var tm = int.MaxValue; // max nodes
                    var ct = 0; // count larger than min?
                    Stats.TrackLevels(0, l); // walk levels to show

                    p = RLINK(0);
                    i = p; // minIndex
                    while (p != 0)
                    {
                        Stats.TrackLevels(1, l, p);
                        if (LEN(p) < tm)
                        {
                            tm = LEN(p);
                            i = p;
                            ct = 1;
                        }
                        else
                        {
                            ct++;
                            // choose random number of nodes to skip (Reservoir Sampling)
                            if (Options.Randomize && UniformRand(ct) == 0)
                                i = p; // best_item

                        }

                        p = RLINK(p);
                    }

                    // i is best for MRV, if not MRV, take first
                    if (!Options.MinimumRemainingValuesHeuristic)
                        i = RLINK(0); // choose first for now

                    Stats.TrackLevels(2, l, -1, i, tm);

                    step = 4;
                    break;
                case 4: // Cover item i using (12) from TAOCP
                    Cover(i, hasColor);
                    x[l] = DLINK(i);
                    step = 5;
                    break;
                case 5: // Try xl 
                    if (x[l] == i)
                        step = 7; // we tried all options for i
                    else
                    {
                        Stats.TrackChoices(l, x[l]);

                        p = x[l] + 1;
                        while (p != x[l])
                        {
                            j = TOP(p);
                            if (j <= 0)
                                p = ULINK(p);
                            else
                            {
                                CommitQ(p, j);
                                //Cover(j);
                                p = p + 1;
                            }
                        }

                        l = l + 1;
                        done |= Stats.TrackLevels(l);
                        if (done) break;

                        step = 2;
                    }

                    break;
                case 6: // Try again
                    p = x[l] - 1;
                    while (p != x[l])
                    {
                        j = TOP(p);
                        if (j <= 0)
                            p = DLINK(p);
                        else
                        {
                            UncommitQ(p, j);
                            p = p - 1;
                        }
                    }

                    i = TOP(x[l]);
                    x[l] = DLINK(x[l]);
                    step = 5;
                    break;
                case 7: // Backtrack
                    Uncover(i, hasColor);
                    step = 8;
                    break;
                case 8: // leave level l
                    if (l == 0)
                    {
                        done = true;
                        break;
                    }

                    l = l - 1;
                    step = 6;
                    break;
                default:
                    throw new InvalidOperationException("Algorithm X invalid step");
            }
        }

        Stats.StopTimer();
        Stats.ShowFinalStats(x);
    }

    /// <summary>
    /// Solve the problem instance.
    /// Answers are written to wherever output was sent.
    /// Solves MCC problem (todo - merge all?)
    /// </summary>
    void SolveM()
    {
        // todo - on randomizing, shuffle items? shuffle options? keep 
        // todo - print options count, item count, node count, secondary item count, etc. here
        // if show totals selected
        // print totals of item covers...?

        SanityCheck(); // todo - remove, check on option

        Stats.ResetStats(optionCount);
        Stats.StartTimer();
        if (Options.Randomize)
            rand = new Random(Options.RandomSeed);

        var step = 1; // represent algo step M1 through M9
        int Z = 0, l = 0, i = 0;

        // for local work
        int j = 0, p = 0;

        // solution
        var x = new int[Stats.maxAllowedLevel]; // overkill in space
        var FT = new int[Stats.maxAllowedLevel]; // "First Tweaks" to track start of pointer chains


        void Chk(int choice)
        {
            return; // there is inf loop happening
            var c = TOP(choice);
            var d = LEN(c);

            int k, p;
            for (k = 1, p = DLINK(c); p != choice; k++)
            {
                p = DLINK(p);
                //if (k > 1000000) 
                //    Console.WriteLine("ERROR");
            }
        }

        var done = false;
        // this follows Knuth TAOCP notation and format
        while (!done)
        {
            if (l > 0 && step == 5)
                Chk(x[l]);

            //Console.WriteLine("Step " + step);
            //Console.WriteLine($"Step: {step}");
            switch (step)
            {
                case 1: // Initialize: load data as above
                    Z = lastSpacerIndex;
                    Trace.Assert(N1 > 0);

                    l = 0; // level
                    step = 2;
                    break;
                case 2: // Enter level l
                    done |= Stats.TrackMemory(l, x); // progress updates
                    if (done)
                        break;

                    if (RLINK(0) == 0) // all items covered
                    {
                        done |= Stats.TrackShape(l);
                        if (done)
                            break;

                        done |= !VisitSolution(x, l); // solution in x0,x1,..,x_{l-1}

                        done |= Stats.TrackSolutions(l);
                        if (done)
                            break;

                        step = 9; // NOTE: different than X and XCC
                    }
                    else
                        step = 3;

                    break;
                case 3: // Choose i
                    // todo - abstract out to func calls?
                    // need one of i1, i2, .., it, where i1= RLINK(0), i_{j+1}=RLINK(i_j), RLINK(it)=0
                    // choose one, here can use MRV (Exercise 9 - todo)

                    // will always walk to get stats


                    var tm = int.MaxValue; // max nodes
                    var ct = 0; // count larger than min?
                    Stats.TrackLevels(0, l); // walk levels to show

                    // see answer to exercise 166
                    // Knuth says he prefers: break ties by smaller SLACK, then longer LEN, exercise 166, replacing this MRV

                    p = RLINK(0);
                    i = p; // minIndex
                    while (p != 0)
                    {
                        Stats.TrackLevels(1, l, p);
                        if (LEN(p) < tm)
                        {
                            tm = LEN(p);
                            i = p;
                            ct = 1;
                        }
                        else
                        {
                            ct++;
                            // choose random number of nodes to skip (Reservoir Sampling)
                            if (Options.Randomize && UniformRand(ct) == 0)
                                i = p; // best_item

                        }

                        p = RLINK(p);
                    }


                    // todo - use MRV of exercise 166 - different than this one!
                    // i is best for MRV, if not MRV, take first
                    if (!Options.MinimumRemainingValuesHeuristic)
                        i = RLINK(0); // choose first for now

                    Stats.TrackLevels(2, l, -1, i, tm);

                    // see exercise 166 answer - get branching factor
                    var θi = Monus(LEN(i) + 1, Monus(BOUND(i), SLACK(i)));
                    if (θi == 0) // NOTE: this branch different than X, XCC
                        step = 9;
                    else
                        step = 4;
                    int Monus(int a, int b) => Int32.Max(a - b, 0);
                    break;
                case 4: // prepare to branch on i
                    x[l] = DLINK(i);
                    BOUND(i, BOUND(i) - 1); // BOUND checks for M
                    if (BOUND(i) == 0) //
                        Cover(i,true);
                    if (BOUND(i) != 0 || SLACK(i) != 0)
                        FT[l] = x[l];
                    step = 5;
                    break;
                case 5: // possibly tweak x[l]
                    // New Step in MCC
                    step = 6; // default
                    if (BOUND(i) == 0 && SLACK(i) == 0)
                    {
                        if (x[l] != i) step = 6;
                        else step = 8; // 8 is like algo C
                    }
                    else if (LEN(i) <= BOUND(i) - SLACK(i))
                    {
                        step = 8; // list i is too short
                    }
                    else if (x[l] != i)
                    {
                        Tweak(x[l], i, BOUND(i) == 0);
                        step = 6;
                    }
                    else if (BOUND(i) != 0)
                    {
                        p = LLINK(i);
                        var q = RLINK(i);
                        RLINK(p, q);
                        LLINK(q, p);
                        step = 6;
                    }

                    break;
                case 6: // Try xl 
                    step = 7; // default
                    if (x[l] != i)
                    {
                        Stats.TrackChoices(l, x[l]);

                        p = x[l] + 1;
                        while (p != x[l])
                        {
                            j = TOP(p);
                            if (j <= 0)
                                p = ULINK(p);
                            else if (j <= N1)
                            {
                                BOUND(j, BOUND(j) - 1);
                                p = p + 1;
                                if (BOUND(j) == 0)
                                    Cover(j, true);
                            }
                            else
                            {

                                Commit(p, j);
                                p = p + 1;
                            }
                        }

                    }
                    l = l + 1;
                    done |= Stats.TrackLevels(l);
                    if (done) break;

                    step = 2;

                    break;
                case 7: // Try again
                    if (x[l] != i)
                    {
                        p = x[l] - 1;
                        while (p != x[l])
                        {
                            j = TOP(p);
                            if (j <= 0)
                                p = DLINK(p);
                            else if (j <= N1)
                            {
                                BOUND(j, BOUND(j) + 1);
                                p = p - 1;
                                if (BOUND(j) == 1)
                                    Uncover(j, true);
                            }
                            else
                            {
                                Uncommit(p, j);
                                p = p - 1;
                            }
                        }
                    }

                    //i = TOP(x[l]);
                    x[l] = DLINK(x[l]);
                    step = 5;
                    break;
                case 8: // Restore i
                    if (BOUND(i) == 0 && SLACK(i) == 0)
                    {
                        Uncover(i, true); // using 52
                    }
                    else
                    {
                        if (BOUND(i) == 0)
                            Untweak(l, FT, true);
                        else
                            Untweak(l, FT, false);
                    }

                    BOUND(i, BOUND(i) + 1);
                    step = 9;
                    break;
                case 9: // leave level l
                    if (l == 0)
                    {
                        done = true;
                        break;
                    }

                    l = l - 1;
                    if (x[l] <= N)
                    {
                        i = x[l];
                        p = LLINK(i);
                        var q = RLINK(i);
                        RLINK(p, i);
                        LLINK(q, i); // reactivates i
                        step = 8;
                    }
                    else
                    {
                        i = TOP(x[l]);
                        step = 7;
                    }

                    break;
                default:
                    throw new InvalidOperationException("Algorithm MCC invalid step");
            }
        }

        Stats.StopTimer();
        Stats.ShowFinalStats(x);
    }

    #region Implementation

    #region Hide, Cover, Purify, Tweak, etc.

    void Tweak(int x, int p, bool primed)
    {
        if (!primed)
            Hide(x,true);
        var d = DLINK(x);
        DLINK(p, d);
        ULINK(d, p);
        LEN(p, LEN(p) - 1);
    }

    void Untweak(int l, IList<int> FT, bool primed)
    {
        var a = FT[l];
        var p = (a <= N ? a : TOP(a));
        var x = a;
        var y = p;
        var z = DLINK(p);
        DLINK(p, x);
        var k = 0;
        while (x != z)
        {
            ULINK(x, y);
            k = k + 1;
            if (!primed)
                Unhide(x,true);
            y = x;
            x = DLINK(x);
        }

        ULINK(z, y);
        LEN(p, LEN(p) + k);
        if (primed)
            Uncover(p, true);
    }

    void CommitQ(int p, int j)
    {
        if (!hasColor)
            Cover(j,false); // AlgorithmX
        else
            Commit(p, j);
    }

    void Commit(int p, int j)
    {
        if (COLOR(p) == 0) Cover(j, true);
        if (COLOR(p) > 0) Purify(p);
    }

    void Purify(int p)
    {
        var c = COLOR(p);
        var i = TOP(p);
        COLOR(i, c);
        var q = DLINK(i);
        while (q != i)
        {
            if (COLOR(q) == c)
                COLOR(q, -1);
            else
                Hide(q, true);
            q = DLINK(q);
        }
    }

    void UncommitQ(int p, int j)
    {
        if (!hasColor)
            Uncover(j, false); // AlgorithmX
        else
            Uncommit(p, j);
    }

    void Uncommit(int p, int j)
    {
        if (COLOR(p) == 0) Uncover(j, true);
        if (COLOR(p) > 0) Unpurify(p);
    }

    void Unpurify(int p)
    {
        var c = COLOR(p);
        var i = TOP(p);
        var q = ULINK(i);
        while (q != i)
        {
            if (COLOR(q) < 0)
                COLOR(q, c);
            else
                Unhide(q,true);
            q = ULINK(q);
        }
    }

    void Cover(int i, bool primed)
    {
        var p = DLINK(i); // MUST BE LOCAL P
        Stats.Update();
        while (p != i)
        {
            Hide(p,primed);
            p = DLINK(p);
        }

        var l = LLINK(i);
        var r = RLINK(i);
        RLINK(l, r);
        LLINK(r, l);
    }

    void Hide(int p, bool primed)
    {
        var q = p + 1;
        while (q != p)
        {
            var x = TOP(q);
            var u = ULINK(q);
            var d = DLINK(q);
            if (x <= 0)
                q = u; // q was a spacer
            else
            {
                if (COLOR(q) >= 0 || !primed)
                {
                    DLINK(u, d);
                    ULINK(d, u);
                    LEN(x, LEN(x) - 1);
                }

                q = q + 1;
                Stats.Update();
            }
        }
    }

    void Uncover(int i, bool primed)
    {
        var l = LLINK(i);
        var r = RLINK(i);
        RLINK(l, i);
        LLINK(r, i);
        var p = ULINK(i);
        while (p != i)
        {
            Unhide(p, primed);
            p = ULINK(p);
        }
    }

    void Unhide(int p, bool primed)
    {
        var q = p - 1;
        while (q != p)
        {
            var x = TOP(q);
            var u = ULINK(q);
            var d = DLINK(q);
            if (x <= 0)
                q = d; // q was a spacer
            else
            {
                if (COLOR(q) >= 0 || !primed)
                {
                    DLINK(u, q);
                    ULINK(d, q);
                    LEN(x, LEN(x) + 1);
                }

                q = q - 1;
            }
        }
    }

    #endregion

    /// <summary>
    /// Get solution described as a list of options, each option given as a list of items
    /// todo - get nicer method?
    /// </summary>
    /// <param name="x"></param>
    /// <param name="l"></param>
    /// <returns></returns>
    List<List<string>> GetSolution(int[] x, int l)
    {
        var ans = new List<List<string>>();
        for (var i = 0; i < l; ++i)
            ans.Add(formatter.GetOptionItems(x[i],normalizeOrder:true).ToList());
        return ans;
    }

    /// <summary>
    /// Record a solution, by sending it to the current text output if asked, 
    /// and to any listeners if present
    /// </summary>
    /// <returns>true to continue search, else false to stop</returns>

    bool VisitSolution(int[] x, int l)
    {
        // solution in x0, x1, ...,x_{l-1}

        ++Stats.SolutionCount;

        var printSoln = Options.OutputFlags.HasFlag(SolverOptions.ShowFlags.AllSolutions);
        var solutionEventHandler = SolutionListener;
        if (solutionEventHandler == null && !printSoln)
            return true; // nothing else to do

        var solutionOptions = GetSolution(x, l);

        if (printSoln)
            formatter.PrintSolution(solutionOptions);

        return solutionEventHandler == null
            ? true
            : solutionEventHandler(Stats.SolutionCount, Stats.Updates, solutionOptions);
    }

    Random? rand = null;

    long UniformRand(long m)
    {
        const long top = 1L << 31;
        long excess = top - (top % m);
        long val;
        do
        {
            val = rand.Next();
        } while (excess <= val);

        return val % m;
    }


    /// <summary>
    /// Add node, return index
    /// </summary>
    /// <returns></returns>
    int AddNode(bool regularNode)
    {
        var nodes1 = regularNode ? nodes : nameNodes;
        nodes1.Add(new());
        return NodeCount - 1;
    }

    void AddSpacer(bool regularNode, bool link = false)
    {
        var x = AddNode(regularNode);
        if (link)
        {
            TOP(x, spacerIndex--);
            ULINK(x, lastSpacerIndex + 1);
            DLINK(lastSpacerIndex, x - 1);
            lastSpacerIndex = x;
        }
    }

    bool SanityCheck()
    {
        int k, p, q, pp, qq, t;
        var root = 0;
        bool ok = true; 
        for (q = root, p = RLINK(q); ; q = p, p = RLINK(p))
        {
            if (LLINK(p) != q)
                Error($"Bad left link at item {NAME(p)}");

            if (p == root) 
                break;

            for (qq = p, pp = DLINK(qq), k = 0; ; qq = pp, pp = DLINK(pp), k++)
            {
                if (ULINK(pp) != qq)
                    Error($"Bad up link at node {pp}");
                if (pp == p) break;
                if (TOP(pp) != p)
                    Error($"Bad TOP at node {pp}");
                if (qq > p && COST(pp) < COST(qq))
                    Error($"Costs out of order at node {pp}");
            }

            if (p < N1 && LEN(p) != k)
                Error($"Bad length in item {NAME(p)}");
        }

        return ok;
        void Error(string s)
        {
            ok = false;
            output.WriteLine(s);
        }
    }

    #region underlying data structure


    int NameNodeCount => nameNodes.Count;

    int NameIndex(string name)
    {
        if (!NameMap.ContainsKey(name))
            throw new Exception($"Items list missing option term {name}");
        return NameMap[name]; // top index
    }

    int NamesCount => Names.Count;
    int NodeCount => nodes.Count;


    // todo - abstract out so I can change it around, only func calls access the other stuff
    string NAME(int i) => Names[nameNodes[i].a];

    void AddName(string name, int index = -1)
    {
        Names.Add(name);
        if (index >= 0)
        {

            // update name nodes
            if (NameMap.ContainsKey(name))
                throw new Exception($"Duplicate name {name} in Dancing Links");

            NameMap.Add(name, index);
        }
    }

    int LLINK(int i)
    {
        Stats.Mem();
        return nameNodes[i].b;
    }

    void LLINK(int i, int val)
    {
        Stats.Mem();
        nameNodes[i].SetB(val);
    }

    int RLINK(int i)
    {
        Stats.Mem();
        return nameNodes[i].c;
    }

    void RLINK(int i, int val)
    {
        Stats.Mem();
        nameNodes[i].SetC(val);
    }

    int LEN(int x)
    {
        Stats.Mem();
        return nodes[x].a;
    }

    void LEN(int x, int val)
    {
        Stats.Mem();
        nodes[x].SetA(val);
    }

    int TOP(int x)
    {
        Stats.Mem();
        return nodes[x].a;
    }

    void TOP(int x, int val)
    {
        Stats.Mem();
        nodes[x].SetA(val);
    }

    int ULINK(int x)
    {
        Stats.Mem();
        return nodes[x].b;
    }

    void ULINK(int x, int val)
    {
        Stats.Mem();
        nodes[x].SetB(val);
    }

    int DLINK(int x)
    {
        Stats.Mem();
        return nodes[x].c;
    }

    void DLINK(int x, int val)
    {
        Stats.Mem();
        nodes[x].SetC(val);
    }

    #region COSTS - todo - finish impl
    bool hasCost = false;

    long COST(int x) => 0; // for now
    private List<long> costs = new(); // one per node
    void COST(int x, long val)
    {
        //todo;
    }
    
    // max heap in array, 1 indexed as in knuth
    // K items
    // thus best(j/2) >= best(j), 1<=j<=K
    private List<long> best = new();
    private int K = 0;

    #endregion

    void ClearData()
    {
        Names.Clear();
        NameMap.Clear();
        nameNodes.Clear();
        nodes.Clear();
        optionCount = 0;
        spacerIndex = 0;
        lastSpacerIndex = 0;
        N2 = 0; 
        hasColor = false;
        hasMultiplicities = false;
        slack.Clear();
        bound.Clear();

        // set best to K items of value max int
        hasCost = false;
        costs.Clear();
        best.Clear();
        K = 0;
    }

int COLOR(int x)
    {
        Stats.Mem();
        return nodes[x].datum;
    }

    void COLOR(int x, int val)
    {
        Stats.Mem();
        nodes[x].SetD(val);
    }

    int ColorIndex(string name)
    {
        if (!ColorMap.ContainsKey(name))
        {
            Colors.Add(name);
            ColorMap.Add(name, Colors.Count);
        }

        return ColorMap[name];
    }

    // state
    int optionCount = 0; // count of options

    // throughout, N = total items, N1 = primary items, N2 = secondary items
    int N => NameNodeCount - 1; // -1 for spacer address
    int N1 => N - N2; // primary item count
    int N2 = 0;       // 


    TextWriter output;
    int spacerIndex = 0; // decrement to fill in TOP
    int lastSpacerIndex = 0;
    bool hasColor = false;
    bool hasMultiplicities = false;


    // todo - big rewrite - there is per class overhead, since array is array of classes
    // better to put all into (for C# memory) one array, each cell 4 ints long, update the accessors, hide the node and such arrays

    // name text to node index
    readonly Dictionary<string, int> NameMap = new();
    readonly List<string> Names = new();

    readonly List<Node> nodes = new();
    readonly List<Node> nameNodes = new(); // one empty node, then one per item

    readonly List<string> Colors = new(); // 0 is not used, other index from node datum
    readonly Dictionary<string, int> ColorMap = new(); // color name to index in Colors

    // for multiplicities, every primary item has SLACK and BOUND
    List<int> slack = new();
    List<int> bound = new();

    int SLACK(int x)
    {
        Stats.Mem();
        return slack[x];
    }

    void SLACK(int x, int val)
    {
        Stats.Mem();
        slack[x] = val;
    }

    int BOUND(int x)
    {
        Stats.Mem();
        return bound[x];
    }


    void BOUND(int x, int val)
    {
        Stats.Mem();
        bound[x] = val;
    }

    // entire thing is an array, each elt is 3 integers, accessed closely together
    // we call them a,b,c, access through accessors as in Knuth TAOCP
    class Node
    {
        public void SetA(int val) => a = val; // trying to get to work as a struct - does not work...
        public void SetB(int val) => b = val; // trying to get to work as a struct - does not work...
        public void SetC(int val) => c = val; // trying to get to work as a struct - does not work...
        public void SetD(int val) => datum = val; // trying to get to work as a struct - does not work...
        public int a = 0, b = 0, c = 0;
        public int datum = 0; // pads to 128 bits, used as info per node
    }

    #endregion

    #endregion



    #region old interface

    [Obsolete("AddColumn in deprecated, please use AddItem instead. Note inversion of mandatory/secondary flags")]
    public void AddColumn(string name, bool mandatory = true) => AddItem(name, !mandatory);

    /// <summary>
    /// Call this to start a new row. 
    /// Then use SetColumn to set entries
    /// </summary>
    [Obsolete("NewRow is deprecated, use AddOption")]
    public void NewRow()
    {
    }

    /// <summary>
    /// Set a column as attached on the current row
    /// </summary>
    /// <param name="name">The name of the column</param>
    [Obsolete("SetColumn is deprecated, use AddOption")]
    public void SetColumn(string name)
    {
    }

    /// <summary>
    /// Set a column as attached on the current row
    /// </summary>
    /// <param name="number">The index of the column</param>
    [Obsolete("SetColumn is deprecated, use AddOption")]
    public void SetColumn(int number)
    {
    }

    /// <summary>
    /// Number of solutions found
    /// </summary>
    [Obsolete("NumSolutions is deprecated, use Stats.SolutionCount")]
    public long NumSolutions => Stats.SolutionCount;

    /// <summary>
    /// Number of item removals performed during the search
    /// </summary>
    [Obsolete("DequeRemovals is deprecated, use Stats.Updates")]
    public long DequeRemovals => Stats.Updates;

    /// <summary>
    /// Use the heuristic Knuth calls S when choosing.
    /// It results in smaller searches at the cost of more
    /// memory access per node. Knuth's tests showed it useful
    /// in all cases he tried. Your mileage may vary.
    /// </summary>
    [Obsolete("UseGolumbHeuristic is deprecated, use Options settings")]
    public bool UseGolumbHeuristic
    {
        get;
        private set; // 
    }

    /// <summary>
    /// Count of NumColumns so far
    /// </summary>
    [Obsolete("ColumnCount is deprecated.")]
    public int ColumnCount => NamesCount;

    /// <summary>
    /// Count of NumRows so far
    /// </summary>
    [Obsolete("RowCount is deprecated.")]
    public int RowCount => optionCount;

    /// <summary>
    /// Disconnect a row
    /// </summary>
    /// <param name="number"></param>
    [Obsolete("DisableRow is deprecated.")]
    public void DisableRow(int number)
    {
    }

    /// <summary>
    /// Enable a row for usage
    /// </summary>
    /// <param name="number"></param>
    [Obsolete("EnableRow is deprecated.")]
    public void EnableRow(int number)
    {
    }

    #endregion

}