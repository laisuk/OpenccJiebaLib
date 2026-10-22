using System;
using System.Text;

namespace OpenccJiebaLib
{
    internal static class DictionaryValidation
    {
        private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);

        internal static void Text(string value, string parameter)
        {
            if (value == null) throw new ArgumentNullException(parameter);
            if (value.IndexOf('\0') >= 0)
                throw new ArgumentException("Text must not contain NUL characters.", parameter);
            try
            {
                _ = StrictUtf8.GetByteCount(value);
            }
            catch (EncoderFallbackException error)
            {
                throw new ArgumentException("Text must contain valid UTF-16.", parameter, error);
            }
        }

        internal static void WordAndTag(string word, string tag)
        {
            Text(word, nameof(word));
            if (word.Length == 0 || ContainsWhitespace(word))
                throw new ArgumentException("Word must not be empty or contain whitespace.", nameof(word));
            if (tag == null) return;
            Text(tag, nameof(tag));
            if (ContainsWhitespace(tag))
                throw new ArgumentException("Tag must not contain whitespace.", nameof(tag));
        }

        private static bool ContainsWhitespace(string value)
        {
            foreach (var character in value)
                if (char.IsWhiteSpace(character))
                    return true;
            return false;
        }

        internal static UIntPtr Frequency(ulong value)
        {
            if (UIntPtr.Size == 4 && value > uint.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(value), value, "Frequency exceeds native size_t.");
            return new UIntPtr(value);
        }

        internal static byte[] Utf8Z(string value)
        {
            if (value == null) return null;
            var bytes = new byte[checked(StrictUtf8.GetByteCount(value) + 1)];
            StrictUtf8.GetBytes(value, 0, value.Length, bytes, 0);
            return bytes;
        }
    }
}