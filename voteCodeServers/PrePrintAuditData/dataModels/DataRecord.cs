using System.Collections.Generic;

public class DataRecord
{
    public int BallotId { get; set; } = 0;
    public List<string[]> Vectors { get; set; } = new List<string[]>();
}
