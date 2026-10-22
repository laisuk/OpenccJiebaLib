using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace OpenccJiebaLib
{
    /// <summary>Owns borrowed, pinned buffers until the synchronous native constructor returns.</summary>
    internal sealed class CustomDictionaryMarshaller : IDisposable
    {
        private readonly GCHandle[] _pins;
        private int _pinCount;
        private readonly bool _hasUsers;
        private readonly bool _hasCustom;
        private bool _disposed;

        internal IntPtr UserEntries { get; private set; }
        internal UIntPtr UserCount { get; }
        internal IntPtr Specifications { get; private set; }
        internal UIntPtr Count { get; }

        // Null selects an absent dictionary type; public constructors reject explicit null collections.
        internal CustomDictionaryMarshaller(IEnumerable<UserDictEntry> users, IEnumerable<CustomDictSpec> specs)
        {
            _hasUsers = users != null;
            _hasCustom = specs != null;
            var userSnapshot = Snapshot(users, nameof(users));
            var specSnapshot = Snapshot(specs, nameof(specs));
            var capacity = checked(2 + checked(userSnapshot.Count * 2));
            foreach (var spec in specSnapshot)
                capacity = checked(capacity + 1 + checked(spec.Entries.Count * 2));
            _pins = new GCHandle[capacity];
            UserCount = new UIntPtr((uint)userSnapshot.Count);
            Count = new UIntPtr((uint)specSnapshot.Count);

            try
            {
                var nativeUsers = new OpenccJiebaNative.NativeUserDictEntry[userSnapshot.Count];
                for (var i = 0; i < nativeUsers.Length; i++)
                {
                    var user = userSnapshot[i];
                    nativeUsers[i] = new OpenccJiebaNative.NativeUserDictEntry
                    {
                        Word = Pin(DictionaryValidation.Utf8Z(user.Word)),
                        Frequency = DictionaryValidation.Frequency(user.Frequency),
                        Tag = Pin(DictionaryValidation.Utf8Z(user.Tag))
                    };
                }

                UserEntries = Pin(nativeUsers);

                var nativeSpecs = new OpenccJiebaNative.NativeCustomDictSpec[specSnapshot.Count];
                for (var i = 0; i < nativeSpecs.Length; i++)
                {
                    var spec = specSnapshot[i];
                    var pairs = new OpenccJiebaNative.NativeCustomPair[spec.Entries.Count];
                    for (var j = 0; j < pairs.Length; j++)
                    {
                        pairs[j].Source = Pin(DictionaryValidation.Utf8Z(spec.Entries[j].Key));
                        pairs[j].Target = Pin(DictionaryValidation.Utf8Z(spec.Entries[j].Value));
                    }

                    nativeSpecs[i] = new OpenccJiebaNative.NativeCustomDictSpec
                    {
                        Slot = (uint)spec.Slot,
                        Mode = (uint)spec.Mode,
                        Pairs = Pin(pairs),
                        PairCount = new UIntPtr((uint)pairs.Length)
                    };
                }

                Specifications = Pin(nativeSpecs);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private static List<T> Snapshot<T>(IEnumerable<T> values, string parameter) where T : class
        {
            var result = new List<T>();
            if (values == null) return result;
            foreach (var value in values)
            {
                if (value == null)
                    throw new ArgumentException("Dictionary collections cannot contain null.", parameter);
                result.Add(value);
            }

            return result;
        }

        internal IntPtr CreateInstance()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(CustomDictionaryMarshaller));
            var instance = _hasUsers && _hasCustom
                ? OpenccJiebaNative.opencc_jieba_new_user_dict_custom(UserEntries, UserCount, Specifications, Count)
                : _hasUsers
                    ? OpenccJiebaNative.opencc_jieba_new_user_dict(UserEntries, UserCount)
                    : OpenccJiebaNative.opencc_jieba_new_custom(Specifications, Count);
            if (instance == IntPtr.Zero)
                throw OpenccJieba.NativeFailure("Failed to initialize OpenCC/Jieba with dictionaries.");
            return instance;
        }

        private IntPtr Pin(Array buffer)
        {
            if (buffer == null || buffer.Length == 0) return IntPtr.Zero;
            // Capacity is reserved before any pins exist, so tracking cannot allocate or throw.
            var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            _pins[_pinCount++] = handle;
            return handle.AddrOfPinnedObject();
        }

        public void Dispose()
        {
            while (_pinCount > 0) _pins[--_pinCount].Free();
            UserEntries = Specifications = IntPtr.Zero;
            _disposed = true;
        }
    }
}