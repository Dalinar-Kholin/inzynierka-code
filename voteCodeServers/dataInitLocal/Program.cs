// Dane wejściowe - pobrane z bb (opublikowane przez EA)
string alphabet = "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM1234567890";
int numberOfCandidates = 3;
int numberOfVoters = 100000;
int safetyParameter = 20;
int numberOfServers = 5;


int serverId = int.Parse(args[0]);

var localBallotData = new LocalBallotData(serverId, alphabet, numberOfVoters, safetyParameter, numberOfServers, numberOfCandidates);
localBallotData.DataInit().Wait();
localBallotData.DataLinking().Wait();
