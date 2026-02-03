using OpenccJiebaLib;

namespace OpenccJiebaLibTests;

[TestClass]
public sealed class OpenccJiebaTests
{
    private readonly OpenccJieba _openccJieba = new();

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
    public void JiebaCutAndJoin_ShouldReturnJoinedSegments()
    {
        // Arrange
        const string input = "我来到北京清华大学"; // Example Chinese input
        const bool hmm = true;
        const string delimiter = "|";

        // Act
        var result = _openccJieba.JiebaCutAndJoin(input, hmm, delimiter);

        // Assert
        Assert.IsNotNull(result, "JiebaCutAndJoin returned null.");
        Assert.AreNotEqual(string.Empty, result, "JiebaCutAndJoin returned an empty string.");

        // Check if the output is joined correctly
        var expectedSegments = new[] { "我", "来到", "北京", "清华大学" };
        var expectedJoined = string.Join(delimiter, expectedSegments);

        Assert.AreEqual(expectedJoined, result, "The joined segmented string does not match the expected output.");
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
                JiebaKeywordAlgorithmExtensions.TryParse(s, out var algo),
                "TryParse should accept: " + s
            );

            var native = algo.ToNativeMethod();
            Assert.IsTrue(
                native == "tfidf" || native == "textrank",
                "Native method should be canonical for: " + s + " => " + native
            );

            // Ensure mapping is stable and canonical
            if (s.IndexOf("tf", StringComparison.OrdinalIgnoreCase) >= 0)
                Assert.AreEqual("tfidf", native, "Expected TF-IDF canonical method for: " + s);

            if (s.IndexOf("rank", StringComparison.OrdinalIgnoreCase) >= 0)
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
    public void JiebaExtractKeywordsWeights_InvalidMethod_ThrowsArgumentException()
    {
        // Arrange
        const string input = "测试文本";
        const int topK = 5;
        const string invalid = "bm25";

        try
        {
            // Act
            _openccJieba.JiebaExtractKeywordsWeights(input, topK, invalid);
            Assert.Fail("Expected ArgumentException was not thrown.");
        }
        catch (ArgumentException ex)
        {
            // Assert
            Assert.Contains("Invalid keyword algorithm", ex.Message);
        }
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