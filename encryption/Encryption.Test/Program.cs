using System;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using Org.BouncyCastle.Math;


class Program
{
    static void Main(string[] args)
    {
        // var publicKey = new PaillierPublicKey();
        // BigInteger n = publicKey.n;
        // BigInteger n_squared = publicKey.n_squared;
        // BigInteger g = publicKey.g;

        // var sharedKey1 = new PaillierSharedKey(serverNumber: 0);
        // var sharedKey2 = new PaillierSharedKey(serverNumber: 1);
        // var sharedKey3 = new PaillierSharedKey(serverNumber: 2);
        // var sharedKey4 = new PaillierSharedKey(serverNumber: 3);
        // var sharedKey5 = new PaillierSharedKey(serverNumber: 4);

        // // Użyj konstruktora BouncyCastle zamiast Parse
        // BigInteger code1 = new BigInteger("1");
        // BigInteger code2 = new BigInteger("1");
        // BigInteger code3 = new BigInteger("1");
        // BigInteger code4 = new BigInteger("5");

        // BigInteger ciphertext1 = publicKey.Encrypt(code1);
        // BigInteger ciphertext2 = publicKey.Encrypt(code2);
        // BigInteger ciphertext3 = publicKey.Encrypt(code3);
        // BigInteger ciphertext4 = publicKey.Encrypt(code4);

        // // HOMOMORFICZNE DODAWANIE
        // // (c1 * c2) mod n_squared
        // // BigInteger ciphertext_sum = ciphertext1.Multiply(ciphertext2).Mod(n_squared);
        // // ciphertext_sum = ciphertext_sum.Multiply(ciphertext3).Mod(n_squared);
        // // ciphertext_sum = ciphertext_sum.Multiply(ciphertext4).Mod(n_squared);

        // // HOMOMORFICZNE MNOŻENIE PRZEZ STAŁĄ:
        // // c^k mod n_squared
        // var ciphertext_sum = ciphertext1.ModPow(code2, n_squared);
        // ciphertext_sum = ciphertext_sum.ModPow(code3, n_squared);
        // ciphertext_sum = ciphertext_sum.ModPow(code4, n_squared);

        // // Oczekiwana suma - używaj metod BouncyCastle
        // BigInteger expectedSum = code1.Add(code2).Add(code3).Add(code4);
        // Console.WriteLine($"Oczekiwana suma: {expectedSum}");
        // Console.WriteLine($"Ciphertext sum: {ciphertext_sum}");

        // var partialDecryptions = new Dictionary<int, BigInteger>();

        // partialDecryptions[1] = sharedKey1.partial_decrypt(ciphertext_sum);
        // partialDecryptions[2] = sharedKey2.partial_decrypt(ciphertext_sum);
        // partialDecryptions[3] = sharedKey3.partial_decrypt(ciphertext_sum);
        // partialDecryptions[4] = sharedKey4.partial_decrypt(ciphertext_sum);
        // partialDecryptions[5] = sharedKey5.partial_decrypt(ciphertext_sum);

        // Console.WriteLine($"Odszyfrowane przed re-encryption: {sharedKey1.decrypt(partialDecryptions)}");

        // Console.WriteLine($"c1: {ciphertext_sum}");

        // ciphertext_sum = publicKey.ReEncrypt(ciphertext_sum);

        // Console.WriteLine($"c2: {ciphertext_sum}");

        // partialDecryptions[1] = sharedKey1.partial_decrypt(ciphertext_sum);
        // partialDecryptions[2] = sharedKey2.partial_decrypt(ciphertext_sum);
        // partialDecryptions[3] = sharedKey3.partial_decrypt(ciphertext_sum);
        // partialDecryptions[4] = sharedKey4.partial_decrypt(ciphertext_sum);
        // partialDecryptions[5] = sharedKey5.partial_decrypt(ciphertext_sum);

        // Console.WriteLine("====================");

        // // Zamienić potem żeby decrypt był statyczny
        // Console.WriteLine($"Odszyfrowane: {sharedKey1.decrypt(partialDecryptions)}");

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

        var publicKey = new PaillierPublicKey();


        var swTotal = System.Diagnostics.Stopwatch.StartNew();
        var sw = System.Diagnostics.Stopwatch.StartNew();

        Parallel.For(0, 30000, i =>
        {
            var encrypted = publicKey.Encrypt(new BigInteger("12345678901234567890"));
        });

        sw.Stop();
        long time1 = sw.ElapsedMilliseconds;
        Console.WriteLine($"\n\n{time1}");

        sw.Restart();

        for (int i = 0; i < 30000; i++)
        {
            var encrypted = publicKey.Encrypt(new BigInteger("12345678901234567890"));
        }

        sw.Stop();
        long time2 = sw.ElapsedMilliseconds;
        Console.WriteLine($"\n\n{time1}");
        Console.WriteLine($"\n\n{time2}");
    }
}
