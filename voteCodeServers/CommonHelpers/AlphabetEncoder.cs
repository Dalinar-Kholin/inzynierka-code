using System;
using System.Text;
using Org.BouncyCastle.Math;

namespace VoteCodeServers.Helpers
{
    public class AlphabetEncoder
    {
        public static readonly AlphabetEncoder Instance = new AlphabetEncoder();

        private readonly string _alphabet;
        private readonly int _baseValue;

        public AlphabetEncoder() : this(Config.Load()) { }

        public AlphabetEncoder(AppSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.Alphabet))
                throw new InvalidOperationException("Alphabet is missing in config.");

            _alphabet = settings.Alphabet;
            _baseValue = _alphabet.Length;
        }

        public string Decode(BigInteger encoded)
        {
            if (encoded.Equals(BigInteger.Zero))
            {
                var a0 = _alphabet[0];
                return a0.ToString();
            }

            var baseValueBig = new BigInteger(_baseValue.ToString());
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

        public BigInteger ShiftLeft(BigInteger value, int positions)
        {
            if (positions < 0)
                throw new ArgumentOutOfRangeException(nameof(positions), "positions must be >= 0");

            if (positions == 0)
                return value;

            var baseValueBig = new BigInteger(_baseValue.ToString());
            var shift = baseValueBig.Pow(positions);
            Console.WriteLine($"Shift value: {shift}");
            return value.Multiply(shift);
        }

        public BigInteger AppendDigit(BigInteger value, int digit)
        {
            if (digit < 0 || digit >= _baseValue)
                throw new ArgumentOutOfRangeException(nameof(digit), $"digit must be between 0 and {_baseValue - 1}");

            var baseValueBig = new BigInteger(_baseValue.ToString());
            return value.Multiply(baseValueBig).Add(new BigInteger(digit.ToString()));
        }

        public BigInteger AppendLetter(BigInteger value, char letter)
        {
            int index = _alphabet.IndexOf(letter);
            if (index == -1)
                throw new ArgumentException($"Letter '{letter}' not found in alphabet.", nameof(letter));

            return AppendDigit(value, index);
        }
    }
}