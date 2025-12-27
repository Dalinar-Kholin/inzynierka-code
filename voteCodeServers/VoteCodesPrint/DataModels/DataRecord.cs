using System.Collections.Generic;

public class DataRecord : IBallotRecord
{
    public int BallotId { get; set; } = 0;
    public string EncryptedVoteCodes { get; set; } = null;
}
