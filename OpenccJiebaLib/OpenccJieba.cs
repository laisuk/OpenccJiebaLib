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

        // Pre-encoded config bytes for common configurations
        private static readonly Dictionary<string, byte[]> EncodedConfigCache =
            new Dictionary<string, byte[]>(StringComparer.Ordinal);

        // Supported configuration names for OpenCC conversion
        private static readonly HashSet<string> ConfigList = new HashSet<string>(
            new[]
            {
                "s2t", "t2s", "s2tw", "tw2s", "s2twp", "tw2sp", "s2hk", "hk2s",
                "t2tw", "t2twp", "t2hk", "tw2t", "tw2tp", "hk2t", "t2jp", "jp2t"
            },
            StringComparer.Ordinal);


        // Static constructor to pre-encode common config strings for efficient native interop
        static OpenccJieba()
        {
            foreach (var config in ConfigList)
            {
                if (EncodedConfigCache.ContainsKey(config))
                    continue; // Defensive: avoid ArgumentException if code is refactored later

                var byteCount = Encoding.UTF8.GetByteCount(config);
                var encodedBytes = new byte[byteCount + 1];
                Encoding.UTF8.GetBytes(config, 0, config.Length, encodedBytes, 0);
                encodedBytes[byteCount] = 0x00;

                EncodedConfigCache[config] = encodedBytes;
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

            // Normalize/validate config with a safe default.
            config = ConfigList.Contains(config) ? config : "s2t";

            if (_openccInstance == IntPtr.Zero)
                throw new InvalidOperationException("Native instance is not initialized or has been disposed.");

            byte[] rented = null;
            var output = IntPtr.Zero;

            try
            {
                if (!EncodedConfigCache.TryGetValue(config, out var configBytes))
                    throw new ArgumentException("Unknown OpenCC configuration: " + config, nameof(config));

                var byteCount = Encoding.UTF8.GetByteCount(input);
                rented = ArrayPool<byte>.Shared.Rent(byteCount + 1);

                // Encode to UTF-8 and null-terminate for the native API.
                Encoding.UTF8.GetBytes(input, 0, input.Length, rented, 0);
                rented[byteCount] = 0x00;

                output = OpenccJiebaNative.opencc_jieba_convert(_openccInstance, rented, configBytes, punctuation);
                return Utf8BytesToString(output);
            }
            finally
            {
                if (rented != null)
                    ArrayPool<byte>.Shared.Return(rented);

                if (output != IntPtr.Zero)
                    OpenccJiebaNative.opencc_jieba_free_string(output);
            }
        }

        /// <summary>
        /// Checks if the input string contains Chinese characters.
        /// </summary>
        /// <param name="input">The input string to check.</param>
        /// <returns>An integer code indicating the result (implementation-defined).</returns>
        /// <exception cref="ObjectDisposedException">If the instance has been disposed.</exception>
        public int ZhoCheck(string input)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(OpenccJieba));
            if (string.IsNullOrEmpty(input)) return 0;

            if (_openccInstance == IntPtr.Zero)
                throw new InvalidOperationException("Native instance is not initialized or has been disposed.");

            byte[] inputBytes = null;
            int code;

            try
            {
                var inputByteCount = Encoding.UTF8.GetByteCount(input);
                inputBytes = ArrayPool<byte>.Shared.Rent(inputByteCount + 1);
                Encoding.UTF8.GetBytes(input, 0, input.Length, inputBytes, 0);
                inputBytes[inputByteCount] = 0x00; // Null-terminate

                code = OpenccJiebaNative.opencc_jieba_zho_check(_openccInstance, inputBytes);
            }
            finally
            {
                if (inputBytes != null)
                    ArrayPool<byte>.Shared.Return(inputBytes);
            }

            return code;
        }

        /// <summary>
        /// Performs Chinese word segmentation using Jieba.
        /// </summary>
        /// <param name="input">The input string to segment.</param>
        /// <param name="hmm">Whether to use the Hidden Markov Model (HMM) for segmentation.</param>
        /// <returns>An array of segmented words.</returns>
        /// <exception cref="ObjectDisposedException">If the instance has been disposed.</exception>
        public string[] JiebaCut(string input, bool hmm)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(OpenccJieba));
            var inputBytes = StringToUtf8Bytes(input);

            if (_openccInstance == IntPtr.Zero)
                throw new InvalidOperationException("Native instance is not initialized or has been disposed.");

            var result = OpenccJiebaNative.opencc_jieba_cut(_openccInstance, inputBytes, hmm);

            if (result == IntPtr.Zero)
                return Array.Empty<string>();

            var words = MarshalNullTerminatedStringArray(result);

            if (result != IntPtr.Zero) OpenccJiebaNative.opencc_jieba_free_string_array(result);

            return words;
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
            if (_openccInstance == IntPtr.Zero)
                throw new InvalidOperationException("Native instance is not initialized or has been disposed.");

            var inputBytes = StringToUtf8Bytes(input);
            var delimiterBytes = StringToUtf8Bytes(delimiter);

            var resultPtr =
                OpenccJiebaNative.opencc_jieba_cut_and_join(_openccInstance, inputBytes, hmm, delimiterBytes);

            if (resultPtr == IntPtr.Zero)
                return string.Empty;

            var result = Utf8BytesToString(resultPtr);

            OpenccJiebaNative.opencc_jieba_free_string(resultPtr);

            return result;
        }

        /// <summary>
        /// Extracts keywords from the input text using the TextRank algorithm.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <param name="topK">The maximum number of keywords to extract.</param>
        /// <returns>An array of extracted keywords.</returns>
        /// <exception cref="ObjectDisposedException">If the instance has been disposed.</exception>
        public string[] JiebaKeywordExtractTextRank(string input, int topK)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(OpenccJieba));
            var inputBytes = StringToUtf8Bytes(input);
            var methodBytes = StringToUtf8Bytes("textrank");

            if (_openccInstance == IntPtr.Zero)
                throw new InvalidOperationException("Native instance is not initialized or has been disposed.");

            var result = OpenccJiebaNative.opencc_jieba_keywords(_openccInstance, inputBytes, topK, methodBytes);

            if (result == IntPtr.Zero)
                return Array.Empty<string>();

            var keywords = MarshalNullTerminatedStringArray(result);

            if (result != IntPtr.Zero) OpenccJiebaNative.opencc_jieba_free_string_array(result);

            return keywords;
        }

        /// <summary>
        /// Extracts keywords from the input text using the TF-IDF algorithm.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <param name="topK">The maximum number of keywords to extract.</param>
        /// <returns>An array of extracted keywords.</returns>
        /// <exception cref="ObjectDisposedException">If the instance has been disposed.</exception>
        public string[] JiebaKeywordExtractTfidf(string input, int topK)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(OpenccJieba));
            var inputBytes = StringToUtf8Bytes(input);
            var methodBytes = StringToUtf8Bytes("tfidf");

            if (_openccInstance == IntPtr.Zero)
                throw new InvalidOperationException("Native instance is not initialized or has been disposed.");

            var result = OpenccJiebaNative.opencc_jieba_keywords(_openccInstance, inputBytes, topK, methodBytes);

            if (result == IntPtr.Zero)
                return Array.Empty<string>();

            var keywords = MarshalNullTerminatedStringArray(result);

            if (result != IntPtr.Zero) OpenccJiebaNative.opencc_jieba_free_string_array(result);

            return keywords;
        }

        /// <summary>
        /// Extracts keywords and their weights from the input text using the specified method.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <param name="topK">The maximum number of keywords to extract.</param>
        /// <param name="method">The extraction method ("tfidf" or "textrank").</param>
        /// <returns>A tuple containing an array of keywords and an array of corresponding weights.</returns>
        /// <exception cref="ObjectDisposedException">If the instance has been disposed.</exception>
        public (string[] keywords, double[] weights) JiebaExtractKeywordsWeights(string input, int topK, string method)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(OpenccJieba));
            var inputBytes = Encoding.UTF8.GetBytes(input);
            var methodBytes = Encoding.UTF8.GetBytes(method);
            var keywordsPtr = IntPtr.Zero;
            var weightsPtr = IntPtr.Zero;
            var keywordCountPtr = IntPtr.Zero;

            try
            {
                if (_openccInstance == IntPtr.Zero)
                    throw new InvalidOperationException("Native instance is not initialized or has been disposed.");

                var result = OpenccJiebaNative.opencc_jieba_keywords_and_weights(
                    _openccInstance,
                    inputBytes,
                    (IntPtr)topK,
                    methodBytes,
                    out keywordCountPtr,
                    out keywordsPtr,
                    out weightsPtr
                );

                if (result != 0)
                {
                    throw new Exception("Keyword extraction failed with error code: " + result);
                }

                var keywordCount = (int)keywordCountPtr;
                var keywords = new string[keywordCount];
                var weights = new double[keywordCount];

                // Marshal keywords and weights from native memory
                for (var i = 0; i < keywordCount; i++)
                {
                    var keywordPtr = Marshal.ReadIntPtr(keywordsPtr, i * IntPtr.Size);
                    keywords[i] = Utf8BytesToString(keywordPtr);
                    weights[i] = Marshal.PtrToStructure<double>(weightsPtr + (i * sizeof(double)));
                }

                return (keywords, weights);
            }
            finally
            {
                // Free memory for keywords and weights using the C API function
                if (keywordsPtr != IntPtr.Zero && weightsPtr != IntPtr.Zero)
                {
                    OpenccJiebaNative.opencc_jieba_free_keywords_and_weights(keywordsPtr, weightsPtr, keywordCountPtr);
                }
            }
        }

        #region Helper Methods

        /// <summary>
        /// Converts a C# string to a UTF-8 encoded null-terminated byte array.
        /// </summary>
        /// <param name="str">The input string.</param>
        /// <returns>UTF-8 encoded byte array, null-terminated.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte[] StringToUtf8Bytes(string str)
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