// Dane wejściowe
string alphabet = "abcdefg";
int numberOfCandidates = 3;
int numberOfVoters = 100000;
int safetyParameter = 20;
int numberOfServers = 5;


int serverId = int.Parse(args[0]);

LocalBallotData localBallotData = new LocalBallotData(serverId, alphabet, numberOfVoters, safetyParameter, numberOfServers, numberOfCandidates);
localBallotData.DataInit().Wait();
localBallotData.DataLinking().Wait();