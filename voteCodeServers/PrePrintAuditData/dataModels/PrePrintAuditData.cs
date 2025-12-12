using System.Collections.Generic;

public class PrePrintAuditData
{
    public string BallotVoteSerial { get; set; } = string.Empty;
    public List<string[]> Vectors { get; set; } = new List<string[]>();
}
