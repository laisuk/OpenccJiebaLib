using OpenccJiebaLib;

namespace OpenccJiebaLibTests;

#pragma warning disable CS0618 // Exercise disposal and synchronization of the retained legacy API.

[TestClass]
public sealed class AddWordTests
{
    private const string Word = "星际量子咖啡馆";

    [TestMethod]
    public void AddAndUpdateWordImmediatelyChangesSegmentationAndTagging()
    {
        using var converter = new OpenccJieba();
        Assert.AreNotEqual(1, converter.JiebaCut(Word, false).Length);
        converter.AddWord(Word, 100000, "nz");
        CollectionAssert.AreEqual(new[] { Word }, converter.JiebaCut(Word, false));
        Assert.AreEqual("nz", converter.JiebaTag(Word, false).Single().Tag);
        converter.AddWord(Word, 200000, "nt");
        // Jieba 0.7.4 updates frequency but retains the original tag for an existing word.
        Assert.AreEqual("nz", converter.JiebaTag(Word, false).Single().Tag);
        // Update a built-in word as well as the newly inserted entry.
        var originalTag = converter.JiebaTag("北京", false).Single().Tag;
        converter.AddWord("北京", 100000, "custom");
        Assert.AreEqual(originalTag, converter.JiebaTag("北京", false).Single().Tag);
    }

    [TestMethod]
    public void NullFrequencyIsDistinctFromExplicitZero()
    {
        using var converter = new OpenccJieba();
        converter.AddWord(Word, tag: "nz");
        CollectionAssert.AreEqual(new[] { Word }, converter.JiebaCut(Word, false));
        converter.AddWord(Word, 0);
        Assert.IsGreaterThan(1, converter.JiebaCut(Word, false).Length);
        converter.AddWord(Word, 100000);
        CollectionAssert.AreEqual(new[] { Word }, converter.JiebaCut(Word, false));
        converter.AddWord(Word, null, "nz");
        CollectionAssert.AreEqual(new[] { Word }, converter.JiebaCut(Word, false));
        Assert.AreEqual("nz", converter.JiebaTag(Word, false).Single().Tag);
    }

    [TestMethod]
    public void NullAndEmptyTagsAndSupplementaryUnicodeAreSupported()
    {
        using var converter = new OpenccJieba();
        converter.AddWord(Word, 100000, "");
        CollectionAssert.AreEqual(new[] { Word }, converter.JiebaCut(Word, false));
        converter.AddWord(Word, 100000, null);
        converter.AddWord("星际𠮷咖啡馆", 100000, "名𠮷");
        Assert.AreEqual("名𠮷", converter.JiebaTag("星际𠮷咖啡馆", false).Single().Tag);
    }

    [TestMethod]
    public void AddWordEnablesExistingConversionMappingAndDoesNotAffectOtherInstances()
    {
        var specs = new[] { new CustomDictSpec(DictSlot.HKPhrasesRev, CustomDictMode.Append,
            [KeyValuePair.Create("細路哥", "小男孩")]) };
        using var first = new OpenccJieba(specs);
        using var second = new OpenccJieba(specs);
        first.AddWord("細路哥", 3);
        Assert.AreEqual("这个小男孩很灵活", first.Convert("這個細路哥很靈活", "hk2sp"));
        Assert.AreEqual("这个细路哥很灵活", second.Convert("這個細路哥很靈活", "hk2sp"));
        for (var i = 0; i < 20; i++) first.AddWord(Word, (ulong)(100000 + i), "nz");
        CollectionAssert.AreEqual(new[] { Word }, first.JiebaCut(Word, false));
        Assert.IsGreaterThan(1, second.JiebaCut(Word, false).Length);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("bad word")]
    [DataRow("bad\tword")]
    [DataRow("bad\u3000word")]
    [DataRow("bad\u0085word")]
    [DataRow("bad\0word")]
    public void InvalidWordsAreRejectedByBothApis(string? word)
    {
        Assert.Throws<ArgumentException>(() => new UserDictEntry(word!, 1));
        using var converter = new OpenccJieba();
        Assert.Throws<ArgumentException>(() => converter.AddWord(word!));
        Assert.AreEqual("龍", converter.Convert("龙", "s2t"));
    }

    [TestMethod]
    [DataRow("n z")]
    [DataRow("n\tz")]
    [DataRow("n\u3000z")]
    [DataRow("n\0z")]
    public void InvalidTagsAreRejectedByBothApis(string tag)
    {
        Assert.Throws<ArgumentException>(() => new UserDictEntry(Word, 1, tag));
        using var converter = new OpenccJieba();
        Assert.Throws<ArgumentException>(() => converter.AddWord(Word, tag: tag));
    }

    [TestMethod]
    public void FrequencyUsesProcessSizeT()
    {
        // Range validation alone avoids intentionally overflowing Jieba's aggregate frequency.
        if (UIntPtr.Size == 4)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new UserDictEntry(Word, (ulong)uint.MaxValue + 1));
            using var converter = new OpenccJieba();
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => converter.AddWord(Word, (ulong)uint.MaxValue + 1));
        }
        else
        {
            Assert.AreEqual(ulong.MaxValue, new UserDictEntry(Word, ulong.MaxValue).Frequency);
            Assert.AreEqual(ulong.MaxValue, DictionaryValidation.Frequency(ulong.MaxValue).ToUInt64());
        }
    }

    [TestMethod]
    public void NativeFailureIncludesDetailsAndSuccessClearsStaleError()
    {
        // A valid public call is prevalidated; use a NULL native instance to force a deterministic ABI failure.
        Assert.IsFalse(OpenccJiebaNative.opencc_jieba_add_word(IntPtr.Zero, DictionaryValidation.Utf8Z(Word), false, UIntPtr.Zero, null!));
        var error = OpenccJieba.NativeFailure("Failed to add Jieba word.");
        Assert.Contains("NULL", error.Message);
        Assert.Contains("Failed to add Jieba word", error.Message);
        using var converter = new OpenccJieba();
        // Seed a stale error after construction, which itself clears errors.
        Assert.IsFalse(OpenccJiebaNative.opencc_jieba_add_word(IntPtr.Zero, DictionaryValidation.Utf8Z(Word), false, UIntPtr.Zero, null!));
        converter.AddWord(Word, 100000, "nz");
        Assert.AreEqual("read No error", OpenccJieba.NativeFailure("read").Message);
        OpenccJiebaNative.opencc_jieba_clear_last_error();
    }

    [TestMethod]
    public void DisposedInstanceRejectsNativeOperationsIncludingEmptyInput()
    {
        var converter = new OpenccJieba();
        converter.Dispose(); converter.Dispose();
        foreach (var input in new[] { "", Word })
        {
            Assert.ThrowsExactly<ObjectDisposedException>(() => converter.AddWord(input));
            Assert.ThrowsExactly<ObjectDisposedException>(() => converter.Convert(input, "s2t"));
            Assert.ThrowsExactly<ObjectDisposedException>(() => converter.Convert(input, OpenccConfig.S2T));
            Assert.ThrowsExactly<ObjectDisposedException>(() => converter.ZhoCheck(input));
            Assert.ThrowsExactly<ObjectDisposedException>(() => converter.JiebaCut(input, false));
            Assert.ThrowsExactly<ObjectDisposedException>(() => converter.JiebaCutAll(input));
            Assert.ThrowsExactly<ObjectDisposedException>(() => converter.JiebaCutForSearch(input, false));
            Assert.ThrowsExactly<ObjectDisposedException>(() => converter.JiebaTag(input, false));
            Assert.ThrowsExactly<ObjectDisposedException>(() => converter.JiebaCutAndJoin(input, false, "/"));
            Assert.ThrowsExactly<ObjectDisposedException>(() => converter.Segment(input, SegmentMode.Cut));
            Assert.ThrowsExactly<ObjectDisposedException>(() => converter.SegmentJoin(input, SegmentMode.Cut));
            Assert.ThrowsExactly<ObjectDisposedException>(() => converter.JiebaKeywordExtract(input, 3, JiebaKeywordAlgorithm.Tfidf));
            Assert.ThrowsExactly<ObjectDisposedException>(() => converter.JiebaExtractKeywordsWeights(input, 3, JiebaKeywordAlgorithm.Tfidf));
        }
    }

    [TestMethod]
    public async Task MutationReadsAndDisposalCanRunConcurrently()
    {
        using var converter = new OpenccJieba();
        using var start = new ManualResetEventSlim();
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tasks = Enumerable.Range(0, 8).Select(worker => Task.Run(() =>
        {
            start.Wait();
            for (var i = 0; i < 40; i++)
            {
                try
                {
                    switch (worker)
                    {
                        case 0: converter.AddWord(Word, 100000, "nz"); ready.TrySetResult(); break;
                        case 1: Assert.AreEqual("龍", converter.Convert("龙", "s2t")); break;
                        case 2: converter.JiebaCut(Word, false); converter.JiebaCutAll(Word); break;
                        case 3: converter.JiebaTag(Word, false); converter.JiebaCutForSearch(Word, false); break;
                        case 4: converter.ZhoCheck(Word); converter.JiebaCutAndJoin(Word, false, "/"); break;
                        case 5: converter.JiebaKeywordExtract(Word, 3, JiebaKeywordAlgorithm.Tfidf); break;
                        case 6: converter.JiebaExtractKeywordsWeights(Word, 3, JiebaKeywordAlgorithm.Tfidf); break;
                        case 7: converter.SegmentJoin(Word, SegmentMode.Tag); break;
                    }
                }
                catch (ObjectDisposedException) { return; }
            }
        })).ToArray();
        start.Set();
        await ready.Task.WaitAsync(TimeSpan.FromSeconds(20));
        converter.Dispose();
        await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(20));
        Assert.ThrowsExactly<ObjectDisposedException>(() => converter.AddWord(Word));
    }
}
