using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using Org.BouncyCastle.Math;


class Program
{
    static void Main(string[] args)
    {
        var publicKey = new PaillierPublicKey();
        BigInteger n = publicKey.n;
        BigInteger n_squared = publicKey.n_squared;
        BigInteger g = publicKey.g;

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
        BigInteger code3 = new BigInteger("5000");
        BigInteger code4 = new BigInteger("5000");

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
        var ciphertext_sum = ciphertext1.Multiply(ciphertext2).Mod(n_squared);
        ciphertext_sum = ciphertext_sum.Multiply(ciphertext3).Mod(n_squared);
        ciphertext_sum = ciphertext_sum.Multiply(ciphertext4).Mod(n_squared);

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

        // Zamienić potem żeby decrypt był statyczny
        Console.WriteLine($"Odszyfrowane: {decryptKey.decrypt(partialDecryptions)}");

        BigInteger c1 = new BigInteger("83435933249985506345500674520811059125984474585755584773098067855507543347292442432298688074746569949061930610902868389861944930811867881755169452782814982443171355937259929968264550518390853577441779063858714308576216718754554248213927915155477307998871836117560540416086548741243569013195875571932375864014814");

        partialDecryptions = BuildPartialDecryptions(c1);

        Console.WriteLine($"Odszyfrowane1: {decryptKey.decrypt(partialDecryptions)}");

        BigInteger c2 = new BigInteger("28626266630509913033785133647230300375667465632676996207216264506191321939617859225919236365860155916665557228175620539891790764577008374081648752122754378826055004720066442024985502908841845152285069452359187064256383183096587230841791150450061059520810231330444778817614471069171456750850778411684202153885714");

        partialDecryptions = BuildPartialDecryptions(c2);

        Console.WriteLine($"Odszyfrowane2: {decryptKey.decrypt(partialDecryptions)}");

        BigInteger c3 = new BigInteger("371730684925409585677465291051102633510281150075831682155337092794417767109778755872145724336340820104043999340151616396884614409816464275120397868973083378913041187818230515281194107620505266067915034209965102475951152352349030223920313792729009990053392965651617816139487820227715665910271041903812808016494100");

        partialDecryptions = BuildPartialDecryptions(c3);

        Console.WriteLine($"Odszyfrowane3: {decryptKey.decrypt(partialDecryptions)}");

        BigInteger c4 = new BigInteger("132676490967287925142016092273703633929874283132813559597571414601067414969423415260699171344652616308861828578674773325729220178899175587721803124720722545494727004491416335134578665712853808074308736532292485340193895330458952514295200939643538825362507121932964783083448460191259702338130419377839562211371957");

        partialDecryptions = BuildPartialDecryptions(c4);

        Console.WriteLine($"Odszyfrowane4: {decryptKey.decrypt(partialDecryptions)}");

        BigInteger c5 = new BigInteger("232417989064014910256072822339409270407342028623025414100672402371587152305797668748373568570947187951581453727909522459554254674112002036998398212228596460460181048620783172845761375499865831232925254248330144444622360608540297184261818148405916888025901031076494002617509459905274405496837931573004284336761584");

        partialDecryptions = BuildPartialDecryptions(c5);

        Console.WriteLine($"Odszyfrowane5: {decryptKey.decrypt(partialDecryptions)}");

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


        // Console.WriteLine("=== ELGAMAL ===");
        // // ElGamalEncryption.GenerateKeyPair();

        // var elGamal = new ElGamalEncryption();

        // long number1 = 42;
        // long number2 = 42;

        // Console.WriteLine($"\nSzyfrowanie liczby: {number1}");
        // var (c1, c2) = elGamal.Encrypt(number1);
        // Console.WriteLine($"Ciphertext c1: {c1}");
        // Console.WriteLine($"Ciphertext c2: {c2}");

        // var decrypted = elGamal.Decrypt(c1, c2);
        // Console.WriteLine($"Odszyfrowane: {decrypted}");

        // var (c1_2, c2_2) = elGamal.Encrypt(number2);
        // var (c1_mult, c2_mult) = elGamal.Multiply((c1, c2), (c1_2, c2_2));
        // var (c1_div, c2_div) = elGamal.Divide((c1, c2), (c1_2, c2_2));

        // var decryptedMult = elGamal.Decrypt(c1_mult, c2_mult);
        // var decryptedDiv = elGamal.Decrypt(c1_div, c2_div);
        // Console.WriteLine($"Mnozenie: {decryptedMult} (oczekiwane: {number1 * number2})");
        // Console.WriteLine($"Dzielenie: {decryptedDiv} (oczekiwane: {number1 / number2})");

        // Console.WriteLine(elGamal.Decrypt(new BigInteger("4640514876916297044899328957381608956615077629818535728280616252427695042395251932258482539303457149034574175648635808347257079857077099943644720367578912"), new BigInteger("7209860587698876778523417001664661979838947644730032041253113599053586259248296048815083090086518962928739526857698563322959706294177326066988110176318443")));
        // // 8

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
