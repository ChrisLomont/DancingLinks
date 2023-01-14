﻿/*
MIT License

Copyright (c) 2023 Chris Lomont

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

*/

using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Lomont.Algorithms;

// copyright Chris Lomont, www.lomont.org
// Replaces my earlier DLX solvers from the past 20-ish years
//
// Chris Lomont, 2023
//     - rewrite to handle all the new variants in Knuth's 
//       "The Art of Computer Programming" (TAOCP) volume 4B,
//       "Combinatorial Algorithms, Part II"
//       Also references his 2002 paper Dancing Links
// Some of my older version comments :
// Chris Lomont, 2015, 
//     - added dumping the state, lineEnd, and solutionEnd, UseGolombHeuristic
//     - added SolutionRecorderDelegate for nicer solution handling
// Chris Lomont, 2013, C# ported from earlier Lomont C++ version


/// <summary>
/// code for Knuth's Dancing Links algorithms from his Dancing Links
/// paper, as extended by his book TAOCP vol 4B.
///
/// This solves basic dancing links, adds support for secondary items,
/// colors, multiplicities, and costs. Planned to handle preprocessing and ZDDs
/// 
/// To use:
/// 0. (Optional) set options
///    TODO - explain
/// 1. Add each item. There is usually an item for each cell to cover and for
///    each piece when solving puzzles. Items can be primary (required to be covered, i.e., multiplicity 1)
///    or secondary (optionally covered, i.e., multiplicity 0 or 1), or can have a multiplicity range [a,b].
///    The multiplicity is the number of times this item needs selected in any solution.
/// 2. Add options which consist of a set of items to cover. An option can assign a 'color' to an item which is
///    a text tag that all options including that item must agree on. An option can have a cost associated
///    when computing the K lowest cost solutions.
/// 3. Set output as desired. The Options can set things, SetOutput sets a stream writer, and
///    event SolutionListener can be attached for getting all solutions and providing a way to stop
///    enumeration during a solve.
/// 4. Call Solve, which automatically calls the correct internal solver.
/// 5. Result count is in SolutionCount, best cost solutions are in TODO. Inspect Stats for other statistics
/// 
/// The solver picks all subsets of options that match the item covering criteria. 
/// </summary>
public class DancingLinksSolver
{
    // Knuth Algorithm X
    // Exact cover via Dancing Links


    /* TODO
    - DONE: MIT license
    - consider how to add memoization so parts of the search space repeatedly computed are skipped.... is this possible? Many search trees have lots of redundancy
    - add user callback every (user settable) many mems or nodes or such, allowing interactive quitting, inspection , etc.
    - can we remove secondary as a flag and simply use 0-1 bounds?
       - do speed testing between algos before deprecating/removing any
    - extend to have a name for each row, and return row names in solution 
    - DONE: move namespace to match other Lomont DL
    - DONE: add color codes algo as generalization XC = exact cover, XCC = with colors
    - DONE: MCC adds multiplicities to color problem
    - add MonteCarlo estimator as in book
    - do all examples and problems in book as examples in code
    - explain colors, secondary items, slack versus secondary, examples, etc.
    - track updates per depth, nodes per depth, like DL paper, show updates/node, etc.
    - add nice description of how to use: mention slack vars and secondary items
    - exercise 19 handles options with no primary items
    - DONE: C$ handles with costs per options, return lowest K costs
    - Z produces XCC solns as ZDDS which can be handles in other ways
    - DONE: make as drop in replacement for my old DancingLinks code
    - make nicer parser for file, command line, other uses
    - DONE: output colors in solutions (not currently working?)
    - DONE: output costs in solutions (not currently working?)
    - error if color assigned to non-secondary item
    - output for bound and slack
    - some sanity checking to ensure structure is valid, useful for debugging, extending
    - extend cost C$ and X$ algos to an M$ algo, send to Knuth
    - better M3 step choices - see book and exercises for ideas
    - items strings, spaced out, '|' splits primary from secondary

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
    option has  ':' for color on secondary items only

    Costs:
    Append things like "$n" where n is nonnegative integer
    to add cost to any option. If multiple such entries in same option, 
    option cost is their sum
     */

    public DancingLinksSolver()
    {
        formatter = new Formatter(this);
        Stats = new LogStats(this);
        Clear();
    }

    /// <summary>
    /// Reset all internals, allows reusing the object
    /// </summary>
    public void Clear()
    {
        ClearData();

        Stats.ResetStats(1);
        formatter.Reset();
        LowestCostSolutions.Clear();

        // initial spacer items
        var spacerName = "_";
        AddRegularSpacer(); // regular nodes all 0s
        AddNameSpacer(spacerName); // name nodes all 0s
        AddName(spacerName); // not in map, so not usable or discoverable by outside
    }

    #region Solver and output options

    public SolverOptions Options { get; set; } = new();

    public class SolverOptions
    {
        /// <summary>
        /// Where to send output
        /// </summary>
        public TextWriter Output { get; set;  } = TextWriter.Null;

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
        /// max mems till 'timeout' and stop search
        /// </summary>
        public long MemsStopThreshold { get; set; } = Int64.MaxValue; // max mems, else timeout

        // todo - describe and use these, test them
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
    public void AddItems(IEnumerable<string> items)
    {
        foreach (var item in items)
            AddItem(item);
    }

    /// <summary>
    /// Add a new item by unique name
    /// Secondary items must come after all primary items
    /// </summary>
    /// <param name="name">The unique text name of the item</param>
    /// <param name="secondary">If this a secondary item or not</param>
    /// <param name="lowerBound">The least number of times to cover this item</param>
    /// <param name="upperBound">The most number of times to cover this item</param>
    public void AddItem(string name, bool secondary = false, int lowerBound = 1, int upperBound = 1)
    {
#if true
        if (upperBound != 1 || lowerBound != 1)
            hasMultiplicities = true;
        items.Add(new(name,secondary, lowerBound, upperBound));
        hasSecondary |= secondary;
#else
        // track N = # items, N1 = primary items, N2 = # secondary items

        // TODO - rewrite to be cleaner: track list of items, then gen all once all are added
        // as in exercise 8

        // handle secondary items
        if (secondary)
            N2++;
        else
            Trace.Assert(N2 == 0); // all must be primary before first secondary, then all secondary


        var i = AddNameNode(name) + 1;
        
        AddName(name, i);

        // set node links
        var len = NameNodeCount;
        RLINK(i, 0);
        LLINK(i, (i - 1 + len) % len);

        // attach prev and next
        LLINK(0, i);
        RLINK((i - 1 + len) % len, i);

        // update general nodes
        var x = AddNode();
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
#endif
    }


#endregion

#region Options Input

    /// <summary>
    /// Split a string on spaces into items and pass them into AddOption
    /// </summary>
    /// <param name="option"></param>
    public void ParseOption(string option) => AddOption(option.Split(' ', StringSplitOptions.RemoveEmptyEntries));

    /// <summary>
    /// Add an option as a space separated list of items
    /// </summary>
    /// <param name="option">Space separated list of items</param>
    public void AddOption(string option) => AddOption(option.Split(' ', StringSplitOptions.RemoveEmptyEntries));

    public void AddOptions(IEnumerable<IList<string>> options)
    {
        foreach (var option in options)
            AddOption(option);
    }


    /// <summary>
    /// Add an option which is a list of items in this option
    /// Every option must include at least one primary item otherwise the option will not be included in the solver
    /// todo - do exercise 19 which allows using options with only secondary items
    /// an option can have suffix string ':color' on any secondary item where 'color'
    /// is text. Then all options with that item must have the same 'color' tag. Note color simply
    /// means all options with that item must have the same tag.
    ///
    /// TODO: add $cost suffix for cost, then solve minimum cost options
    /// </summary>
    /// <param name="items"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void AddOption(IEnumerable<string> items)
    {
        var pat = @"^(?<name>[^$:|]*)((:(?<color>[^$:|]+))|(\$(?<cost>[0-9]+))){0,2}$";
        var reg = new Regex(pat);
        var opt = new List<(string, string, long)>();
        foreach (var item in items)
        {
            var m = reg.Match(item);
            if (!m.Success)
                throw new Exception($"Invalid item {item} in option");
            var name  = m.Groups["name"].Value;
            var color = m.Groups["color"].Success ? m.Groups["color"].Value : "";
            var cost  = m.Groups["cost"].Success ? Int64.Parse(m.Groups["cost"].Value) : -1;
            
            // for debugging
            //Console.WriteLine($"Item: name:{name} color {color} cost {cost}");

            if (color != "")
                hasColor = true;
            if (cost != -1)
                hasCost = true;

            opt.Add(new(name, color, cost));
        }

        options.Add(new Option(opt));
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
        dumpOutput.WriteLine("------------------");
        dumpOutput.WriteLine("i NAME LLINK RLINK");
        for (var i = 0; i < NameNodeCount; ++i)
            dumpOutput.WriteLine($"{i}: {NAME(i)} {LLINK(i)} {RLINK(i)}");

        var cs = "";
        if (hasColor)
        {
            cs = "COLOR";
        }

        dumpOutput.WriteLine("------------------");
        dumpOutput.WriteLine($"x LEN ULINK DLINK {cs}");
        for (var x = 0; x < NameNodeCount; ++x)
        {
            var cc = COLOR(x);
            var ce = (hasColor && cc > 0) ? Colors[cc - 1] : cc.ToString();
            dumpOutput.WriteLine($"{x}: {LEN(x)} {ULINK(x)} {DLINK(x)} {ce}");
        }

        dumpOutput.WriteLine("------------------");
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

    /// <summary>
    /// Number of options added
    /// </summary>
    public int OptionCount { get; private set; } = 0; // count of options

    public long SolutionCount => Stats.SolutionCount;
    public LogStats Stats { get; }

    public class LogStats
    {

        public LogStats(DancingLinksSolver dl) => this.dl = dl;

        public long SolutionCount { get; set; }
        public long Updates { get; private set; }

        public void Update() => Updates++;

        // max possible level 
        public int MaxAllowedLevel = 0;

        /// <summary>
        /// Increment mems
        /// </summary>
        public void Mem() => MemAccesses++;

        public void ResetStats(int optionCount)
        {
            // tracking of mem and other usage
            SolutionCount = 0;
            Updates = 0;
            nodeCount = 0;
            maxDegree = 0;
            MaxLevelSeen = 0;
            MemAccesses = 0; 
            inputMemAccesses = 0;
            nextMemoryDump = -1; // memory threshold
            
            // max allowed levels is (optionCount+1) * max of bounds
            MaxAllowedLevel = optionCount + 1;
            if (dl.hasMultiplicities)
                MaxAllowedLevel *= dl.bound.Max();

            profile = new long[MaxAllowedLevel];
            choice = new int[optionCount + 1];
        }

        // timer for timing entire solution search
        readonly Stopwatch timer = new();

        public void StartTimer() => timer.Restart();

        public void StopTimer() => timer.Stop();
        
        /// <summary>
        /// Track some memory stats
        /// Dumps state or progress if asked
        /// Stop progress if past mems threshold
        /// Called at step 2, on entering a new level
        /// </summary>
        /// <param name="level"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        public bool TrackMemory(int level, int[] x)
        {
            if (dl.Options.OutputFlags.HasFlag(SolverOptions.ShowFlags.Profile))
                profile[level]++;

            nodeCount++; // nodes explored

            if (nextMemoryDump == -1) nextMemoryDump = dl.Options.MemsDumpStepSize;
            if (dl.Options.MemsDumpStepSize > 0 && (MemAccesses >= nextMemoryDump) && 
                dl.Options.OutputFlags.HasFlag(SolverOptions.ShowFlags.Basics))
            {
                nextMemoryDump += dl.Options.MemsDumpStepSize;
                if (dl.Options.OutputFlags.HasFlag(SolverOptions.ShowFlags.FullState)) 
                    dl.formatter.PrintState(level, x);
                else
                    dl.formatter.PrintProgress(level, x);
            }

            if (MemAccesses >= dl.Options.MemsStopThreshold)
            {
                dl.Output.WriteLine("TIMEOUT!");
                return true; // done!
            }

            return false;
        }

        public bool TrackLevels(int l)
        {
            if (l > MaxLevelSeen)
            {
                if (l >= MaxAllowedLevel)
                {
                    dl.Output.WriteLine("Too many levels!");
                    return true; // done
                }

                MaxLevelSeen = l;
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
                dl.Output.WriteLine($"{SolutionCount}:");
                for (var k = 0; k < l; k++)
                    dl.formatter.PrintOption(choice[k]);
                dl.Output.Flush();
            }

            nodeCount++; 

            if (SolutionCount >= dl.Options.MaxSolutionCount)
                return true; // done
            return false;
        }

        public void TrackLevels(int type, int l, int p = -1, int bestItem = -1, int tmax = -1)
        {
            var good = dl.Options.OutputFlags.HasFlag(SolverOptions.ShowFlags.Details) &&
                       l < dl.Options.ShowChoicesMax && l >= MaxLevelSeen - dl.Options.ShowChoicesGap;
            if (!good) return;

            if (type == 0)
                dl.Output.Write($"Level {l}");
            if (type == 1)
                dl.Output.Write($" {dl.NAME(p)} {dl.LEN(p)}");

            if (type == 2)
            {
                dl.Output.WriteLine($" branching on {dl.NAME(bestItem)} {tmax}");
                if (dl.Options.ShapeOutput != null)
                {
                    dl.Options.ShapeOutput.WriteLine($"{tmax} {dl.NAME(bestItem)}");
                    dl.Options.ShapeOutput.Flush();
                }

                dl.Stats.maxDegree = Math.Max(dl.Stats.maxDegree, tmax);
            }
        }

        public void TrackChoices(int l, int xl)
        {
            if (dl.Options.OutputFlags.HasFlag(SolverOptions.ShowFlags.Choices) && l < dl.Options.ShowChoicesMax)
            {
                dl.Output.Write($"L{l}:");
                dl.formatter.PrintOption(xl);
            }
        }

        public void ShowFinalStats(int[] x)
        {
            var (imems, mems) = (imems: this.inputMemAccesses, mems: this.MemAccesses); // store updates here before reporting
            if (dl.Options.OutputFlags.HasFlag(SolverOptions.ShowFlags.Totals))
            {
                dl.Output.Write("Item totals:");
                for (var k = 1; k < dl.NameNodeCount; k++)
                {
                    if (k == dl.N1)
                        dl.Output.Write(" |");
                    dl.Output.Write($" {dl.LEN(k)}");
                }

                dl.Output.WriteLine("");
            }

            if (dl.Options.OutputFlags.HasFlag(SolverOptions.ShowFlags.Profile))
            {
                dl.Output.WriteLine("Profile:");
                for (var level = 0; level <= MaxLevelSeen; level++)
                    dl.Output.WriteLine($"   {level} {profile[level]}");
            }

            if (dl.Options.OutputFlags.HasFlag(SolverOptions.ShowFlags.MaxDegree))
                dl.Output.WriteLine($"The maximum branching degree was {maxDegree}");

            if (dl.Options.OutputFlags.HasFlag(SolverOptions.ShowFlags.Basics))
            {

                var nodeSize = 4 * sizeof(int);
                var bytes = dl.NodeCount * nodeSize + dl.NameNodeCount * nodeSize + x.Length * sizeof(int) +
                            dl.Names.Sum(n => n.Length); // treat as ASCII or utf8


                var plural = SolutionCount == 1 ? "" : "s";
                dl.Output.WriteLine($"{SolutionCount} solution{plural} in {timer.Elapsed}");
                dl.Output.WriteLine($"{imems}+{mems} mems, {Updates} updates, {bytes} bytes memory, {nodeCount} nodes");
                // todo - mems/soln? Nodes/sec? solns/sec?
            }

            if (dl.Options.ShapeOutput != null)
                dl.Options.ShapeOutput.Close(); // todo -  dont; close console.out - how to cloe file but not this?
        }

        #region Implementation

        readonly DancingLinksSolver dl;
        public int MaxLevelSeen = 0; // max level seen while searching
        int maxDegree = 0;

        long nodeCount = 0; // nodes walked
        // (one up and down is one mem, basically # of 64 bit line accesses
        // nodes padded with _reserve_ field to pad to 128 bits to match Knuth

        public long MemAccesses = 0; // each mem access 
        long inputMemAccesses = 0; // mems to initialize, set in solver
        long[] profile = new long[1];
        long nextMemoryDump = -1; // next memory threshold for output, or -1 when eeding set
        int[] choice = new int[1]; // current choice index



#endregion
    } // stats class

#endregion

/// <summary>
/// Abstract out formatting of various things in the memory structures
/// </summary>
    class Formatter
    {
        readonly DancingLinksSolver dl;
        
        public Formatter(DancingLinksSolver dl) => this.dl = dl;

        long ItemCost(int p)
        {
            if (p < dl.NameNodeCount || p >= dl.NodeCount || dl.TOP(p) <= 0)
            {
                dl.Output.WriteLine($"Illegal option {p}!");
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
                dl.Output.WriteLine($"Illegal option {p}!");
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
                var cq = dl.COLOR(q);
                if (dl.hasColor &&  cq != 0)
                {
                    var qt = dl.TOP(q);
                    var index = cq > 0 ? cq : dl.COLOR(qt);
                    if (index >= dl.Colors.Count)
                        name += ":ERR"; //todo - we hit this sometimes = fix it
                    else
                        name += $":{dl.Colors[index-1]}";
                }

                if (dl.hasCost && dl.COST(q) > 0)
                    name += $"${dl.COST(q)}";
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
                    dl.Output.WriteLine($"Illegal option {p}!");
                    return;
                }
                foreach (var itemText in GetOptionItems(p, normalizeOrder: false))
                    dl.Output.WriteLine(itemText);
                var s = ItemCost(p);

                int k, j, q;

                for (q = dl.DLINK(dl.TOP(p)), k = 1; q != p; k++)
                {
                    if (q == dl.TOP(p))
                    {
                        dl.Output.Write(" (?)");
                        goto finish;
                    }
                    q = dl.DLINK(q);
                }

                for (q = dl.DLINK(dl.TOP(p)), j = 0; q >= dl.NameNodeCount; q = dl.DLINK(q), j++)
                    if (dl.COST(q) >= costThreshold)
                        break;
                dl.Output.Write($" ({k} of {j})");

                finish:

                if (s + dl.COST(p) != 0)
                    dl.Output.Write($" {s + dl.COST(p)} [{dl.COST(p)}]");
                dl.Output.WriteLine();
        }

        public void PrintState(int level, int[] choice)
        {
            // based on Exercise #12, as noted on page 73

            dl.Output.WriteLine($"Current state (level {level})");
            for (var l = 0; l < level; l++)
            {
                PrintOption(choice[l]);
                if (l >= dl.Options.ShowLevelMax)
                {
                    dl.Output.WriteLine(" ...");
                    break;
                }
            }

            dl.Output.WriteLine($"{dl.SolutionCount} solutions, {dl.Stats.MemAccesses} mems, and max level {dl.Stats.MaxLevelSeen} so far.");
        }

        public void PrintProgress(int level, int[] choice)
        {
            // based on Exercise #12, as noted on page 73

            dl.Output.Write($" after {dl.Stats.MemAccesses} mems: {dl.SolutionCount} sols, ");
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
                        dl.Output.WriteLine("ERROR - bad fields in Print Progress!");
                        break;
                    }

                    p = dl.DLINK(p);
                }

                fd *= d;
                f += (k - 1) / fd;
                dl.Output.Write($"{Enc(k)}{Enc(d)} ");

                if (l >= dl.Options.ShowLevelMax)
                {
                    dl.Output.Write("...");
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

            dl.Output.WriteLine($" {(f + 0.5 / fd):F5}");
        }

        /// <summary>
        /// Write out a solution to the current output
        /// </summary>
        public void PrintSolution(IEnumerable<List<string>> solutionNodes)
        {
            var os = dl.Output;
            os.WriteLine($"Solution {dl.SolutionCount} (found after {dl.Stats.Updates} deque removals):");
            foreach (var line in solutionNodes)
            {
                foreach (var s in line)
                    os.Write($"{s} ");
                os.WriteLine(LineEnd);
            }

            os.WriteLine(SolutionEnd);
        }

        // set from outside, useful for separating for parsers
        public string LineEnd = "";
        public string SolutionEnd = "";

      
        public void Reset()
        {
            LineEnd = "";
            SolutionEnd = "";
        }
    }

    private readonly Formatter formatter;

    /// <summary>
    /// Solve the problem instance.
    /// Answers are written to wherever output was sent.
    /// </summary>
    public void Solve(int topKbyCosts = 1)
    {
        // todo - count initialize mem accesses here.
        PrepItemsAndOptions();

        // determine instance type from hasSecondary, hasColor, hasMultiplicities, hasCost
        var type = (hasSecondary, hasColor, hasMultiplicities, hasCost) switch
        {// second, color,  mult,  cost
            (false, false, false, false) => "AlgorithmX",
            (true , false, false, false) => "AlgorithmX (with secondary)",
            (false, true , false, false) => "AlgorithmC",
            (true , true , false, false) => "AlgorithmC (with secondary)",
            (false, false, true , false) => "AlgorithmM",
            (true , false, true , false) => "AlgorithmM (with secondary)",
            (false, true , true , false) => "AlgorithmM (with colors)",
            (true , true , true , false) => "AlgorithmM (with colors and secondary)",

            (false, false, false, true ) => "AlgorithmX$ (X with costs)",
            (true , false, false, true ) => "AlgorithmX$ (X with costs and secondary)",
            (false, true , false, true ) => "AlgorithmC$ (C with costs)",
            (true , true , false, true ) => "AlgorithmC$ (C with costs and secondary)",
            (_, _, true , true ) => throw new Exception("Problems cannot have both costs and multiplicities (other than secondary items)"),
        };
        

        // todo - output flavor of problem too: DLX, DLC< DLM, X$, etc.
        if (Options.OutputFlags.HasFlag(SolverOptions.ShowFlags.Basics))
            Output.WriteLine($"Solving with {items.Count} items and {options.Count} options using algorithm {type}");
        
        if (hasCost)
        {
            SolveC(topKbyCosts);
            return;
        }

        if (hasMultiplicities)
        { // can make all use SolveM if bounds added for each item.
            SolveM(); // todo - unify all solutions into one solver?
            return;
        }

        // solve AlgorithmX and AlgorithmC problems


        // todo - on randomizing, shuffle items? shuffle options? keep 
        // todo - print options count, item count, node count, secondary item count, etc. here
        // if show totals selected
        // print totals of item covers...?

        Stats.ResetStats(OptionCount);
        Stats.StartTimer();
        if (Options.Randomize)
            rand = new Random(Options.RandomSeed);

        var step = 1; // represent algo step X1 through X8
        int Z = 0, l = 0, i = 0; //, xl = 0;

        // for local work
        int j = 0, p = 0;

        // solution
        var x = new int[Stats.MaxAllowedLevel]; // overkill in space

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

                        done |= !VisitSolution(x, l, 0); // solution in x0,x1,..,x_{l-1}

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

        Stats.ResetStats(OptionCount);
        Stats.StartTimer();
        if (Options.Randomize)
            rand = new Random(Options.RandomSeed);

        var step = 1; // represent algo step M1 through M9
        int Z = 0, l = 0, i = 0;

        // for local work
        int j = 0, p = 0;

        // solution
        var x = new int[Stats.MaxAllowedLevel]; // overkill in space
        var FT = new int[Stats.MaxAllowedLevel]; // "First Tweaks" to track start of pointer chains


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

                        done |= !VisitSolution(x, l, 0); // solution in x0,x1,..,x_{l-1}

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
                    step = θi == 0 ? 9 : 4; // NOTE: this branch different than X, XCC
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
                        step = x[l] != i ? 6 : 8; // 8 is like algo C
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
                        Untweak(l, FT, BOUND(i) == 0);
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

    /// <summary>
    /// If instance had costs, and lowest cost solutions were desired,
    /// they are returned here, lowest cost first.
    /// </summary>
    public List<(long cost, List<List<string>> solution)> LowestCostSolutions { get; } = new();

    /// <summary>
    /// Solve the problem instance.
    /// Answers are written to wherever output was sent.
    /// Solves Knuth Algorithm X$ and C$ problems
    /// </summary>
    void SolveC(int topKbyCosts)
    {
#if true
        // todo - on randomizing, shuffle items? shuffle options? keep 
        // todo - print options count, item count, node count, secondary item count, etc. here
        // if show totals selected
        // print totals of item covers...?

        Stats.ResetStats(OptionCount);
        Stats.StartTimer();
        if (Options.Randomize)
            rand = new Random(Options.RandomSeed);

        var step = 1; // represent algo step C$1 through C$8
        int Z = 0, l = 0, i = 0; //, xl = 0;
        long [] C = new long[Stats.MaxAllowedLevel]; // cost by level
        long[] TH0 = new long[Stats.MaxAllowedLevel]; // threshold_0 by level, = BEST- C[l] - COST(x[l])
        long[] TH = new long[Stats.MaxAllowedLevel]; // threshold by level, decreases

        best = new MaxHeap<List<List<string>>>(topKbyCosts); // track top K best costs
        // todo; - track best solutions also
        //long threshold = Int64.MaxValue; // cost cutoff
        long thresh = Int64.MaxValue;

        var sanityChecking = false; // todo - make Option

        // for local work
        int j = 0, p = 0;

        // solution
        var x = new int[Stats.MaxAllowedLevel]; // overkill in space

        var done = false;
        // this follows Knuth TAOCP notation and format
        while (!done)
        {
            //Console.WriteLine($"Step {step}");
            switch (step)
            {
                case 1: // Initialize: load data as above
                    Z = lastSpacerIndex;
                    l = 0; // level
                    step = 2;
                    break;
                case 2: // Enter level l
                    if (sanityChecking) 
                        CheckSanity();

                    done |= Stats.TrackMemory(l, x); // progress updates
                    if (done)
                        break;

                    if (RLINK(0) == 0) // all items covered
                    {
                        done |= Stats.TrackShape(l);
                        if (done)
                            break;

                        done |= !VisitSolution(x, l, C[l - 1] + COST(x[l-1])); // solution in x0,x1,..,x_{l-1}

                        done |= Stats.TrackSolutions(l);
                        if (done)
                            break;

                        step = 8;
                    }
                    else
                        step = 3;

                    break;
                case 3: // Choose i

                    step = 4; // default next step

                    // todo - abstract out to func calls?
                    // need one of i1, i2, .., it, where i1= RLINK(0), i_{j+1}=RLINK(i_j), RLINK(it)=0
                    // choose one, here can use MRV (Exercise 9 - todo)

                    // will always walk to get stats

                    Stats.TrackLevels(0, l); // walk levels to show
                    var tm = int.MaxValue; // max nodes

                    // old way to choose i
                    var ct = 0; // count larger than min?
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
                    // todo - cannot choose RLINK(0) blindly without considering costs?
                    if (!Options.MinimumRemainingValuesHeuristic)
                        i = RLINK(0); // choose first for now



                    {
                        // cost related cutoffs? and choose i
                        // exercise 248 has some details here
                        var t = long.MaxValue;
                    long c = 0;
                    long cp = 0;
                    var L = 10; // Knuth uses 10 in exercise 248, is a parameter
                    j = RLINK(0);
                    while (j > 0)
                    {
                        p = DLINK(j);
                        cp = COST(p);
                        if (p == j || cp >= thresh)
                        {
                            step = 8;
                            break;
                        }

                        var s = 1;
                        p = DLINK(p);
                        while (true)
                        {
                            if (p == j || COST(p) >= thresh)
                            {
                                break;

                            }
                            else if (s == t)
                            {
                                s++;
                                break;

                            }
                            else if (s >= L)
                            {
                                s = LEN(j);
                                tm = Math.Min(tm, s);
                                break;
                            }
                            else
                            {
                                s += 1;
                                p = DLINK(p);
                            }
                        }

                        if (s < t || (s == t && c < cp))
                        {
                            t = s;
                            i = j;
                            c = cp;
                        }

                        j = RLINK(j);
                    }
                    }
                    // todo - restore Stats.TrackLevels(1,l,p)

                  

                    Stats.TrackLevels(2, l, -1, i, tm);

                    break;
                case 4: // Cover item i using (12) from TAOCP
                    // C[l] is cost of x0,x1,..x_{l-1}
                    if (l>0)
                        C[l] = COST(x[l-1]) + C[l - 1];
                    x[l] = DLINK(i);
                    TH0[l] = best.Top - C[l] - COST(x[l]);
                    //TH0[l] = Int64.MaxValue; // todo: remove line
                    Trace.Assert(TH0[l]>0); // item i chosen to make this so
                    Cover_S(i, hasColor, TH0[l]);
                    //Console.WriteLine($"Cover {l}:{TH0[l]}");
                    step = 5;
                    break;
                case 5: // Try xl 
                    thresh = best.Top - C[l] - COST(x[l]); // may change time to time?
                    //thresh = Int64.MaxValue; // todo - remove
                    if (x[l] == i || thresh <= 0)
                        step = 7; // we tried all options for i
                    else
                    {
                        TH[l] = thresh; // +1 for level increase later
                        Stats.TrackChoices(l, x[l]);

                        p = x[l] + 1;
                        while (p != x[l])
                        {
                            j = TOP(p);
                            if (j <= 0)
                                p = ULINK(p);
                            else
                            {
                                CommitQ_S(p, j, TH[l]); // to match undo in step 6
                                //Console.WriteLine($"Commit {l}:{TH[l]}");
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
                            UncommitQ_S(p, j, TH[l]);
                            //Console.WriteLine($"Uncommit {l}:{TH[l]}");
                            p = p - 1;
                        }
                    }

                    i = TOP(x[l]);
                    x[l] = DLINK(x[l]);
                    step = 5;
                    break;
                case 7: // Backtrack
                    Uncover_S(i, hasColor, TH0[l]);
                    //Console.WriteLine($"Uncover {l}:{TH0[l]}");
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
        if (sanityChecking) 
            CheckSanity();


        Stats.StopTimer();
        Stats.ShowFinalStats(x);

        // get best solutions
        LowestCostSolutions.AddRange(best.Values!);
        // sort by non-decreasing cost
        LowestCostSolutions.Sort((a,b)=> a.cost.CompareTo(b.cost));

        void CheckSanity()
        {
            int k, q, p, pp, qq, t;
            for (q = 0, p = RLINK(q); ; q = p, p = RLINK(p))
            { // next = RLINK, pref = LLINK
                if (LLINK(p) != q)
                    Output.WriteLine($"Bad LLINK field at item {NAME(p)}");
                if (p == 0) break;

                for (qq = p, pp = DLINK(qq), k = 0; ; qq = pp, pp = DLINK(pp), k++)
                {
                    if (ULINK(pp) != qq)
                        Output.WriteLine($"Bad ULINK field at node {pp}");
                    if (pp == p) break;
                    if (TOP(pp) != p)
                        Output.WriteLine($"Bad TOP field at node {pp}");
                    if (qq > p && COST(pp) < COST(qq))
                        Output.WriteLine($"Costs out of order between nodes {pp} and {qq}");
                }
                if (p < N && LEN(p) != k)  // should this be N? N1? N2?
                    Output.WriteLine($"Bad LEN field in item {NAME(p)}");
            }
        }
#endif


    }


#region Implementation

#region Hide, Cover, Purify, Tweak, etc.

#region Cost versions 

// suffix _S represents _$

    void CommitQ_S(int p, int j, long threshold)
    {
        if (!hasColor)
            Cover_S(j, false, threshold); // AlgorithmX$
        else
            Commit_S(p, j, threshold);
    }
    void Commit_S(int p, int j, long threshold)
    {
        if (COLOR(p) == 0) Cover_S(j, true, threshold);
        if (COLOR(p) > 0) Purify_S(p, threshold);
    }
    void UncommitQ_S(int p, int j, long threshold)
    {
        if (!hasColor)
            Uncover_S(j, false, threshold); // AlgorithmX
        else
            Uncommit_S(p, j, threshold);
    }
    void Uncommit_S(int p, int j, long threshold)
    {
        if (COLOR(p) == 0) Uncover_S(j, true, threshold);
        if (COLOR(p) > 0) Unpurify_S(p, threshold);
    }
    void Unpurify_S(int p, long threshold)
    {
        var c = COLOR(p); 
        var i = TOP(p);   
        var q = DLINK(i); 
        while (q != i && COST(q) < threshold)
        {
            if (COLOR(q) < 0)
                COLOR(q, c);
            else
                Unhide_S(q, true, threshold);
            q = DLINK(q);
        }
    }
    void Cover_S(int i, bool primed, long threshold)
    {
        var p = DLINK(i); 
        Stats.Update();
        while (p != i && COST(p) < threshold)
        {
            Hide_S(p, primed);
            p = DLINK(p);
        }

        var l = LLINK(i);
        var r = RLINK(i);
        RLINK(l, r);
        LLINK(r, l);
    }
    void Purify_S(int p, long threshold)
    {
        var c = COLOR(p);
        var i = TOP(p);
        COLOR(i, c);
        var q = DLINK(i);
        while (q != i && COST(q) < threshold)
        {
            if (COLOR(q) == c)
                COLOR(q, -1);
            else
                Hide_S(q, true);
            q = DLINK(q);
        }
    }
    void Uncover_S(int i, bool primed, long threshold)
    {
        var l = LLINK(i);
        var r = RLINK(i);
        RLINK(l, i);
        LLINK(r, i);
        var p = DLINK(i);
        while (p != i && COST(p) < threshold)
        {
            Unhide_S(p, primed, threshold);
            p = DLINK(p);
        }
    }
    void Hide_S(int p, bool primed)
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

    void Unhide_S(int p, bool primed, long threshold)
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
                    DLINK(u, q);
                    ULINK(d, q);
                    LEN(x, LEN(x) + 1);
                }

                q = q + 1;
            }
        }
    }
#endregion


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

    bool VisitSolution(int[] x, int l, long currentCost)
    {
        // solution in x0, x1, ...,x_{l-1}
        List<List<string>>? solutionOptions = null;

        ++Stats.SolutionCount;
        if (hasCost)
        {
            solutionOptions = GetSolution(x, l);
            best.Insert(currentCost, solutionOptions); // todo - pass in solution for tracking and later printing
        }

        var printSoln = Options.OutputFlags.HasFlag(SolverOptions.ShowFlags.AllSolutions);
        var solutionEventHandler = SolutionListener;
        if (solutionEventHandler == null && !printSoln)
            return true; // nothing else to do
        
        solutionOptions ??= GetSolution(x, l);

        if (printSoln)
            formatter.PrintSolution(solutionOptions);

        return solutionEventHandler == null || solutionEventHandler(Stats.SolutionCount, Stats.Updates, solutionOptions);
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
    int AddNameNode(string name)
    {
        var nodes1 = nameNodes;
        nodes1.Add(new(name));
        return NodeCount - 1;
    }

    /// <summary>
    /// Add node, return index
    /// </summary>
    /// <returns></returns>
    int AddNode()
    {
        var nodes1 = nodes;
        nodes1.Add(new());
        return NodeCount - 1;
    }

    void AddNameSpacer(string name)
    {
        AddNameNode(name);
    }


    void AddRegularSpacer(bool link = false)
    {
        var x = AddNode();
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
                if (qq > p && hasCost && COST(pp) < COST(qq))
                    Error($"Costs out of order at node {pp}");
            }

            if (p < N1 && LEN(p) != k)
                Error($"Bad length in item {NAME(p)}");
        }

        return ok;
        void Error(string s)
        {
            ok = false;
            Output.WriteLine(s);
        }
    }

    /// <summary>
    /// Add stored items and options to system
    /// Allows some processing of them as a batch if needed
    /// </summary>
    void PrepItemsAndOptions()
    {
        long TotalCost(Option opt) => opt.Items.Sum(v => (v.Cost>0?v.Cost:0));

        foreach (var (name, secondary, lowerBound, upperBound) in items)
        {
            // track N = # items, N1 = primary items, N2 = # secondary items

            // TODO - rewrite to be cleaner: track list of items, then gen all once all are added
            // as in exercise 8

            // handle secondary items
            if (secondary)
                N2++;
            else
                Trace.Assert(N2 == 0); // all must be primary before first secondary, then all secondary


            var i = AddNameNode(name) + 1;

            AddName(name, i);

            // set node links
            var len = NameNodeCount;
            RLINK(i, 0);
            LLINK(i, (i - 1 + len) % len);

            // attach prev and next
            LLINK(0, i);
            RLINK((i - 1 + len) % len, i);

            // update general nodes
            var x = AddNode();
            LEN(x, 0);
            ULINK(x, x); // point to self
            DLINK(x, x);

            if (N2 > 0)
            {
                // (link one less than second to 0)
                LLINK(0, N1);
                RLINK(N1, 0);

                // (link second to end)
                LLINK(N1 + 1, N);
                RLINK(N, N1 + 1);
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

        var opts2 = options;
        if (hasCost)
            opts2.Sort((a,b)=>TotalCost(a).CompareTo(TotalCost(b))); // sort increasing

        foreach (var option in opts2)
        {
            if (NodeCount == NamesCount)
            {
                // first option after items added
                AddRegularSpacer(true); // initial spacer
            }

            var cost = TotalCost(option); // each node in the option gets set to total option cost

            foreach (var item in option.Items)
            {
                var name = item.ItemName;
                var color = item.Color;

                //string color = ""; // empty for none
                //if (name.Contains(':'))
                //{
                //    hasColor = true;
                //    var w = name.Split(':', StringSplitOptions.RemoveEmptyEntries);
                //    Trace.Assert(w.Length == 2);
                //    color = w[1];
                //    name = w[0];
                //}

                // if (name.Contains('$'))
                // {
                //     //todo.
                //     // lots to do to make it work
                //     throw new NotImplementedException("Option costs not implemented");
                //     //name = ;
                // }

                var i = NameIndex(name);

                var x = AddNode(); // node index
                if (hasCost)
                    COST(x, cost); // set it

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

            AddRegularSpacer(true);
            ++OptionCount;
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
    string NAME(int i) => nameNodes[i].Name;

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

#region COSTS
    bool hasCost = false;

    long COST(int x)
    {
        Stats.Mem();
        return costs[x];
    }

    private readonly List<long> costs = new(); // one per node
    void COST(int x, long val)
    {
        while (costs.Count <= x)
            costs.Add(0);
        Stats.Mem();
        costs[x] = val;
    }
    
    // max heap in array, 0 indexed (Knuth was 1 indexed)
    // holds K items, fixed length K
    public class MaxHeap<T>
    {
        public MaxHeap(int size)
        {

            Size = size;
            Values = new (long,T?)[size];
            Clear();
        }

        public void Clear()
        {
            for (var i = 0; i < Size; ++i)
                Values[i] = (Int64.MaxValue,default);
        }

        public (long value, T? data)[] Values { get; }
        public int Size { get; }

        // largest value
        public long Top => Values[0].value;

        public void Insert(long val, T data)
        {
            // insert into best if suitable
            if (val >= Top) return; // no update
            // val replaces best[0] in array
            Values[0] = (val,data);
            // filter down to ensure heap condition
            // node i has left(i) = 2*i+1, right(i) = 2*i+2
            var i = 0; // tree under i needs heapified
            var j = 1; // left child of i
            while (j < Size) // is there any child to this node?
            {
                // if right child larger, choose that one
                if (j + 1 < Size && Values[j].value < Values[j + 1].value)
                    j++; // right exists and is larger
                // if parent >= both children, done
                if (Values[i].value >= Values[j].value)
                    break; // done
                // else swap values and recurse on subtree
                (Values[i], Values[j]) = (Values[j], Values[i]);
                i = j; // other subtree is unchanged, so is ok. subtree under i=j needs checked
                j = 2 * i + 1; // assume left is larger
            }

            Sanity();
        }

        [Conditional("DEBUG")]
        void Sanity()
        {
            for (var i = 0; i < Size / 2 - 1; ++i)
            {
                Trace.Assert(Values[i].value >= Values[2 * i + 1].value && Values[i].value >= Values[2 * i + 2].value);
            }
        }


        /// <summary>
        /// test heap - dumps to console. throws errors
        /// </summary>
        public static void Test()
        {
            // testing heap
            for (var pass = 0; pass < 1000; ++pass)
            {
                var rr = new Random(pass);
                var h = new MaxHeap<int>(5 + pass/6);
                Console.WriteLine($"New tree size {h.Size}");
                for (var k = 0; k < 2 * (pass + 1); ++k)
                {
                    var v1 = rr.Next(10, 4*h.Size+10);
                    Console.Write($"Insert {v1} : ");
                    h.Insert(v1,0);
                    Dump(h.Values.Select(t=>t.value).ToArray());
                }

                // now pop all out, ensure all are ordered
                Console.Write("ordered: ");
                var v = h.Top;
                for (var k = 0; k < h.Size; ++k)
                {
                    Trace.Assert(h.Top <= v && v >= 0);
                    v = h.Top;
                    Console.Write($"{v} ");
                    h.Insert(-1,0); // pushes out others
                }

                Console.Write(" rest -1 : ");
                // should all be -1
                Dump(h.Values.Select(t => t.value).ToArray());
                void Dump(long[] vals)
                {
                    foreach (var v in vals)
                        Console.Write($"{v} ");
                    Console.WriteLine();
                }
            }
        }
    }

    // best stores solutions and scores
    MaxHeap<List<List<string>>> best = new(5);

#endregion

    record Item(string Name, bool Secondary, int LowerBound, int UpperBound);
    readonly List<Item> items = new();
    // color "" if none, Cost -1 if none
    record Option(List<(string ItemName, string Color, long Cost)> Items);
    readonly List<Option> options = new();

    void ClearData()
    {
        Names.Clear();
        NameMap.Clear();
        nameNodes.Clear();
        nodes.Clear();
        OptionCount = 0;
        spacerIndex = 0;
        lastSpacerIndex = 0;
        N2 = 0; 
        hasColor = false;
        hasMultiplicities = false;
        hasSecondary = false;
        slack.Clear();
        bound.Clear();

        // set best to K items of value max int
        hasCost = false;
        costs.Clear();
        best.Clear();
        items.Clear();
        options.Clear();
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

    // throughout, N = total items, N1 = primary items, N2 = secondary items
    int N => NameNodeCount - 1; // -1 for spacer address
    int N1 => N - N2; // primary item count
    int N2 = 0;       // 


    TextWriter Output => Options.Output;
    int spacerIndex = 0; // decrement to fill in TOP
    int lastSpacerIndex = 0;
    bool hasColor = false;
    bool hasMultiplicities = false;
    bool hasSecondary = false;


    // todo - big rewrite - there is per class overhead, since array is array of classes
    // better to put all into (for C# memory) one array, each cell 4 ints long, update the accessors, hide the node and such arrays

    // name text to node index
    readonly Dictionary<string, int> NameMap = new(); // used to detect duplicates
    readonly List<string> Names = new();

    readonly List<Node> nodes = new();
    readonly List<NameNode> nameNodes = new(); // one empty node, then one per item

    readonly List<string> Colors = new(); // 0 is not used, other index from node datum
    readonly Dictionary<string, int> ColorMap = new(); // color name to index in Colors

    // for multiplicities, every primary item has SLACK and BOUND
    readonly List<int> slack = new();
    readonly List<int> bound = new();

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
    class NameNode
    {
        public NameNode(string name) => Name = name;
        //public void SetA(int val) => a = val; // trying to get to work as a struct - does not work...
        public void SetB(int val) => b = val; // trying to get to work as a struct - does not work...
        public void SetC(int val) => c = val; // trying to get to work as a struct - does not work...
        public void SetD(int val) => datum = val; // trying to get to work as a struct - does not work...
        public string Name { get; }
        public int b = 0, c = 0;
        public int datum = 0; // pads to 128 bits, used as info per node
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
    public int RowCount => OptionCount;

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

    /// <summary>
    /// Set where the output text goes
    /// Can be used like SetOutput(Console.Out);
    /// </summary>
    /// <param name="outputWriter">the text writer where output goes</param>
    /// <param name="newLineEnd">An optional marker to denote line ends in each solution</param>
    /// <param name="newSolutionEnd">An optional marker to denote a solution end</param>
    [Obsolete("SetOutput is deprecated.")]
    public void SetOutput(TextWriter outputWriter, string newLineEnd = "", string newSolutionEnd = "")
    {
        throw new NotImplementedException("Use Options.Output");
    }


    #endregion

}