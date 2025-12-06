using System;
using System.Security.Cryptography;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;

public class PaillierPublicKey
{
    public BigInteger n { get; }
    public BigInteger g { get; }
    public BigInteger n_squared { get; }
    private static readonly ThreadLocal<SecureRandom> ThreadRandom =
    new ThreadLocal<SecureRandom>(() => new SecureRandom());

    public PaillierPublicKey(string folderPath = "../paillierKeys")
    {
        string publicKeyPath = Path.Combine(folderPath, "paillier_keys_public.json");

        string publicKeyJson = File.ReadAllText(publicKeyPath);
        JObject publicKey = JObject.Parse(publicKeyJson);
        this.n = new BigInteger(publicKey["n"]!.ToString());
        this.n_squared = new BigInteger(publicKey["n_squared"]!.ToString());
        this.g = new BigInteger(publicKey["g"]!.ToString());
    }

    public PaillierPublicKey(BigInteger n, BigInteger g)
    {
        this.n = n;
        this.g = g;
        this.n_squared = n.Multiply(n);
    }

    public BigInteger Encrypt(BigInteger m)
    {
        if (m.CompareTo(BigInteger.Zero) < 0 || m.CompareTo(this.n) >= 0)
            throw new ArgumentException("Message out of range");

        BigInteger r = BigIntegers.CreateRandomInRange(
            BigInteger.One,
            n.Subtract(BigInteger.One),
            ThreadRandom.Value
        );

        // term1 = g^m mod n^2, but g = n+1 so 1+nm mod n^2
        BigInteger term1 = BigInteger.One.Add(m.Multiply(n)).Mod(n_squared);

        // term2 = r^n mod n^2

        ///////////////////////////////////////////////////////////////////////////////////////////////////
        // mozna probowac jakos zoptymalizowac tzn uzyc jakies innej biblioteki do tego czy nawet jezyka //
        ///////////////////////////////////////////////////////////////////////////////////////////////////

        BigInteger term2 = r.ModPow(n, n_squared);

        // ciphertext = term1 * term2 mod n^2
        return term1.Multiply(term2).Mod(n_squared);
    }

    public BigInteger EncryptHash(string hashString)
    {
        // Konwersja hex string na byte[]
        byte[] hashBytes = Enumerable.Range(0, hashString.Length)
            .Where(x => x % 2 == 0)
            .Select(x => Convert.ToByte(hashString.Substring(x, 2), 16))
            .ToArray();

        // Byte[] na BigInteger (dodajemy 1 na początku, żeby uniknąć ujemnych)
        byte[] positiveBytes = new byte[hashBytes.Length + 1];
        Array.Copy(hashBytes, 0, positiveBytes, 1, hashBytes.Length);
        positiveBytes[0] = 0x00; // gwarantuje, że BigInteger będzie dodatni

        BigInteger hashAsBigInt = new BigInteger(positiveBytes);

        // Sprawdź, czy hash mieści się w zakresie n
        if (hashAsBigInt.CompareTo(n) >= 0)
        {
            throw new ArgumentException("Hash jest za duży dla tego klucza Paillier");
        }

        return Encrypt(hashAsBigInt);
    }

    public BigInteger ReEncrypt(BigInteger ciphertext)
    {

        BigInteger r = BigIntegers.CreateRandomInRange(
            BigInteger.One,
            n.Subtract(BigInteger.One),
            ThreadRandom.Value
        );

        // fresh
        BigInteger randomizer = r.ModPow(n, n_squared);

        // re-encrypt(c) = c * r^n mod n^2
        return ciphertext.Multiply(randomizer).Mod(n_squared);
    }

    public string BigIntegerToHash(BigInteger decryptedHash)
    {
        // na byte[]
        byte[] bytes = decryptedHash.ToByteArray();

        if (bytes.Length > 0 && bytes[0] == 0x00)
        {
            byte[] trimmedBytes = new byte[bytes.Length - 1];
            Array.Copy(bytes, 1, trimmedBytes, 0, trimmedBytes.Length);
            return BitConverter.ToString(trimmedBytes).Replace("-", "").ToLower();
        }

        return BitConverter.ToString(bytes).Replace("-", "").ToLower();
    }
}