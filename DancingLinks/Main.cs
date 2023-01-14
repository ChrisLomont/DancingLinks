// Chris Lomont 2023
// various problems solved from Knuth books and papers to
// illustrate use of Dancing Links variants

using Lomont.Algorithms;
using Lomont.Algorithms.Examples;

// testing to make insert works well
//DancingLinksSolver.MaxHeap.Test();

Console.WriteLine("Dancing links testing and experiments");

// call this to do some regression testing
// throws if a test fails
//Utils.RegressionTesting();

// reuse options - allows nicer control of settings from here for running experiments
var opts = new DancingLinksSolver.SolverOptions();

// useful options
opts.Output = Console.Out;
// opts.MinimumRemainingValuesHeuristic = false;
//opts.OutputFlags = DancingLinksSolver.SolverOptions.ShowFlags.All;
// opts.OutputFlags = DancingLinksSolver.SolverOptions.ShowFlags.None;
//opts.OutputFlags |= DancingLinksSolver.SolverOptions.ShowFlags.AllSolutions;
//opts.MemsDumpStepSize = 1;
//opts.MemsDumpStepSize = 100_000;
//opts.MemsDumpStepSize = 1_000_000;
//opts.MemsDumpStepSize = 10_000_000;
opts.MemsDumpStepSize = 100_000_000;
//opts.MemsDumpStepSize = 1_000_000_000;

// some examples. See each major class for more problems

// how many ways to place 8 queens on 8x8 chess board? 92 solutions
ChessPuzzles.NQueens(opts, 8, mrv: true, topK: 0); 

// some packing problems
 GeometricPuzzles.ScottsPentominoProblem(opts); // 520 solutions
// GeometricPuzzles.PolyominoesRectangle(opts, 15, 4); // 1472 solutions with reflections
// GeometricPuzzles.Dudeney(opts); // slow!
// GeometricPuzzles.PackYSquare(opts); // slow!

// Some number problems
// 3x3 square of primes with maximal product
NumberPuzzles.PrimeSquares(opts, 1, 3, 3); // answer: 997 787 733
// 3x3 square of primes with minimal product
// NumberPuzzles.PrimeSquares(opts, -1, 3, 3); // answer: 113 307 139
NumberPuzzles.LangfordPairs(opts, 7, false); // 52 answers



// inside each of these are many examples, most commented out
// since running them all would take very, very long
// ChessPuzzles.RunAll(opts);
// GeometricPuzzles.RunAll(opts);
// MiscPuzzles.RunAll(opts);
// NumberPuzzles.RunAll(opts);
// WordPuzzles.RunAll(opts);




