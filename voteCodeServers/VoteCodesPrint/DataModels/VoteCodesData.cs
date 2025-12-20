using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlTypes;
using System.Diagnostics;

public class VoteCodesData
{
    public string EncryptedVoteCodes { get; set; } = string.Empty;
    public bool IsUsed { get; set; } = false;
}
