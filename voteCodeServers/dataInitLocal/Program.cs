// Dane wejściowe
string alphabet = "abcdefg";
int numberOfCandidates = 3;
int numberOfVoters = 1;
int safetyParameter = 1;
int numberOfServers = 3;


int serverId = int.Parse(args[0]);

DataInit dataInit = new DataInit(serverId, alphabet, numberOfVoters, safetyParameter, numberOfServers, numberOfCandidates);
dataInit.PartI().Wait();
dataInit.PartII().Wait();
dataInit.DataLinking().Wait();