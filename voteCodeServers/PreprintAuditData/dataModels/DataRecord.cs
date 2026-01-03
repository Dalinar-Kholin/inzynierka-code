using System.Collections.Generic;

public class DataRecord : IBallotRecord
{
    public int BallotId { get; set; } = 0;
    public List<string[]> Vectors { get; set; } = new List<string[]>();
}
