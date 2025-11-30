using System;
using System.IO;
using System.Text.Json;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Utilities;

public class ElGamalEncryption
{
    private readonly ElGamalPrivateKeyParameters _privateKey;
    private readonly ElGamalPublicKeyParameters _publicKey;
    private readonly SecureRandom _random;
    private readonly BigInteger _p;
    public record PublicKeyJson(string p, string g, string y);
    public record PrivateKeyJson(string p, string g, string x);
    public ElGamalEncryption(string folderPath = "../elGamalKeys/")
    {
        _random = new SecureRandom();

        string publicKeyPath = Path.Combine(folderPath, "elGamal_key_public.json");
        string privateKeyPath = Path.Combine(folderPath, "elGamal_key_private.json");

        if (!File.Exists(publicKeyPath)) throw new FileNotFoundException("No public key file", publicKeyPath);
        if (!File.Exists(privateKeyPath)) throw new FileNotFoundException("No private key file", privateKeyPath);

        var pubJson = JsonSerializer.Deserialize<PublicKeyJson>(File.ReadAllText(publicKeyPath));
        var privJson = JsonSerializer.Deserialize<PrivateKeyJson>(File.ReadAllText(privateKeyPath));

        var p = new BigInteger(pubJson.p);
        var g = new BigInteger(pubJson.g);
        var y = new BigInteger(pubJson.y);
        var x = new BigInteger(privJson.x);

        if (!p.Equals(new BigInteger(privJson.p)) || !g.Equals(new BigInteger(privJson.g)))
            throw new Exception("Public and private key have different p/g parameters - incompatible");

        var parameters = new ElGamalParameters(p, g);
        _publicKey = new ElGamalPublicKeyParameters(y, parameters);
        _privateKey = new ElGamalPrivateKeyParameters(x, parameters);
        _p = p;
    }

    public static void GenerateKeyPair(string folderPath = "../elGamalKeys/", int strength = 512)
    {
        Directory.CreateDirectory(folderPath);

        var random = new SecureRandom();
        var paramGen = new ElGamalParametersGenerator();

        paramGen.Init(strength, 80, random);
        var parameters = paramGen.GenerateParameters();

        if (parameters == null || parameters.P == null || parameters.G == null)
            throw new Exception("Failed to generate ElGamal parameters");

        var keyGen = new ElGamalKeyPairGenerator();
        keyGen.Init(new ElGamalKeyGenerationParameters(random, parameters));
        var keyPair = keyGen.GenerateKeyPair();

        var privateKey = (ElGamalPrivateKeyParameters)keyPair.Private;
        var publicKey = (ElGamalPublicKeyParameters)keyPair.Public;

        var pubJson = new PublicKeyJson(
            parameters.P.ToString(),
            parameters.G.ToString(),
            publicKey.Y.ToString()
        );

        var privJson = new PrivateKeyJson(
            parameters.P.ToString(),
            parameters.G.ToString(),
            privateKey.X.ToString()
        );

        string pubPath = Path.Combine(folderPath, "elGamal_key_public.json");
        string privPath = Path.Combine(folderPath, "elGamal_key_private.json");

        var options = new JsonSerializerOptions { WriteIndented = true };

        File.WriteAllText(pubPath, JsonSerializer.Serialize(pubJson, options));
        File.WriteAllText(privPath, JsonSerializer.Serialize(privJson, options));

        var pubContent = File.ReadAllText(pubPath);
        var privContent = File.ReadAllText(privPath);
        Console.WriteLine($"Public key file size: {pubContent.Length} chars");
        Console.WriteLine($"Private key file size: {privContent.Length} chars");
    }

    private AsymmetricCipherKeyPair GenerateKeyPair(ElGamalParameters parameters)
    {
        var keyGen = new ElGamalKeyPairGenerator();
        keyGen.Init(new ElGamalKeyGenerationParameters(_random, parameters));
        return keyGen.GenerateKeyPair();
    }

    public (BigInteger c1, BigInteger c2) Encrypt(long message)
    {
        var m = new BigInteger(message.ToString());
        return Encrypt(m);
    }

    public (BigInteger c1, BigInteger c2) Encrypt(BigInteger message)
    {
        if (message.SignValue < 0)
            throw new ArgumentException("Message must be non-negative", nameof(message));

        if (message.CompareTo(_p) >= 0)
            throw new ArgumentException("Message too large for modulus p", nameof(message));

        var g = _publicKey.Parameters.G;
        var y = _publicKey.Y;

        var k = BigIntegers.CreateRandomInRange(BigInteger.One, _p.Subtract(BigInteger.One), _random);

        // c1 = g^k mod p
        var c1 = g.ModPow(k, _p);

        // c2 = m * y^k mod p
        var c2 = message.Multiply(y.ModPow(k, _p)).Mod(_p);

        return (c1, c2);
    }

    public BigInteger Decrypt(BigInteger c1, BigInteger c2)
    {
        if (c1 == null || c2 == null)
            throw new ArgumentNullException("Ciphertext components cannot be null");

        if (c1.SignValue == 0 || c2.SignValue == 0)
            throw new ArgumentNullException("Ciphertext components cannot be 0");

        var x = _privateKey.X;

        // s = c1^x mod p
        var s = c1.ModPow(x, _p);

        // s^{-1} mod p
        var sInverse = s.ModInverse(_p);

        // m = c2 * s^{-1} mod p
        var m = c2.Multiply(sInverse).Mod(_p);

        return m;
    }

    public (BigInteger c1, BigInteger c2) Multiply(
        (BigInteger c1, BigInteger c2) cipher1,
        (BigInteger c1, BigInteger c2) cipher2)
    {
        var c1_result = cipher1.c1.Multiply(cipher2.c1).Mod(_p);
        var c2_result = cipher1.c2.Multiply(cipher2.c2).Mod(_p);

        return (c1_result, c2_result);
    }

    public (BigInteger c1, BigInteger c2) Divide(
        (BigInteger c1, BigInteger c2) cipher1,
        (BigInteger c1, BigInteger c2) cipher2)
    {
        var c1_inv = cipher2.c1.ModInverse(_p);
        var c2_inv = cipher2.c2.ModInverse(_p);

        var c1_result = cipher1.c1.Multiply(c1_inv).Mod(_p);
        var c2_result = cipher1.c2.Multiply(c2_inv).Mod(_p);

        return (c1_result, c2_result);
    }
}
