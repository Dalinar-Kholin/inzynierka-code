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

        // Zamienić potem żeby decrypt był statyczny
        Console.WriteLine($"Odszyfrowane: {decryptKey.decrypt(partialDecryptions)}");

        BigInteger c1 = new BigInteger("88265803092502272316242682631820277998280193432857734804739992979536648668121716894095065051005043296850665133724313635534482165382931777731658752185049110567644859719790816005546529135461674896444372562929032262638114798140409008062766786755300118313494867361457458434935066704471722863400905675831615013122583");

        partialDecryptions = BuildPartialDecryptions(c1);

        Console.WriteLine($"Odszyfrowane1: {decryptKey.decrypt(partialDecryptions)}");

        BigInteger c2 = new BigInteger("269820656837430012966876552738441354728057807232462144974085782319099165931942174778381542887842066851651660875584221112992253953780944924148284084382874466935778003627786505769735567725747129860282431227094021159043213346723261199728713660043130720578765421406271680102516128856069392337409052496494510402273461");

        partialDecryptions = BuildPartialDecryptions(c2);

        Console.WriteLine($"Odszyfrowane2: {decryptKey.decrypt(partialDecryptions)}");

        BigInteger c3 = new BigInteger("357556615350698804250629668294293373095592029577077308021633757280814223478419623314327878831071532669556061783005730720848074587041771750393537440943372978553935021191174062589954607376322789271285428001102265770100976244161763134305639768985936197741096750694462876789624678108908535220085812440005618618918361");

        partialDecryptions = BuildPartialDecryptions(c3);

        Console.WriteLine($"Odszyfrowane3: {decryptKey.decrypt(partialDecryptions)}");

        BigInteger c4 = new BigInteger("219687982057007707488705358705331841057491745949487440876171418758984968509093500613837391496471293437057958238023279389276621486856096575470172602839291590102482995206556763676360872116318475164601870960178702390567306229090958820379396732294844072912591696934923459240622917469824249272911871974223206876872777");

        partialDecryptions = BuildPartialDecryptions(c4);

        Console.WriteLine($"Odszyfrowane4: {decryptKey.decrypt(partialDecryptions)}");

        BigInteger c5 = new BigInteger("141437058141936194855669637738337457638334355984463969486865994284586679280491469589428409378992652318362987452733338226090467915690747703632405186827906320268385097332945395108528945586497824512120466678701542497458383366520275463636739544983276533715939614081149880806587987860677291294853448125597840290428017");
        //JuzDclEsZwSSxSSXPXPPAMMMAYUYYYhiiihyyyEEwkwwkYJYYJ11K1KTlllT
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
