using System;
using FluentAssertions;
using Moq;
using Xunit;

namespace HashidsNet.test
{
    public class HashIds_test
    {
        HashIds _hashIds;
        private string _salt = "this is my salt";
        private string _defaultAlphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
        private string _defaultSeparators = "cfhistuCFHISTU";

        public HashIds_test()
        {
            _hashIds = new HashIds(_salt);
        }

        [Fact]
        public void Default_alphabet_should_be_correct()
        {
            HashIds.DefaultAlphabet.Should().Be(_defaultAlphabet);
        }

        [Fact]
        public void Default_separators_should_be_correct()
        {
            HashIds.DefaultSeparators.Should().Be(_defaultSeparators);
        }

        [Fact]
        public void Default_salt_should_be_correct()
        {
            new HashIds().Encode(1,2,3).Should().Be("o2fXhV");
        }

        [Theory]
        [InlineData(1, "NV")]
        [InlineData(22, "K4")]
        [InlineData(333, "OqM")]
        [InlineData(9999, "kQVg")]
        [InlineData(123000, "58LzD")]
        [InlineData(456000000, "5gn6mQP")]
        [InlineData(987654321, "oyjYvry")]
        public void Encode_single_number(int value, string expected)
        {
            _hashIds.Encode(value).Should().Be(expected);
        }

        [Theory]
        [InlineData(1L, "NV")]
        [InlineData(2147483648L, "21OjjRK")]
        [InlineData(4294967296L, "D54yen6")]
        [InlineData(666555444333222L, "KVO9yy1oO5j")]
        [InlineData(12345678901112L, "4bNP1L26r")]
        [InlineData(Int64.MaxValue, "jvNx4BjM5KYjv")]
        public void Encode_a_single_long(long value, string expected)
        {
            _hashIds.EncodeLong(value).Should().Be(expected);
        }

        [Theory]
        [InlineData("laHquq", 1, 2, 3)]
        [InlineData("44uotN", 2, 4, 6)]
        [InlineData("97Jun", 99, 25)]
        [InlineData("1Wc8cwcE", 5, 5, 5, 5)]
        [InlineData("7xKhrUxm", 1337, 42, 314)]
        [InlineData("aBMswoO2UB3Sj", 683, 94108, 123, 5)]
        [InlineData("kRHnurhptKcjIDTWC3sx", 1, 2, 3, 4, 5, 6, 7, 8, 9, 10)]
        [InlineData("3RoSDhelEyhxRsyWpCx5t1ZK", 547, 31, 241271, 311, 31397, 1129, 71129)]
        [InlineData("p2xkL3CK33JjcrrZ8vsw4YRZueZX9k", 21979508, 35563591, 57543099, 93106690, 150649789)]
        public void Encode_a_list_of_numbers(string expected, params int [] numbers)
        {
            _hashIds.Encode(numbers).Should().Be(expected);
        }

        [Fact]
        public void Encode_a_list_of_longs()
        {
            _hashIds.EncodeLong(666555444333222L, 12345678901112L).Should().Be("mPVbjj7yVMzCJL215n69");
        }

        [Fact]
        public void Should_throw_if_no_numbers()
        {
            Action act = () => _hashIds.Encode();

            act.ShouldThrow<ArgumentException>();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(4140, 21147, 115975, 678570, 4213597, 27644437)]
        public void Encode_to_a_minimum_length(params int[] numbers)
        {
            const int minLength = 18;
            var hashIds = new HashIds(_salt, minLength);

            var result = hashIds.Encode(numbers);
            result.Length.Should().BeGreaterOrEqualTo(18);
        }

        [Theory]
        [InlineData("6nhmFDikA0", "ABCDEFGhijklmn34567890-:", 1, 2, 3, 4, 5)]
        public void Encode_with_a_custom_alphabet(string expected, string alphabet, params int[] numbers)
        {
            var hashIds = new HashIds(_salt, 0, alphabet);

            hashIds.Encode(numbers).Should().Be(expected);
        }
        

        [Fact]
        public void Incrementing_number_hashes_are_not_similar()
        {
            // need a better way to test this /:
            _hashIds.Encode(1).Should().Be("NV");
            _hashIds.Encode(2).Should().Be("6m");
            _hashIds.Encode(3).Should().Be("yD");
            _hashIds.Encode(4).Should().Be("2l");
            _hashIds.Encode(5).Should().Be("rD");
        }

        [Theory]
        [InlineData("FA", "l5l")]
        [InlineData("26dd", "rD7D")]
        [InlineData("FF1A", "lB8P")]
        [InlineData("12abC", "3mve")]
        [InlineData("185b0", "n29yE")]
        [InlineData("17b8d", "orxZN")]
        [InlineData("1d7f21dd38", "9e45pXZa")]
        [InlineData("20015111d", "ba19nW4N")]
        public void Encode_hex_string(string hex, string expected)
        {
            _hashIds.EncodeHex(hex).Should().Be(expected);
        }

        [Fact]
        public void Should_throw_on_non_hex_string()
        {
            Action act = () =>_hashIds.EncodeHex("XYZ123");

            act.ShouldThrow<ArgumentException>();
        }

        [Theory]
        [InlineData("NkK9", 12345)]
        [InlineData("5O8yp5P", 666555444)]
        [InlineData("Wzo", 1337)]
        [InlineData("DbE", 808)]
        [InlineData("yj8", 303)]
        public void Should_decode_an_encoded_number(string encoded, params int [] decoded)
        {
            _hashIds.Decode(encoded).Should().Equal(decoded);
        }

        [Theory]
        [InlineData("NV", 1L)]
        [InlineData("21OjjRK", 2147483648L)]
        [InlineData("D54yen6", 4294967296L)]
        [InlineData("KVO9yy1oO5j", 666555444333222L)]
        [InlineData("4bNP1L26r", 12345678901112L)]
        [InlineData("jvNx4BjM5KYjv", Int64.MaxValue)]
        [InlineData("mPVbjj7yVMzCJL215n69", 666555444333222L, 12345678901112L)]
        public void Should_decode_an_encoded_long(string encoded, params long[] decoded)
        {
            _hashIds.DecodeLong(encoded).Should().Equal(decoded);
        }

        [Theory]
        [InlineData("1gRYUwKxBgiVuX", 66655, 5444333, 2, 22)]
        [InlineData("aBMswoO2UB3Sj", 683, 94108, 123, 5)]
        [InlineData("jYhp", 3, 4)]
        [InlineData("k9Ib", 6, 5)]
        [InlineData("EMhN", 31, 41)]
        [InlineData("glSgV", 13, 89)]
        public void Should_decode_a_list_of_encoded_numbers(string encoded, params int[] decoded)
        {
            _hashIds.Decode(encoded).Should().Equal(decoded);
        }
        
        [Fact]
        // TODO: this should probably throw
        public void Should_not_decode_with_a_different_salt()
        {
            var peppers = new HashIds("this is my pepper");
            _hashIds.Decode("NkK9").Should().Equal(new []{ 12345 });
            peppers.Decode("NkK9").Should().Equal(new int [0]);
        }

        [Theory]
        [InlineData("gB0NV05e", 1)]
        [InlineData("mxi8XH87", 25, 100, 950)]
        [InlineData("KQcmkIW8hX", 5, 200, 195, 1)]
        public void Should_decode_from_a_hash_with_a_minimum_length(string encoded, params int [] decoded)
        {
            const int minLength = 8;
            var h = new HashIds(_salt, minLength);
            h.Decode(encoded).Should().Equal(decoded);
        }

        [Theory]
        [InlineData("lzY", "1FA")]
        [InlineData("eBMrb", "1FF1A")]
        [InlineData("D9NPE", "112ABC")]

        public void Should_decode_hex(string encodedHex, string decodedHex)
        {
            _hashIds.DecodeHex(encodedHex).Should().Be(decodedHex);
        }

        [Fact]
        public void Should_raise_argument_null_exception_when_alphabet_is_null()
        {
            Action invocation = () => new HashIds(alphabet: null);
            invocation.ShouldThrow<ArgumentNullException>();
        }

        [Fact(Skip = "Fix me later")]
        public void Should_raise_an_argument_null_exception_if_alphabet_contains_less_than_4_unique_characters()
        {
            Action invocation = () => new HashIds(alphabet: "aadsss");
            invocation.ShouldThrow<ArgumentException>();
        }

        [Fact]
        public void Should_encode_and_decode_numbers_starting_with_0()
        {
            var hash = _hashIds.Encode(0, 1, 2);
            _hashIds.Decode(hash).Should().Equal(new[] { 0, 1, 2 });
        }

        [Fact]
        public void Should_encode_and_decode_numbers_ending_with_0()
        {
            var hash = _hashIds.Encode(1, 2, 0);
            _hashIds.Decode(hash).Should().Equal(new[] { 1, 2, 0 });
        }

        [Fact(Skip = "Fix me later")]
        public void Public_methods_can_be_mocked()
        {
            var mock = new Mock<HashIds>();
            mock.Setup(hashids => hashids.Encode(It.IsAny<int[]>())).Returns("It works");
            mock.Object.Encode(new[] { 1 }).Should().Be("It works");
        }

        [Fact]
        public void Should_decode_encode_DEADBEEF_correctly()
        {
            var hashids = new HashIds("this is my salt");
            var encoded = hashids.EncodeHex("DEADBEEF");
            encoded.Should().Be("zEMBllj");

            var decoded = hashids.DecodeHex(encoded);
            decoded.Should().Be("DEADBEEF");
        }
    }
}
