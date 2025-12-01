using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using System.IO;
using Newtonsoft.Json.Linq;

public class PaillierSharedKey
{
    public int player_id { get; }
    public BigInteger n { get; }
    public BigInteger n_squared { get; }
    public BigInteger theta_inv { get; }
    public int degree { get; }
    public int fac_of_parties { get; }
    public BigInteger share { get; }

    public PaillierSharedKey(int serverNumber = 0, string sharedKeyPath = "../paillierKeys/paillier_keys_private.json")
    {
        string sharedKeyJson = File.ReadAllText(sharedKeyPath);
        JObject sharedKey = JObject.Parse(sharedKeyJson);
        int numberOfServers = sharedKey.Count;
        JObject? serverKey = null;
        foreach (JProperty property in sharedKey.Properties())
        {
            int serverId = int.Parse(property.Name);
            if (serverId == serverNumber)
            {
                serverKey = (JObject)property.Value;
                break;
            }
        }
        if (serverKey == null)
        {
            throw new ArgumentException($"Server number {serverNumber} not found in the file.");
        }

        this.player_id = int.Parse(serverKey["player_id"]!.ToString());
        this.n = BigInteger.Parse(serverKey["n"]!.ToString());
        this.n_squared = BigInteger.Parse(serverKey["n_squared"]!.ToString());
        this.theta_inv = BigInteger.Parse(serverKey["theta_inv"]!.ToString());
        this.degree = int.Parse(serverKey["degree"]!.ToString());
        this.fac_of_parties = int.Parse(serverKey["fac_of_parties"]!.ToString());
        this.share = BigInteger.Parse(serverKey["share"]!.ToString());
    }

    public PaillierSharedKey(BigInteger n, BigInteger n_squared, int player_id, BigInteger theta_inv, int degree, BigInteger share, int fac_of_parties)
    {
        this.n = n;
        this.n_squared = n_squared;
        this.player_id = player_id;
        this.theta_inv = theta_inv;
        this.degree = degree;
        this.share = share;
        this.fac_of_parties = fac_of_parties;
    }

    public BigInteger partial_decrypt(BigInteger ciphertext_value)
    {
        var other_honest_players = new List<int>();
        for (int i = 0; i <= this.degree; i++)
        {
            if (i + 1 != this.player_id)
            {
                other_honest_players.Add(i + 1);
            }
        }

        BigInteger lagrange_interpol_enumerator = mult_list(other_honest_players);

        var denominators = new List<int>();
        foreach (int j in other_honest_players)
        {
            denominators.Add(j - this.player_id);
        }
        BigInteger lagrange_interpol_denominator = mult_list(denominators);

        BigInteger exp = (this.fac_of_parties * lagrange_interpol_enumerator * this.share) / lagrange_interpol_denominator;

        if (exp < 0)
        {
            ciphertext_value = mod_inv(ciphertext_value, this.n_squared);
            exp = -exp;
        }
        BigInteger partial_decryption = BigInteger.ModPow(ciphertext_value, exp, this.n_squared);
        return partial_decryption;
    }

    public BigInteger decrypt(Dictionary<int, BigInteger> partial_dict)
    {
        var partial_decryptions = new List<BigInteger>();
        for (int i = 0; i <= this.degree; i++)
        {
            partial_decryptions.Add(partial_dict[i + 1]);
        }

        if (partial_decryptions.Count < this.degree + 1)
        {
            throw new ArgumentException("Not enough shares.");
        }

        BigInteger combined_decryption = mult_list(partial_decryptions.Take(this.degree + 1).ToList()) % this.n_squared;

        if ((combined_decryption - 1) % this.n != 0)
        {
            throw new ArgumentException(
                "Combined decryption minus one is not divisible by N. This might be caused by the " +
                "fact that the ciphertext that is being decrypted, differs between the parties."
            );
        }

        BigInteger message = (((combined_decryption - 1) / this.n) * this.theta_inv) % this.n;

        return message;
    }

    // gdzieś przenieść lub zobaczy sie (wszystkie parametry są ogolno dostepne)
    public static BigInteger decrypt(Dictionary<int, BigInteger> partial_dict, BigInteger theta_inv, int n, int n_squared, int degree)
    {
        var partial_decryptions = new List<BigInteger>();
        for (int i = 0; i <= degree; i++)
        {
            partial_decryptions.Add(partial_dict[i + 1]);
        }

        if (partial_decryptions.Count < degree + 1)
        {
            throw new ArgumentException("Not enough shares.");
        }

        BigInteger combined_decryption = mult_list(partial_decryptions.Take(degree + 1).ToList()) % n_squared;

        if ((combined_decryption - 1) % n != 0)
        {
            throw new ArgumentException(
                "Combined decryption minus one is not divisible by N. This might be caused by the " +
                "fact that the ciphertext that is being decrypted, differs between the parties."
            );
        }

        BigInteger message = (((combined_decryption - 1) / n) * theta_inv) % n;

        return message;
    }


    private static BigInteger mult_list(List<BigInteger> list_, BigInteger? modulus = null)
    {
        BigInteger @out = 1;
        if (modulus == null)
        {
            foreach (var element in list_)
            {
                @out = @out * element;
            }
        }
        else
        {
            foreach (var element in list_)
            {
                @out = (@out * element) % modulus.Value;
            }
        }
        return @out;
    }

    private static BigInteger mult_list(List<int> list_, BigInteger? modulus = null)
    {
        BigInteger @out = 1;
        if (modulus == null)
        {
            foreach (var element in list_)
            {
                @out = @out * element;
            }
        }
        else
        {
            foreach (var element in list_)
            {
                @out = (@out * element) % modulus.Value;
            }
        }
        return @out;
    }

    private (BigInteger gcd, BigInteger x, BigInteger y) ExtendedEuclidean(BigInteger num_a, BigInteger num_b)
    {
        // a*x + b*y = gcd
        BigInteger x_old = 0, x_cur = 1;
        BigInteger y_old = 1, y_cur = 0;

        while (num_a != 0)
        {
            BigInteger quotient = num_b / num_a;
            BigInteger temp = num_a;
            num_a = num_b % num_a;
            num_b = temp;

            temp = y_cur;
            y_cur = y_old - quotient * y_cur;
            y_old = temp;

            temp = x_cur;
            x_cur = x_old - quotient * x_cur;
            x_old = temp;
        }

        return (num_b, x_old, y_old);
    }

    private BigInteger mod_inv(BigInteger value, BigInteger modulus)
    {
        value = value % modulus;

        var (gcd, inverse, _) = ExtendedEuclidean(value, modulus);

        if (gcd != 1)
            throw new DivideByZeroException($"Inverse of {value} mod {modulus} does not exist.");

        return inverse;
    }
}