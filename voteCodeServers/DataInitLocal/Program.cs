// Dane wejściowe - pobrane z bb (opublikowane przez EA)
string alphabet = "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM1234567890";
int numberOfCandidates = 5;
int numberOfVoters = 10000;
int safetyParameter = 200;
int numberOfServers = 10;


int serverId = int.Parse(args[0]);

var localBallotData = new LocalBallotData(serverId, alphabet, numberOfVoters, safetyParameter, numberOfServers, numberOfCandidates);
localBallotData.DataInit().Wait();
localBallotData.DataLinking().Wait();
