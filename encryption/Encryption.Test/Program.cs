using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using Org.BouncyCastle.Math;
using VoteCodeServers.Helpers;


class Program
{
    static void Main(string[] args)
    {
        string _alphabet = "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM1234567890";
        int _baseValue = _alphabet.Length;

        var publicKey = new PaillierPublicKey();
        BigInteger n_squared = publicKey.n_squared;

        var sharedKeys = new List<PaillierSharedKey>
        {
            new PaillierSharedKey(serverNumber: 0),
            new PaillierSharedKey(serverNumber: 1),
            new PaillierSharedKey(serverNumber: 2),
            new PaillierSharedKey(serverNumber: 3),
            new PaillierSharedKey(serverNumber: 4),
            new PaillierSharedKey(serverNumber: 5),
            new PaillierSharedKey(serverNumber: 6),
            new PaillierSharedKey(serverNumber: 7),
            new PaillierSharedKey(serverNumber: 8),
            new PaillierSharedKey(serverNumber: 9)
        };

        int thresholdPlayers = sharedKeys.First().degree + 1;

        var missingPlayers = Enumerable.Range(1, thresholdPlayers)
            .Where(id => !sharedKeys.Any(k => k.player_id == id))
            .ToList();

        if (missingPlayers.Any())
        {
            throw new InvalidOperationException($"Missing shares for players: {string.Join(",", missingPlayers)}");
        }

        PaillierSharedKey decryptKey = sharedKeys.First(k => k.player_id == 1);

        Dictionary<int, BigInteger> BuildPartialDecryptions(BigInteger ciphertext)
        {
            var partialDecryptions = new Dictionary<int, BigInteger>();
            foreach (var key in sharedKeys)
            {
                if (key.player_id <= thresholdPlayers)
                {
                    partialDecryptions[key.player_id] = key.partial_decrypt(ciphertext);
                }
            }
            return partialDecryptions;
        }

        // Użyj konstruktora BouncyCastle zamiast Parse
        BigInteger code1 = new BigInteger("1");
        BigInteger code2 = new BigInteger("1");
        BigInteger code3 = new BigInteger("1");
        BigInteger code4 = new BigInteger("1");

        var swTotal = System.Diagnostics.Stopwatch.StartNew();
        var sw = System.Diagnostics.Stopwatch.StartNew();

        BigInteger ciphertext1 = publicKey.Encrypt(code1);
        BigInteger ciphertext2 = publicKey.Encrypt(code2);
        BigInteger ciphertext3 = publicKey.Encrypt(code3);
        BigInteger ciphertext4 = publicKey.Encrypt(code4);

        sw.Stop();
        long time = sw.ElapsedMilliseconds;
        Console.WriteLine($"czas = {time}\n");

        // HOMOMORFICZNE DODAWANIE: mnożenie szyfrogramów
        ciphertext1 = ciphertext1.ModPow(BigInteger.One, n_squared);
        var partialDecryptions1 = BuildPartialDecryptions(ciphertext1);
        ciphertext2 = ciphertext2.ModPow(BigInteger.One, n_squared);
        var partialDecryptions2 = BuildPartialDecryptions(ciphertext2);
        ciphertext3 = ciphertext3.ModPow(BigInteger.One, n_squared);
        var partialDecryptions3 = BuildPartialDecryptions(ciphertext3);
        ciphertext4 = publicKey.Encrypt(BigInteger.Zero);
        var partialDecryptions4 = BuildPartialDecryptions(ciphertext4);

        Console.WriteLine($"c1: {ciphertext1}");
        Console.WriteLine($"Odszyfrowane c1: {decryptKey.decrypt(partialDecryptions1)}");
        Console.WriteLine($"c2: {ciphertext2}");
        Console.WriteLine($"Odszyfrowane c2: {decryptKey.decrypt(partialDecryptions2)}");
        Console.WriteLine($"c3: {ciphertext3}");
        Console.WriteLine($"Odszyfrowane c3: {decryptKey.decrypt(partialDecryptions3)}");
        Console.WriteLine($"c4: {ciphertext4}");
        Console.WriteLine($"Odszyfrowane c4: {decryptKey.decrypt(partialDecryptions4)}");

        BigInteger ciphertext_sum = ciphertext1.Multiply(ciphertext2).Mod(n_squared)
            .Multiply(ciphertext3).Mod(n_squared)
            .Multiply(ciphertext4).Mod(n_squared);

        // Oczekiwana suma - używaj metod BouncyCastle
        BigInteger expectedSum = code1.Add(code2).Add(code3).Add(code4);
        Console.WriteLine($"Oczekiwana suma: {expectedSum}");
        Console.WriteLine($"Ciphertext sum: {ciphertext_sum}");

        var partialDecryptions = BuildPartialDecryptions(ciphertext_sum);

        Console.WriteLine($"Odszyfrowane przed re-encryption: {decryptKey.decrypt(partialDecryptions)}");

        Console.WriteLine($"c1: {ciphertext_sum}");

        ciphertext_sum = publicKey.ReEncrypt(ciphertext_sum);

        Console.WriteLine($"c2: {ciphertext_sum}");



        partialDecryptions = BuildPartialDecryptions(ciphertext_sum);

        Console.WriteLine("====================");

        // Om6tX0ZJir 0000c ROROO 6x6xx 20022 iPiPi vvvav QBBBQ ZZ22Z HH444 pOpOO
        // 0R62ivQZHp

        // Zamienić potem żeby decrypt był statyczny
        Console.WriteLine($"Odszyfrowane: {decryptKey.decrypt(partialDecryptions)}");

        BigInteger c1 = new BigInteger("440075574264028484998834063708480863771918521428078426954966538795772539586469840180017462517143828099652281098232090225483136048537317995658114748527663531852406872436482192485313764406497363242065067147090785843731356921197316566264927542475509744292475274254803952862700490527197837216594189541345519053182320");

        partialDecryptions = BuildPartialDecryptions(c1);

        Console.WriteLine($"Odszyfrowane1: {decryptKey.decrypt(partialDecryptions)}");

        BigInteger c2 = new BigInteger("151222323663197096567572726776225969752656604321533843952494212002802725388554814925476690288426859989021391607254949125008681791893437084618742549516959799498221802549415206721507489745114992816939243687741835664667742988230057134879038765065081092473365005978645594833389114572525144856742467229010031981920134");

        partialDecryptions = BuildPartialDecryptions(c2);

        Console.WriteLine($"Odszyfrowane2: {decryptKey.decrypt(partialDecryptions)}");

        BigInteger c3 = new BigInteger("462604156213138511416411829204751403421925606795452489810243966312678409761961299316950917751154915520365278655340048962053337381129618987212904035256912535515400760684364967538252473269394880509309118177494987783378262696831896639440458784194395383330445252452962901232058438294249414116169710729440385397129099");

        partialDecryptions = BuildPartialDecryptions(c3);

        Console.WriteLine($"Odszyfrowane3: {decryptKey.decrypt(partialDecryptions)}");

        BigInteger c4 = new BigInteger("440677361578013499652263874671936368698552529407776131370396875003238414010105113909854934366174575862112011052910644484885573002645339780948829971193067267342156043432414922612002592098252917042305322445479543178847580275216792470063437840910921376876673587892102337547405081414154426889100012626149325689937036");

        partialDecryptions = BuildPartialDecryptions(c4);

        Console.WriteLine($"Odszyfrowane4: {decryptKey.decrypt(partialDecryptions)}");

        BigInteger c5 = new BigInteger("347710853462785125735189641340304479067088364798453427178468081685212465390728576093006956208193391150908429402552903768604545284829906931890848773873462554611291217112528534951040174790064869873596077080539605224298198214142355927366551546469490076785300944789726932387827435842675223386041397210369566521386229");
        partialDecryptions = BuildPartialDecryptions(c5);

        var decrypted5 = decryptKey.decrypt(partialDecryptions);
        var decrypted = Decode(new BigInteger(decrypted5.ToString()));

        Console.WriteLine($"Odszyfrowane5: {decrypted}");

        string Decode(BigInteger encoded)
        {
            if (encoded.Equals(BigInteger.Zero))
            {
                var a0 = _alphabet[0];
                return a0.ToString();
            }

            int baseValue = _baseValue;
            var baseValueBig = new BigInteger(baseValue.ToString());
            var result = new StringBuilder();

            while (encoded.CompareTo(BigInteger.Zero) > 0)
            {
                BigInteger remainder = encoded.Mod(baseValueBig);
                int index = int.Parse(remainder.ToString());
                result.Insert(0, _alphabet[index]);
                encoded = encoded.Divide(baseValueBig);
            }

            return result.ToString();
        }

        // using var sha256 = SHA256.Create();

        // var input = Encoding.UTF8.GetBytes($"aa");
        // byte[] hash = sha256.ComputeHash(input);
        // string hashedValue = Convert.ToHexString(hash).ToLower();

        // var c_hash = publicKey.EncryptHash(hashedValue);

        // Console.WriteLine($"hash: {hashedValue}");
        // Console.WriteLine($"c_hash: {c_hash}");

        // partialDecryptions[1] = sharedKey1.partial_decrypt(c_hash);
        // partialDecryptions[2] = sharedKey2.partial_decrypt(c_hash);
        // partialDecryptions[3] = sharedKey3.partial_decrypt(c_hash);
        // partialDecryptions[4] = sharedKey4.partial_decrypt(c_hash);
        // partialDecryptions[5] = sharedKey5.partial_decrypt(c_hash);

        // var decrypted = sharedKey1.decrypt(partialDecryptions);

        // Console.WriteLine($"Odszyfrowany hash: {publicKey.BigIntegerToHash(decrypted)}");



        // Console.WriteLine("=== ELGAMAL - MULTI SERVER TEST ===\n");

        // // Ścieżka do foldera z kluczami
        // string keyFolder = "../elGamalKeys";

        // // 1. Wygeneruj klucze dla 3 serwerów (tylko dla testu)
        // Console.WriteLine("1. Generowanie kluczy dla 10 serwerów...");
        // ElGamalEncryption.GenerateAndSaveServerKeys(numberOfServers: 10, folderPath: keyFolder, strength: 512);
        // Console.WriteLine();

        // // 2. Każdy serwer załaduje swoje klucze
        // Console.WriteLine("2. Ładowanie kluczy przez każdy serwer...");
        // var server1 = new ElGamalEncryption(serverId: 1, folderPath: keyFolder);
        // var server2 = new ElGamalEncryption(serverId: 2, folderPath: keyFolder);
        // var server3 = new ElGamalEncryption(serverId: 3, folderPath: keyFolder);
        // Console.WriteLine();

        // // 3. Serwer 1 szyfruje wiadomość dla serwera 2
        // Console.WriteLine("3. Serwer 1 szyfruje wiadomość dla serwera 2...");
        // long testMessage = 42;
        // var (c1_s1_to_s2, c2_s1_to_s2) = server1.Encrypt(testMessage, targetServerId: 2);
        // Console.WriteLine($"   Wiadomość: {testMessage}");
        // Console.WriteLine($"   Ciphertext c1: {c1_s1_to_s2}");
        // Console.WriteLine($"   Ciphertext c2: {c2_s1_to_s2}");
        // Console.WriteLine();

        // // 4. Serwer 2 re-randomizuje i deszyfruje wiadomość
        // Console.WriteLine("4. Serwer 2 re-randomizuje i deszyfruje wiadomość...");
        // var (c1_s1_to_s2_r, c2_s1_to_s2_r) = server2.ReEncrypt((c1_s1_to_s2, c2_s1_to_s2));
        // var decrypted_s2 = server2.Decrypt((c1_s1_to_s2_r, c2_s1_to_s2_r));
        // Console.WriteLine($"   Odszyfrowano: {decrypted_s2}");
        // Console.WriteLine($"   Poprawnie: {decrypted_s2.Equals(new BigInteger(testMessage.ToString()))}\n");

        // // 5. Testuj operacje homomorficzne między serwerami
        // Console.WriteLine("5. Operacje homomorficzne (Serwer 1)...");
        // long msg1 = 10;
        // long msg2 = 5;
        // var (c1_msg1, c2_msg1) = server1.Encrypt(msg1, targetServerId: 3);
        // var (c1_msg2, c2_msg2) = server1.Encrypt(msg2, targetServerId: 3);

        // // Mnożenie szyfrogramów = mnożenie tekstu jawnego
        // var (c1_mult, c2_mult) = server1.Multiply((c1_msg1, c2_msg1), (c1_msg2, c2_msg2));
        // var (c1_mult_r, c2_mult_r) = server3.ReEncrypt((c1_mult, c2_mult));
        // var decrypted_mult = server3.Decrypt((c1_mult_r, c2_mult_r));
        // Console.WriteLine($"   {msg1} * {msg2} = {decrypted_mult} (oczekiwane: {msg1 * msg2})");

        // // Dzielenie szyfrogramów = dzielenie tekstu jawnego
        // var (c1_div, c2_div) = server1.Divide((c1_msg1, c2_msg1), (c1_msg2, c2_msg2));
        // var (c1_div_r, c2_div_r) = server3.ReEncrypt((c1_div, c2_div));
        // var decrypted_div = server3.Decrypt((c1_div_r, c2_div_r));
        // Console.WriteLine($"   {msg1} / {msg2} = {decrypted_div} (oczekiwane: {msg1 / msg2})\n");

        // // 6. Serwer 3 szyfruje dla serwera 1
        // Console.WriteLine("6. Serwer 3 szyfruje wiadomość dla serwera 1...");
        // long testMessage3 = 99;
        // var (c1_s3_to_s1, c2_s3_to_s1) = server3.Encrypt(testMessage3, targetServerId: 1);
        // var (c1_s3_to_s1_r, c2_s3_to_s1_r) = server1.ReEncrypt((c1_s3_to_s1, c2_s3_to_s1));
        // var decrypted_s1 = server1.Decrypt((c1_s3_to_s1_r, c2_s3_to_s1_r));
        // Console.WriteLine($"   Wiadomość: {testMessage3}");
        // Console.WriteLine($"   Odszyfrowano przez Serwer 1: {decrypted_s1}");
        // Console.WriteLine($"   Poprawnie: {decrypted_s1.Equals(new BigInteger(testMessage3.ToString()))}\n");

        // Console.WriteLine("=== TESTY ZAKONCZONE ===");

        // Wydajność ===============================
        // var publicKey = new PaillierPublicKey();
        // var publicKeyFast = new FastPaillier();

        // for (int i = 0; i < 100; i++)
        // {
        //     publicKey.Encrypt(new BigInteger("12345678901234567890"));
        // }

        // Console.WriteLine("\n\nSzybki======================================================================================");
        // for (int i = 0; i < 100; i++)
        // {
        //     var message = publicKeyFast.RandomBigInteger(1, 12345678901234567890);
        //     publicKeyFast.Encrypt(message);
        // }


        // var swTotal = System.Diagnostics.Stopwatch.StartNew();
        // var sw = System.Diagnostics.Stopwatch.StartNew();


        // publicKeyFast.Test(100);

        // sw.Stop();
        // long time = sw.ElapsedMilliseconds;
        // Console.WriteLine($"\n\n{time}");

        // sw.Restart();

        // publicKey.Test(100);

        // sw.Stop();
        // time = sw.ElapsedMilliseconds;
        // Console.WriteLine($"\n\n{time}");

        ///////////////////////////////////////

        // var publicKey = new PaillierPublicKey();


        // var swTotal = System.Diagnostics.Stopwatch.StartNew();
        // var sw = System.Diagnostics.Stopwatch.StartNew();

        // Parallel.For(0, 30000, i =>
        // {
        //     var encrypted = publicKey.Encrypt(new BigInteger("12345678901234567890"));
        // });

        // sw.Stop();
        // long time1 = sw.ElapsedMilliseconds;
        // Console.WriteLine($"\n\n{time1}");

        // sw.Restart();

        // for (int i = 0; i < 30000; i++)
        // {
        //     var encrypted = publicKey.Encrypt(new BigInteger("12345678901234567890"));
        // }

        // sw.Stop();
        // long time2 = sw.ElapsedMilliseconds;
        // Console.WriteLine($"\n\n{time1}");
        // Console.WriteLine($"\n\n{time2}");
    }
}
