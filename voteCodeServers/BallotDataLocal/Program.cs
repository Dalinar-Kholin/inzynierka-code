using System;
using System.Text;

// Dane wejściowe - pobrane z bb
string alphabet = "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM1234567890";
int numberOfCandidates = 3;
int numberOfVoters = 100000;
int safetyParameter = 20;
int numberOfServers = 5;

int serverId = int.Parse(args[0]);

// pobranie wartosci losowej z BB (bedzie jedna ustalona gdy każdy serwer zrobi dataInitLocal)
string random = "1248643466348348237845284235235251";

var CodeSetting = new CodeSetting(serverId, alphabet.Length, numberOfCandidates);
CodeSetting.Execute(random).Wait();

Console.WriteLine((50 + 1751693911880985878) % alphabet.Length);