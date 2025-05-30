# OpenccJiebaLib

A .NET Standard 2.0 library providing a managed C# wrapper for the OpenCC and Jieba C API, enabling efficient Chinese text conversion (Simplified/Traditional) and segmentation/keyword extraction in .NET applications.

## Features

- **Chinese Text Conversion**: Convert between Simplified, Traditional, and other Chinese variants using OpenCC.
- **Word Segmentation**: Segment Chinese text into words using Jieba.
- **Keyword Extraction**: Extract keywords using TF-IDF or TextRank algorithms.
- **Native Performance**: Leverages native OpenCC/Jieba libraries for high performance.

## Requirements

- .NET Standard 2.0 or higher (.NET Core, .NET Framework, Mono, Xamarin, etc.)
- Native `opencc_jieba_capi` library (must be available in your application's runtime path)

## Installation

1. **Build or obtain the native `opencc_jieba_capi` library** for your platform (Windows, Linux, macOS).
2. Place the native library in your application's output directory or ensure it is discoverable via your system's PATH.
3. Add `OpenccJiebaLib` to your .NET project (copy the source or add as a project reference).

## Usage
```csharp
using OpenccJiebaLib;
// Create an instance (allocates native resources) using (var openccJieba = new OpenccJieba()) { // Convert Simplified to Traditional Chinese string traditional = openccJieba.Convert("汉字转换测试", "s2t");
// Segment text
string[] words = openccJieba.JiebaCut("我来到北京清华大学", hmm: true);

// Extract keywords (TF-IDF)
string[] keywords = openccJieba.JiebaKeywordExtractTfidf("这是一个用于关键词提取的测试文本", topK: 5);

// Extract keywords with weights (TextRank)
var (kw, weights) = openccJieba.JiebaExtractKeywords("这是一个用于关键词提取的测试文本", 5, "textrank");
}
```
## Supported OpenCC Configurations

- `s2t`, `t2s`, `s2tw`, `tw2s`, `s2twp`, `tw2sp`, `s2hk`, `hk2s`, `t2tw`, `t2twp`, `t2hk`, `tw2t`, `tw2tp`, `hk2t`, `t2jp`, `jp2t`

## API Overview

- `Convert(string input, string config, bool punctuation = false)`
- `JiebaCut(string input, bool hmm)`
- `JiebaCutAndJoin(string input, bool hmm, string delimiter)`
- `JiebaKeywordExtractTfidf(string input, int topK)`
- `JiebaKeywordExtractTextRank(string input, int topK)`
- `JiebaExtractKeywords(string input, int topK, string method)`

## Notes

- `OpenccJieba` instance is self-dispose to free native resources.
- The native library must be present and compatible with your platform/architecture.

## License

[MIT](LICENSE.txt)

## Acknowledgements

- [OpenCC](https://github.com/BYVoid/OpenCC)
- [jieba](https://github.com/fxsjy/jieba)
- [opencc-jieba-rs](https://github.com/laisuk/opencc-jieba-rs)

---

*Powered by OpenCC and Jieba. C# wrapper by [laisuk].*

## API Reference
