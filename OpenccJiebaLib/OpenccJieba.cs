using System;
using System.Buffers;
using System.Collections.Generic;
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

        // Define constants
        private const string DllPath = "opencc_jieba_capi"; // Name of the native DLL

        // Pre-encoded config bytes for common configurations
        private static readonly Dictionary<string, byte[]> PreEncodedConfigs = new Dictionary<string, byte[]>();

        // Supported configuration names for OpenCC conversion
        private static readonly HashSet<string> ConfigList = new HashSet<string>
        {
            "s2t", "t2s", "s2tw", "tw2s", "s2twp", "tw2sp", "s2hk", "hk2s", "t2tw", "t2twp", "t2hk", "tw2t", "tw2tp",
            "hk2t", "t2jp", "jp2t"
        };

        // Static constructor to pre-encode common config strings for efficient native interop
        static OpenccJieba()
        {
            foreach (var config in ConfigList)
            {
                // GetByteCount + 1 for null terminator
                int byteCount = Encoding.UTF8.GetByteCount(config);
                byte[] encodedBytes = new byte[byteCount + 1];
                Encoding.UTF8.GetBytes(config, 0, config.Length, encodedBytes, 0);
                encodedBytes[byteCount] = 0x00; // Null-terminate
                PreEncodedConfigs.Add(config, encodedBytes);
            }
        }

        #region Native Function Imports

        // Native function imports using P/Invoke

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr opencc_jieba_new();

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        private static extern void opencc_jieba_delete(IntPtr opencc);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr opencc_jieba_convert(IntPtr opencc, byte[] input, byte[] config, bool punctuation);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        private static extern int opencc_jieba_zho_check(IntPtr opencc, byte[] input);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        private static extern void opencc_jieba_free_string(IntPtr str);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr opencc_jieba_cut(IntPtr opencc, byte[] input, bool hmm);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr opencc_jieba_cut_and_join(IntPtr opencc, byte[] input, bool hmm, byte[] delimiter);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr opencc_jieba_free_string_array(IntPtr array);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr opencc_jieba_keywords(IntPtr opencc, byte[] input, int topK, byte[] method);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        private static extern int opencc_jieba_keywords_and_weights(
            IntPtr instance,
            byte[] input,
            IntPtr topK,
            byte[] method,
            out IntPtr outLen,
            out IntPtr outKeywords,
            out IntPtr outWeights
        );

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        private static extern void opencc_jieba_free_keywords_and_weights(
            IntPtr keywords,
            IntPtr weights,
            IntPtr len
        );

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenccJieba"/> class and allocates the native resources.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the native instance cannot be initialized.</exception>
        public OpenccJieba()
        {
            _openccInstance = opencc_jieba_new();
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
                opencc_jieba_delete(_openccInstance);
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
        /// Converts Chinese text using the specified OpenCC configuration.
        /// </summary>
        /// <param name="input">The input string to convert.</param>
        /// <param name="config">The OpenCC configuration name (e.g., "s2t", "t2s").</param>
        /// <param name="punctuation">Whether to convert punctuation as well.</param>
        /// <returns>The converted string, or an empty string if input is null or empty.</returns>
        /// <exception cref="ObjectDisposedException">If the instance has been disposed.</exception>
        public string Convert(string input, string config, bool punctuation = false)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(OpenccJieba));
            if (string.IsNullOrEmpty(input)) return string.Empty;

            // Ensure config is valid, default to "s2t" if not
            config = ConfigList.Contains(config) ? config : "s2t";

            return ConvertBy(input, config, punctuation);
        }

        /// <summary>
        /// Internal conversion helper using pre-encoded config bytes.
        /// </summary>
        private string ConvertBy(string input, string config, bool punctuation = false)
        {
            if (_openccInstance == IntPtr.Zero)
                throw new InvalidOperationException("Native instance is not initialized or has been disposed.");

            byte[] inputBytes = null;
            byte[] configBytes = PreEncodedConfigs[config];
            IntPtr output = IntPtr.Zero;
            string convertedString;

            try
            {
                int inputByteCount = Encoding.UTF8.GetByteCount(input);
                inputBytes = ArrayPool<byte>.Shared.Rent(inputByteCount + 1);
                Encoding.UTF8.GetBytes(input, 0, input.Length, inputBytes, 0);
                inputBytes[inputByteCount] = 0x00; // Null-terminate

                output = opencc_jieba_convert(_openccInstance, inputBytes, configBytes, punctuation);
                convertedString = Utf8BytesToString(output);
            }
            finally
            {
                if (inputBytes != null)
                    ArrayPool<byte>.Shared.Return(inputBytes);

                if (output != IntPtr.Zero) opencc_jieba_free_string(output);
            }

            return convertedString;
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
                int inputByteCount = Encoding.UTF8.GetByteCount(input);
                inputBytes = ArrayPool<byte>.Shared.Rent(inputByteCount + 1);
                Encoding.UTF8.GetBytes(input, 0, input.Length, inputBytes, 0);
                inputBytes[inputByteCount] = 0x00; // Null-terminate

                code = opencc_jieba_zho_check(_openccInstance, inputBytes);
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

            var result = opencc_jieba_cut(_openccInstance, inputBytes, hmm);

            if (result == IntPtr.Zero)
                return Array.Empty<string>();

            var words = MarshalNullTerminatedStringArray(result);

            if (result != IntPtr.Zero) opencc_jieba_free_string_array(result);

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

            IntPtr resultPtr = opencc_jieba_cut_and_join(_openccInstance, inputBytes, hmm, delimiterBytes);

            if (resultPtr == IntPtr.Zero)
                return string.Empty;

            string result = Utf8BytesToString(resultPtr);

            opencc_jieba_free_string(resultPtr);

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

            var result = opencc_jieba_keywords(_openccInstance, inputBytes, topK, methodBytes);

            if (result == IntPtr.Zero)
                return Array.Empty<string>();

            var keywords = MarshalNullTerminatedStringArray(result);

            if (result != IntPtr.Zero) opencc_jieba_free_string_array(result);

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

            var result = opencc_jieba_keywords(_openccInstance, inputBytes, topK, methodBytes);

            if (result == IntPtr.Zero)
                return Array.Empty<string>();

            var keywords = MarshalNullTerminatedStringArray(result);

            if (result != IntPtr.Zero) opencc_jieba_free_string_array(result);

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
        public (string[] keywords, double[] weights) JiebaExtractKeywords(string input, int topK, string method)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(OpenccJieba));
            var inputBytes = Encoding.UTF8.GetBytes(input);
            var methodBytes = Encoding.UTF8.GetBytes(method);
            IntPtr keywordsPtr = IntPtr.Zero;
            IntPtr weightsPtr = IntPtr.Zero;
            IntPtr keywordCountPtr = IntPtr.Zero;

            try
            {
                if (_openccInstance == IntPtr.Zero)
                    throw new InvalidOperationException("Native instance is not initialized or has been disposed.");

                var result = opencc_jieba_keywords_and_weights(
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
                    IntPtr keywordPtr = Marshal.ReadIntPtr(keywordsPtr, i * IntPtr.Size);
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
                    opencc_jieba_free_keywords_and_weights(keywordsPtr, weightsPtr, keywordCountPtr);
                }
            }
        }

        #region Helper Methods

        /// <summary>
        /// Converts a C# string to a UTF-8 encoded byte array.
        /// </summary>
        /// <param name="str">The input string.</param>
        /// <returns>UTF-8 encoded byte array.</returns>
        private byte[] StringToUtf8Bytes(string str)
        {
            return Encoding.UTF8.GetBytes(str);
        }

        /// <summary>
        /// Converts a pointer to a null-terminated UTF-8 string to a managed string.
        /// </summary>
        /// <param name="ptr">Pointer to the UTF-8 string.</param>
        /// <returns>The managed string.</returns>
        private static unsafe string Utf8BytesToString(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
                return string.Empty;

            byte* bytePtr = (byte*)ptr;
            int length = 0;

            // Find null-terminator length
            for (byte* p = bytePtr; *p != 0; p++)
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
        private static unsafe string[] MarshalNullTerminatedStringArray(IntPtr stringArrayPtr)
        {
            if (stringArrayPtr == IntPtr.Zero)
                return Array.Empty<string>();

            var strings = new List<string>();
            byte** current = (byte**)stringArrayPtr;

            while (*current != null)
            {
                byte* str = *current;

                // Calculate string length (null-terminated)
                int len = 0;
                for (byte* p = str; *p != 0; p++)
                    len++;

                strings.Add(Encoding.UTF8.GetString(str, len));
                current++;
            }

            return strings.ToArray();
        }

        #endregion
    }
}