using System;
using System.IO;
using System.Text.Json;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Utilities;

public class ElGamalEncryption
{
    private readonly ElGamalPrivateKeyParameters _privateKey;
    private readonly Dictionary<int, ElGamalPublicKeyParameters> _allServersPublicKeys;
    private readonly SecureRandom _random;
    private readonly BigInteger _p;
    private readonly int _serverId;

    public record PublicKeyJson(int serverId, string p, string g, string y);
    public record PrivateKeyJson(int serverId, string p, string g, string x);

    public ElGamalEncryption(int serverId, string folderPath = "../elGamalKeys")
    {
        _serverId = serverId;
        _random = new SecureRandom();

        string publicKeysPath = Path.Combine(folderPath, "elGamal_servers_public.json");
        string privateKeysPath = Path.Combine(folderPath, "elGamal_servers_private.json");

        if (!File.Exists(publicKeysPath))
            throw new FileNotFoundException("No public keys file", publicKeysPath);
        if (!File.Exists(privateKeysPath))
            throw new FileNotFoundException("No private keys file", privateKeysPath);

        var publicKeysJson = File.ReadAllText(publicKeysPath);
        var privateKeysJson = File.ReadAllText(privateKeysPath);

        var allPublicKeys = JsonSerializer.Deserialize<List<PublicKeyJson>>(publicKeysJson);
        var allPrivateKeys = JsonSerializer.Deserialize<List<PrivateKeyJson>>(privateKeysJson);

        if (allPublicKeys == null || allPublicKeys.Count == 0)
            throw new Exception("No public keys found in file");
        if (allPrivateKeys == null || allPrivateKeys.Count == 0)
            throw new Exception("No private keys found in file");

        // load all public keys
        _allServersPublicKeys = new Dictionary<int, ElGamalPublicKeyParameters>();
        foreach (var key in allPublicKeys)
        {
            var p = new BigInteger(key.p);
            var g = new BigInteger(key.g);
            var y = new BigInteger(key.y);
            var parameters = new ElGamalParameters(p, g);
            _allServersPublicKeys[key.serverId] = new ElGamalPublicKeyParameters(y, parameters);
        }

        // load own private key
        var myPrivateKey = allPrivateKeys.FirstOrDefault(k => k.serverId == serverId);
        if (myPrivateKey == null)
            throw new KeyNotFoundException($"Private key for server {serverId} not found");

        var myP = new BigInteger(myPrivateKey.p);
        var myG = new BigInteger(myPrivateKey.g);
        var myX = new BigInteger(myPrivateKey.x);
        var myParams = new ElGamalParameters(myP, myG);
        _privateKey = new ElGamalPrivateKeyParameters(myX, myParams);
        _p = myP;

        Console.WriteLine($"Server {serverId}: Loaded {_allServersPublicKeys.Count} public keys");
    }

    // generating own key for production (each server generates its own key)
    public static void GenerateAndSaveOwnKey(int serverId, string folderPath = "../elGamalKeys", int strength = 512)
    {
        Directory.CreateDirectory(folderPath);

        var random = new SecureRandom();
        var options = new JsonSerializerOptions { WriteIndented = true };

        string publicKeysPath = Path.Combine(folderPath, "elGamal_servers_public.json");
        string privateKeysPath = Path.Combine(folderPath, "elGamal_servers_private.json");

        // check if parameters (p, g) already exist in existing keys
        ElGamalParameters parameters;
        List<PublicKeyJson> existingPublicKeys = new List<PublicKeyJson>();
        List<PrivateKeyJson> existingPrivateKeys = new List<PrivateKeyJson>();

        if (File.Exists(publicKeysPath))
        {
            var publicKeysJson = File.ReadAllText(publicKeysPath);
            existingPublicKeys = JsonSerializer.Deserialize<List<PublicKeyJson>>(publicKeysJson) ?? new List<PublicKeyJson>();

            if (existingPublicKeys.Any(k => k.serverId == serverId))
                throw new Exception($"Server {serverId} already has a key pair");

            if (existingPublicKeys.Count > 0)
            {
                // reuse existing parameters
                var firstKey = existingPublicKeys[0];
                var p = new BigInteger(firstKey.p);
                var g = new BigInteger(firstKey.g);
                parameters = new ElGamalParameters(p, g);
                Console.WriteLine($"Using existing ElGamal parameters from other servers");
            }
            else
            {
                // generate new parameters
                var paramGen = new ElGamalParametersGenerator();
                paramGen.Init(strength, 80, random);
                parameters = paramGen.GenerateParameters();
                Console.WriteLine($"Generated new ElGamal parameters");
            }
        }
        else
        {
            // generate new parameters
            var paramGen = new ElGamalParametersGenerator();
            paramGen.Init(strength, 80, random);
            parameters = paramGen.GenerateParameters();
            Console.WriteLine($"Generated new ElGamal parameters");
        }

        if (File.Exists(privateKeysPath))
        {
            var privateKeysJson = File.ReadAllText(privateKeysPath);
            existingPrivateKeys = JsonSerializer.Deserialize<List<PrivateKeyJson>>(privateKeysJson) ?? new List<PrivateKeyJson>();
        }

        var keyGen = new ElGamalKeyPairGenerator();
        keyGen.Init(new ElGamalKeyGenerationParameters(random, parameters));
        var keyPair = keyGen.GenerateKeyPair();

        var privateKey = (ElGamalPrivateKeyParameters)keyPair.Private;
        var publicKey = (ElGamalPublicKeyParameters)keyPair.Public;

        var pubKey = new PublicKeyJson(
            serverId,
            parameters.P.ToString(),
            parameters.G.ToString(),
            publicKey.Y.ToString());

        var privKey = new PrivateKeyJson(
            serverId,
            parameters.P.ToString(),
            parameters.G.ToString(),
            privateKey.X.ToString());

        existingPublicKeys.Add(pubKey);
        existingPrivateKeys.Add(privKey);

        // save updated key lists
        File.WriteAllText(publicKeysPath, JsonSerializer.Serialize(existingPublicKeys, options));
        Console.WriteLine($"Saved public key for server {serverId} to {publicKeysPath}");

        File.WriteAllText(privateKeysPath, JsonSerializer.Serialize(existingPrivateKeys, options));
        Console.WriteLine($"Saved private key for server {serverId} to {privateKeysPath}");
    }

    // generating keys for all servers for testing
    public static void GenerateAndSaveServerKeys(int numberOfServers, string folderPath = "../elGamalKeys", int strength = 512)
    {
        Directory.CreateDirectory(folderPath);

        var random = new SecureRandom();
        var paramGen = new ElGamalParametersGenerator();
        paramGen.Init(strength, 80, random);
        var parameters = paramGen.GenerateParameters();

        if (parameters == null || parameters.P == null || parameters.G == null)
            throw new Exception("Failed to generate ElGamal parameters");

        var publicKeys = new List<PublicKeyJson>();
        var privateKeys = new List<PrivateKeyJson>();

        for (int i = 1; i <= numberOfServers; i++)
        {
            var keyGen = new ElGamalKeyPairGenerator();
            keyGen.Init(new ElGamalKeyGenerationParameters(random, parameters));
            var keyPair = keyGen.GenerateKeyPair();

            var privateKey = (ElGamalPrivateKeyParameters)keyPair.Private;
            var publicKey = (ElGamalPublicKeyParameters)keyPair.Public;

            var pubKey = new PublicKeyJson(
                i,
                parameters.P.ToString(),
                parameters.G.ToString(),
                publicKey.Y.ToString());

            var privKey = new PrivateKeyJson(
                i,
                parameters.P.ToString(),
                parameters.G.ToString(),
                privateKey.X.ToString());

            publicKeys.Add(pubKey);
            privateKeys.Add(privKey);

            Console.WriteLine($"Generated key pair for server {i}");
        }

        var options = new JsonSerializerOptions { WriteIndented = true };

        string publicKeysPath = Path.Combine(folderPath, "elGamal_servers_public.json");
        File.WriteAllText(publicKeysPath, JsonSerializer.Serialize(publicKeys, options));
        Console.WriteLine($"Saved all {publicKeys.Count} public keys to {publicKeysPath}");

        string privateKeysPath = Path.Combine(folderPath, "elGamal_servers_private.json");
        File.WriteAllText(privateKeysPath, JsonSerializer.Serialize(privateKeys, options));
        Console.WriteLine($"Saved all {privateKeys.Count} private keys to {privateKeysPath}");
    }

    public (BigInteger c1, BigInteger c2) Encrypt(BigInteger message, int targetServerId = -1)
    {
        if (message.SignValue < 0)
            throw new ArgumentException("Message must be non-negative", nameof(message));

        int serverId = targetServerId == -1 ? _serverId : targetServerId;

        if (!_allServersPublicKeys.ContainsKey(serverId))
            throw new KeyNotFoundException($"Public key for server {serverId} not found");

        var publicKey = _allServersPublicKeys[serverId];
        var p = publicKey.Parameters.P;
        var g = publicKey.Parameters.G;
        var y = publicKey.Y;

        if (message.CompareTo(p) >= 0)
            throw new ArgumentException("Message too large for modulus p", nameof(message));

        var k = BigIntegers.CreateRandomInRange(BigInteger.One, p.Subtract(BigInteger.One), _random);

        // c1 = g^k mod p
        var c1 = g.ModPow(k, p);

        // c2 = m * y^k mod p
        var c2 = message.Multiply(y.ModPow(k, p)).Mod(p);

        return (c1, c2);
    }

    public (BigInteger c1, BigInteger c2) Encrypt(long message, int targetServerId = -1)
    {
        return Encrypt(new BigInteger(message.ToString()), targetServerId);
    }

    public BigInteger Decrypt((BigInteger c1, BigInteger c2) ciphertext)
    {
        if (ciphertext.c1 == null || ciphertext.c2 == null)
            throw new ArgumentNullException("Ciphertext components cannot be null");

        if (ciphertext.c1.SignValue == 0 || ciphertext.c2.SignValue == 0)
            throw new ArgumentException("Ciphertext components cannot be 0");

        var x = _privateKey.X;

        // s = c1^x mod p
        var s = ciphertext.c1.ModPow(x, _p);

        // s^{-1} mod p
        var sInverse = s.ModInverse(_p);

        // m = c2 * s^{-1} mod p
        var m = ciphertext.c2.Multiply(sInverse).Mod(_p);

        return m;
    }

    // re-encryption of ciphertext to target server
    public (BigInteger c1, BigInteger c2) ReEncrypt((BigInteger c1, BigInteger c2) ciphertext, int targetServerId = -1)
    {
        int serverId = targetServerId == -1 ? _serverId : targetServerId;

        if (!_allServersPublicKeys.ContainsKey(serverId))
            throw new KeyNotFoundException($"Public key for server {serverId} not found");

        var publicKey = _allServersPublicKeys[serverId];
        var g = publicKey.Parameters.G;
        var y = publicKey.Y;
        var p = publicKey.Parameters.P;

        // generate random k'
        var k_prime = BigIntegers.CreateRandomInRange(BigInteger.One, p.Subtract(BigInteger.One), _random);

        // c1' = c1 * g^k' mod p
        var c1_new = ciphertext.c1.Multiply(g.ModPow(k_prime, p)).Mod(p);

        // c2' = c2 * y^k' mod p
        var c2_new = ciphertext.c2.Multiply(y.ModPow(k_prime, p)).Mod(p);

        return (c1_new, c2_new);
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
