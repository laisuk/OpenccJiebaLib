using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace OpenccJiebaLib
{
    /// <summary>
    /// Represents a segmented token together with its part-of-speech tag.
    /// </summary>
    public readonly struct JiebaTagItem
    {
        /// <summary>
        /// Gets the segmented token text.
        /// </summary>
        public string Word { get; }

        /// <summary>
        /// Gets the part-of-speech tag associated with <see cref="Word"/>.
        /// </summary>
        public string Tag { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="JiebaTagItem"/> struct.
        /// </summary>
        /// <param name="word">The segmented token text.</param>
        /// <param name="tag">The part-of-speech tag.</param>
        public JiebaTagItem(string word, string tag)
        {
            Word = word ?? string.Empty;
            Tag = tag ?? string.Empty;
        }

        /// <summary>
        /// Returns a string in <c>"word/tag"</c> format.
        /// </summary>
        /// <returns>A readable string representation of this tagged token.</returns>
        public override string ToString() => Word + "/" + Tag;
    }

    /// <summary>
    /// Specifies the Jieba segmentation or tagging mode to use.
    /// </summary>
    public enum SegmentMode
    {
        /// <summary>
        /// Accurate mode segmentation suitable for general-purpose tokenization.
        /// </summary>
        Cut,

        /// <summary>
        /// Search mode segmentation that produces finer-grained tokens for indexing and search.
        /// </summary>
        Search,

        /// <summary>
        /// Full mode segmentation that returns as many possible tokens as can be matched.
        /// </summary>
        Full,

        /// <summary>
        /// Part-of-speech tagging mode that returns tokens in <c>"word/tag"</c> form.
        /// </summary>
        Tag
    }

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
        /// <exception cref="DllNotFoundException">Thrown if the native OpenCC-Jieba library cannot be found.</exception>
        /// <exception cref="EntryPointNotFoundException">Thrown if the native library does not export a required entry point.</exception>
        /// <exception cref="BadImageFormatException">Thrown if the native library is incompatible with the current process architecture.</exception>
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
        /// (for example, <c>0.8.0</c>), identifying the native library build.
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
        /// <remarks>
        /// This overload accepts a string configuration name for compatibility with existing code and user input.
        /// The name is normalized case-insensitively and mapped to the canonical OpenCC identifier before calling the native API.
        /// <para/>
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
                configId = OpenccConfigExtensions.DefaultConfig();

            return ConvertCore(input, configId, punctuation);
        }

        /// <summary>
        /// Converts Chinese text using the specified OpenCC configuration enum,
        /// optionally including punctuation conversion.
        /// </summary>
        /// <param name="input">The input string to convert.</param>
        /// <param name="configId">The OpenCC configuration identifier (managed enum).</param>
        /// <param name="punctuation">Whether to convert punctuation as well.</param>
        /// <returns>The converted string; returns an empty string if <paramref name="input"/> is null or empty.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if this instance has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the native instance is not initialized or has been disposed.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="configId"/> is not a valid <see cref="OpenccConfig"/> value.</exception>
        /// <remarks>
        /// This overload avoids parsing string configuration names and is the preferred choice when the configuration
        /// is already known at compile time.
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
            return string.IsNullOrEmpty(input) ? string.Empty : ConvertCore(input, configId, punctuation);
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
        /// OpenCC-Jieba instance owned by this object.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">If the instance has been disposed.</exception>
        /// <exception cref="InvalidOperationException">If the native instance is not initialized.</exception>
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
        /// The returned tokens preserve the original text order.
        /// Cut mode provides a balanced segmentation between accuracy and performance,
        /// suitable for general text processing.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">If the instance has been disposed.</exception>
        /// <exception cref="InvalidOperationException">If the native instance is not initialized.</exception>
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
        /// Performs Jieba search-mode word segmentation on the input text.
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
        /// The returned tokens preserve the original text order.
        /// Search mode may produce finer-grained tokens suitable for indexing or searching.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">If the instance has been disposed.</exception>
        /// <exception cref="InvalidOperationException">If the native instance is not initialized.</exception>
        public string[] JiebaCutForSearch(string input, bool hmm)
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

                result = OpenccJiebaNative.opencc_jieba_cut_for_search(_openccInstance, inputBytes, hmm);
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
        /// Performs Jieba full-mode word segmentation on the input text.
        /// </summary>
        /// <param name="input">
        /// Input text to tokenize.
        /// </param>
        /// <returns>
        /// An array of segmented tokens.
        /// Returns <see cref="Array.Empty{String}"/> if the input is empty
        /// or if the native tokenizer returns no result.
        /// </returns>
        /// <remarks>
        /// This method uses the native Jieba tokenizer via OpenCC-Jieba.
        /// The returned tokens preserve the original text order.
        /// Full mode attempts to return all possible words in the sentence.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">If the instance has been disposed.</exception>
        /// <exception cref="InvalidOperationException">If the native instance is not initialized.</exception>
        public string[] JiebaCutAll(string input)
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

                result = OpenccJiebaNative.opencc_jieba_cut_all(_openccInstance, inputBytes);
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
        /// Performs Jieba part-of-speech tagging on the input text.
        /// </summary>
        /// <param name="input">
        /// Input text to tokenize and tag.
        /// </param>
        /// <param name="hmm">
        /// Whether to enable HMM-based segmentation.
        /// When enabled, unknown words may be inferred using a Hidden Markov Model.
        /// </param>
        /// <returns>
        /// An array of tagged tokens.
        /// Returns <see cref="Array.Empty{T}"/> if the input is empty
        /// or if the native tagger returns no result.
        /// </returns>
        /// <remarks>
        /// This method uses the native Jieba tagger via OpenCC-Jieba.
        /// The returned items preserve the original text order.
        /// Tag mode returns segmented tokens with part-of-speech labels,
        /// suitable for linguistic analysis.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">If the instance has been disposed.</exception>
        /// <exception cref="InvalidOperationException">If the native instance is not initialized.</exception>
        public JiebaTagItem[] JiebaTag(string input, bool hmm)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(OpenccJieba));

            if (string.IsNullOrEmpty(input))
                return Array.Empty<JiebaTagItem>();

            if (_openccInstance == IntPtr.Zero)
                throw new InvalidOperationException("Native instance is not initialized or has been disposed.");

            byte[] inputBytes = null;
            var result = IntPtr.Zero;

            try
            {
                RentUtf8Z(input, out inputBytes);

                result = OpenccJiebaNative.opencc_jieba_tag(_openccInstance, inputBytes, hmm);
                return result == IntPtr.Zero
                    ? Array.Empty<JiebaTagItem>()
                    : MarshalNullTerminatedTagArray(result);
            }
            finally
            {
                ReturnRented(inputBytes);

                if (result != IntPtr.Zero)
                    OpenccJiebaNative.opencc_jieba_free_tag_array(result);
            }
        }

        /// <summary>
        /// Performs Jieba part-of-speech tagging and returns results as "word/tag" strings.
        /// </summary>
        /// <param name="input">
        /// Input text to tokenize and tag.
        /// </param>
        /// <param name="hmm">
        /// Whether to enable HMM-based segmentation.
        /// </param>
        /// <param name="separator">
        /// Specify tag separator string (default to "/").
        /// </param>
        /// <returns>
        /// An array of strings in the format "word/tag".
        /// Returns <see cref="Array.Empty{String}"/> if the input is empty
        /// or if the native tagger returns no result.
        /// </returns>
        /// <remarks>
        /// This is a convenience wrapper over <see cref="JiebaTag(string, bool)"/>.
        /// Suitable for display, logging, or CLI output.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">If the instance has been disposed.</exception>
        /// <exception cref="InvalidOperationException">If the native instance is not initialized.</exception>
        public string[] JiebaTagAsString(string input, bool hmm, string separator = "/")
        {
            if (_disposed) throw new ObjectDisposedException(nameof(OpenccJieba));

            var tags = JiebaTag(input, hmm);

            if (tags.Length == 0)
                return Array.Empty<string>();

            var result = new string[tags.Length];

            for (var i = 0; i < tags.Length; i++)
            {
                // Avoid interpolation overhead in hot path
                var item = tags[i];
                result[i] = item.Word + separator + item.Tag;
            }

            return result;
        }

        /// <summary>
        /// Performs Jieba accurate-mode segmentation and joins the resulting tokens into a single string.
        /// </summary>
        /// <param name="input">The input text to segment.</param>
        /// <param name="hmm">
        /// Whether to enable Hidden Markov Model (HMM) support for unknown-word recognition.
        /// </param>
        /// <param name="delimiter">
        /// The delimiter used to join the segmented tokens.
        /// If <c>null</c>, it is treated as an empty string.
        /// </param>
        /// <returns>
        /// A single string containing the segmented tokens joined by <paramref name="delimiter"/>.
        /// Returns an empty string if <paramref name="input"/> is null or empty.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// Thrown if this instance has been disposed.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown if the native instance is not initialized.</exception>
        /// <remarks>
        /// This method is deprecated. Use <see cref="SegmentJoin(string, SegmentMode, bool, string, string)"/> instead.
        /// <para/>
        /// Equivalent to:
        /// <code>
        /// SegmentJoin(input, SegmentMode.Cut, hmm, delimiter)
        /// </code>
        /// </remarks>
        [Obsolete(
            "JiebaCutAndJoin is deprecated. Use SegmentJoin(input, SegmentMode.Cut, hmm, delimiter, separator) instead.")]
        public string JiebaCutAndJoin(string input, bool hmm, string delimiter)
        {
            // Keep exact behavior mapping to new API
            return SegmentJoin(input, SegmentMode.Cut, hmm, delimiter ?? string.Empty);
        }

        /// <summary>
        /// Performs segmentation or part-of-speech tagging using the specified <see cref="SegmentMode"/>.
        /// </summary>
        /// <param name="input">The input text to process.</param>
        /// <param name="mode">The segmentation or tagging mode to apply.</param>
        /// <param name="hmm">
        /// Whether to enable Hidden Markov Model (HMM) support when applicable.
        /// This parameter is used by <see cref="SegmentMode.Cut"/>, <see cref="SegmentMode.Search"/>,
        /// and <see cref="SegmentMode.Tag"/>, and is ignored by <see cref="SegmentMode.Full"/>.
        /// </param>
        /// <returns>
        /// An array of tokens produced by the selected mode.
        /// For <see cref="SegmentMode.Tag"/>, each element is returned in <c>"word/tag"</c> format.
        /// Returns <see cref="Array.Empty{String}"/> if <paramref name="input"/> is null or empty.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// Thrown if this instance has been disposed.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown if the native instance is not initialized.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="mode"/> is not a supported <see cref="SegmentMode"/> value.</exception>
        /// <remarks>
        /// Use <see cref="SegmentJoin(string, SegmentMode, bool, string, string)"/> when a single joined string
        /// is preferred for display, UI, or CLI output.
        /// </remarks>
        public string[] Segment(string input, SegmentMode mode, bool hmm = true)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(OpenccJieba));

            if (string.IsNullOrEmpty(input))
                return Array.Empty<string>();

            switch (mode)
            {
                case SegmentMode.Cut:
                    return JiebaCut(input, hmm);

                case SegmentMode.Search:
                    return JiebaCutForSearch(input, hmm);

                case SegmentMode.Full:
                    return JiebaCutAll(input);

                case SegmentMode.Tag:
                    return JiebaTagAsString(input, hmm);

                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported segment mode.");
            }
        }

        /// <summary>
        /// Performs segmentation or part-of-speech tagging on the input text and returns the result as a single joined string.
        /// </summary>
        /// <param name="input">The input text to process.</param>
        /// <param name="mode">
        /// The segmentation or tagging mode to apply:
        /// <list type="bullet">
        /// <item><description><see cref="SegmentMode.Cut"/> - accurate mode segmentation.</description></item>
        /// <item><description><see cref="SegmentMode.Search"/> - finer-grained segmentation for search and indexing.</description></item>
        /// <item><description><see cref="SegmentMode.Full"/> - full mode segmentation.</description></item>
        /// <item><description><see cref="SegmentMode.Tag"/> - part-of-speech tagging mode.</description></item>
        /// </list>
        /// </param>
        /// <param name="hmm">
        /// Whether to enable Hidden Markov Model (HMM) support when applicable.
        /// This parameter is used by <see cref="SegmentMode.Cut"/>, <see cref="SegmentMode.Search"/>,
        /// and <see cref="SegmentMode.Tag"/>, and is ignored by <see cref="SegmentMode.Full"/>.
        /// </param>
        /// <param name="delimiter">
        /// The delimiter used to join the output tokens. Defaults to a single space.
        /// If <c>null</c>, it is treated as an empty string.
        /// </param>
        /// <param name="separator">
        /// The separator used to connect the tag output. Defaults to "/".
        /// If <c>null</c>, it is treated as an empty string.
        /// </param>
        /// <returns>
        /// A single string containing the processed tokens joined by <paramref name="delimiter"/>.
        /// For <see cref="SegmentMode.Tag"/>, tokens are formatted as <c>"word/tag"</c>.
        /// Returns an empty string if <paramref name="input"/> is null or empty, or if no tokens are produced.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// Thrown if this instance has been disposed.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown if the native instance is not initialized.</exception>
        /// <remarks>
        /// This method is intended for UI, display, and CLI scenarios where a single formatted string is preferred.
        /// Use <see cref="Segment(string, SegmentMode, bool)"/> when structured token output is needed.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="mode"/> is not a supported <see cref="SegmentMode"/> value.</exception>
        public string SegmentJoin(
            string input,
            SegmentMode mode,
            bool hmm = true,
            string delimiter = " ",
            string separator = "/")
        {
            if (_disposed) throw new ObjectDisposedException(nameof(OpenccJieba));

            if (string.IsNullOrEmpty(input))
                return string.Empty;

            delimiter = delimiter ?? string.Empty;
            separator = separator ?? string.Empty;

            switch (mode)
            {
                case SegmentMode.Tag:
                {
                    var tags = JiebaTag(input, hmm);
                    if (tags.Length == 0)
                        return string.Empty;

                    var sb = new StringBuilder(tags.Length * 8);

                    for (var i = 0; i < tags.Length; i++)
                    {
                        if (i > 0)
                            sb.Append(delimiter);

                        var t = tags[i];
                        sb.Append(t.Word);
                        sb.Append(separator);
                        sb.Append(t.Tag);
                    }

                    return sb.ToString();
                }

                case SegmentMode.Cut:
                case SegmentMode.Search:
                case SegmentMode.Full:
                default:
                {
                    var tokens = Segment(input, mode, hmm);
                    return tokens.Length == 0 ? string.Empty : string.Join(delimiter, tokens);
                }
            }
        }

        private string ConvertCore(string input, OpenccConfig configId, bool punctuation)
        {
            if (_openccInstance == IntPtr.Zero)
                throw new InvalidOperationException("Native instance is not initialized or has been disposed.");

            byte[] inputBytes = null;
            var output = IntPtr.Zero;

            try
            {
                var configName = configId.ToCanonicalName();

                // Defensive fallback: should never fail for validated enum values,
                // but fall back to default config bytes instead of throwing.
                if (!EncodedConfigCache.TryGetValue(configName, out var configBytes))
                {
                    var defaultConfig = OpenccConfigExtensions.DefaultConfig().ToCanonicalName();
                    configBytes = EncodedConfigCache[defaultConfig];
                }

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
        /// Extracts keywords from the input text using the Jieba TextRank algorithm,
        /// with optional part-of-speech filtering.
        /// </summary>
        /// <param name="input">
        /// Input text from which keywords will be extracted.
        /// </param>
        /// <param name="topK">
        /// Maximum number of keywords to return.
        /// If the value is less than or equal to zero, no keywords are returned.
        /// </param>
        /// <param name="allowedPos">
        /// Optional space-separated list of allowed part-of-speech tags,
        /// for example <c>"n nr ns nt nz v vn"</c>.
        /// Pass <see cref="string.Empty"/> to disable POS filtering.
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
        /// <para>
        /// If <paramref name="allowedPos"/> is provided, only tokens matching the specified
        /// part-of-speech tags are considered during extraction.
        /// </para>
        ///
        /// <para>
        /// This method uses the native OpenCC-Jieba instance owned by this object.
        /// </para>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">If the instance has been disposed.</exception>
        /// <exception cref="InvalidOperationException">If the native instance is not initialized.</exception>
        public string[] JiebaKeywordExtractTextRank(string input, int topK, string allowedPos = "")
        {
            return JiebaKeywordExtractCore(input, topK, JiebaKeywordAlgorithm.TextRank, allowedPos);
        }

        /// <summary>
        /// Extracts keywords from the input text using the Jieba TF-IDF algorithm,
        /// with optional part-of-speech filtering.
        /// </summary>
        /// <param name="input">
        /// Input text from which keywords will be extracted.
        /// </param>
        /// <param name="topK">
        /// Maximum number of keywords to return.
        /// If the value is less than or equal to zero, no keywords are returned.
        /// </param>
        /// <param name="allowedPos">
        /// Optional space-separated list of allowed part-of-speech tags,
        /// for example <c>"n nr ns nt nz v vn"</c>.
        /// Pass <see cref="string.Empty"/> to disable POS filtering.
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
        /// <para>
        /// Compared to TextRank, TF-IDF tends to favor frequently occurring terms
        /// and is well-suited for longer or content-heavy texts.
        /// </para>
        ///
        /// <para>
        /// If <paramref name="allowedPos"/> is provided, only tokens matching the specified
        /// part-of-speech tags are considered during extraction.
        /// </para>
        ///
        /// <para>
        /// This method uses the native OpenCC-Jieba instance owned by this object.
        /// </para>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">If the instance has been disposed.</exception>
        /// <exception cref="InvalidOperationException">If the native instance is not initialized.</exception>
        public string[] JiebaKeywordExtractTfidf(string input, int topK, string allowedPos = "")
        {
            return JiebaKeywordExtractCore(input, topK, JiebaKeywordAlgorithm.Tfidf, allowedPos);
        }

        /// <summary>
        /// Extracts keywords from the input text using the specified Jieba keyword extraction algorithm,
        /// with optional part-of-speech filtering.
        /// </summary>
        /// <param name="input">Input text from which keywords will be extracted.</param>
        /// <param name="topK">
        /// Maximum number of keywords to return.
        /// If the value is less than or equal to zero, no keywords are returned.
        /// </param>
        /// <param name="algorithm">Keyword extraction algorithm to use.</param>
        /// <param name="allowedPos">
        /// Optional space-separated list of allowed part-of-speech tags,
        /// for example <c>"n nr ns nt nz v vn"</c>.
        /// Pass <see cref="string.Empty"/> to disable POS filtering.
        /// </param>
        /// <returns>
        /// An array of extracted keywords ordered by relevance or importance,
        /// depending on the selected algorithm.
        /// </returns>
        /// <exception cref="ObjectDisposedException">If the instance has been disposed.</exception>
        /// <exception cref="InvalidOperationException">If the native instance is not initialized.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="algorithm"/> is not a supported <see cref="JiebaKeywordAlgorithm"/> value.</exception>
        public string[] JiebaKeywordExtract(
            string input,
            int topK,
            JiebaKeywordAlgorithm algorithm,
            string allowedPos = "")
        {
            return JiebaKeywordExtractCore(input, topK, algorithm, allowedPos);
        }

        /// <summary>
        /// Extracts keywords from the input text using the specified Jieba keyword extraction algorithm,
        /// with optional part-of-speech filtering.
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
        /// <param name="allowedPos">
        /// Optional space-separated list of allowed part-of-speech tags,
        /// for example <c>"n nr ns nt nz v vn"</c>.
        /// Pass <see cref="string.Empty"/> to disable POS filtering.
        /// </param>
        /// <returns>
        /// An array of extracted keywords ordered by relevance.
        /// Returns <see cref="Array.Empty{String}"/> if the input is empty
        /// or if the native extractor returns no result.
        /// </returns>
        /// <remarks>
        /// This method uses the native OpenCC-Jieba instance owned by this object.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">If the instance has been disposed.</exception>
        /// <exception cref="InvalidOperationException">If the native instance is not initialized.</exception>
        private string[] JiebaKeywordExtractCore(
            string input,
            int topK,
            JiebaKeywordAlgorithm algorithm,
            string allowedPos = "")
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(OpenccJieba));

            var methodBytes = GetKeywordMethodBytes(algorithm);

            if (string.IsNullOrEmpty(input) || topK <= 0)
                return Array.Empty<string>();

            if (_openccInstance == IntPtr.Zero)
                throw new InvalidOperationException("Native instance is not initialized or has been disposed.");

            allowedPos = allowedPos ?? string.Empty;

            byte[] inputBytes = null;
            byte[] allowedPosBytes = null;
            var result = IntPtr.Zero;

            try
            {
                RentUtf8Z(input, out inputBytes);
                RentUtf8Z(allowedPos, out allowedPosBytes);

                result = OpenccJiebaNative.opencc_jieba_keywords_pos(
                    _openccInstance,
                    inputBytes,
                    (UIntPtr)topK,
                    methodBytes,
                    allowedPosBytes);

                return result == IntPtr.Zero
                    ? Array.Empty<string>()
                    : MarshalNullTerminatedStringArray(result);
            }
            finally
            {
                ReturnRented(inputBytes);
                ReturnRented(allowedPosBytes);

                if (result != IntPtr.Zero)
                    OpenccJiebaNative.opencc_jieba_free_string_array(result);
            }
        }

        /// <summary>
        /// Extracts top keywords and their corresponding weights from the input text
        /// using the specified Jieba keyword extraction method,
        /// with optional part-of-speech filtering.
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
        /// <param name="allowedPos">
        /// Optional space-separated list of allowed part-of-speech tags,
        /// for example <c>"n nr ns nt nz v vn"</c>.
        /// Pass <see cref="string.Empty"/> to disable POS filtering.
        /// </param>
        /// <returns>
        /// A tuple containing:
        /// <list type="bullet">
        ///   <item><description><c>keywords</c>: extracted keywords ordered by relevance (highest first)</description></item>
        ///   <item><description><c>weights</c>: keyword weights aligned by index with <c>keywords</c></description></item>
        /// </list>
        /// Returns empty arrays if <paramref name="input"></paramref> is empty or <paramref name="topK"></paramref> is not positive.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This overload accepts a string method name for compatibility with existing code and UI inputs.
        /// </para>
        /// <para>
        /// The method name is parsed into a strongly typed <see cref="JiebaKeywordAlgorithm"/>
        /// before calling the native API.
        /// </para>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">
        /// Thrown if this instance has been disposed.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="method"></paramref> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="method"></paramref> is empty or not a supported algorithm name.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the native instance is not initialized or has been disposed,
        /// or if the native keyword extraction call fails.
        /// </exception>
        public (string[] keywords, double[] weights) JiebaExtractKeywordsWeights(
            string input,
            int topK,
            string method,
            string allowedPos = "")
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));
            if (method.Length == 0)
                throw new ArgumentException("Method must be non-empty.", nameof(method));

            return !KeywordAlgorithmExtensions.TryParse(method, out var algorithm)
                ? throw new ArgumentException("Invalid keyword algorithm: " + method, nameof(method))
                : JiebaExtractKeywordsWeightsCore(input, topK, algorithm, allowedPos);
        }

        /// <summary>
        /// Extracts keywords and their corresponding weights using the specified Jieba keyword algorithm,
        /// with optional part-of-speech filtering.
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
        /// <param name="allowedPos">
        /// Optional space-separated list of allowed part-of-speech tags,
        /// for example <c>"n nr ns nt nz v vn"</c>.
        /// Pass <see cref="string.Empty"/> to disable POS filtering.
        /// </param>
        /// <returns>
        /// A tuple containing:
        /// <list type="bullet">
        ///   <item><description><c>keywords</c>: extracted keywords ordered by relevance (highest first)</description></item>
        ///   <item><description><c>weights</c>: keyword weights aligned by index with <c>keywords</c></description></item>
        /// </list>
        /// Returns empty arrays if <paramref name="input"></paramref> is empty or <paramref name="topK"></paramref> is not positive.
        /// </returns>
        /// <remarks>
        /// This overload provides a strongly typed alternative to the string-based API.
        /// The specified <paramref name="algorithm"></paramref> is mapped directly to its canonical
        /// native representation without parsing.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">
        /// Thrown if the instance has been disposed.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the native instance is not initialized.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="algorithm"/> is not a supported <see cref="JiebaKeywordAlgorithm"/> value.
        /// </exception>
        public (string[] keywords, double[] weights) JiebaExtractKeywordsWeights(
            string input,
            int topK,
            JiebaKeywordAlgorithm algorithm,
            string allowedPos = "")
        {
            return JiebaExtractKeywordsWeightsCore(input, topK, algorithm, allowedPos);
        }

        /// <summary>
        /// Extracts keywords and their weights using the Jieba TF-IDF algorithm,
        /// with optional part-of-speech filtering.
        /// </summary>
        /// <param name="input">Input text from which keywords will be extracted.</param>
        /// <param name="topK">Maximum number of keywords to return.</param>
        /// <param name="allowedPos">
        /// Optional space-separated list of allowed part-of-speech tags,
        /// for example <c>"n nr ns nt nz v vn"</c>.
        /// Pass <see cref="string.Empty"/> to disable POS filtering.
        /// </param>
        /// <returns>
        /// A tuple containing extracted keywords and aligned weights.
        /// </returns>
        /// <exception cref="ObjectDisposedException">Thrown if this instance has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the native instance is not initialized or if native keyword extraction fails.</exception>
        public (string[] keywords, double[] weights) JiebaKeywordExtractTfidfWeights(
            string input,
            int topK,
            string allowedPos = "")
        {
            return JiebaExtractKeywordsWeightsCore(input, topK, JiebaKeywordAlgorithm.Tfidf, allowedPos);
        }

        /// <summary>
        /// Extracts keywords and their weights using the Jieba TextRank algorithm,
        /// with optional part-of-speech filtering.
        /// </summary>
        /// <param name="input">Input text from which keywords will be extracted.</param>
        /// <param name="topK">Maximum number of keywords to return.</param>
        /// <param name="allowedPos">
        /// Optional space-separated list of allowed part-of-speech tags,
        /// for example <c>"n nr ns nt nz v vn"</c>.
        /// Pass <see cref="string.Empty"/> to disable POS filtering.
        /// </param>
        /// <returns>
        /// A tuple containing extracted keywords and aligned weights.
        /// </returns>
        /// <exception cref="ObjectDisposedException">Thrown if this instance has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the native instance is not initialized or if native keyword extraction fails.</exception>
        public (string[] keywords, double[] weights) JiebaKeywordExtractTextRankWeights(
            string input,
            int topK,
            string allowedPos = "")
        {
            return JiebaExtractKeywordsWeightsCore(input, topK, JiebaKeywordAlgorithm.TextRank, allowedPos);
        }

        /// <summary>
        /// Extracts keywords and their corresponding weights from the input text
        /// using the specified Jieba keyword extraction algorithm,
        /// with optional part-of-speech filtering.
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
        /// <param name="allowedPos">
        /// Optional UTF-8 space-separated part-of-speech filter string,
        /// for example <c>"n nr ns nt nz v vn"</c>.
        /// Pass <see cref="string.Empty"/> to disable POS filtering.
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
        /// For efficiency, the input text and <paramref name="allowedPos"/> filter are encoded
        /// to pooled UTF-8 buffers (null-terminated) for the native call,
        /// and returned to the shared pool in a <c>finally</c> block.
        /// The keyword algorithm is passed as a cached, pre-encoded, null-terminated UTF-8 byte sequence.
        /// </para>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">
        /// Thrown if this instance has been disposed.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the native instance is not initialized or has been disposed,
        /// or if the native keyword extraction call fails.
        /// </exception>
        private (string[] keywords, double[] weights) JiebaExtractKeywordsWeightsCore(
            string input,
            int topK,
            JiebaKeywordAlgorithm algorithm,
            string allowedPos = "")
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(OpenccJieba));

            var methodBytes = GetKeywordMethodBytes(algorithm);

            if (string.IsNullOrEmpty(input) || topK <= 0)
                return (Array.Empty<string>(), Array.Empty<double>());

            if (_openccInstance == IntPtr.Zero)
                throw new InvalidOperationException("Native instance is not initialized or has been disposed.");

            allowedPos = allowedPos ?? string.Empty;

            byte[] inputBytes = null;
            byte[] allowedPosBytes = null;

            var keywordsPtr = IntPtr.Zero;
            var weightsPtr = IntPtr.Zero;
            var keywordCountPtr = UIntPtr.Zero;

            try
            {
                // Pooled UTF-8 + NUL input buffer for native API.
                RentUtf8Z(input, out inputBytes);
                RentUtf8Z(allowedPos, out allowedPosBytes);

                var rc = OpenccJiebaNative.opencc_jieba_keywords_and_weights_pos(
                    _openccInstance,
                    inputBytes,
                    (UIntPtr)topK,
                    methodBytes,
                    allowedPosBytes,
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
                ReturnRented(allowedPosBytes);

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

        #region Helper Methods

        /*
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
        */

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

        private static unsafe JiebaTagItem[] MarshalNullTerminatedTagArray(IntPtr arrayPtr)
        {
            if (arrayPtr == IntPtr.Zero)
                return Array.Empty<JiebaTagItem>();

            var list = new List<JiebaTagItem>();

            var structSize = sizeof(OpenccJiebaNative.OpenccJiebaTagNative);
            var current = (byte*)arrayPtr;

            while (true)
            {
                var native = *(OpenccJiebaNative.OpenccJiebaTagNative*)current;

                // Sentinel: both null
                if (native.word == IntPtr.Zero && native.tag == IntPtr.Zero)
                    break;

                var word = Utf8BytesToString(native.word);
                var tag = Utf8BytesToString(native.tag);

                list.Add(new JiebaTagItem(word, tag));

                current += structSize;
            }

            return list.Count == 0 ? Array.Empty<JiebaTagItem>() : list.ToArray();
        }

        /// <summary>
        /// Returns the cached UTF-8, null-terminated native method name bytes
        /// for the specified Jieba keyword extraction algorithm.
        /// </summary>
        /// <param name="algorithm">Keyword extraction algorithm.</param>
        /// <returns>
        /// Cached UTF-8 bytes for the native method name.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="algorithm"/> is not a supported value.
        /// </exception>
        private static byte[] GetKeywordMethodBytes(JiebaKeywordAlgorithm algorithm)
        {
            switch (algorithm)
            {
                case JiebaKeywordAlgorithm.Tfidf:
                    return TfidfMethodBytes;
                case JiebaKeywordAlgorithm.TextRank:
                    return TextrankMethodBytes;
                default:
                    throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, "Unknown algorithm.");
            }
        }

        #endregion
    }
}
