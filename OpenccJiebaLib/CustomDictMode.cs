namespace OpenccJiebaLib
{
    /// <summary>Controls how custom entries are applied to a native dictionary slot.</summary>
    public enum CustomDictMode : uint
    {
        /// <summary>Merges entries into the built-in slot. Later entries replace earlier mappings for the same key.</summary>
        Append = 1,

        /// <summary>Clears the selected slot before applying the custom entries. Other slots are unaffected.</summary>
        Override = 2
    }
}