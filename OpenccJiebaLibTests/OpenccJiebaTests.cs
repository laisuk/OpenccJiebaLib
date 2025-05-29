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
        var result = _openccJieba.Convert("这是一项意大利商务项目", "s2twp");
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
        string result = _openccJieba.JiebaCutAndJoin(input, hmm, delimiter);

        // Assert
        Assert.IsNotNull(result, "JiebaCutAndJoin returned null.");
        Assert.AreNotEqual(string.Empty, result, "JiebaCutAndJoin returned an empty string.");

        // Check if the output is joined correctly
        var expectedSegments = new[] { "我", "来到", "北京", "清华大学" };
        string expectedJoined = string.Join(delimiter, expectedSegments);

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
    public void TestJiebaExtractKeywords()
    {
        // Arrange
        const string input = "该剧讲述三位男女在平安夜这一天各自的故事。平安夜的0点，横滨山下码头发生枪杀事件。";
        const int topK = 5; // Number of top keywords to extract
        const string method = "textrank";

        // Act
        var (keywords, weights) = _openccJieba.JiebaExtractKeywords(input, topK, method);

        // Assert
        Assert.IsNotNull(keywords, "Keywords should not be null.");
        Assert.IsNotNull(weights, "Weights should not be null.");
        Assert.AreEqual(topK, keywords.Length, "The number of extracted keywords does not match the expected count.");
        Assert.AreEqual(topK, weights.Length, "The number of extracted weights does not match the expected count.");

        // Additional assertions can be made on the keywords and weights if expected values are known
        // For example:
        Console.WriteLine("Extracted Keywords:");
        for (var i = 0; i < keywords.Length; i++)
        {
            Console.WriteLine($"Keyword: {keywords[i]}, Weight: {weights[i]}");
        }
    }
}