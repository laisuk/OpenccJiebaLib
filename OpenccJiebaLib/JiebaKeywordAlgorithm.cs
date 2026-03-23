using System;

namespace OpenccJiebaLib
{
    /// <summary>
    /// Represents the keyword extraction algorithms supported by Jieba.
    /// </summary>
    /// <remarks>
    /// These algorithms are used to extract the most important keywords from a given text.
    /// </remarks>
    public enum JiebaKeywordAlgorithm
    {
        /// <summary>
        /// TF-IDF (Term Frequency–Inverse Document Frequency) algorithm.
        /// </summary>
        /// <remarks>
        /// Suitable for extracting keywords based on statistical importance.
        /// Works well for general-purpose keyword extraction.
        /// </remarks>
        Tfidf = 1,

        /// <summary>
        /// TextRank graph-based ranking algorithm.
        /// </summary>
        /// <remarks>
        /// Uses a graph-based ranking model similar to PageRank.
        /// Often produces more context-aware and phrase-level keywords.
        /// </remarks>
        TextRank = 2,
    }

    /// <summary>
    /// Provides helper methods for parsing and mapping <see cref="JiebaKeywordAlgorithm"/> values.
    /// </summary>
    /// <remarks>
    /// Supports case-insensitive parsing and common alias formats such as
    /// <c>"tf-idf"</c>, <c>"tf_idf"</c>, <c>"text-rank"</c>, and <c>"text_rank"</c>.
    /// </remarks>
    public static class KeywordAlgorithmExtensions
    {
        /// <summary>
        /// Attempts to parse a keyword algorithm name into a <see cref="JiebaKeywordAlgorithm"/> value.
        /// </summary>
        /// <param name="value">
        /// The algorithm name to parse. Case-insensitive.
        /// Supported values include:
        /// <list type="bullet">
        /// <item><description><c>"tfidf"</c>, <c>"tf-idf"</c>, <c>"tf_idf"</c></description></item>
        /// <item><description><c>"textrank"</c>, <c>"text-rank"</c>, <c>"text_rank"</c></description></item>
        /// </list>
        /// </param>
        /// <param name="algorithm">
        /// When this method returns, contains the parsed <see cref="JiebaKeywordAlgorithm"/> value
        /// if the parsing succeeded; otherwise, the default value.
        /// </param>
        /// <returns>
        /// <c>true</c> if parsing succeeded; otherwise, <c>false</c>.
        /// </returns>
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
        /// Parses a keyword algorithm name into a <see cref="JiebaKeywordAlgorithm"/> value.
        /// </summary>
        /// <param name="value">
        /// The algorithm name to parse. Case-insensitive.
        /// See <see cref="TryParse"/> for supported formats.
        /// </param>
        /// <returns>
        /// The corresponding <see cref="JiebaKeywordAlgorithm"/> value.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="value"/> is null, empty, or not a valid algorithm name.
        /// </exception>
        public static JiebaKeywordAlgorithm Parse(string value)
        {
            return !TryParse(value, out var algo)
                ? throw new ArgumentException("Invalid keyword algorithm: " + value, nameof(value))
                : algo;
        }

        /// <summary>
        /// Converts the specified <see cref="JiebaKeywordAlgorithm"/> to its canonical native method name.
        /// </summary>
        /// <param name="algorithm">The keyword algorithm.</param>
        /// <returns>
        /// A lowercase string representing the native method name used by the underlying C API
        /// (e.g., <c>"tfidf"</c> or <c>"textrank"</c>).
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="algorithm"/> is not a valid enum value.
        /// </exception>
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