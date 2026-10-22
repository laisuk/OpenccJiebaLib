namespace OpenccJiebaLib
{
    /// <summary>An immutable Jieba segmentation entry, separate from OpenCC conversion mappings.</summary>
    public sealed class UserDictEntry
    {
        /// <summary>Gets the nonempty word, without whitespace or NUL characters.</summary>
        public string Word { get; }
        /// <summary>Gets the explicit native size_t frequency, including zero.</summary>
        public ulong Frequency { get; }
        /// <summary>Gets the optional POS tag. Null omits the tag; an empty tag remains an empty tag.</summary>
        public string Tag { get; }

        /// <summary>Creates a validated entry. Frequency must fit the current process's native size_t.</summary>
        /// <param name="word">Valid Unicode word without whitespace or NUL.</param>
        /// <param name="frequency">Explicit Jieba frequency; zero is permitted.</param>
        /// <param name="tag">Optional valid Unicode POS tag without whitespace or NUL.</param>
        /// <exception cref="System.ArgumentNullException">Word is null.</exception>
        /// <exception cref="System.ArgumentException">Word or tag violates the text validation rules.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Frequency exceeds native size_t.</exception>
        public UserDictEntry(string word, ulong frequency, string tag = null)
        {
            DictionaryValidation.WordAndTag(word, tag);
            DictionaryValidation.Frequency(frequency);
            Word = word;
            Frequency = frequency;
            Tag = tag;
        }
    }
}
