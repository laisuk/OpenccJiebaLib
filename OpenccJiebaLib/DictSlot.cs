namespace OpenccJiebaLib
{
    // ReSharper disable InconsistentNaming

    /// <summary>Conversion dictionary slots and IDs from opencc_jieba_capi.h v0.8.1.1.</summary>
    /// <remarks>Select the slot used by the desired conversion configuration. This does not select a configuration.</remarks>
    public enum DictSlot : uint
    {
        /// <summary>Simplified to Traditional characters.</summary>
        STCharacters = 1,

        /// <summary>Simplified to Traditional phrases.</summary>
        STPhrases = 2,

        /// <summary>Traditional to Simplified characters.</summary>
        TSCharacters = 3,

        /// <summary>Traditional to Simplified phrases.</summary>
        TSPhrases = 4,

        /// <summary>Traditional to Taiwan phrases.</summary>
        TWPhrases = 5,

        /// <summary>Taiwan to Traditional reverse phrases.</summary>
        TWPhrasesRev = 6,

        /// <summary>Traditional to Hong Kong phrases.</summary>
        HKPhrases = 7,

        /// <summary>Hong Kong to Traditional reverse phrases.</summary>
        HKPhrasesRev = 8,

        /// <summary>Traditional to Taiwan variants.</summary>
        TWVariants = 9,

        /// <summary>Traditional to Taiwan phrase variants.</summary>
        TWVariantsPhrases = 10,

        /// <summary>Taiwan to Traditional reverse variants.</summary>
        TWVariantsRev = 11,

        /// <summary>Taiwan to Traditional reverse phrase variants.</summary>
        TWVariantsRevPhrases = 12,

        /// <summary>Traditional to Hong Kong variants.</summary>
        HKVariants = 13,

        /// <summary>Traditional to Hong Kong phrase variants.</summary>
        HKVariantsPhrases = 14,

        /// <summary>Hong Kong to Traditional reverse variants.</summary>
        HKVariantsRev = 15,

        /// <summary>Hong Kong to Traditional reverse phrase variants.</summary>
        HKVariantsRevPhrases = 16,

        /// <summary>Japanese Shinjitai characters.</summary>
        JPSCharacters = 17,

        /// <summary>Japanese Shinjitai reverse characters.</summary>
        JPSCharactersRev = 18,

        /// <summary>Japanese Shinjitai phrases.</summary>
        JPSPhrases = 19
    }

    // ReSharper restore InconsistentNaming
}