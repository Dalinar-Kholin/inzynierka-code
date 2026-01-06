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

        BigInteger c1 = new BigInteger("302937938599975307586506391337231455044244253214778137288847949041929453448139042589623072556655181795980970646532450094255299371664951812451560109234983991670554507611969416223873558476121512888309807046913846590986506750369593169173059366314990457830617822960405460716937454502557445272234240218008852173515708");

        partialDecryptions = BuildPartialDecryptions(c1);

        Console.WriteLine($"Odszyfrowane1: {decryptKey.decrypt(partialDecryptions)}");

        BigInteger c2 = new BigInteger("372172294027012142512731726825442383773758488727768367591169426874085123221609477283761703304720964219392984795865780585256188297514206465718982150967499295048898361994763297183045897831979351867395514069804239466548044553764414750764259810611718367769230555067892179304923521693596031052992449273855797692298607");

        partialDecryptions = BuildPartialDecryptions(c2);

        Console.WriteLine($"Odszyfrowane2: {decryptKey.decrypt(partialDecryptions)}");

        BigInteger c3 = new BigInteger("283164486449091898722801098673478776373124453891234648837672179948348477816329140190019195553729387851768706833790150994836739702674103741865549506398773368675886427276470197944851712308768165949129837641806263808814969947379551062883501338552817202690695386789551076862791504569590384397296160918770710651307588");

        partialDecryptions = BuildPartialDecryptions(c3);

        Console.WriteLine($"Odszyfrowane3: {decryptKey.decrypt(partialDecryptions)}");

        BigInteger c4 = new BigInteger("243025234673901210740876293732726992292986818944238856396498659987947416493929424968604114608351383436130904498747752205197958778750696380053981912164399071818885619338091604534232788794903612230011306735484351926513629893302855757248970434798857618196532694598252666141120304647160497149023813358672596803403690");

        partialDecryptions = BuildPartialDecryptions(c4);

        Console.WriteLine($"Odszyfrowane4: {decryptKey.decrypt(partialDecryptions)}");

        BigInteger c5 = new BigInteger("444918154645120356607719314932232743489163315686190178971740042854232640000921367201964583962946806767124332299545675296533427355471571446004982487421755526188272992813710553024569157370712114992778568412682299373380223380728012519191762689601183586842440648399565419203362732782207649036567331868289572081628135");
        partialDecryptions = BuildPartialDecryptions(c5);

        var decrypted5 = decryptKey.decrypt(partialDecryptions);
        var decrypted = Decode(new BigInteger(decrypted5.ToString()));

        Console.WriteLine($"Odszyfrowane5: {decrypted}");
        // dEHm8jBYiR TTyTT ddd4d offfo yyyyy MMbMM eddee IIIDD r1r11 mZZmZ ffXfX
        // TdoyMeIrmf

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
