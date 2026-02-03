using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace OpenccJiebaLib
{
    /// <summary>
    /// Provides a managed wrapper for OpenCC and Jieba C API functions, enabling Chinese text conversion and segmentation.
    /// </summary>
    /// <remarks>
    /// This class manages the native OpenCC/Jieba instance and exposes methods for text conversion, segmentation, and keyword extraction.
    /// </remarks>
    public sealed class OpenccJieba : IDisposable
    {
        private IntPtr _openccInstance; // Native instance pointer
        private bool _disposed; // Tracks whether Dispose has been called

        // Pre-encoded config bytes for common configurations (canonical lowercase -> UTF-8 nul-terminated)
        private static readonly Dictionary<string, byte[]> EncodedConfigCache =
            new Dictionary<string, byte[]>(capacity: 16, comparer: StringComparer.Ordinal);

        // Keyword algorithm names (UTF-8, null-terminated) for native interop.
        private static readonly byte[] TextrankMethodBytes =
        {
            (byte)'t', (byte)'e', (byte)'x', (byte)'t',
            (byte)'r', (byte)'a', (byte)'n', (byte)'k', 0
        };

        private static readonly byte[] TfidfMethodBytes =
        {
            (byte)'t', (byte)'f', (byte)'i', (byte)'d', (byte)'f', 0
        };

        static OpenccJieba()
        {
            // Single source of truth: OpenccConfig enum values.
            // Populate cache with canonical config names.
            foreach (OpenccConfig id in Enum.GetValues(typeof(OpenccConfig)))
            {
                if (!OpenccConfigExtensions.TryGetConfigName(id, out var name))
                    continue;

                // name is canonical lowercase (e.g. "s2t")
                if (EncodedConfigCache.ContainsKey(name))
                    continue;

                var byteCount = Encoding.UTF8.GetByteCount(name);
                var encodedBytes = new byte[byteCount + 1];
                Encoding.UTF8.GetBytes(name, 0, name.Length, encodedBytes, 0);
                encodedBytes[byteCount] = 0x00;

                EncodedConfigCache[name] = encodedBytes;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenccJieba"/> class and allocates the native resources.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the native instance cannot be initialized.</exception>
        public OpenccJieba()
        {
            _openccInstance = OpenccJiebaNative.opencc_jieba_new();
            if (_openccInstance == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to initialize native OpenCC/Jieba instance.");
            }
        }

        /// <summary>
        /// Releases all resources used by the <see cref="OpenccJieba"/> instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        /// <param name="disposing">True if called from Dispose; false if called from finalizer.</param>
        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // Free any other managed objects here.
            }

            // Free unmanaged resources
            if (_openccInstance != IntPtr.Zero)
            {
                OpenccJiebaNative.opencc_jieba_delete(_openccInstance);
                _openccInstance = IntPtr.Zero;
            }

            _disposed = true;
        }

        /// <summary>
        /// Finalizer to ensure native resources are released if Dispose is not called.
        /// </summary>
        ~OpenccJieba()
        {
            Dispose(disposing: false);
        }
        
        /// <summary>
        /// Gets the numeric ABI version of the underlying native Opencc-Jieba library.
        /// </summary>
        /// <remarks>
        /// The ABI number represents the native binary interface version and is used
        /// to ensure compatibility between this managed wrapper and the native library.
        /// <para/>
        /// A change in ABI number indicates a breaking change at the native interface level.
        /// </remarks>
        /// <returns>
        /// An integer representing the native ABI version.
        /// </returns>
        public static int GetNativeAbiNumber()
        {
            return (int)OpenccJiebaNative.opencc_jieba_abi_number();
        }

        /// <summary>
        /// Gets the version string of the underlying native Opencc-Jieba library.
        /// </summary>
        /// <remarks>
        /// The returned value is a semantic version string in the form <c>x.y.z</c>
        /// (for example, <c>0.7.3</c>), identifying the native library build.
        /// <para/>
        /// This value is intended for diagnostics, logging, and display purposes.
        /// </remarks>
        /// <returns>
        /// A semantic version string (<c>x.y.z</c>) reported by the native library.
        /// </returns>
        public static string GetNativeVersionString()
        {
            return Utf8BytesToString(OpenccJiebaNative.opencc_jieba_version_string());
        }

        /// <summary>
        /// Converts Chinese text using the specified OpenCC configuration, optionally including punctuation conversion.
        /// This method validates the instance state, normalizes the configuration (defaulting to <c>"s2t"</c> if unknown),
        /// and calls the native converter using pooled UTF-8 buffers for efficiency.
        /// </summary>
        /// <param name="input">The input string to convert.</param>
        /// <param name="config">
        /// The OpenCC configuration name (e.g., <c>"s2t"</c>, <c>"t2s"</c>). If the value is not recognized,
        /// the method falls back to <c>"s2t"</c>.
        /// </param>
        /// <param name="punctuation">Whether to convert punctuation as well.</param>
        /// <returns>The converted string; returns an empty string if <paramref name="input"/> is null or empty.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if this instance has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the native instance is not initialized or has been disposed.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown if the (normalized) configuration cannot be resolved to a cached byte sequence.
        /// </exception>
        /// <remarks>
        /// Implementation details:
        /// <list type="bullet">
        /// <item>Uses <see cref="ArrayPool{T}"/> to rent a UTF-8 buffer and appends a null terminator for the native API.</item>
        /// <item>Ensures native output memory is freed via <c>opencc_jieba_free_string</c> in a <c>finally</c> block.</item>
        /// <item>Relies on a precomputed <c>EncodedConfigCache</c> (config → UTF-8 bytes) for fast lookups.</item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <code>
        /// using (var converter = new OpenccJieba())
        /// {
        ///     string original = "汉字简繁转换";
        ///     string converted = converter.Convert(original, "s2t", punctuation: true);
        ///     Console.WriteLine(converted); // Output: 漢字簡繁轉換
        /// }
        /// </code>
        /// </example>
        public string Convert(string input, string config, bool punctuation = false)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(OpenccJieba));
            if (string.IsNullOrEmpty(input)) return string.Empty;

            // Normalize/validate config with a safe default (case-insensitive).
            if (!OpenccConfigExtensions.TryParseConfig(config, out var configId))
                configId = OpenccConfig.S2T;

            config = configId.ToCanonicalName();

            if (_openccInstance == IntPtr.Zero)
                throw new InvalidOperationException("Native instance is not initialized or has been disposed.");

            byte[] inputBytes = null;
            var output = IntPtr.Zero;

            try
            {
                // Defensive fallback: should never fail after normalization,
                // but fall back to default config bytes instead of throwing.
                if (!EncodedConfigCache.TryGetValue(config, out var configBytes))
                {
                    var defaultConfig = OpenccConfigExtensions.DefaultConfig().ToCanonicalName();
                    configBytes = EncodedConfigCache[defaultConfig];
                }

                // Pooled UTF-8 + NUL input buffer
                RentUtf8Z(input, out inputBytes);

                output = OpenccJiebaNative.opencc_jieba_convert(_openccInstance, inputBytes, configBytes, punctuation);
                return Utf8BytesToString(output);
            }
            finally
            {
                ReturnRented(inputBytes);

                if (output != IntPtr.Zero)
                    OpenccJiebaNative.opencc_jieba_free_string(output);
            }
        }

        /// <summary>
        /// Converts Chinese text using the specified OpenCC configuration enum,
        /// optionally including punctuation conversion.
        /// </summary>
        /// <param name="input">The input string to convert.</param>
        /// <param name="configId">
        /// The OpenCC configuration identifier (managed enum). If invalid, the method falls back to
        /// <see cref="OpenccConfig.S2T"/>.
        /// </param>
        /// <param name="punctuation">Whether to convert punctuation as well.</param>
        /// <returns>The converted string; returns an empty string if <paramref name="input"/> is null or empty.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if this instance has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the native instance is not initialized or has been disposed.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown if the (normalized) configuration cannot be resolved to a cached byte sequence.
        /// </exception>
        /// <remarks>
        /// This overload is a convenience wrapper that maps <see cref="OpenccConfig"/> to its canonical
        /// OpenCC config name (e.g. "s2t", "t2s") and forwards to <see cref="Convert(string,string,bool)"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// using (var converter = new OpenccJieba())
        /// {
        ///     string original = "汉字简繁转换";
        ///     string converted = converter.Convert(original, OpenccConfig.S2T, punctuation: true);
        ///     Console.WriteLine(converted); // Output: 漢字簡繁轉換
        /// }
        /// </code>
        /// </example>
        public string Convert(string input, OpenccConfig configId, bool punctuation = false)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(OpenccJieba));
            if (string.IsNullOrEmpty(input)) return string.Empty;

            // Normalize/validate configId with a safe default.
            if (!OpenccConfigExtensions.TryGetConfigName(configId, out _))
                configId = OpenccConfigExtensions.DefaultConfig();

            // Forward using canonical name; avoids parsing in string overload.
            return Convert(input, configId.ToCanonicalName(), punctuation);
        }

        /// <summary>
        /// Detects whether the input text contains Chinese (ZHO) characters,
        /// and identifies the dominant script type.
        /// </summary>
        /// <param name="input">
        /// Input text to analyze.
        /// </param>
        /// <returns>
        /// An integer code indicating the detected script:
        /// <list type="table">
        ///   <listheader>
        ///     <term>Value</term>
        ///     <description>Meaning</description>
        ///   </listheader>
        ///   <item>
        ///     <term>0</term>
        ///     <description>Non-Chinese or mixed / undetermined text</description>
        ///   </item>
        ///   <item>
        ///     <term>1</term>
        ///     <description>Traditional Chinese (zh-Hant)</description>
        ///   </item>
        ///   <item>
        ///     <term>2</term>
        ///     <description>Simplified Chinese (zh-Hans)</description>
        ///   </item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// This method performs a lightweight language/script check using the native
        /// OpenCC-Jieba engine.
        /// A temporary native instance is created and released for each call.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">If the instance has been disposed.</exception>
        public int ZhoCheck(string input)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(OpenccJieba));
            if (string.IsNullOrEmpty(input)) return 0;

            if (_openccInstance == IntPtr.Zero)
                throw new InvalidOperationException("Native instance is not initialized or has been disposed.");

            byte[] inputBytes = null;

            try
            {
                RentUtf8Z(input, out inputBytes);
                return OpenccJiebaNative.opencc_jieba_zho_check(_openccInstance, inputBytes);
            }
            finally
            {
                ReturnRented(inputBytes);
            }
        }

        /// <summary>
        /// Performs Jieba word segmentation (tokenization) on the input text.
        /// </summary>
        /// <param name="input">
        /// Input text to tokenize.
        /// </param>
        /// <param name="hmm">
        /// Whether to enable HMM-based segmentation.
        /// When enabled, unknown words may be inferred using a Hidden Markov Model.
        /// </param>
        /// <returns>
        /// An array of segmented tokens.
        /// Returns <see cref="Array.Empty{String}"/> if the input is empty
        /// or if the native tokenizer returns no result.
        /// </returns>
        /// <remarks>
        /// This method uses the native Jieba tokenizer via OpenCC-Jieba.
        /// A temporary native instance is created and released for each call.
        /// The returned tokens preserve the original text order.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">If the instance has been disposed.</exception>
        public string[] JiebaCut(string input, bool hmm)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(OpenccJieba));

            if (string.IsNullOrEmpty(input))
                return Array.Empty<string>();

            if (_openccInstance == IntPtr.Zero)
                throw new InvalidOperationException("Native instance is not initialized or has been disposed.");

            byte[] inputBytes = null;
            var result = IntPtr.Zero;

            try
            {
                RentUtf8Z(input, out inputBytes);

                result = OpenccJiebaNative.opencc_jieba_cut(_openccInstance, inputBytes, hmm);
                return result == IntPtr.Zero ? Array.Empty<string>() : MarshalNullTerminatedStringArray(result);
            }
            finally
            {
                ReturnRented(inputBytes);

                if (result != IntPtr.Zero)
                    OpenccJiebaNative.opencc_jieba_free_string_array(result);
            }
        }

        /// <summary>
        /// Performs Chinese word segmentation and joins the result with a delimiter.
        /// </summary>
        /// <param name="input">The input string to segment.</param>
        /// <param name="hmm">Whether to use the Hidden Markov Model (HMM) for segmentation.</param>
        /// <param name="delimiter">The delimiter to use for joining the segmented words.</param>
        /// <returns>A single string with segmented words joined by the delimiter.</returns>
        /// <exception cref="ObjectDisposedException">If the instance has been disposed.</exception>
        public string JiebaCutAndJoin(string input, bool hmm, string delimiter)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(OpenccJieba));

            if (string.IsNullOrEmpty(input))
                return string.Empty;

            if (_openccInstance == IntPtr.Zero)
                throw new InvalidOperationException("Native instance is not initialized or has been disposed.");

            // Prefer predictable behavior: treat null delimiter as empty delimiter.
            if (delimiter == null)
                delimiter = string.Empty;

            byte[] inputBytes = null;
            var delimiterBytes = StringToUtf8BytesZ(delimiter);
            var resultPtr = IntPtr.Zero;

            try
            {
                RentUtf8Z(input, out inputBytes);

                resultPtr = OpenccJiebaNative.opencc_jieba_cut_and_join(
                    _openccInstance, inputBytes, hmm, delimiterBytes);

                if (resultPtr == IntPtr.Zero)
                    return string.Empty;

                return Utf8BytesToString(resultPtr) ?? string.Empty;
            }
            finally
            {
                ReturnRented(inputBytes);

                if (resultPtr != IntPtr.Zero)
                    OpenccJiebaNative.opencc_jieba_free_string(resultPtr);
            }
        }

        /// <summary>
        /// Extracts keywords from the input text using the Jieba TextRank algorithm.
        /// </summary>
        /// <param name="input">
        /// Input text from which keywords will be extracted.
        /// </param>
        /// <param name="topK">
        /// Maximum number of keywords to return.
        /// If the value is less than or equal to zero, no keywords are returned.
        /// </param>
        /// <returns>
        /// An array of extracted keywords ordered by relevance (highest first).
        /// Returns <see cref="Array.Empty{String}"/> if the input is empty
        /// or if the native extractor returns no result.
        /// </returns>
        /// <remarks>
        /// TextRank is a graph-based ranking algorithm that does not rely on
        /// term frequency statistics and is suitable for short or well-structured texts.
        ///
        /// This method uses the native OpenCC-Jieba instance owned by this object.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">If the instance has been disposed.</exception>
        /// <exception cref="InvalidOperationException">If the native instance is not initialized.</exception>
        public string[] JiebaKeywordExtractTextRank(string input, int topK)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(OpenccJieba));

            if (string.IsNullOrEmpty(input) || topK <= 0)
                return Array.Empty<string>();

            if (_openccInstance == IntPtr.Zero)
                throw new InvalidOperationException("Native instance is not initialized or has been disposed.");

            byte[] inputBytes = null;
            var result = IntPtr.Zero;

            try
            {
                RentUtf8Z(input, out inputBytes);

                result = OpenccJiebaNative.opencc_jieba_keywords(
                    _openccInstance, inputBytes, (UIntPtr)topK, TextrankMethodBytes);

                return result == IntPtr.Zero ? Array.Empty<string>() : MarshalNullTerminatedStringArray(result);
            }
            finally
            {
                ReturnRented(inputBytes);

                if (result != IntPtr.Zero)
                    OpenccJiebaNative.opencc_jieba_free_string_array(result);
            }
        }

        /// <summary>
        /// Extracts keywords from the input text using the Jieba TF-IDF algorithm.
        /// </summary>
        /// <param name="input">
        /// Input text from which keywords will be extracted.
        /// </param>
        /// <param name="topK">
        /// Maximum number of keywords to return.
        /// If the value is less than or equal to zero, no keywords are returned.
        /// </param>
        /// <returns>
        /// An array of extracted keywords ordered by importance (highest first).
        /// Returns <see cref="Array.Empty{String}"/> if the input is empty
        /// or if the native extractor returns no result.
        /// </returns>
        /// <remarks>
        /// TF-IDF (Term Frequency–Inverse Document Frequency) ranks keywords
        /// based on term frequency and inverse document frequency statistics.
        ///
        /// Compared to TextRank, TF-IDF tends to favor frequently occurring terms
        /// and is well-suited for longer or content-heavy texts.
        ///
        /// This method uses the native OpenCC-Jieba instance owned by this object.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">If the instance has been disposed.</exception>
        /// <exception cref="InvalidOperationException">If the native instance is not initialized.</exception>
        public string[] JiebaKeywordExtractTfidf(string input, int topK)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(OpenccJieba));

            if (string.IsNullOrEmpty(input) || topK <= 0)
                return Array.Empty<string>();

            if (_openccInstance == IntPtr.Zero)
                throw new InvalidOperationException("Native instance is not initialized or has been disposed.");

            byte[] inputBytes = null;
            var result = IntPtr.Zero;

            try
            {
                RentUtf8Z(input, out inputBytes);

                result = OpenccJiebaNative.opencc_jieba_keywords(
                    _openccInstance, inputBytes, (UIntPtr)topK, TfidfMethodBytes);

                return result == IntPtr.Zero ? Array.Empty<string>() : MarshalNullTerminatedStringArray(result);
            }
            finally
            {
                ReturnRented(inputBytes);

                if (result != IntPtr.Zero)
                    OpenccJiebaNative.opencc_jieba_free_string_array(result);
            }
        }

        /// <summary>
        /// Extracts top keywords and their corresponding weights from the input text
        /// using the specified Jieba keyword extraction method.
        /// </summary>
        /// <param name="input">
        /// Input text from which keywords will be extracted.
        /// </param>
        /// <param name="topK">
        /// Maximum number of keywords to return.
        /// If the value is less than or equal to zero, no keywords are returned.
        /// </param>
        /// <param name="method">
        /// Keyword extraction method name (case-insensitive).
        /// Common values include <c>"tfidf"</c> and <c>"textrank"</c>
        /// (aliases such as <c>"tf-idf"</c>, <c>"tf_idf"</c>, <c>"text-rank"</c>, <c>"text_rank"</c>
        /// are also accepted).
        /// </param>
        /// <returns>
        /// A tuple containing:
        /// <list type="bullet">
        ///   <item><description><c>keywords</c>: extracted keywords ordered by relevance (highest first)</description></item>
        ///   <item><description><c>weights</c>: keyword weights aligned by index with <c>keywords</c></description></item>
        /// </list>
        /// Returns empty arrays if <paramref name="input"/> is empty or <paramref name="topK"/> is not positive.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method uses the native OpenCC-Jieba instance owned by this object.
        /// </para>
        /// <para>
        /// The native API returns two unmanaged arrays:
        /// an array of UTF-8 keyword string pointers and an array of <c>double</c> weights.
        /// Both arrays MUST be released by calling <c>opencc_jieba_free_keywords_and_weights</c>.
        /// </para>
        /// <para>
        /// For efficiency, the input text is encoded to a pooled UTF-8 buffer (null-terminated) for the native call,
        /// and returned to the shared pool in a <c>finally</c> block.
        /// The keyword method string is passed as a pre-encoded, null-terminated UTF-8 byte sequence.
        /// </para>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">
        /// Thrown if this instance has been disposed.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="method"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="method"/> is empty or not a supported algorithm name.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the native instance is not initialized or has been disposed,
        /// or if the native keyword extraction call fails.
        /// </exception>
        public (string[] keywords, double[] weights) JiebaExtractKeywordsWeights(string input, int topK, string method)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(OpenccJieba));

            if (string.IsNullOrEmpty(input) || topK <= 0)
                return (Array.Empty<string>(), Array.Empty<double>());

            if (method == null)
                throw new ArgumentNullException(nameof(method));
            if (method.Length == 0)
                throw new ArgumentException("Method must be non-empty.", nameof(method));

            if (!JiebaKeywordAlgorithmExtensions.TryParse(method, out var algorithm))
                throw new ArgumentException("Invalid keyword algorithm: " + method, nameof(method));

            if (_openccInstance == IntPtr.Zero)
                throw new InvalidOperationException("Native instance is not initialized or has been disposed.");

            // Resolve method bytes from the parsed algorithm (no allocation, no pooling).
            byte[] methodBytes;
            switch (algorithm)
            {
                case JiebaKeywordAlgorithm.Tfidf:
                    methodBytes = TfidfMethodBytes;
                    break;
                case JiebaKeywordAlgorithm.TextRank:
                    methodBytes = TextrankMethodBytes;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, "Unknown algorithm.");
            }

            byte[] inputBytes = null;

            var keywordsPtr = IntPtr.Zero;
            var weightsPtr = IntPtr.Zero;
            var keywordCountPtr = UIntPtr.Zero;

            try
            {
                // Pooled UTF-8 + NUL input buffer for native API.
                RentUtf8Z(input, out inputBytes);

                var rc = OpenccJiebaNative.opencc_jieba_keywords_and_weights(
                    _openccInstance,
                    inputBytes,
                    (UIntPtr)topK,
                    methodBytes,
                    out keywordCountPtr,
                    out keywordsPtr,
                    out weightsPtr
                );

                if (rc != 0)
                    throw new InvalidOperationException("Keyword extraction failed with error code: " + rc);

                var count64 = keywordCountPtr.ToUInt64();
                if (count64 > int.MaxValue)
                    throw new InvalidOperationException("Keyword count too large: " + count64);

                var count = (int)count64;

                if (count == 0 || keywordsPtr == IntPtr.Zero || weightsPtr == IntPtr.Zero)
                    return (Array.Empty<string>(), Array.Empty<double>());

                var keywords = new string[count];
                var weights = new double[count];

                for (var i = 0; i < count; i++)
                {
                    var kwPtr = Marshal.ReadIntPtr(keywordsPtr, i * IntPtr.Size);
                    keywords[i] = Utf8BytesToString(kwPtr) ?? string.Empty;

                    var wPtr = IntPtr.Add(weightsPtr, i * sizeof(double));
                    weights[i] = Marshal.PtrToStructure<double>(wPtr);
                }

                return (keywords, weights);
            }
            finally
            {
                ReturnRented(inputBytes);

                // Defensive: free if either pointer is non-zero to avoid edge-case native leaks.
                if (keywordsPtr != IntPtr.Zero || weightsPtr != IntPtr.Zero)
                {
                    OpenccJiebaNative.opencc_jieba_free_keywords_and_weights(
                        keywordsPtr,
                        weightsPtr,
                        keywordCountPtr
                    );
                }
            }
        }

        /// <summary>
        /// Extracts keywords and their corresponding weights using the specified Jieba keyword algorithm.
        /// </summary>
        /// <param name="input">
        /// Input text from which keywords will be extracted.
        /// </param>
        /// <param name="topK">
        /// Maximum number of keywords to return.
        /// If the value is less than or equal to zero, no keywords are returned.
        /// </param>
        /// <param name="algorithm">
        /// Keyword extraction algorithm to use.
        /// </param>
        /// <returns>
        /// A tuple containing:
        /// <list type="bullet">
        ///   <item><description><c>keywords</c>: extracted keywords ordered by relevance (highest first)</description></item>
        ///   <item><description><c>weights</c>: keyword weights aligned by index with <c>keywords</c></description></item>
        /// </list>
        /// Returns empty arrays if <paramref name="input"/> is empty or <paramref name="topK"/> is not positive.
        /// </returns>
        /// <remarks>
        /// This overload provides a strongly typed alternative to the string-based API.
        /// The specified <paramref name="algorithm"/> is mapped directly to its canonical
        /// native representation without parsing.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">
        /// Thrown if the instance has been disposed.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the native instance is not initialized.
        /// </exception>
        public (string[] keywords, double[] weights) JiebaExtractKeywordsWeights(
            string input,
            int topK,
            JiebaKeywordAlgorithm algorithm)
        {
            // Canonical mapping, no parsing needed.
            return JiebaExtractKeywordsWeights(input, topK, algorithm.ToNativeMethod());
        }

        #region Helper Methods

        /// <summary>
        /// Allocates a UTF-8 encoded, null-terminated (C-string) byte array
        /// from a managed <see cref="string"/>.
        /// </summary>
        /// <param name="str">
        /// The input string. If <c>null</c>, a single null byte (<c>0x00</c>) is returned.
        /// </param>
        /// <returns>
        /// A newly allocated UTF-8 byte array terminated with a null byte,
        /// suitable for passing to native C APIs.
        /// </returns>
        /// <remarks>
        /// This method always allocates a new managed array.
        /// For large or frequently used strings, prefer pooled helpers
        /// such as <c>RentUtf8Z</c>.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte[] StringToUtf8BytesZ(string str)
        {
            if (str == null)
                return new byte[] { 0 }; // Just a single null if input is null

            var byteCount = Encoding.UTF8.GetByteCount(str);
            var buffer = new byte[byteCount + 1]; // +1 for null terminator
            Encoding.UTF8.GetBytes(str, 0, str.Length, buffer, 0);
            buffer[byteCount] = 0x00; // Explicit null termination
            return buffer;
        }

        /// <summary>
        /// Rents a UTF-8 buffer from <see cref="ArrayPool{T}"/>, encodes the string,
        /// and appends a null terminator (C-string).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void RentUtf8Z(string s, out byte[] rented)
        {
            if (s == null) s = string.Empty;

            var byteCount = Encoding.UTF8.GetByteCount(s);
            rented = ArrayPool<byte>.Shared.Rent(byteCount + 1);

            Encoding.UTF8.GetBytes(s, 0, s.Length, rented, 0);
            rented[byteCount] = 0x00;
        }

        /// <summary>
        /// Returns a previously rented buffer to the shared <see cref="ArrayPool{T}"/>.
        /// </summary>
        /// <param name="rented">
        /// The buffer to return. If <c>null</c>, the call is ignored.
        /// </param>
        /// <remarks>
        /// This helper is intended for use in <c>finally</c> blocks to ensure
        /// pooled buffers are always returned, even when exceptions occur.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReturnRented(byte[] rented)
        {
            if (rented != null)
                ArrayPool<byte>.Shared.Return(rented);
        }

        /// <summary>
        /// Converts a pointer to a null-terminated UTF-8 string to a managed string.
        /// </summary>
        /// <param name="ptr">Pointer to the UTF-8 string.</param>
        /// <returns>The managed string.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe string Utf8BytesToString(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
                return string.Empty;

            var bytePtr = (byte*)ptr;
            var length = 0;

            // Find null-terminator length
            for (var p = bytePtr; *p != 0; p++)
            {
                length++;
            }

            return Encoding.UTF8.GetString(bytePtr, length);
        }

        /// <summary>
        /// Marshals a null-terminated array of UTF-8 string pointers to a managed string array.
        /// </summary>
        /// <param name="stringArrayPtr">Pointer to the array of string pointers.</param>
        /// <returns>Managed array of strings.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe string[] MarshalNullTerminatedStringArray(IntPtr stringArrayPtr)
        {
            if (stringArrayPtr == IntPtr.Zero)
                return Array.Empty<string>();

            var strings = new List<string>();
            var current = (byte**)stringArrayPtr;

            while (*current != null)
            {
                var str = *current;

                // Calculate string length (null-terminated)
                var len = 0;
                for (var p = str; *p != 0; p++)
                    len++;

                strings.Add(Encoding.UTF8.GetString(str, len));
                current++;
            }

            return strings.ToArray();
        }

        #endregion
    }
}