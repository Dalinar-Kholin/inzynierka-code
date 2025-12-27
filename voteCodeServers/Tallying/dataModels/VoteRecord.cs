using System.Collections.Generic;

public class VoteRecord : IBallotRecord
{
    public int BallotId { get; set; } = 0;
    public List<string> VoteVector { get; set; } = new List<string>();
}
