using System.Collections.Generic;

public class VoteCodeRecord : IBallotRecord
{
    public int BallotId { get; set; } = 0;
    public List<string> EncryptedVoteCodeC1 { get; set; } = new List<string>();
    public List<string> EncryptedVoteCodeC2 { get; set; } = new List<string>();
    public List<string> VoteVector { get; set; } = new List<string>();
}
