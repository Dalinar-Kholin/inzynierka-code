using System;
using System.Text;

// Dane wejściowe - pobrane z bb
string alphabet = "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM1234567890";
int numberOfCandidates = 5;
int numberOfVoters = 10000;
int safetyParameter = 200;
int numberOfServers = 10;

int serverId = int.Parse(args[0]);

// pobranie wartosci losowej z BB (bedzie jedna ustalona gdy każdy serwer zrobi dataInitLocal)
string random = "1248643466348348237845284235235251";

var CodeSetting = new CodeSetting(serverId, alphabet.Length, numberOfCandidates);
CodeSetting.Execute(random).Wait();

Console.WriteLine((50 + 1751693911880985878) % alphabet.Length);