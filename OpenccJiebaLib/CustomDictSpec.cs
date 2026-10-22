using System;
using System.Collections.Generic;

namespace OpenccJiebaLib
{
    /// <summary>An immutable, ordered snapshot of custom mappings for one dictionary slot.</summary>
    /// <remarks>
    /// Specifications are applied in constructor order. Duplicate source keys use the last mapping.
    /// Entries are literal source/target strings; they are not parsed as dictionary file rows.
    /// Empty dictionaries are permitted: append leaves the slot unchanged, while override clears it.
    /// Empty keys or values are passed to native validation, which rejects them during construction.
    /// </remarks>
    public sealed class CustomDictSpec
    {
        /// <summary>Gets the native dictionary slot to customize.</summary>
        public DictSlot Slot { get; }

        /// <summary>Gets the merge mode for this specification.</summary>
        public CustomDictMode Mode { get; }

        /// <summary>Gets the read-only snapshot of source/target mappings in input order.</summary>
        public IReadOnlyList<KeyValuePair<string, string>> Entries { get; }

        /// <summary>Creates a custom dictionary specification and copies its entries.</summary>
        /// <param name="slot">A defined native dictionary slot.</param>
        /// <param name="mode">Append or override behavior for the selected slot.</param>
        /// <param name="entries">Ordered source/target mappings; may be empty but cannot be null.</param>
        /// <exception cref="ArgumentOutOfRangeException">The slot or mode is undefined.</exception>
        /// <exception cref="ArgumentNullException">The entries collection is null.</exception>
        /// <exception cref="ArgumentException">An entry contains null, an embedded NUL, or invalid UTF-16.</exception>
        public CustomDictSpec(DictSlot slot, CustomDictMode mode,
            IEnumerable<KeyValuePair<string, string>> entries)
        {
            if (!Enum.IsDefined(typeof(DictSlot), slot))
                throw new ArgumentOutOfRangeException(nameof(slot), slot, "Unknown dictionary slot.");
            if (!Enum.IsDefined(typeof(CustomDictMode), mode))
                throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown custom dictionary mode.");
            if (entries == null) throw new ArgumentNullException(nameof(entries));

            var snapshot = new List<KeyValuePair<string, string>>();
            foreach (var entry in entries)
            {
                DictionaryValidation.Text(entry.Key, nameof(entries));
                DictionaryValidation.Text(entry.Value, nameof(entries));
                snapshot.Add(entry);
            }

            Slot = slot;
            Mode = mode;
            Entries = snapshot.AsReadOnly();
        }
    }
}
