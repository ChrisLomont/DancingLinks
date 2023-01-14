namespace Lomont.Algorithms.Utility;

public class PuzzleAttribute : Attribute
{
    public PuzzleAttribute(string description)
    {
        Description= description;

    }
    public string Description { get;  }
}