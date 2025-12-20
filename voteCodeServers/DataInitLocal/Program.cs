using VoteCodeServers.Helpers;
// Dane wejściowe - z konfiguracji współdzielonej
var cfg = Config.Load();
string alphabet = cfg.Alphabet;
int numberOfCandidates = cfg.NumberOfCandidates;
int numberOfVoters = cfg.NumberOfVoters;
int safetyParameter = cfg.SafetyParameter;
int numberOfServers = cfg.NumberOfServers;


int serverId = int.Parse(args[0]);

var localBallotData = new LocalBallotData(serverId, alphabet, numberOfVoters, safetyParameter, numberOfServers, numberOfCandidates);
localBallotData.DataInit().Wait();
localBallotData.DataLinking().Wait();
