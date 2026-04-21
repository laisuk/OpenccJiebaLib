using System;
using System.Runtime.CompilerServices;

namespace OpenccJiebaLib
{
    // ReSharper disable InconsistentNaming
    // ReSharper disable IdentifierTypo

    /// <summary>
    /// Numeric OpenCC configuration identifiers (opencc_config_t).
    /// These values must match the native enum exactly.
    /// </summary>
    /// <remarks>
    /// Available in the managed API since version 1.2.0.
    /// </remarks>
    // NOTE:
    // Enum member names intentionally follow OpenCC canonical identifiers
    // (S2TW, T2HK, etc.) to preserve cross-language consistency.
    // Do NOT rename to satisfy C# naming rules.
    public enum OpenccConfig
    {
        /// <summary>Simplified Chinese → Traditional Chinese</summary>
        S2T = 1,

        /// <summary>Simplified → Traditional (Taiwan)</summary>
        S2TW = 2,

        /// <summary>Simplified → Traditional (Taiwan, with phrases)</summary>
        S2TWP = 3,

        /// <summary>Simplified → Traditional (Hong Kong)</summary>
        S2HK = 4,

        /// <summary>Traditional Chinese → Simplified Chinese</summary>
        T2S = 5,

        /// <summary>Traditional → Taiwan Traditional</summary>
        T2TW = 6,

        /// <summary>Traditional → Taiwan Traditional (with phrases)</summary>
        T2TWP = 7,

        /// <summary>Traditional → Hong Kong Traditional</summary>
        T2HK = 8,

        /// <summary>Taiwan Traditional → Simplified</summary>
        TW2S = 9,

        /// <summary>Taiwan Traditional → Simplified (variant)</summary>
        TW2SP = 10,

        /// <summary>Taiwan Traditional → Traditional</summary>
        TW2T = 11,

        /// <summary>Taiwan Traditional → Traditional (variant)</summary>
        TW2TP = 12,

        /// <summary>Hong Kong Traditional → Simplified</summary>
        HK2S = 13,

        /// <summary>Hong Kong Traditional → Traditional</summary>
        HK2T = 14,

        /// <summary>Japanese Kanji variants → Traditional Chinese</summary>
        JP2T = 15,

        /// <summary>Traditional Chinese → Japanese Kanji variants</summary>
        T2JP = 16
    }

    // ReSharper restore IdentifierTypo
    // ReSharper restore InconsistentNaming

    /// <summary>
    /// Extension helpers for <see cref="OpenccConfig"/>.
    /// </summary>
    /// <remarks>
    /// This class provides utility methods that operate on <see cref="OpenccConfig"/>
    /// without exposing numeric configuration IDs or native OpenCC details.
    /// Available in the managed API since version 1.2.0.
    /// </remarks>
    public static class OpenccConfigExtensions
    {
        /// <summary>
        /// Gets the default OpenCC configuration used by the managed API.
        /// </summary>
        /// <returns>
        /// The default <see cref="OpenccConfig"/> value.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This is a managed-layer default policy and does not affect native
        /// OpenCC behavior.
        /// </para>
        /// <para>
        /// The current default is <see cref="OpenccConfig.S2T"/>
        /// (Simplified Chinese → Traditional Chinese).
        /// </para>
        /// <para>
        /// Centralizing the default configuration avoids scattering magic enum
        /// values across the codebase.
        /// </para>
        /// <para>
        /// Recommended usage:
        /// <code>
        /// using static OpenccJiebaLib.OpenccConfigExtensions;
        /// var config = DefaultConfig();
        /// </code>
        /// </para>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OpenccConfig DefaultConfig()
        {
            return OpenccConfig.S2T;
        }

        /// <summary>
        /// Attempts to parse a canonical OpenCC configuration name into a typed
        /// <see cref="OpenccConfig"/> value.
        /// </summary>
        /// <param name="name">
        /// Canonical configuration name (e.g. "s2t", "S2TWP", "t2hk").
        /// Case-insensitive and culture-invariant.
        /// </param>
        /// <param name="configId">
        /// When this method returns <c>true</c>, contains the parsed
        /// <see cref="OpenccConfig"/> value; otherwise set to <c>default</c>.
        /// </param>
        /// <returns>
        /// <c>true</c>
        /// if <paramref name="name"/> is a supported OpenCC configuration name;
        /// otherwise <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This is a pure-managed parser and does not invoke native code.
        /// It never throws and performs no allocations.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryParseConfig(string name, out OpenccConfig configId)
        {
            configId = default;

            if (string.IsNullOrEmpty(name))
                return false;

            // Fast path: length-based + ordinal ignore-case
            switch (name.Length)
            {
                case 3:
                    if (EqualsIgnoreCase(name, "s2t"))
                    {
                        configId = OpenccConfig.S2T;
                        return true;
                    }

                    if (!EqualsIgnoreCase(name, "t2s")) return false;
                    configId = OpenccConfig.T2S;
                    return true;

                case 4:
                    if (EqualsIgnoreCase(name, "s2tw"))
                    {
                        configId = OpenccConfig.S2TW;
                        return true;
                    }

                    if (EqualsIgnoreCase(name, "s2hk"))
                    {
                        configId = OpenccConfig.S2HK;
                        return true;
                    }

                    if (EqualsIgnoreCase(name, "t2tw"))
                    {
                        configId = OpenccConfig.T2TW;
                        return true;
                    }

                    if (EqualsIgnoreCase(name, "t2hk"))
                    {
                        configId = OpenccConfig.T2HK;
                        return true;
                    }

                    if (EqualsIgnoreCase(name, "tw2s"))
                    {
                        configId = OpenccConfig.TW2S;
                        return true;
                    }

                    if (EqualsIgnoreCase(name, "tw2t"))
                    {
                        configId = OpenccConfig.TW2T;
                        return true;
                    }

                    if (EqualsIgnoreCase(name, "hk2s"))
                    {
                        configId = OpenccConfig.HK2S;
                        return true;
                    }

                    if (EqualsIgnoreCase(name, "hk2t"))
                    {
                        configId = OpenccConfig.HK2T;
                        return true;
                    }

                    if (EqualsIgnoreCase(name, "jp2t"))
                    {
                        configId = OpenccConfig.JP2T;
                        return true;
                    }

                    if (!EqualsIgnoreCase(name, "t2jp")) return false;
                    configId = OpenccConfig.T2JP;
                    return true;

                case 5:
                    if (EqualsIgnoreCase(name, "s2twp"))
                    {
                        configId = OpenccConfig.S2TWP;
                        return true;
                    }

                    if (EqualsIgnoreCase(name, "t2twp"))
                    {
                        configId = OpenccConfig.T2TWP;
                        return true;
                    }

                    if (EqualsIgnoreCase(name, "tw2sp"))
                    {
                        configId = OpenccConfig.TW2SP;
                        return true;
                    }

                    if (!EqualsIgnoreCase(name, "tw2tp")) return false;
                    configId = OpenccConfig.TW2TP;
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Attempts to retrieve the canonical OpenCC configuration name for a given
        /// <see cref="OpenccConfig"/> value.
        /// </summary>
        /// <param name="configId">
        /// The OpenCC configuration identifier.
        /// </param>
        /// <param name="name">
        /// When this method returns <c>true</c>, contains the canonical lowercase OpenCC
        /// configuration name (e.g. "s2t", "t2hk"); otherwise set to an empty string.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="configId"/> is valid; otherwise <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This is a pure-managed helper and does not invoke native code.
        /// It never throws.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetConfigName(OpenccConfig configId, out string name)
        {
            name = string.Empty;

            // Use a Try-pattern instead of throwing, to keep this helper cheap and predictable.
            switch (configId)
            {
                case OpenccConfig.S2T:
                    name = "s2t";
                    return true;
                case OpenccConfig.S2TW:
                    name = "s2tw";
                    return true;
                case OpenccConfig.S2TWP:
                    name = "s2twp";
                    return true;
                case OpenccConfig.S2HK:
                    name = "s2hk";
                    return true;
                case OpenccConfig.T2S:
                    name = "t2s";
                    return true;
                case OpenccConfig.T2TW:
                    name = "t2tw";
                    return true;
                case OpenccConfig.T2TWP:
                    name = "t2twp";
                    return true;
                case OpenccConfig.T2HK:
                    name = "t2hk";
                    return true;
                case OpenccConfig.TW2S:
                    name = "tw2s";
                    return true;
                case OpenccConfig.TW2SP:
                    name = "tw2sp";
                    return true;
                case OpenccConfig.TW2T:
                    name = "tw2t";
                    return true;
                case OpenccConfig.TW2TP:
                    name = "tw2tp";
                    return true;
                case OpenccConfig.HK2S:
                    name = "hk2s";
                    return true;
                case OpenccConfig.HK2T:
                    name = "hk2t";
                    return true;
                case OpenccConfig.JP2T:
                    name = "jp2t";
                    return true;
                case OpenccConfig.T2JP:
                    name = "t2jp";
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Parses a canonical OpenCC configuration name into an
        /// <see cref="OpenccConfig"/> value.
        /// </summary>
        /// <param name="name">
        /// Canonical configuration name (e.g. "s2t", "S2TWP", "t2hk").
        /// Case-insensitive.
        /// </param>
        /// <returns>
        /// The corresponding <see cref="OpenccConfig"/> value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="name"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="name"/> is not a supported OpenCC configuration.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OpenccConfig Parse(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (TryParseConfig(name, out var id))
                return id;

            throw new ArgumentException(
                "Unsupported OpenCC config name: '" + name + "'.",
                nameof(name));
        }

        /// <summary>
        /// Determines whether a string is a supported canonical OpenCC
        /// configuration name.
        /// </summary>
        /// <param name="name">
        /// Canonical configuration name to test (case-insensitive).
        /// </param>
        /// <returns>
        /// True if supported; otherwise false.
        /// </returns>
        /// <remarks>
        /// This method never throws and performs no allocations.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidConfig(string name)
        {
            return TryParseConfig(name, out _);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool EqualsIgnoreCase(string a, string b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Converts an <see cref="OpenccConfig"/> value to its canonical OpenCC
        /// configuration name.
        /// </summary>
        /// <param name="config">
        /// The OpenCC configuration enum value.
        /// </param>
        /// <returns>
        /// A lowercase canonical configuration name
        /// (for example, <c>"s2t"</c>, <c>"s2twp"</c>, <c>"t2s"</c>),
        /// suitable for logging, display, file naming, or interoperability
        /// with OpenCC-compatible tools.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The returned string follows the official OpenCC canonical naming
        /// convention and is independent of the enum member name casing.
        /// </para>
        /// <para>
        /// This method does not invoke native code.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="config"/> is not a valid <see cref="OpenccConfig"/> value.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToCanonicalName(this OpenccConfig config)
        {
            // Keep the throwing behavior for the public extension method.
            switch (config)
            {
                case OpenccConfig.S2T: return "s2t";
                case OpenccConfig.S2TW: return "s2tw";
                case OpenccConfig.S2TWP: return "s2twp";
                case OpenccConfig.S2HK: return "s2hk";
                case OpenccConfig.T2S: return "t2s";
                case OpenccConfig.T2TW: return "t2tw";
                case OpenccConfig.T2TWP: return "t2twp";
                case OpenccConfig.T2HK: return "t2hk";
                case OpenccConfig.TW2S: return "tw2s";
                case OpenccConfig.TW2SP: return "tw2sp";
                case OpenccConfig.TW2T: return "tw2t";
                case OpenccConfig.TW2TP: return "tw2tp";
                case OpenccConfig.HK2S: return "hk2s";
                case OpenccConfig.HK2T: return "hk2t";
                case OpenccConfig.JP2T: return "jp2t";
                case OpenccConfig.T2JP: return "t2jp";
                default:
                    throw new ArgumentOutOfRangeException(nameof(config), config, "Invalid OpenCC config");
            }
        }
    }
}