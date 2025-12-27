using System;
using System.Text;

// Dane wejściowe - pobrane z bb
var cfg = VoteCodeServers.Helpers.Config.Load();
string alphabet = cfg.Alphabet;
int numberOfCandidates = cfg.NumberOfCandidates;
int numberOfVoters = cfg.NumberOfVoters;
int safetyParameter = cfg.SafetyParameter;
int numberOfServers = cfg.NumberOfServers;

int serverId = int.Parse(args[0]);

// pobranie wartosci losowej z BB (bedzie jedna ustalona gdy każdy serwer zrobi dataInitLocal)
string random = "1248643466348348237845284235235251";

var CodeSetting = new CodeSetting(serverId, numberOfServers, alphabet.Length, numberOfCandidates);
CodeSetting.Execute(random).Wait();

Console.WriteLine((50 + 1751693911880985878) % alphabet.Length);