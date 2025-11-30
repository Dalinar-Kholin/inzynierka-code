using System;
using System.Security.Cryptography;
using System.Text;

public static class SummandDraw
{
    public static long GenerateSummand(byte[] r_x, int j, int i)
    {
        using (var sha256 = SHA256.Create())
        {
            byte[] input = Encoding.UTF8.GetBytes($"summand_{r_x}_{j}_{i}");
            byte[] hash = sha256.ComputeHash(input);
            var result = BitConverter.ToInt64(hash, 0);

            if (result == long.MinValue)
                return long.MaxValue;

            return result < 0 ? -result : result;
        }
    }

    public static bool GenerateRandomBit(byte[] r_x, int j, int i, int m)
    {
        using (var sha256 = SHA256.Create())
        {
            byte[] input = Encoding.UTF8.GetBytes($"random_bit_{r_x}_{j}_{i}_{m}");
            byte[] hash = sha256.ComputeHash(input);
            return (hash[0] & 1) == 1;
        }
    }
}