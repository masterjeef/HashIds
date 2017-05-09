﻿using System;
using System.Collections.Generic;
using Xunit;
using FluentAssertions;

namespace HashidsNet.test
{
    public class HashIds_issues
    {
        [Fact]
        void issue_8_should_not_throw_out_of_range_exception()
        {
            var hashids = new HashIds("janottaa", 6);
            var numbers = hashids.Decode("NgAzADEANAA=");
        }

        // This issue came from downcasting to int at the wrong place,
        // seems to happen when you are encoding A LOT of longs at the same time.
        // see if it is possible to make this a faster test (or remove it since it is unlikely that it will reapper).
        [Fact]
        void issue_12_should_not_throw_out_of_range_exception()
        {
            var hash = new HashIds("zXZVFf2N38uV");
            var longs = new List<long>();
            var rand = new Random();
            var valueBuffer = new byte[8];
            var randLong = 0L;
            for (var i = 0; i < 100000; i++)
            {
                rand.NextBytes(valueBuffer);
                randLong = BitConverter.ToInt64(valueBuffer, 0);
                longs.Add(Math.Abs(randLong));
            }

            var encoded = hash.EncodeLong(longs);
            var decoded = hash.DecodeLong(encoded);
            decoded.Should().Equal(longs.ToArray());
        }

        [Fact(Skip = "Fix me later")]
        void issue_14_it_should_decode_encode_hex_correctly()
        {
            var hashids = new HashIds("this is my salt");
            var encoded = hashids.EncodeHex("DEADBEEF");
            encoded.Should().Be("kRNrpKlJ");

            var decoded = hashids.DecodeHex(encoded);
            decoded.Should().Be("DEADBEEF");

            var encoded2 = hashids.EncodeHex("1234567890ABCDEF");
            var decoded2 = hashids.DecodeHex(encoded2);
            decoded2.Should().Be("1234567890ABCDEF");
        }

        [Fact(Skip = "Fix me later")]
        void issue_18_it_should_return_empty_string_if_negative_numbers()
        {
            var hashids = new HashIds("this is my salt");
            hashids.Encode(1, 4, 5, -3).Should().Be(string.Empty);
            hashids.EncodeLong(4, 5, 2, -4).Should().Be(string.Empty);
        }

        [Fact(Skip = "Fix me later")]
        void issue_15_it_should_return_emtpy_array_when_decoding_characters_missing_in_alphabet()
        {
            var hashids = new HashIds(salt: "Salty stuff", alphabet: "qwerty1234!¤%&/()=", separators: "1234");
            var numbers = hashids.Decode("abcd");
            numbers.Length.Should().Be(0);

            var hashids2 = new HashIds();
            hashids.Decode("13-37").Length.Should().Be(0);
            hashids.DecodeLong("32323kldffd!").Length.Should().Be(0);

            var hashids3 = new HashIds(alphabet: "1234567890;:_!#¤%&/()=", separators: "!#¤%&/()=");
            hashids.Decode("asdfb").Length.Should().Be(0);
            hashids.DecodeLong("asdfgfdgdfgkj").Length.Should().Be(0);
        }
    }
}
