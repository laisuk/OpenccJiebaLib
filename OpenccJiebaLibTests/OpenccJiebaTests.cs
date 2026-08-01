using OpenccJiebaLib;

namespace OpenccJiebaLibTests;

[TestClass]
public sealed class OpenccJiebaTests
{
    private readonly OpenccJieba _openccJieba = new();

    [TestCleanup]
    public void Cleanup()
    {
        _openccJieba.Dispose();
    }

    [TestMethod]
    public void Convert_Test()
    {
        var result = _openccJieba.Convert("龙马精神", "s2t");
        Assert.AreEqual("龍馬精神", result);
    }

    [TestMethod]
    public void Convert_s2twp_Test()
    {
        const string config = "S2Twp";
        Assert.IsTrue(OpenccConfigExtensions.IsValidConfig(config));
        var result = _openccJieba.Convert("这是一项意大利商务项目", config);
        Assert.AreEqual("這是一項義大利商務專案", result);
    }

    [TestMethod]
    public void Convert_configId_s2twp_Test()
    {
        var result = _openccJieba.Convert("这是一项意大利商务项目", OpenccConfig.S2TWP);
        Assert.AreEqual("這是一項義大利商務專案", result);
    }

    [TestMethod]
    public void OpenccConfig_HongKongPhraseConfigs_MatchNativeV080()
    {
        var configs = new[]
        {
            (Name: "s2hkp", Config: OpenccConfig.S2HKP, NativeId: 17),
            (Name: "hk2sp", Config: OpenccConfig.HK2SP, NativeId: 18),
            (Name: "t2hkp", Config: OpenccConfig.T2HKP, NativeId: 19),
            (Name: "hk2tp", Config: OpenccConfig.HK2TP, NativeId: 20)
        };

        foreach (var item in configs)
        {
            Assert.AreEqual(item.NativeId, (int)item.Config);
            Assert.IsTrue(OpenccConfigExtensions.TryParseConfig(item.Name.ToUpperInvariant(), out var parsed));
            Assert.AreEqual(item.Config, parsed);
            Assert.AreEqual(item.Name, item.Config.ToCanonicalName());
            Assert.IsTrue(OpenccConfigExtensions.TryGetConfigName(item.Config, out var canonicalName));
            Assert.AreEqual(item.Name, canonicalName);
        }
    }

    [TestMethod]
    public void Convert_HongKongPhraseConfigs_Test()
    {
        Assert.AreEqual("滑鼠", _openccJieba.Convert("鼠标", OpenccConfig.S2HKP));
        Assert.AreEqual("鼠标", _openccJieba.Convert("滑鼠", OpenccConfig.HK2SP));
        Assert.AreEqual("滑鼠", _openccJieba.Convert("鼠標", OpenccConfig.T2HKP));
        Assert.AreEqual("鼠標", _openccJieba.Convert("滑鼠", OpenccConfig.HK2TP));
    }

    [TestMethod]
    public void ConvertWithPunct_Test()
    {
        var result = _openccJieba.Convert("“龙马精神”", "s2tw", true);
        Assert.AreEqual("「龍馬精神」", result);
    }

    [TestMethod]
    public void Change_Conversion_Test()
    {
        var result = _openccJieba.Convert("龙马精神", "s2t");
        Assert.AreEqual("龍馬精神", result);
        var result1 = _openccJieba.Convert("龍馬精神", "t2s");
        Assert.AreEqual("龙马精神", result1);
    }

    [TestMethod]
    public void ZhoCheck_Test()
    {
        var result = _openccJieba.ZhoCheck("龙马精神");
        Assert.AreEqual(2, result);
    }

    [TestMethod]
    public void JiebaCut_ShouldReturnCorrectSegments()
    {
        // Arrange
        const string input = "我来到北京清华大学"; // Example Chinese input
        const bool hmm = true;

        // Act
        string[] result = _openccJieba.JiebaCut(input, hmm);

        // Assert
        Assert.IsNotNull(result, "JiebaCut returned null.");
        Assert.AreNotEqual(0, result.Length, "JiebaCut returned an empty array.");

        // Check for expected segmented results (the actual results may vary based on the segmentation algorithm and dictionary)
        var expectedSegments = new[] { "我", "来到", "北京", "清华大学" };
        CollectionAssert.AreEqual(expectedSegments, result, "The segmented words do not match the expected output.");
    }

    [TestMethod]
    public void JiebaCutForSearch_ShouldReturnCorrectSegments()
    {
        // Arrange
        const string input = "我来到北京清华大学";
        const bool hmm = true;

        // Act
        string[] result = _openccJieba.JiebaCutForSearch(input, hmm);

        // Assert
        Assert.IsNotNull(result, "JiebaCutForSearch returned null.");
        Assert.AreNotEqual(0, result.Length, "JiebaCutForSearch returned an empty array.");

        // Search mode is finer-grained, so just check key tokens exist
        CollectionAssert.Contains(result, "我");
        CollectionAssert.Contains(result, "来到");
        CollectionAssert.Contains(result, "北京");
        CollectionAssert.Contains(result, "清华大学");

        // Optional: search mode usually splits more
        Assert.IsGreaterThanOrEqualTo(4, result.Length, "Search mode should produce finer-grained tokens.");
    }

    [TestMethod]
    public void JiebaCutAll_ShouldReturnCorrectSegments()
    {
        // Arrange
        const string input = "我来到北京清华大学";

        // Act
        string[] result = _openccJieba.JiebaCutAll(input);

        // Assert
        Assert.IsNotNull(result, "JiebaCutAll returned null.");
        Assert.AreNotEqual(0, result.Length, "JiebaCutAll returned an empty array.");

        // Full mode returns all possible words, so only check key presence
        CollectionAssert.Contains(result, "我");
        CollectionAssert.Contains(result, "来到");
        CollectionAssert.Contains(result, "北京");
        CollectionAssert.Contains(result, "清华大学");

        // Optional: full mode should be >= cut mode
        Assert.IsGreaterThanOrEqualTo(4, result.Length, "Full mode should produce equal or more tokens than cut mode.");
    }

    [TestMethod]
    public void JiebaTag_ShouldReturnTaggedSegments()
    {
        // Arrange
        const string input = "我来到北京清华大学";
        const bool hmm = true;

        // Act
        var result = _openccJieba.JiebaTag(input, hmm);

        // Assert basic correctness
        Assert.IsNotNull(result, "JiebaTag returned null.");
        Assert.AreNotEqual(0, result.Length, "JiebaTag returned an empty array.");

        // Print output (for first-run inspection)
        Console.WriteLine("JiebaTag Output:");
        foreach (var item in result)
        {
            Console.Write($"{item.Word}/{item.Tag} ");
        }

        Console.WriteLine();

        // Minimal correctness checks (non-strict)
        Assert.Contains(x => x.Word == "我", result, "Missing token: 我");
        Assert.Contains(x => x.Word == "来到", result, "Missing token: 来到");
        Assert.Contains(x => x.Word == "北京", result, "Missing token: 北京");
        Assert.Contains(x => x.Word == "清华大学", result, "Missing token: 清华大学");

        // Ensure tags exist
        Assert.IsTrue(result.All(x => !string.IsNullOrEmpty(x.Tag)), "Some tokens have empty tags.");
    }

    [TestMethod]
    public void JiebaTag_ShouldReturnCorrectTaggedSegments()
    {
        // Arrange
        const string input = "我来到北京清华大学";
        const bool hmm = true;

        // Act
        var result = _openccJieba.JiebaTag(input, hmm);

        // Assert
        Assert.IsNotNull(result, "JiebaTag returned null.");
        Assert.AreNotEqual(0, result.Length, "JiebaTag returned an empty array.");

        var expected = new[]
        {
            new JiebaTagItem("我", "r"),
            new JiebaTagItem("来到", "v"),
            new JiebaTagItem("北京", "ns"),
            new JiebaTagItem("清华大学", "nt"),
        };

        Assert.HasCount(expected.Length, result, "Unexpected number of tagged tokens.");

        for (var i = 0; i < expected.Length; i++)
        {
            Assert.AreEqual(expected[i].Word, result[i].Word, $"Word mismatch at index {i}.");
            Assert.AreEqual(expected[i].Tag, result[i].Tag, $"Tag mismatch at index {i}.");
        }
    }

    [TestMethod]
    public void JiebaSegment_ShouldReturnCorrectSegments()
    {
        // Arrange
        const string input = "我来到北京清华大学";
        // const bool hmm = true;

        // Act
        string[] result = _openccJieba.Segment(input, SegmentMode.Search);

        // Assert
        Assert.IsNotNull(result, "JiebaCutForSearch returned null.");
        Assert.AreNotEqual(0, result.Length, "JiebaCutForSearch returned an empty array.");

        // Search mode is finer-grained, so just check key tokens exist
        CollectionAssert.Contains(result, "我");
        CollectionAssert.Contains(result, "来到");
        CollectionAssert.Contains(result, "北京");
        CollectionAssert.Contains(result, "清华大学");

        // Optional: search mode usually splits more
        Assert.IsGreaterThanOrEqualTo(4, result.Length, "Search mode should produce finer-grained tokens.");
    }

    [TestMethod]
    public void JiebaCutAndJoin_ShouldReturnJoinedSegments()
    {
        // Arrange
        const string input = "我来到北京清华大学"; // Example Chinese input
        const bool hmm = true;
        const string delimiter = "|";

        // Act
        var result = _openccJieba.SegmentJoin(input, SegmentMode.Cut, hmm, delimiter);

        // Assert
        Assert.IsNotNull(result, "JiebaCutAndJoin returned null.");
        Assert.AreNotEqual(string.Empty, result, "JiebaCutAndJoin returned an empty string.");

        // Check if the output is joined correctly
        var expectedSegments = new[] { "我", "来到", "北京", "清华大学" };
        var expectedJoined = string.Join(delimiter, expectedSegments);

        Assert.AreEqual(expectedJoined, result, "The joined segmented string does not match the expected output.");
    }

    [TestMethod]
    public void JiebaTagAsString_ShouldReturnFormattedOutput()
    {
        const string input = "我来到北京清华大学";
        const bool hmm = true;

        var result = _openccJieba.JiebaTagAsString(input, hmm);

        CollectionAssert.AreEqual(
            new[] { "我/r", "来到/v", "北京/ns", "清华大学/nt" },
            result
        );
    }

    [TestMethod]
    public void JiebaTagAsStringWithSeparator_ShouldReturnFormattedOutput()
    {
        const string input = "我来到北京清华大学";
        const bool hmm = true;

        var result = _openccJieba.JiebaTagAsString(input, hmm, ":");

        CollectionAssert.AreEqual(
            new[] { "我:r", "来到:v", "北京:ns", "清华大学:nt" },
            result
        );
    }

    [TestMethod]
    public void JiebaKeywordExtractTextRank_Test()
    {
        // Arrange
        const string input = "我来到北京清华大学"; // Example Chinese input
        const int topK = 5;

        // Act
        string[] result = _openccJieba.JiebaKeywordExtractTextRank(input, topK);
        foreach (var keyword in result)
        {
            Console.WriteLine(keyword);
        }


        // Assert
        Assert.IsNotNull(result, "JiebaKeyword returned null.");
        Assert.AreNotEqual(0, result.Length, "JiebaKeyword returned an empty array.");

        // Check for expected segmented results (the actual results may vary based on the segmentation algorithm and dictionary)
        var expectedSegments = new[] { "清华大学", "北京", "来到", "我" };
        CollectionAssert.AreEqual(expectedSegments, result, "The segmented words do not match the expected output.");
    }

    [TestMethod]
    public void JiebaKeywordExtractTfidf_Test()
    {
        // Arrange
        const string input = "我来到北京清华大学"; // Example Chinese input
        const int topK = 5;

        // Act
        string[] result = _openccJieba.JiebaKeywordExtractTfidf(input, topK);
        foreach (var keyword in result)
        {
            Console.WriteLine(keyword);
        }


        // Assert
        Assert.IsNotNull(result, "JiebaKeyword returned null.");
        Assert.AreNotEqual(0, result.Length, "JiebaKeyword returned an empty array.");

        // Check for expected segmented results (the actual results may vary based on the segmentation algorithm and dictionary)
        var expectedSegments = new[] { "清华大学", "来到", "北京" };
        CollectionAssert.AreEqual(expectedSegments, result, "The segmented words do not match the expected output.");
    }


    [TestMethod]
    public void TestJiebaExtractKeywordsWeights()
    {
        // Arrange
        const string input = "该剧讲述三位男女在平安夜这一天各自的故事。平安夜的0点，横滨山下码头发生枪杀事件。";
        const int topK = 5; // Number of top keywords to extract
        const JiebaKeywordAlgorithm method = JiebaKeywordAlgorithm.TextRank;

        // Act
        var (keywords, weights) = _openccJieba.JiebaExtractKeywordsWeights(input, topK, method);

        // Assert
        Assert.IsNotNull(keywords, "Keywords should not be null.");
        Assert.IsNotNull(weights, "Weights should not be null.");
        Assert.HasCount(topK, keywords, "The number of extracted keywords does not match the expected count.");
        Assert.HasCount(topK, weights, "The number of extracted weights does not match the expected count.");
        Assert.IsGreaterThanOrEqualTo(weights[1], weights[0]);
        Assert.IsGreaterThanOrEqualTo(weights[2], weights[1]);


        // Additional assertions can be made on the keywords and weights if expected values are known
        // For example:
        Console.WriteLine("Extracted Keywords:");
        for (var i = 0; i < keywords.Length; i++)
        {
            Console.WriteLine($"Keyword: {keywords[i]}, Weight: {weights[i]}");
        }
    }

    [TestMethod]
    public void JiebaKeywordAlgorithm_TryParse_NormalizesCommonVariants()
    {
        // Arrange
        var inputs = new[]
        {
            "tfidf", "TFIDF", "TfIdF", "tf-idf", "TF-IDF", "tf_idf", "  tfidf  ",
            "textrank", "TextRank", "TEXTRANK", "text-rank", "TEXT-RANK", "text_rank", "  textrank  "
        };

        // Act + Assert
        foreach (var s in inputs)
        {
            Assert.IsTrue(
                KeywordAlgorithmExtensions.TryParse(s, out var algo),
                "TryParse should accept: " + s
            );

            var native = algo.ToNativeMethod();
            Assert.IsTrue(
                native is "tfidf" or "textrank",
                "Native method should be canonical for: " + s + " => " + native
            );

            // Ensure mapping is stable and canonical
            if (s.Contains("tf", StringComparison.OrdinalIgnoreCase))
                Assert.AreEqual("tfidf", native, "Expected TF-IDF canonical method for: " + s);

            if (s.Contains("rank", StringComparison.OrdinalIgnoreCase))
                Assert.AreEqual("textrank", native, "Expected TextRank canonical method for: " + s);
        }
    }

    [TestMethod]
    public void JiebaExtractKeywordsWeights_StringMethod_NormalizesCaseAndAliases()
    {
        // Arrange
        const string input = "该剧讲述三位男女在平安夜这一天各自的故事。平安夜的0点，横滨山下码头发生枪杀事件。";
        const int topK = 5;

        // A few representative variants (don’t need all)
        var methods = new[] { "TextRank", "text_rank", "TEXT-RANK", "tf-idf", "TFIDF" };

        foreach (var method in methods)
        {
            // Act
            var (keywords, weights) = _openccJieba.JiebaExtractKeywordsWeights(input, topK, method);

            // Assert
            Assert.IsNotNull(keywords, "Keywords should not be null for method: " + method);
            Assert.IsNotNull(weights, "Weights should not be null for method: " + method);
            Assert.HasCount(topK, keywords, "Keyword count mismatch for method: " + method);
            Assert.HasCount(topK, weights, "Weight count mismatch for method: " + method);
        }
    }

    [TestMethod]
    public void JiebaExtractKeywordsWeights_StringMethod_WithAllowedPos_Works()
    {
        // Arrange
        const string input = "该剧讲述三位男女在平安夜这一天各自的故事。平安夜的0点，横滨山下码头发生枪杀事件。";
        const int topK = 5;
        const string allowedPos = "n nr ns nt nz v vn";

        var methods = new[] { "TextRank", "text_rank", "TF-IDF", "tfidf" };

        foreach (var method in methods)
        {
            // Act
            var (keywords, weights) = _openccJieba.JiebaExtractKeywordsWeights(input, topK, method, allowedPos);

            // Assert
            Assert.IsNotNull(keywords, "Keywords should not be null for method: " + method);
            Assert.IsNotNull(weights, "Weights should not be null for method: " + method);
            Assert.HasCount(keywords.Length, weights, "Keywords/weights length mismatch for method: " + method);
            Assert.IsLessThanOrEqualTo(topK, keywords.Length,
                "Keyword count should not exceed topK for method: " + method);

            for (var i = 0; i < keywords.Length; i++)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(keywords[i]),
                    "Keyword should not be null/empty at index " + i + " for method: " + method);
            }
        }
    }

    [TestMethod]
    public void JiebaExtractKeywordsWeights_EnumMethod_WithAllowedPos_Works()
    {
        // Arrange
        const string input = "春眠不觉晓，处处闻啼鸟。夜来风雨声，花落知多少。";
        const int topK = 5;
        const string allowedPos = "n v vn";
        var methods = new[]
        {
            JiebaKeywordAlgorithm.Tfidf,
            JiebaKeywordAlgorithm.TextRank
        };

        foreach (var method in methods)
        {
            // Act
            var (keywords, weights) = _openccJieba.JiebaExtractKeywordsWeights(input, topK, method, allowedPos);

            // Assert
            Assert.IsNotNull(keywords, "Keywords should not be null for method: " + method);
            Assert.IsNotNull(weights, "Weights should not be null for method: " + method);
            Assert.HasCount(keywords.Length, weights, "Keywords/weights length mismatch for method: " + method);
            Assert.IsLessThanOrEqualTo(topK, keywords.Length,
                "Keyword count should not exceed topK for method: " + method);

            for (var i = 0; i < keywords.Length; i++)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(keywords[i]),
                    "Keyword should not be null/empty at index " + i + " for method: " + method);
            }
        }
    }

    [TestMethod]
    public void JiebaKeywordExtractTfidfWeights_WithAllowedPos_Works()
    {
        // Arrange
        const string input = "该剧讲述三位男女在平安夜这一天各自的故事。平安夜的0点，横滨山下码头发生枪杀事件。";
        const int topK = 5;
        const string allowedPos = "n nr ns nt nz";

        // Act
        var (keywords, weights) = _openccJieba.JiebaKeywordExtractTfidfWeights(input, topK, allowedPos);

        // Assert
        Assert.IsNotNull(keywords);
        Assert.IsNotNull(weights);
        Assert.HasCount(keywords.Length, weights);
        Assert.IsLessThanOrEqualTo(topK, keywords.Length);
    }

    [TestMethod]
    public void JiebaKeywordExtractTextRankWeights_WithAllowedPos_Works()
    {
        // Arrange
        const string input = "该剧讲述三位男女在平安夜这一天各自的故事。平安夜的0点，横滨山下码头发生枪杀事件。";
        const int topK = 5;
        const string allowedPos = "n nr ns nt nz";

        // Act
        var (keywords, weights) = _openccJieba.JiebaKeywordExtractTextRankWeights(input, topK, allowedPos);

        // Assert
        Assert.IsNotNull(keywords);
        Assert.IsNotNull(weights);
        Assert.HasCount(keywords.Length, weights);
        Assert.IsLessThanOrEqualTo(topK, keywords.Length);
    }

    [TestMethod]
    public void JiebaExtractKeywordsWeights_EnumMethod_DefaultAllowedPos_Works()
    {
        // Arrange
        const string input = "该剧讲述三位男女在平安夜这一天各自的故事。平安夜的0点，横滨山下码头发生枪杀事件。";
        const int topK = 5;

        // Act
        var (keywords, weights) = _openccJieba.JiebaExtractKeywordsWeights(
            input,
            topK,
            JiebaKeywordAlgorithm.TextRank);

        // Assert
        Assert.IsNotNull(keywords);
        Assert.IsNotNull(weights);
        Assert.HasCount(keywords.Length, weights);
        Assert.IsLessThanOrEqualTo(topK, keywords.Length);
    }

    [TestMethod]
    public void JiebaExtractKeywordsWeights_InvalidMethod_ThrowsArgumentException()
    {
        // Arrange
        const string input = "测试文本";
        const int topK = 5;
        const string invalid = "bm25";

        ArgumentException? ex = null;

        try
        {
            // Act
            _openccJieba.JiebaExtractKeywordsWeights(input, topK, invalid);
            Assert.Fail("Expected ArgumentException was not thrown.");
        }
        catch (ArgumentException e)
        {
            ex = e;
        }

        // Assert
        Assert.Contains("Invalid keyword algorithm", ex.Message);
    }

    [TestMethod]
    [DataRow(null, 5)]
    [DataRow("", 5)]
    [DataRow("测试文本", 0)]
    public void JiebaKeywordExtract_InvalidEnum_ThrowsBeforeEarlyReturn(string? input, int topK)
    {
        var invalid = (JiebaKeywordAlgorithm)999;

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            _openccJieba.JiebaKeywordExtract(input!, topK, invalid));
    }

    [TestMethod]
    [DataRow(null, 5)]
    [DataRow("", 5)]
    [DataRow("测试文本", 0)]
    public void JiebaExtractKeywordsWeights_InvalidEnum_ThrowsBeforeEarlyReturn(string? input, int topK)
    {
        var invalid = (JiebaKeywordAlgorithm)999;

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            _openccJieba.JiebaExtractKeywordsWeights(input!, topK, invalid));
    }

    [TestMethod]
    public void Convert_WithInvalidEnumValue_ThrowsArgumentOutOfRangeException()
    {
        var invalid = (OpenccConfig)999;
        ArgumentOutOfRangeException? ex = null;

        try
        {
            _openccJieba.Convert("测试文本", invalid);
            Assert.Fail("Expected ArgumentOutOfRangeException was not thrown.");
        }
        catch (ArgumentOutOfRangeException e)
        {
            ex = e;
        }

        Assert.IsNotNull(ex);
        Assert.AreEqual("config", ex.ParamName);
    }

    [TestMethod]
    public void Segment_WithInvalidMode_ThrowsArgumentOutOfRangeException()
    {
        var invalid = (SegmentMode)999;
        ArgumentOutOfRangeException? ex = null;

        try
        {
            _openccJieba.Segment("我来到北京清华大学", invalid);
            Assert.Fail("Expected ArgumentOutOfRangeException was not thrown.");
        }
        catch (ArgumentOutOfRangeException e)
        {
            ex = e;
        }

        Assert.IsNotNull(ex);
        Assert.AreEqual("mode", ex.ParamName);
    }

    [TestMethod]
    public void SegmentJoin_WithInvalidMode_ThrowsArgumentOutOfRangeException()
    {
        var invalid = (SegmentMode)999;
        ArgumentOutOfRangeException? ex = null;

        try
        {
            _openccJieba.SegmentJoin("我来到北京清华大学", invalid);
            Assert.Fail("Expected ArgumentOutOfRangeException was not thrown.");
        }
        catch (ArgumentOutOfRangeException e)
        {
            ex = e;
        }

        Assert.IsNotNull(ex);
        Assert.AreEqual("mode", ex.ParamName);
    }

    [TestMethod]
    public void AbiNoAndVersionStringTest()
    {
        var abiNum = OpenccJieba.GetNativeAbiNumber();
        var abiVersion = OpenccJieba.GetNativeVersionString();

        Assert.AreEqual(1, abiNum, "AbiNum should be 1.");

        Assert.IsFalse(string.IsNullOrWhiteSpace(abiVersion), "Version string should not be empty.");

        var parts = abiVersion.Split('.');
        Assert.HasCount(3, parts, "Version should have format x.y.z.");

        foreach (var part in parts)
        {
            Assert.IsTrue(
                int.TryParse(part, out var value) && value >= 0,
                $"Version component '{part}' must be a non-negative integer."
            );
        }
    }
}
