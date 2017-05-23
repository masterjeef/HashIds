using System.Collections.Generic;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HashidsNet
{
    /// <summary>
    /// Generate YouTube-like hashes from one or many numbers. Use HashIds when you do not want to expose your database ids to the user.
    /// </summary>
    public sealed class HashIds : IHashIds
    {
        public const string DefaultAlphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
        public const string DefaultSeparators = "cfhistuCFHISTU";

        private const int MinAlphabetLength = 2;
        private const double SepDiv = 3.5;
        private const double GuardDiv = 12.0;

        private string _alphabet;
        private readonly string _salt;
        private string _separators;
        private string _guards;
        private readonly int _minHashLength;

        private Regex _guardsRegex;
        private Regex _separatorsRegex;

        private static readonly Lazy<Regex> HexValidator = new Lazy<Regex>(() => new Regex("^[0-9a-fA-F]+$"));
        private static readonly Lazy<Regex> HexSplitter = new Lazy<Regex>(() => new Regex(@"[\w\W]{1,12}"));
        private const int HexBase = 16;

        /// <summary>
        /// Instantiates a new HashIds with the default setup.
        /// </summary>
        public HashIds() : this("")
        {
        }

        /// <summary>
        /// Instantiates a new HashIds en/de-coder.
        /// </summary>
        /// <param name="salt"></param>
        /// <param name="minHashLength"></param>
        /// <param name="alphabet"></param>
        /// <param name="separators"></param>
        public HashIds(string salt = "", int minHashLength = 0, string alphabet = DefaultAlphabet, string separators = DefaultSeparators)
        {
            if (string.IsNullOrWhiteSpace(alphabet))
            {
                throw new ArgumentNullException(nameof(alphabet));
            }

            _salt = salt;
            _alphabet = new string(alphabet.ToCharArray().Distinct().ToArray());
            _separators = separators;
            _minHashLength = minHashLength;

            if (_alphabet.Length < MinAlphabetLength)
            {
                throw new ArgumentException($"alphabet must contain at least {MinAlphabetLength} unique characters.", nameof(alphabet));
            }

            SetupSeparators();
            SetupGuards();
        }

        /// <summary>
        /// Encodes the provided numbers into a hashed string
        /// </summary>
        /// <param name="numbers">the numbers to encode</param>
        /// <returns>the hashed string</returns>
        public string Encode(params int[] numbers)
        {
            if (numbers.Any(n => n < 0))
            {
                throw new ArgumentException("Negative numbers are not allowed!");
            }

            return GenerateHashFrom(numbers.Select(n => (long)n).ToArray());
        }

        /// <summary>
        /// Encodes the provided numbers into a hashed string
        /// </summary>
        /// <param name="numbers">the numbers to encode</param>
        /// <returns>the hashed string</returns>
        public string Encode(IEnumerable<int> numbers)
        {
            return Encode(numbers.ToArray());
        }

        /// <summary>
        /// Decodes the provided hash into
        /// </summary>
        /// <param name="hash">the hash</param>
        /// <exception cref="T:System.OverflowException">if the decoded number overflows integer</exception>
        /// <returns>the numbers</returns>
        public int[] Decode(string hash)
        {
            return GetNumbersFrom(hash).Select(n => (int)n).ToArray();
        }

        /// <summary>
        /// Encodes the provided hex string to a HashIds hash.
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        public string EncodeHex(string hex)
        {
            if (!HexValidator.Value.IsMatch(hex))
            {
                throw new ArgumentException(nameof(hex));
            }

            var matches = HexSplitter.Value.Matches(hex);

            var numbers = (from Match match in matches
                           select Convert.ToInt64(match.Value, HexBase))
                           .ToArray();

            return EncodeLong(numbers);
        }

        /// <summary>
        /// Decodes the provided hash into a hex-string
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        public string DecodeHex(string hash)
        {
            var ret = new StringBuilder();
            var numbers = DecodeLong(hash);

            foreach (var number in numbers)
            {
                ret.Append($"{number:X}");
            }

            return ret.ToString();
        }

        /// <summary>
        /// Decodes the provided hashed string into an array of longs 
        /// </summary>
        /// <param name="hash">the hashed string</param>
        /// <returns>the numbers</returns>
        public long[] DecodeLong(string hash)
        {
            return GetNumbersFrom(hash);
        }

        /// <summary>
        /// Encodes the provided longs to a hashed string
        /// </summary>
        /// <param name="numbers">the numbers</param>
        /// <returns>the hashed string</returns>
        public string EncodeLong(params long[] numbers)
        {
            return GenerateHashFrom(numbers);
        }

        /// <summary>
        /// Encodes the provided longs to a hashed string
        /// </summary>
        /// <param name="numbers">the numbers</param>
        /// <returns>the hashed string</returns>
        public string EncodeLong(IEnumerable<long> numbers)
        {
            return EncodeLong(numbers.ToArray());
        }

        /// <summary>
        /// 
        /// </summary>
        private void SetupSeparators()
        {
            // separators should contain only characters present in alphabet; 
            _separators = new string(_separators.ToCharArray().Intersect(_alphabet.ToCharArray()).ToArray());

            // alphabet should not contain separators.
            _alphabet = new string(_alphabet.ToCharArray().Except(_separators.ToCharArray()).ToArray());

            _separators = ConsistentShuffle(_separators, _salt);

            // ReSharper disable once PossibleLossOfFraction
            if (_separators.Length == 0 || _alphabet.Length / _separators.Length > SepDiv)
            {
                var separatorsLength = (int)Math.Ceiling(_alphabet.Length / SepDiv);
                if (separatorsLength == 1)
                {
                    separatorsLength = 2;
                }

                if (separatorsLength > _separators.Length)
                {
                    var diff = separatorsLength - _separators.Length;
                    _separators += _alphabet.Substring(0, diff);
                    _alphabet = _alphabet.Substring(diff);
                }
                else
                {
                    _separators = _separators.Substring(0, separatorsLength);
                }
            }

            _separatorsRegex = new Regex($"[{_separators}]");
            _alphabet = ConsistentShuffle(_alphabet, _salt);
        }

        /// <summary>
        /// 
        /// </summary>
        private void SetupGuards()
        {
            var guardCount = (int)Math.Ceiling(_alphabet.Length / GuardDiv);

            if (_alphabet.Length < 3)
            {
                _guards = _separators.Substring(0, guardCount);
                _separators = _separators.Substring(guardCount);
            }
            else
            {
                _guards = _alphabet.Substring(0, guardCount);
                _alphabet = _alphabet.Substring(guardCount);
            }

            _guardsRegex = new Regex($"[{_guards}]");
        }

        /// <summary>
        /// Internal function that does the work of creating the hash
        /// </summary>
        /// <param name="numbers"></param>
        /// <returns></returns>
        private string GenerateHashFrom(long[] numbers)
        {
            if (numbers == null || !numbers.Any())
            {
                throw new ArgumentException(nameof(numbers));
            }

            var result = new StringBuilder();
            var alphabet = _alphabet;

            long numbersHashInt = 0;
            for (var i = 0; i < numbers.Length; i++)
            {
                numbersHashInt += (int)(numbers[i] % (i + 100));
            }

            var lottery = alphabet[(int)(numbersHashInt % alphabet.Length)];
            result.Append(lottery.ToString());

            for (var i = 0; i < numbers.Length; i++)
            {
                var number = numbers[i];
                var buffer = lottery + _salt + alphabet;

                alphabet = ConsistentShuffle(alphabet, buffer.Substring(0, alphabet.Length));
                var last = Hash(number, alphabet);

                result.Append(last);

                if (i + 1 >= numbers.Length)
                {
                    continue;
                }

                number %= last[0] + i;
                var separatorsIndex = (int)number % _separators.Length;

                result.Append(_separators[separatorsIndex]);
            }

            if (result.Length < _minHashLength)
            {
                var guardIndex = (int)(numbersHashInt + result[0]) % _guards.Length;
                var guard = _guards[guardIndex];

                result.Insert(0, guard);

                if (result.Length < _minHashLength)
                {
                    guardIndex = (int)(numbersHashInt + result[2]) % _guards.Length;
                    guard = _guards[guardIndex];

                    result.Append(guard);
                }
            }

            var halfLength = alphabet.Length / 2;
            while (result.Length < _minHashLength)
            {
                alphabet = ConsistentShuffle(alphabet, alphabet);
                result.Insert(0, alphabet.Substring(halfLength));
                result.Append(alphabet.Substring(0, halfLength));

                var excess = result.Length - _minHashLength;

                if (excess <= 0)
                {
                    continue;
                }

                result.Remove(0, excess / 2);
                result.Remove(_minHashLength, result.Length - _minHashLength);
            }

            return result.ToString();
        }

        private static string Hash(long input, string alphabet)
        {
            var hash = new StringBuilder();

            do
            {
                hash.Insert(0, alphabet[(int)(input % alphabet.Length)]);
                input = input / alphabet.Length;
            } while (input > 0);

            return hash.ToString();
        }

        private static long UnHash(string input, string alphabet)
        {
            long number = 0;

            checked
            {
                for (var i = 0; i < input.Length; i++)
                {
                    var position = alphabet.IndexOf(input[i]);
                    number += (long)(position * Math.Pow(alphabet.Length, input.Length - i - 1));
                }
            }
            
            return number;
        }

        private long[] GetNumbersFrom(string hash)
        {
            if (string.IsNullOrWhiteSpace(hash))
            {
                throw new ArgumentException(nameof(hash));
            }

            var alphabet = new string(_alphabet.ToCharArray());
            var result = new List<long>();
            var i = 0;

            var hashBreakdown = _guardsRegex.Replace(hash, " ");
            var hashArray = hashBreakdown.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (hashArray.Length == 3 || hashArray.Length == 2)
            {
                i = 1;
            }

            hashBreakdown = hashArray[i];

            if (hashBreakdown[0] == default(char))
            {
                return result.ToArray();
            }

            var lottery = hashBreakdown[0];
            hashBreakdown = hashBreakdown.Substring(1);

            hashBreakdown = _separatorsRegex.Replace(hashBreakdown, " ");
            hashArray = hashBreakdown.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var subHash in hashArray)
            {
                var buffer = lottery + _salt + alphabet;

                alphabet = ConsistentShuffle(alphabet, buffer.Substring(0, alphabet.Length));

                var unhashed = UnHash(subHash, alphabet);

                result.Add(unhashed);
            }

            if (EncodeLong(result.ToArray()) != hash)
            {
                throw new ArgumentException($"hash \"{hash}\" is incorrect!");
            }

            return result.ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="alphabet"></param>
        /// <param name="salt"></param>
        /// <returns></returns>
        private static string ConsistentShuffle(string alphabet, string salt)
        {
            if (string.IsNullOrWhiteSpace(salt))
            {
                return alphabet;
            }

            var letters = alphabet.ToCharArray();
            for (int i = letters.Length - 1, v = 0, p = 0; i > 0; i--, v++)
            {
                int n;
                v %= salt.Length;
                p += n = salt[v];
                var j = (n + v + p) % i;
                // swap characters at positions i and j
                var temp = letters[j];
                letters[j] = letters[i];
                letters[i] = temp;
            }

            return new string(letters);
        }
    }
}