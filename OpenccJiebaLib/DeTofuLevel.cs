namespace OpenccJiebaLib
{
    /// <summary>Inclusive extension threshold for the native built-in DeTofu display fallback.</summary>
    /// <remarks>
    /// Values match <c>opencc_jieba_detofu_level_t</c> in the C ABI. Only characters with a built-in mapping
    /// in the selected extension or a supported later extension are replaced. Unmapped characters remain
    /// unchanged. There is no disabled level; omit the optional DeTofu call to disable fallback.
    /// </remarks>
    public enum DeTofuLevel : uint
    {
        /// <summary>Applies mappings for Extension B through Extension I; the broadest threshold.</summary>
        ExtB = 0,
        /// <summary>Applies mappings for Extension C through Extension I.</summary>
        ExtC = 1,
        /// <summary>Applies mappings for Extension D through Extension I.</summary>
        ExtD = 2,
        /// <summary>Applies mappings for Extension E through Extension I.</summary>
        ExtE = 3,
        /// <summary>Applies mappings for Extension F through Extension I.</summary>
        ExtF = 4,
        /// <summary>Applies mappings for Extension G through Extension I.</summary>
        ExtG = 5,
        /// <summary>Applies mappings for Extension H and Extension I.</summary>
        ExtH = 6,
        /// <summary>Applies mappings for Extension I only.</summary>
        ExtI = 7
    }
}
