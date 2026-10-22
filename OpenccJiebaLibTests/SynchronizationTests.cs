using OpenccJiebaLib;

namespace OpenccJiebaLibTests;

[TestClass]
public sealed class SynchronizationTests
{
    private const string Text = "這個細路哥很靈活";

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(Text)]
    public void DisposedConvenienceMethodsPreserveDisposalPrecedence(string? input)
    {
        var converter = new OpenccJieba();
        converter.Dispose();
        AssertDisposedConvenienceMethods(converter, input);
    }

    private static void AssertDisposedConvenienceMethods(OpenccJieba converter, string? input)
    {
        Assert.ThrowsExactly<ObjectDisposedException>(() => converter.JiebaTagAsString(input!, false));
        foreach (var mode in Enum.GetValues<SegmentMode>().Append((SegmentMode)999))
        {
            Assert.ThrowsExactly<ObjectDisposedException>(() => converter.Segment(input!, mode));
            Assert.ThrowsExactly<ObjectDisposedException>(() => converter.SegmentJoin(input!, mode));
        }
#pragma warning disable CS0618 // Retained compatibility API must keep disposal behavior.
        Assert.ThrowsExactly<ObjectDisposedException>(() => converter.JiebaCutAndJoin(input!, false, null!));
#pragma warning restore CS0618
        Assert.ThrowsExactly<ObjectDisposedException>(() => converter.JiebaKeywordExtractTextRank(input!, 3));
        Assert.ThrowsExactly<ObjectDisposedException>(() => converter.JiebaKeywordExtractTfidf(input!, 3));
        Assert.ThrowsExactly<ObjectDisposedException>(() => converter.JiebaKeywordExtractTfidfWeights(input!, 3));
        Assert.ThrowsExactly<ObjectDisposedException>(() => converter.JiebaKeywordExtractTextRankWeights(input!, 3));
    }

    [TestMethod]
    public void LiveConvenienceMethodsPreserveEmptyInputModeValidationAndFormatting()
    {
        using var converter = new OpenccJieba();
        foreach (var input in new[] { null, "" })
        {
            Assert.AreSequenceEqual([], converter.JiebaTagAsString(input!, false));
            foreach (var mode in Enum.GetValues<SegmentMode>().Append((SegmentMode)999))
            {
                Assert.AreSequenceEqual([], converter.Segment(input!, mode));
                Assert.AreEqual("", converter.SegmentJoin(input!, mode));
            }
        }

        AssertInvalidMode(converter);
        var tags = converter.JiebaTag(Text, false);
        Assert.AreSequenceEqual([.. tags.Select(t => t.Word + ":" + t.Tag)],
            converter.JiebaTagAsString(Text, false, ":"));
        Assert.AreEqual(string.Concat(tags.Select(t => t.Word + t.Tag)),
            converter.SegmentJoin(Text, SegmentMode.Tag, false, null!, null!));
        Assert.AreEqual(string.Join("|", converter.JiebaCut(Text, false)),
            converter.SegmentJoin(Text, SegmentMode.Cut, false, "|"));
    }

    private static void AssertInvalidMode(OpenccJieba converter)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => converter.Segment(Text, (SegmentMode)999));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => converter.SegmentJoin(Text, (SegmentMode)999));
    }

    [TestMethod]
    public async Task ConvenienceMethodsCanRaceWithDisposal()
    {
        var converter = new OpenccJieba();
        try
        {
            var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var firstCalls = new TaskCompletionSource[3];
            var workers = new Task[3];
            var inputs = new[] { null, "", Text };
            for (var i = 0; i < workers.Length; i++)
            {
                firstCalls[i] = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                workers[i] = RunConvenienceCallsAsync(converter, inputs[i], start.Task, firstCalls[i]);
            }

            start.SetResult();
            try
            {
                foreach (var firstCall in firstCalls)
                    await firstCall.Task.WaitAsync(TimeSpan.FromSeconds(20));
                converter.Dispose();
            }
            finally
            {
                // Borrowers finish before final cleanup, including on failure.
                await Task.WhenAll(workers);
            }

            AssertDisposedConvenienceMethods(converter, Text);
        }
        finally
        {
            converter.Dispose();
        }
    }

    private static async Task RunConvenienceCallsAsync(OpenccJieba converter, string? input,
        Task start, TaskCompletionSource firstCall)
    {
        await start;
        for (var i = 0; i < 100; i++)
        {
            try
            {
                converter.JiebaTagAsString(input!, false);
                foreach (var mode in Enum.GetValues<SegmentMode>())
                {
                    converter.Segment(input!, mode, false);
                    converter.SegmentJoin(input!, mode, false);
                }

                firstCall.TrySetResult();
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            await Task.Yield();
        }
    }

    [TestMethod]
    public async Task AddWordAndConvenienceReadsPreserveSegmentationAndConversion()
    {
        var specs = new[]
        {
            new CustomDictSpec(DictSlot.HKPhrasesRev, CustomDictMode.Append,
                [KeyValuePair.Create("細路哥", "小男孩")])
        };
        using var converter = new OpenccJieba(new[] { new UserDictEntry("細路哥", 3) }, specs);
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var workers = new Task[3];
        for (var i = 0; i < workers.Length; i++)
            workers[i] = RunMutationOrReadAsync(converter, i, start.Task);
        start.SetResult();
        await Task.WhenAll(workers);
    }

    private static async Task RunMutationOrReadAsync(OpenccJieba converter, int worker, Task start)
    {
        await start;
        for (var i = 0; i < 40; i++)
        {
            switch (worker)
            {
                case 0:
                    converter.AddWord("細路哥", 3);
                    break;
                case 1:
                    Assert.AreEqual("這個/細路哥/很/靈活", converter.SegmentJoin(Text, SegmentMode.Cut, true, "/"));
                    Assert.AreEqual("細路哥", converter.JiebaTag(Text, true)[1].Word);
                    break;
                case 2:
                    Assert.AreEqual("这个小男孩很灵活", converter.Convert(Text, OpenccConfig.HK2SP));
                    Assert.AreEqual("这个小男孩很灵活", converter.Convert(Text, "hk2sp"));
                    break;
            }

            await Task.Yield();
        }
    }
}