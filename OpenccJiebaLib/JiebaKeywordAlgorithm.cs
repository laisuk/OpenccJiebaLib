using System;

namespace OpenccJiebaLib
{
    /// <summary>
    /// Keyword extraction algorithms supported by Jieba.
    /// </summary>
    public enum JiebaKeywordAlgorithm
    {
        /// <summary>TF-IDF (Term Frequency–Inverse Document Frequency).</summary>
        Tfidf = 1,

        /// <summary>TextRank graph-based ranking algorithm.</summary>
        TextRank = 2,
    }

    /// <summary>
    /// Helpers for <see cref="JiebaKeywordAlgorithm"/> parsing and native mapping.
    /// </summary>
    public static class KeywordAlgorithmExtensions
    {
        /// <summary>
        /// Tries to parse a keyword algorithm name (case-insensitive) and common aliases.
        /// </summary>
        public static bool TryParse(string value, out JiebaKeywordAlgorithm algorithm)
        {
            algorithm = default;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            var s = value.Trim();

            // fast path for common exact forms
            if (string.Equals(s, "tfidf", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s, "tf-idf", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s, "tf_idf", StringComparison.OrdinalIgnoreCase))
            {
                algorithm = JiebaKeywordAlgorithm.Tfidf;
                return true;
            }

            if (!string.Equals(s, "textrank", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(s, "text-rank", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(s, "text_rank", StringComparison.OrdinalIgnoreCase)) return false;
            algorithm = JiebaKeywordAlgorithm.TextRank;
            return true;

        }

        /// <summary>
        /// Parses a keyword algorithm name (case-insensitive). Throws if invalid.
        /// </summary>
        public static JiebaKeywordAlgorithm Parse(string value)
        {
            return !TryParse(value, out var algo) ? throw new ArgumentException("Invalid keyword algorithm: " + value, nameof(value)) : algo;
        }

        /// <summary>
        /// Returns the canonical native method name used by the C API.
        /// </summary>
        public static string ToNativeMethod(this JiebaKeywordAlgorithm algorithm)
        {
            switch (algorithm)
            {
                case JiebaKeywordAlgorithm.Tfidf:
                    return "tfidf";
                case JiebaKeywordAlgorithm.TextRank:
                    return "textrank";
                default:
                    throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, "Unknown algorithm.");
            }
        }
    }
}