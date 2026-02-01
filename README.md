# OpenccJiebaLib

[![NuGet](https://img.shields.io/nuget/v/OpenccJiebaLib.svg)](https://www.nuget.org/packages/OpenccJiebaLib/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/OpenccJiebaLib.svg?label=downloads&color=blue)](https://www.nuget.org/packages/OpenccJiebaLib/)
[![License](https://img.shields.io/github/license/laisuk/OpenccJiebaLib.svg)](https://github.com/laisuk/OpenccJiebaLib/blob/master/LICENSE)

A .NET Standard 2.0 library providing a managed C# wrapper for the Rust-based OpenCC and Jieba C API, enabling efficient
Chinese text conversion (Simplified/Traditional), segmentation, and keyword extraction in .NET applications.

## Features

- **Chinese Text Conversion**: Convert between Simplified, Traditional, and other Chinese variants using OpenCC.
- **Word Segmentation**: Segment Chinese text into words using Jieba.
- **Keyword Extraction**: Extract keywords using TF-IDF or TextRank algorithms.
- **Native Performance**: Leverages native OpenCC/Jieba libraries for high performance.

## Supported OpenCC Configurations

`s2t`, `t2s`, `s2tw`, `tw2s`, `s2twp`, `tw2sp`, `s2hk`, `hk2s`, `t2tw`,  
`t2twp`, `t2hk`, `tw2t`, `tw2tp`, `hk2t`, `t2jp`, `jp2t`

## Getting Started

### Prerequisites

- .NET Standard 2.0 or higher (.NET Framework, .NET Core/5+/6+, Mono, Xamarin, etc.).
- .NET 6.0 or later recommended.
- Native **`opencc_jieba_capi`** library (must be available to the runtime).

### Installation

#### Option 1 — As Project Reference

- Add a project reference to **OpenccJiebaLib** in your solution.
- **Manually copy** the native binary to your app’s output directory (`bin/<Config>/<TFM>`):
    - Windows: `opencc_jieba_capi.dll`
    - Linux: `libopencc_jieba_capi.so`
    - macOS: `libopencc_jieba_capi.dylib`
- Alternative: mark the native file **Copy to Output Directory: Copy always/if newer**.

> 🧪 **Unit tests** (MSTest/xUnit/nUnit) also need the native binaries in the test project’s output folder. Use the same
> copy strategy as above or add a `Target` to auto-copy natives after build.

#### Option 2 — From NuGet

- Install via NuGet:
  ```sh
  dotnet add package OpenccJiebaLib
  ```  
- The NuGet package includes platform-specific native runtimes and will **automatically deploy them**. No manual copying
  needed.

### Usage

```csharp
using OpenccJiebaLib;

using (var openccJieba = new OpenccJieba())
{
    // Convert Simplified → Traditional
    string traditional = openccJieba.Convert("汉字转换测试", "s2t");
    Console.WriteLine(traditional); // 漢字轉換測試

    // Segment text
    string[] words = openccJieba.JiebaCut("我来到北京清华大学", hmm: true);
    // => ["我", "来到", "北京", "清华大学"]

    // Extract keywords (TF-IDF)
    string[] keywords = openccJieba.JiebaKeywordExtractTfidf("这是一个用于关键词提取的测试文本", topK: 5);
    // 提取/ 关键词/ 测试/ 用于/ 文本

    // Extract keywords with weights (TextRank)
    var (kw, weights) = openccJieba.JiebaExtractKeywordsWeights("这是一个用于关键词提取的测试文本", 5, "textrank");  
    // Keywords Weights TextRank: [('提取', 12214076549.586092), ('关键词', 12213038715.272404), ('测试', 9971894336.779804), ('用于', 9968689471.76825), ('文本', 7771637141.591653)]
}
```

### Error Handling

If initialization fails or a native error occurs, an `InvalidOperationException` is thrown.  

## API Overview

- `Convert(string input, string config, bool punctuation = false)`
- `Convert(string input, OpenccConfig configId, bool punctuation = false)`
- `JiebaCut(string input, bool hmm)`
- `JiebaCutAndJoin(string input, bool hmm, string delimiter)`
- `JiebaKeywordExtractTfidf(string input, int topK)`
- `JiebaKeywordExtractTextRank(string input, int topK)`
- `JiebaExtractKeywordsWeights(string input, int topK, string method)`

## Troubleshooting

### 1) `DllNotFoundException` / `Unable to load shared library 'opencc_jieba_capi'`

- Ensure the native file exists in your app output folder or is discoverable via PATH/LD_LIBRARY_PATH.
- If using NuGet, clean + rebuild (natives are auto-copied).

### 2) `BadImageFormatException`

- Architecture mismatch. Match your app (x64 vs x86) with the native build.

### 3) Platform-specific Notes

- **Linux:** may require `LD_LIBRARY_PATH` adjustment if `.so` not next to the app.
- **macOS:** remove Gatekeeper quarantine flags for `.dylib`:
  ```bash
  xattr -dr com.apple.quarantine libopencc_jieba_capi.dylib
  ```

### 4) Crashes / Thread Safety

- Create separate `OpenccJieba` instances per thread, or ensure calls are thread-safe.
- Dispose properly after use (`using` block recommended).

> ✅ **Tip:** NuGet is easiest for handling natives. Use manual copy only when debugging custom native builds.

## License

This project is licensed under the MIT License.  
See [LICENSE](https://github.com/laisuk/OpenccJiebaLib/blob/master/LICENSE) for details.

## Acknowledgements

- [OpenCC](https://github.com/BYVoid/OpenCC)
- [jieba](https://github.com/fxsjy/jieba)
- [opencc-jieba-rs](https://github.com/laisuk/opencc-jieba-rs)

---

*Powered by **OpenCC** and **Jieba**. C# wrapper by [laisuk](https://github.com/laisuk).*
