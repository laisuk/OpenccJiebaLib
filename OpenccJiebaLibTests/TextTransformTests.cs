using OpenccJiebaLib;

namespace OpenccJiebaLibTests;

[TestClass]
public sealed class TextTransformTests
{
    [TestMethod]
    public void NormalizationUsesBuiltInTablesAndPreservesOtherText()
    {
        using var converter = new OpenccJieba();
        Assert.AreEqual("豈 ABC 😀", converter.NormalizeCompat("\uF900 ABC 😀"));
        Assert.AreEqual("一", converter.NormalizeCompatExtended("\u2F00"));
        Assert.AreEqual("\u2F00", converter.NormalizeCompat("\u2F00"));
        foreach (var level in Enum.GetValues<DeTofuLevel>())
            Assert.AreEqual("汉字 ABC 😀", converter.DeTofu("汉字 ABC 😀", level));
    }

    [TestMethod]
    public void TransformsValidateInputAndKeepTheInstanceUsable()
    {
        var converter = new OpenccJieba();
        try
        {
            foreach (var transform in Transforms(converter))
            {
                Assert.AreEqual("", transform(null!));
                Assert.AreEqual("", transform(""));
                foreach (var invalid in new[] { "a\0b", "\uD800", "\uDC00" })
                    Assert.AreEqual("text",
                        Assert.ThrowsExactly<ArgumentException>(() => transform(invalid)).ParamName);
                Assert.AreEqual("ABC 😀", transform("ABC 😀"));
            }

            foreach (var input in new[] { null, "", "text" })
                AssertInvalidDeTofuLevel(converter, input);
            Assert.AreEqual("漢字", converter.Convert("汉字", OpenccConfig.S2T));
        }
        finally
        {
            converter.Dispose();
        }
    }

    [TestMethod]
    public void DisposalTakesPrecedenceOverTextAndLevelValidation()
    {
        var converter = new OpenccJieba();
        converter.Dispose();
        foreach (var transform in Transforms(converter))
        foreach (var input in new[] { null, "", "a\0b", "text" })
            Assert.ThrowsExactly<ObjectDisposedException>(() => transform(input!));
        Assert.ThrowsExactly<ObjectDisposedException>(() => converter.DeTofu(null!, (DeTofuLevel)uint.MaxValue));
    }

    private static void AssertInvalidDeTofuLevel(OpenccJieba converter, string? input)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => converter.DeTofu(input!, (DeTofuLevel)uint.MaxValue));
    }

    private static Func<string, string>[] Transforms(OpenccJieba converter) =>
    [
        converter.NormalizeCompat,
        converter.NormalizeCompatExtended,
        text => converter.DeTofu(text, DeTofuLevel.ExtB)
    ];
}