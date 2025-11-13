using System;
using System.Numerics;
using System.Security.Cryptography;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public class PaillierPublicKey
{
    public BigInteger n { get; }
    public BigInteger g { get; }
    public BigInteger n_squared { get; }

    public PaillierPublicKey(string publicKeyPath = "../keys/paillier_keys_public.json")
    {
        string publicKeyJson = File.ReadAllText(publicKeyPath);
        JObject publicKey = JObject.Parse(publicKeyJson);
        this.n = BigInteger.Parse(publicKey["n"]!.ToString());
        this.n_squared = BigInteger.Parse(publicKey["n_squared"]!.ToString());
        this.g = BigInteger.Parse(publicKey["g"]!.ToString());
    }

    public PaillierPublicKey(BigInteger n, BigInteger g)
    {
        this.n = n;
        this.g = g;
        this.n_squared = n * n;
    }

    public BigInteger Encrypt(BigInteger m)
    {
        // trzeba chyba jakies ograniczenie zrobic zeby zostawić zapas na dodawanie
        if (m < 0 || m >= this.n)
            throw new ArgumentException("Message out of range");

        BigInteger r;
        do
        {
            r = RandomBigInteger(1, this.n - 1);
        } while (BigInteger.GreatestCommonDivisor(r, this.n) != 1);

        BigInteger term1 = BigInteger.ModPow(this.g, m, this.n_squared);
        BigInteger term2 = BigInteger.ModPow(r, this.n, this.n_squared);

        return (term1 * term2) % this.n_squared;
    }


    private BigInteger RandomBigInteger(BigInteger minValue, BigInteger maxValue)
    {
        byte[] bytes = maxValue.ToByteArray();
        BigInteger result;

        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            do
            {
                rng.GetBytes(bytes);
                bytes[bytes.Length - 1] &= 0x7F;
                result = new BigInteger(bytes);
            } while (result < minValue || result > maxValue);
        }

        return result;
    }
}