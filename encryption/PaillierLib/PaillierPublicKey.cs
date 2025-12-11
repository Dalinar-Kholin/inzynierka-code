using System;
using System.Security.Cryptography;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using System.Runtime.InteropServices;
// using StackExchange.Redis;


public class PaillierPublicKey
{
    public BigInteger n { get; }
    public BigInteger g { get; }
    public BigInteger n_squared { get; }
    private static readonly ThreadLocal<SecureRandom> ThreadRandom =
    new ThreadLocal<SecureRandom>(() => new SecureRandom());

    // private IDatabase _redis;
    // private const string REDIS_HOST = "127.0.0.1";
    // private const int REDIS_PORT = 6379;
    // private const string REDIS_QUEUE_NAME = "paillier_rn_fifo";
    // private const int BATCH_SIZE = 150000;

    // private Queue<BigInteger> _rnCache = new Queue<BigInteger>();
    // private readonly object _cacheLock = new object();

    // private long _encrypt1Counter = 0;
    // private readonly object _counterLock = new object();

    // private void RefillCacheFromRedis()
    // {
    //     try
    //     {
    //         if (_redis == null)
    //         {
    //             var options = ConfigurationOptions.Parse($"{REDIS_HOST}:{REDIS_PORT}");
    //             var connection = ConnectionMultiplexer.Connect(options);
    //             _redis = connection.GetDatabase();
    //         }

    //         // Pobierz BATCH_SIZE wartości na raz
    //         var batch = _redis.CreateBatch();
    //         var tasks = new List<Task<RedisValue>>();

    //         for (int i = 0; i < BATCH_SIZE; i++)
    //         {
    //             tasks.Add(batch.ListLeftPopAsync(REDIS_QUEUE_NAME));
    //         }

    //         batch.Execute();
    //         Task.WaitAll(tasks.ToArray());

    //         foreach (var task in tasks)
    //         {
    //             if (!task.Result.IsNull)
    //             {
    //                 _rnCache.Enqueue(new BigInteger(task.Result.ToString()));
    //             }
    //         }

    //         if (_rnCache.Count == 0)
    //         {
    //             throw new Exception("Redis queue is empty");
    //         }
    //     }
    //     catch (Exception ex)
    //     {
    //         throw new Exception($"Failed to fetch r^n from Redis: {ex.Message}");
    //     }
    // }

    // private BigInteger FetchRnFromRedis()
    // {
    //     lock (_cacheLock)
    //     {
    //         if (_rnCache.Count == 0)
    //         {
    //             RefillCacheFromRedis();
    //         }

    //         return _rnCache.Dequeue();
    //     }
    // }

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

    // pobiera z kolejki
    // public BigInteger EncryptWithPreComputed(BigInteger m)
    // {
    //     if (m.CompareTo(BigInteger.Zero) < 0 || m.CompareTo(this.n) >= 0)
    //         throw new ArgumentException("Message out of range");

    //     // term1 = g^m mod n^2, but g = n+1 so 1+nm mod n^2
    //     BigInteger term1 = BigInteger.One.Add(m.Multiply(n)).Mod(n_squared);

    //     // term2 = r^n mod n^2 - fetched from Redis
    //     BigInteger term2 = FetchRnFromRedis();

    //     // ciphertext = term1 * term2 mod n^2
    //     return term1.Multiply(term2).Mod(n_squared);
    // }

    // uzywa wrappera GMP
    public BigInteger Encrypt(BigInteger m)
    {
        if (m.CompareTo(BigInteger.Zero) < 0 || m.CompareTo(this.n) >= 0)
            throw new ArgumentException("Message out of range");

        BigInteger r = BigIntegers.CreateRandomInRange(
            BigInteger.One,
            n.Subtract(BigInteger.One),
            ThreadRandom.Value
        );

        BigInteger term1 = BigInteger.One.Add(m.Multiply(n)).Mod(n_squared);

        // można poprawić zeby nie zamieniać na stringa
        BigInteger term2 = new BigInteger(GMP.ModPow(r.ToString(), n.ToString(), n_squared.ToString()));

        return term1.Multiply(term2).Mod(n_squared);
    }


    public BigInteger EncryptHash(string hashString)
    {
        // konwersja hex string na byte[]
        byte[] hashBytes = Enumerable.Range(0, hashString.Length)
            .Where(x => x % 2 == 0)
            .Select(x => Convert.ToByte(hashString.Substring(x, 2), 16))
            .ToArray();

        // byte[] na BigInteger (dodajemy 1 na początku, żeby uniknąć ujemnych)
        byte[] positiveBytes = new byte[hashBytes.Length + 1];
        Array.Copy(hashBytes, 0, positiveBytes, 1, hashBytes.Length);
        positiveBytes[0] = 0x00; // gwarantuje, że BigInteger będzie dodatni

        BigInteger hashAsBigInt = new BigInteger(positiveBytes);

        // sprawdź czy hash mieści się w zakresie n
        if (hashAsBigInt.CompareTo(n) >= 0)
        {
            throw new ArgumentException("Hash is too large for this Paillier key");
        }

        return Encrypt(hashAsBigInt);
    }

    public BigInteger ReEncrypt(BigInteger ciphertext)
    {
        BigInteger r = BigIntegers.CreateRandomInRange(
            BigInteger.One,
            n.Subtract(BigInteger.One),
            ThreadRandom.Value);

        // fresh r^n mod n^2
        BigInteger randomizer = new BigInteger(GMP.ModPow(r.ToString(), n.ToString(), n_squared.ToString()));

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

public static class GMP
{
    [DllImport("libgmp_wrapper.so", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern void modpow(string baseStr, string expStr, string modStr,
                                      [Out] char[] resultStr, int resultSize);

    [DllImport("libgmp_wrapper.so", CallingConvention = CallingConvention.Cdecl)]
    private static extern void modpow_bytes(byte[] baseBytes, int baseLen,
                                           byte[] expBytes, int expLen,
                                           byte[] modBytes, int modLen,
                                           byte[] resultBytes, ref int resultLen);

    public static string ModPow(string b, string e, string m)
    {
        char[] buf = new char[8192];
        modpow(b, e, m, buf, buf.Length);
        return new string(buf).TrimEnd('\0');
    }

    // naprawic
    public static BigInteger ModPowFast(BigInteger b, BigInteger e, BigInteger m)
    {
        // ToByteArray zwraca little-endian z bajtem znaku
        byte[] baseBytes = b.ToByteArray();
        byte[] expBytes = e.ToByteArray();
        byte[] modBytes = m.ToByteArray();
        byte[] resultBytes = new byte[8192];
        int resultLen = 0;

        // Wrapper teraz akceptuje little-endian bezpośrednio
        modpow_bytes(baseBytes, baseBytes.Length,
                    expBytes, expBytes.Length,
                    modBytes, modBytes.Length,
                    resultBytes, ref resultLen);

        // Weź tylko użyte bajty
        byte[] result = new byte[resultLen];
        Array.Copy(resultBytes, 0, result, 0, resultLen);

        return new BigInteger(result);
    }
}