using System;
using System.Runtime.InteropServices;

namespace OpenccJiebaLib
{
    /// <summary>
    /// Provides raw P/Invoke bindings to the native <c>opencc_jieba_capi</c> library.
    /// </summary>
    /// <remarks>
    /// This internal static class defines one-to-one mappings of the unmanaged C API functions
    /// exposed by <c>libopencc_jieba_capi</c>. It should never be used directly from application code.
    /// <para>
    /// The managed wrapper <see cref="OpenccJieba"/> provides safe access, memory ownership handling,
    /// and UTF-8 marshaling for all these functions.
    /// </para>
    /// </remarks>
    internal static class OpenccJiebaNative
    {
        /// <summary>
        /// Gets the Opencc-Jieba C API ABI version number.
        /// </summary>
        /// <remarks>
        /// This value is intended for <b>runtime binary compatibility checks</b>.
        /// It changes <b>only</b> when the native C ABI is broken (for example,
        /// when function signatures or calling conventions change).
        ///
        /// <para>
        /// Managed bindings (P/Invoke, JNI, ctypes, etc.) should verify this value
        /// before invoking other native functions.
        /// </para>
        /// </remarks>
        /// <returns>
        /// A monotonically increasing ABI version number.
        /// </returns>
        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint opencc_jieba_abi_number();

        /// <summary>
        /// Gets the Opencc-Jieba native library version string.
        /// </summary>
        /// <remarks>
        /// The returned string is a UTF-8, null-terminated version identifier
        /// (for example, <c>"0.7.3"</c>).
        ///
        /// <para>
        /// The returned pointer is owned by the native library and remains valid
        /// for the lifetime of the process. Callers must not free it.
        /// </para>
        /// </remarks>
        /// <returns>
        /// A pointer to a UTF-8 encoded, null-terminated version string.
        /// </returns>
        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr opencc_jieba_version_string();
        
        /// <summary>
        /// The platform-neutral library name. The .NET runtime automatically resolves it to:
        /// <list type="bullet">
        /// <item><description><c>opencc_jieba_capi.dll</c> on Windows</description></item>
        /// <item><description><c>libopencc_jieba_capi.so</c> on Linux</description></item>
        /// <item><description><c>libopencc_jieba_capi.dylib</c> on macOS</description></item>
        /// </list>
        /// </summary>
        private const string DllPath = "opencc_jieba_capi";

        // ─────────────────────────────────────────────────────────────
        // Core Lifecycle
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a new native OpenCC + Jieba instance.
        /// </summary>
        /// <returns>
        /// Pointer to the allocated native instance, or <see cref="IntPtr.Zero"/> on failure.
        /// </returns>
        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr opencc_jieba_new();

        /// <summary>
        /// Deletes a previously created native instance and frees associated resources.
        /// </summary>
        /// <param name="opencc">Pointer to a native instance created by <see cref="opencc_jieba_new"/>.</param>
        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void opencc_jieba_delete(IntPtr opencc);

        // ─────────────────────────────────────────────────────────────
        // Conversion and Language Detection
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Converts input text according to the specified OpenCC configuration.
        /// </summary>
        /// <param name="opencc">Pointer to a valid native instance.</param>
        /// <param name="input">UTF-8 encoded null-terminated input text.</param>
        /// <param name="config">UTF-8 encoded null-terminated configuration name (e.g. <c>"s2t"</c>).</param>
        /// <param name="punctuation">Whether to convert punctuation marks as well.</param>
        /// <returns>
        /// Pointer to a newly allocated UTF-8 null-terminated string containing the converted text.
        /// Must be freed using <see cref="opencc_jieba_free_string"/>.
        /// </returns>
        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr opencc_jieba_convert(
            IntPtr opencc,
            byte[] input,
            byte[] config,
            [MarshalAs(UnmanagedType.I1)] bool punctuation);

        /// <summary>
        /// Determines the script type (Simplified or Traditional) of the given Chinese text.
        /// </summary>
        /// <param name="opencc">Pointer to a valid native instance.</param>
        /// <param name="input">UTF-8 encoded null-terminated input text.</param>
        /// <returns>
        /// Integer result defined by the native implementation:
        /// <list type="bullet">
        /// <item><description><c>1</c> = Traditional Chinese</description></item>
        /// <item><description><c>2</c> = Simplified Chinese</description></item>
        /// <item><description><c>0</c> = Unknown or mixed</description></item>
        /// </list>
        /// </returns>
        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int opencc_jieba_zho_check(IntPtr opencc, byte[] input);

        /// <summary>
        /// Frees a UTF-8 string previously allocated by <see cref="opencc_jieba_convert"/>.
        /// </summary>
        /// <param name="str">Pointer to the string returned from a native call.</param>
        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void opencc_jieba_free_string(IntPtr str);

        // ─────────────────────────────────────────────────────────────
        // Jieba Segmentation
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Performs word segmentation on UTF-8 text using Jieba.
        /// </summary>
        /// <param name="opencc">Pointer to the native instance.</param>
        /// <param name="input">UTF-8 encoded null-terminated input string.</param>
        /// <param name="hmm">Whether to enable HMM (Hidden Markov Model) for segmentation.</param>
        /// <returns>
        /// Pointer to a null-terminated array of UTF-8 string pointers.
        /// Must be freed via <see cref="opencc_jieba_free_string_array"/>.
        /// </returns>
        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr opencc_jieba_cut(IntPtr opencc, byte[] input, bool hmm);

        /// <summary>
        /// Performs word segmentation and joins the result with a specified delimiter.
        /// </summary>
        /// <param name="opencc">Pointer to the native instance.</param>
        /// <param name="input">UTF-8 encoded null-terminated input string.</param>
        /// <param name="hmm">Whether to enable HMM (Hidden Markov Model) for segmentation.</param>
        /// <param name="delimiter">UTF-8 encoded null-terminated delimiter string.</param>
        /// <returns>
        /// Pointer to a UTF-8 null-terminated string with the joined segmentation result.
        /// Must be freed via <see cref="opencc_jieba_free_string"/>.
        /// </returns>
        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr opencc_jieba_cut_and_join(IntPtr opencc, byte[] input, bool hmm, byte[] delimiter);

        /// <summary>
        /// Frees a null-terminated array of UTF-8 string pointers created by <see cref="opencc_jieba_cut"/>.
        /// </summary>
        /// <param name="array">Pointer to the array returned from <see cref="opencc_jieba_cut"/>.</param>
        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr opencc_jieba_free_string_array(IntPtr array);

        // ─────────────────────────────────────────────────────────────
        // Keyword Extraction
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Extracts top keywords from text using the specified algorithm.
        /// </summary>
        /// <param name="opencc">Pointer to the native instance.</param>
        /// <param name="input">UTF-8 encoded null-terminated input string.</param>
        /// <param name="topK">The maximum number of keywords to extract.</param>
        /// <param name="method">UTF-8 encoded null-terminated string ("tfidf" or "textrank").</param>
        /// <returns>
        /// Pointer to a null-terminated array of UTF-8 keyword strings.
        /// Must be freed via <see cref="opencc_jieba_free_string_array"/>.
        /// </returns>
        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr opencc_jieba_keywords(
            IntPtr opencc,
            byte[] input,
            UIntPtr topK,
            byte[] method);

        /// <summary>
        /// Extracts top keywords and their weights from text using the specified algorithm.
        /// </summary>
        /// <param name="instance">Pointer to the native instance.</param>
        /// <param name="input">UTF-8 encoded null-terminated input text.</param>
        /// <param name="topK">Maximum number of keywords to extract (as UIntPtr).</param>
        /// <param name="method">UTF-8 encoded null-terminated string ("tfidf" or "textrank").</param>
        /// <param name="outLen">Output pointer to the number of keywords extracted.</param>
        /// <param name="outKeywords">Output pointer to an array of UTF-8 keyword pointers.</param>
        /// <param name="outWeights">Output pointer to an array of <c>double</c> weights.</param>
        /// <returns>0 on success, or nonzero error code on failure.</returns>
        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int opencc_jieba_keywords_and_weights(
            IntPtr instance,
            byte[] input,
            UIntPtr topK,
            byte[] method,
            out UIntPtr outLen,
            out IntPtr outKeywords,
            out IntPtr outWeights);

        /// <summary>
        /// Frees keyword and weight arrays previously allocated by <see cref="opencc_jieba_keywords_and_weights"/>.
        /// </summary>
        /// <param name="keywords">Pointer to keyword array.</param>
        /// <param name="weights">Pointer to weight array.</param>
        /// <param name="len">Pointer to the integer count of entries.</param>
        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void opencc_jieba_free_keywords_and_weights(
            IntPtr keywords,
            IntPtr weights,
            UIntPtr len);
    }
}
