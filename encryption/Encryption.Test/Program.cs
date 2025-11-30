using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.CodeAnalysis;

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

        // BigInteger code1 = BigInteger.Parse("1");
        // BigInteger code2 = BigInteger.Parse("0");
        // BigInteger code3 = BigInteger.Parse("1");
        // BigInteger code4 = BigInteger.Parse("1");

        // BigInteger ciphertext1 = publicKey.Encrypt(code1);
        // BigInteger ciphertext2 = publicKey.Encrypt(code2);
        // BigInteger ciphertext3 = publicKey.Encrypt(code3);
        // BigInteger ciphertext4 = publicKey.Encrypt(code4);

        // // HOMOMORFICZNE DODAWANIE
        // // BigInteger ciphertext_sum = (ciphertext1 * ciphertext2) % (n_squared);
        // // ciphertext_sum = (ciphertext_sum * ciphertext3) % (n_squared);
        // // ciphertext_sum = (ciphertext_sum * ciphertext4) % (n_squared);

        // // HOMOMORFICZNE MNOZENIE PRZEZ STALA...
        // BigInteger ciphertext_sum = BigInteger.ModPow(ciphertext1, 1, n_squared);
        // ciphertext_sum = BigInteger.ModPow(ciphertext_sum, 1, n_squared);
        // ciphertext_sum = BigInteger.ModPow(ciphertext_sum, 1, n_squared);

        // Console.WriteLine($"Oczekiwana suma: {code1 + code2}");
        // Console.WriteLine($"Ciphertext sum: {ciphertext_sum}");

        // var partialDecryptions = new Dictionary<int, BigInteger>();

        // partialDecryptions[1] = sharedKey1.partial_decrypt(ciphertext_sum);
        // partialDecryptions[2] = sharedKey2.partial_decrypt(ciphertext_sum);
        // partialDecryptions[3] = sharedKey3.partial_decrypt(ciphertext_sum);
        // partialDecryptions[4] = sharedKey4.partial_decrypt(ciphertext_sum);
        // partialDecryptions[5] = sharedKey5.partial_decrypt(ciphertext_sum);

        // Console.WriteLine("====================");

        // // zamienic potem zeby decrypt był statyczny
        // Console.WriteLine($"Odszyfrowane: {sharedKey1.decrypt(partialDecryptions)}");

        Console.WriteLine("=== ELGAMAL ===");
        // ElGamalEncryption.GenerateKeyPair();

        var elGamal = new ElGamalEncryption();

        long number1 = 42;
        long number2 = 42;

        Console.WriteLine($"\nSzyfrowanie liczby: {number1}");
        var (c1, c2) = elGamal.Encrypt(number1);
        Console.WriteLine($"Ciphertext c1: {c1}");
        Console.WriteLine($"Ciphertext c2: {c2}");

        var decrypted = elGamal.Decrypt(c1, c2);
        Console.WriteLine($"Odszyfrowane: {decrypted}");

        var (c1_2, c2_2) = elGamal.Encrypt(number2);
        var (c1_mult, c2_mult) = elGamal.Multiply((c1, c2), (c1_2, c2_2));
        var (c1_div, c2_div) = elGamal.Divide((c1, c2), (c1_2, c2_2));

        var decryptedMult = elGamal.Decrypt(c1_mult, c2_mult);
        var decryptedDiv = elGamal.Decrypt(c1_div, c2_div);
        Console.WriteLine($"Mnozenie: {decryptedMult} (oczekiwane: {number1 * number2})");
        Console.WriteLine($"Dzielenie: {decryptedDiv} (oczekiwane: {number1 / number2})");
    }
}
