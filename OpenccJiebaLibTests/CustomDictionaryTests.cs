using System.Runtime.InteropServices;
using OpenccJiebaLib;

namespace OpenccJiebaLibTests;

[TestClass]
public sealed class CustomDictionaryTests
{
    private static CustomDictSpec Spec(DictSlot slot = DictSlot.STCharacters,
        CustomDictMode mode = CustomDictMode.Append, string source = "龙", string target = "訂") =>
        new(slot, mode, [KeyValuePair.Create(source, target)]);

    [TestMethod]
    public void EmptyConstructorCollectionsUseBuiltInDictionaries()
    {
        using var standard = new OpenccJieba();
        using var custom = new OpenccJieba(Array.Empty<CustomDictSpec>());
        using var users = new OpenccJieba(Array.Empty<UserDictEntry>());
        using var both = new OpenccJieba(Array.Empty<UserDictEntry>(), Array.Empty<CustomDictSpec>());
        foreach (var converter in new[] { standard, custom, users, both })
            Assert.AreEqual("龍馬精神", converter.Convert("龙马精神", "s2t"));
    }

    [TestMethod]
    public void UserDictionaryOnlyPreservesWordAndTag()
    {
        using var converter = new OpenccJieba(new[] { new UserDictEntry("帕兰蒂尔", 100000, "nz") });
        CollectionAssert.AreEqual(new[] { "帕兰蒂尔" }, converter.JiebaCut("帕兰蒂尔", false));
        Assert.AreEqual("nz", converter.JiebaTag("帕兰蒂尔", false).Single().Tag);
        Assert.AreEqual("龍", converter.Convert("龙", "s2t"));
    }

    [TestMethod]
    public void CustomPhraseRequiresJiebaWord()
    {
        using var converter = new OpenccJieba(new[] { Spec(DictSlot.HKPhrasesRev, source: "細路哥", target: "小男孩") });
        CollectionAssert.AreEqual(new[] { "這個", "細路", "哥", "很", "靈活" }, converter.JiebaCut("這個細路哥很靈活", true));
        Assert.AreEqual("这个细路哥很灵活", converter.Convert("這個細路哥很靈活", "hk2sp"));
    }

    [TestMethod]
    public void CombinedDictionariesEnableHongKongPhrase()
    {
        using var converter = new OpenccJieba(new[] { new UserDictEntry("細路哥", 3) },
            new[] { Spec(DictSlot.HKPhrasesRev, source: "細路哥", target: "小男孩") });
        CollectionAssert.AreEqual(new[] { "這個", "細路哥", "很", "靈活" }, converter.JiebaCut("這個細路哥很靈活", true));
        Assert.AreEqual("这个小男孩很灵活", converter.Convert("這個細路哥很靈活", OpenccConfig.HK2SP));
    }

    [TestMethod]
    public void CombinedDictionariesEnablePalantirPhrase()
    {
        using var converter = new OpenccJieba(new[] { new UserDictEntry("帕兰蒂尔", 100000, "nz") },
            new[] { Spec(DictSlot.STPhrases, source: "帕兰蒂尔", target: "柏蘭蒂爾") });
        CollectionAssert.AreEqual(new[] { "帕兰蒂尔" }, converter.JiebaCut("帕兰蒂尔", false));
        Assert.AreEqual("nz", converter.JiebaTag("帕兰蒂尔", false).Single().Tag);
        Assert.AreEqual("柏蘭蒂爾是一家公司", converter.Convert("帕兰蒂尔是一家公司", "s2t"));
    }

    [TestMethod]
    public void SnapshotPreservesOrderAndIsReadOnly()
    {
        var entries = new List<KeyValuePair<string, string>> { KeyValuePair.Create("龙", "一"), KeyValuePair.Create("龙", "二") };
        var spec = new CustomDictSpec(DictSlot.STCharacters, CustomDictMode.Append, entries);
        entries.Clear();
        CollectionAssert.AreEqual(new[] { "一", "二" }, spec.Entries.Select(e => e.Value).ToArray());
        Assert.ThrowsExactly<NotSupportedException>(() => ((IList<KeyValuePair<string, string>>)spec.Entries).Clear());
        using var converter = new OpenccJieba(new[] { spec });
        Assert.AreEqual("二", converter.Convert("龙", "s2t"));
    }

    [TestMethod]
    public void RepeatedSpecificationsApplyInOrder()
    {
        using var converter = new OpenccJieba(new[]
        {
            Spec(source: "汉", target: "前"), Spec(target: "先"),
            Spec(mode: CustomDictMode.Override, target: "覆"), Spec(source: "马", target: "後")
        });
        Assert.AreEqual("汉覆後", converter.Convert("汉龙马", "s2t"));
        Assert.AreEqual("龙", converter.Convert("龍", "t2s"));
    }

    [TestMethod]
    public void MultipleArraysSurviveCallerMutationAndGarbageCollection()
    {
        var users = new List<UserDictEntry> { new("帕兰蒂尔", 100000, "nz"), new("細路哥", 3) };
        var specs = new List<CustomDictSpec>
        {
            new(DictSlot.STCharacters, CustomDictMode.Append, [KeyValuePair.Create("龙", "訂"), KeyValuePair.Create("汉", "自")]),
            Spec(DictSlot.TSCharacters, source: "龍", target: "己"),
            Spec(DictSlot.STPhrases, source: "帕兰蒂尔", target: "柏蘭蒂爾"),
            Spec(DictSlot.HKPhrasesRev, source: "細路哥", target: "小男孩")
        };
        using var converter = new OpenccJieba(users, specs);
        users.Clear(); specs.Clear();
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        Assert.AreEqual("訂自", converter.Convert("龙汉", "s2t"));
        Assert.AreEqual("己", converter.Convert("龍", "t2s"));
        Assert.AreEqual("柏蘭蒂爾", converter.Convert("帕兰蒂尔", "s2t"));
        Assert.AreEqual("小男孩", converter.Convert("細路哥", "hk2sp"));
    }

    [TestMethod]
    [DataRow(CustomDictMode.Append, "龍")]
    [DataRow(CustomDictMode.Override, "龙")]
    public void EmptyMappingSemantics(CustomDictMode mode, string expected)
    {
        using var converter = new OpenccJieba(new[] { new CustomDictSpec(DictSlot.STCharacters, mode, []) });
        Assert.AreEqual(expected, converter.Convert("龙", "s2t"));
    }

    [TestMethod]
    public void SupplementaryUnicodeRoundTripsThroughConstructorBuffers()
    {
        using var converter = new OpenccJieba(new[]
        {
            Spec(source: "𠀀", target: "𠮷🚀")
        });
        Assert.AreEqual("𠮷🚀", converter.Convert("𠀀", "s2t"));
        using var users = new OpenccJieba(new[] { new UserDictEntry("星际𠮷咖啡馆", 100000, "名𠮷") });
        Assert.AreEqual("名𠮷", users.JiebaTag("星际𠮷咖啡馆", false).Single().Tag);
    }

    [TestMethod]
    public void AllSupportedSlotsUseHeaderIds()
    {
        CollectionAssert.AreEqual(Enumerable.Range(1, 19).Select(i => (uint)i).ToArray(), Enum.GetValues<DictSlot>().Select(s => (uint)s).ToArray());
        Assert.AreEqual(1u, (uint)CustomDictMode.Append);
        Assert.AreEqual(2u, (uint)CustomDictMode.Override);
        using var converter = new OpenccJieba(Enum.GetValues<DictSlot>().Select(s => new CustomDictSpec(s, CustomDictMode.Append, [])));
        Assert.AreEqual("龍", converter.Convert("龙", "s2t"));
    }

    [TestMethod]
    public void NullCollectionsAndElementsAreRejected()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new OpenccJieba((IEnumerable<UserDictEntry>)null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => new OpenccJieba((IEnumerable<CustomDictSpec>)null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => new OpenccJieba(null!, Array.Empty<CustomDictSpec>()));
        Assert.ThrowsExactly<ArgumentNullException>(() => new OpenccJieba(Array.Empty<UserDictEntry>(), null!));
        Assert.ThrowsExactly<ArgumentException>(() => new OpenccJieba(new UserDictEntry[] { null! }));
        Assert.ThrowsExactly<ArgumentException>(() => new OpenccJieba(new CustomDictSpec[] { null! }));
        Assert.ThrowsExactly<ArgumentNullException>(() => new CustomDictSpec(DictSlot.STCharacters, CustomDictMode.Append, null!));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("x\0y")]
    public void InvalidMappingStringsAreRejected(string? value)
    {
        Assert.Throws<ArgumentException>(() => Spec(source: value!));
        Assert.Throws<ArgumentException>(() => Spec(target: value!));
    }

    [TestMethod]
    public void InvalidEnumsAreRejected()
    {
        foreach (var slot in new[] { 0u, 20u, 25u, uint.MaxValue })
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Spec((DictSlot)slot));
        foreach (var mode in new[] { 0u, 3u, uint.MaxValue })
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Spec(mode: (CustomDictMode)mode));
    }

    [TestMethod]
    public void InvalidUtf16IsRejectedBeforeMarshaling()
    {
        // Attribute strings cannot represent unpaired surrogates reliably in .NET metadata.
        foreach (var value in new[] { "\ud800", "\udc00", "x\ud800y", "\udc00\ud800" })
        {
            Assert.ThrowsExactly<ArgumentException>(() => Spec(source: value));
            Assert.ThrowsExactly<ArgumentException>(() => Spec(target: value));
            Assert.ThrowsExactly<ArgumentException>(() => new UserDictEntry(value, 1));
            Assert.ThrowsExactly<ArgumentException>(() => new UserDictEntry("word", 1, value));
            using var converter = new OpenccJieba();
            Assert.ThrowsExactly<ArgumentException>(() => converter.AddWord(value));
            Assert.ThrowsExactly<ArgumentException>(() => converter.AddWord("word", tag: value));
        }
    }

    [TestMethod]
    [DataRow("", "value", "key")]
    [DataRow("key", "", "value")]
    public void NativeEmptyMappingFailurePropagatesThroughPublicConstructor(string source, string target, string field)
    {
        // Empty mappings differ from empty collections: native conversion dictionaries reject empty keys/values.
        var error = Assert.ThrowsExactly<InvalidOperationException>(() => new OpenccJieba(new[] { Spec(source: source, target: target) }));
        Assert.Contains("custom dictionary " + field + " must not be empty", error.Message);
        using var recovered = new OpenccJieba(new[] { Spec() });
        Assert.AreEqual("訂", recovered.Convert("龙", "s2t"));
    }

    [TestMethod]
    public void InstancesRemainIndependentAcrossRepeatedDisposal()
    {
        using var standard = new OpenccJieba();
        for (var i = 0; i < 5; i++)
        {
            using var first = new OpenccJieba(new[] { Spec(target: "一") });
            var second = new OpenccJieba(new[] { Spec(target: "二") });
            Assert.AreEqual("一", first.Convert("龙", "s2t"));
            Assert.AreEqual("二", second.Convert("龙", "s2t"));
            second.Dispose(); second.Dispose();
            Assert.ThrowsExactly<ObjectDisposedException>(() => second.Convert("龙", "s2t"));
            Assert.ThrowsExactly<ObjectDisposedException>(() => second.JiebaCut("龙", false));
            Assert.AreEqual("一", first.Convert("龙", "s2t"));
        }
        Assert.AreEqual("龍", standard.Convert("龙", "s2t"));
    }

    [TestMethod]
    [DataRow(0, "slot")]
    [DataRow(4, "mode")]
    public void NativeInitializationFailurePreservesErrorAndReleasesPins(int offset, string field)
    {
        var buffers = new CustomDictionaryMarshaller(new[] { new UserDictEntry("細路哥", 3) }, new[] { Spec() });
        try
        {
            // Corrupt a valid borrowed buffer to exercise the real native failure path past managed validation.
            Marshal.WriteInt32(buffers.Specifications, offset, 999);
            var error = Assert.ThrowsExactly<InvalidOperationException>(() => buffers.CreateInstance());
            Assert.Contains(field, error.Message);
            Assert.Contains("999", error.Message);
        }
        finally { buffers.Dispose(); }
        Assert.AreEqual(IntPtr.Zero, buffers.Specifications);
        Assert.AreEqual(IntPtr.Zero, buffers.UserEntries);
        buffers.Dispose();
        Assert.ThrowsExactly<ObjectDisposedException>(() => buffers.CreateInstance());
        using var recovered = new OpenccJieba(new[] { Spec() });
        Assert.AreEqual("訂", recovered.Convert("龙", "s2t"));
    }

    [TestMethod]
    public void NativeConstructorsRejectNullArraysWithNonzeroCounts()
    {
        var instance = OpenccJiebaNative.opencc_jieba_new_custom(IntPtr.Zero, new UIntPtr(1));
        try
        {
            Assert.AreEqual(IntPtr.Zero, instance);
            Assert.Contains("specs is NULL", OpenccJieba.NativeFailure("constructor").Message);
        }
        finally { if (instance != IntPtr.Zero) OpenccJiebaNative.opencc_jieba_delete(instance); }
        instance = OpenccJiebaNative.opencc_jieba_new_user_dict(IntPtr.Zero, new UIntPtr(1));
        try
        {
            Assert.AreEqual(IntPtr.Zero, instance);
            Assert.Contains("entries is NULL", OpenccJieba.NativeFailure("constructor").Message);
        }
        finally { if (instance != IntPtr.Zero) OpenccJiebaNative.opencc_jieba_delete(instance); }
    }

    [TestMethod]
    public void NativeLayoutsArePointerSizedAndSequential()
    {
        Assert.AreEqual(IntPtr.Size * 3, Marshal.SizeOf<OpenccJiebaNative.NativeUserDictEntry>());
        Assert.AreEqual(IntPtr.Size * 2, Marshal.SizeOf<OpenccJiebaNative.NativeCustomPair>());
        Assert.AreEqual(8 + IntPtr.Size * 2, Marshal.SizeOf<OpenccJiebaNative.NativeCustomDictSpec>());
        Assert.AreEqual(new IntPtr(IntPtr.Size), Marshal.OffsetOf<OpenccJiebaNative.NativeUserDictEntry>("Frequency"));
        Assert.AreEqual(new IntPtr(8), Marshal.OffsetOf<OpenccJiebaNative.NativeCustomDictSpec>("Pairs"));
    }
}
