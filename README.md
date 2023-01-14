# Dancing Links

This is an MIT licensed implementation of many variants of the [Dancing Links Algorithm](https://en.wikipedia.org/wiki/Dancing_Links), as presented in Knuth's [The Art of Computer Programming (TAOCP), Volume 4B, "Combinatorial Algorithms, Part 2"](https://www.amazon.com/Art-Computer-Programming-Combinatorial-Information/dp/0201038064/) (copyright 2023, given to me by my fiancée for Xmas 2022).

Dancing Links is a very general, very efficient backtracking algorithm useful for counting number of solutions to various problems. 

When I read the section from Knuth's book and saw the vast generalizations he described in the text and exercises, and given how much I've used original DLX, I spent Xmas break implementing all the variants to replace my older libraries.

This version implements the standard DLX, adds secondary items, and adds the newer versions Knuth covers including support for colors, general multiplicities, and costs.

I originally implemented a version over 20 years ago based on [Knuth's paper](https://arxiv.org/abs/cs/0011047), and have used it many times over the years to solve all sorts of problems from making puzzles to finding solutions for algorithm design questions to playing games.

## Code

The only file you need to add all these variants to your code is the single file [`DancingLinksSolver.cs`](https://github.com/ChrisLomont/DancingLinks/blob/master/DancingLinks/DancingLinksSolver.cs). The rest of this project includes a Visual Studio solution with dozens of examples from literature and books being solved using the library.

## Usage

See the top of the dancing links file above, or the examples.

## History

Jan14, 2023 - v 0.5 - Release with all versions working released

Jan 2, 2023 - Initial check in



## TODO

For now, these are peppered throughout the code. I plan to organize better over time.

