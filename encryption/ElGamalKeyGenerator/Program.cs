using System;

class Program
{
    static void Main(string[] args)
    {
        int numberOfServers = 10; // Możesz zmienić liczbę serwerów
        string folderPath = "../elGamalKeys"; // Domyślna ścieżka
        int strength = 512; // Domyślna siła klucza

        if (args.Length > 0)
            int.TryParse(args[0], out numberOfServers);
        if (args.Length > 1)
            folderPath = args[1];
        if (args.Length > 2)
            int.TryParse(args[2], out strength);

        ElGamalEncryption.GenerateAndSaveServerKeys(numberOfServers, folderPath, strength);
        Console.WriteLine($"Wygenerowano klucze ElGamal dla {numberOfServers} serwerów w folderze {folderPath}.");
    }
}
