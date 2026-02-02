# OpenccJiebaLib

[![NuGet](https://img.shields.io/nuget/v/OpenccJiebaLib.svg)](https://www.nuget.org/packages/OpenccJiebaLib/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/OpenccJiebaLib.svg?label=downloads\&color=blue)](https://www.nuget.org/packages/OpenccJiebaLib/)
[![License](https://img.shields.io/github/license/laisuk/OpenccJiebaLib.svg)](https://github.com/laisuk/OpenccJiebaLib/blob/master/LICENSE)

A .NET Standard 2.0 library providing a managed C# wrapper for the Rust-based OpenCC and Jieba C API, enabling efficient
Chinese text conversion (Simplified/Traditional), segmentation, and keyword extraction in .NET applications.

## Features

* **Chinese Text Conversion**: Convert between Simplified, Traditional, and other Chinese variants using OpenCC.
* **Word Segmentation**: Segment Chinese text into words using Jieba.
* **Keyword Extraction**: Extract keywords using TF-IDF or TextRank algorithms.
* **Native Performance**: Leverages native OpenCC/Jieba libraries for high performance.

## Supported OpenCC Configurations

`s2t`, `t2s`, `s2tw`, `tw2s`, `s2twp`, `tw2sp`, `s2hk`, `hk2s`, `t2tw`,
`t2twp`, `t2hk`, `tw2t`, `tw2tp`, `hk2t`, `t2jp`, `jp2t`

## Getting Started

### Prerequisites

* .NET Standard 2.0 or higher (.NET Framework, .NET Core/5+/6+, Mono, Xamarin, etc.).
* .NET 6.0 or later recommended.
* Native **`opencc_jieba_capi`** library (must be available to the runtime).

### Installation

#### Option 1 — As Project Reference

* Add a project reference to **OpenccJiebaLib** in your solution.
* Provide the native binary so it can be found at runtime.

Recommended layout (same as NuGet):

```
runtimes/<RID>/native/
```

Expected filenames:

* Windows: `opencc_jieba_capi.dll`
* Linux: `libopencc_jieba_capi.so`
* macOS: `libopencc_jieba_capi.dylib`

> 🧪 **Unit tests** (MSTest/xUnit/nUnit) also need the native binaries in the test project’s output folder.
> Use the same copy strategy as above or add a `Target` to auto-copy natives after build.

#### Option 2 — From NuGet

```sh
dotnet add package OpenccJiebaLib
```

* The NuGet package includes platform-specific native runtimes and deploys them under:

```
runtimes/<RID>/native/
```

> **Shipped RIDs:** `win-x64`, `linux-x64`, `osx-arm64`
> Other RIDs can be supported via the drop-in mechanism below.

---

## Custom native runtimes (drop-in)

OpenccJiebaLib loads the native library from the standard NuGet layout:

```
runtimes/<RID>/native/<library>
```

To add support for another platform, simply drop in your own native binary:

1. Create the directory:

```
runtimes/<RID>/native/
```

2. Copy the native library using the expected filename:

* Windows: `opencc_jieba_capi.dll`
* Linux: `libopencc_jieba_capi.so`
* macOS: `libopencc_jieba_capi.dylib`

Example (Intel macOS):

```
runtimes/osx-x64/native/libopencc_jieba_capi.dylib
```

The loader will pick it up automatically at runtime.

---

## Usage

```csharp
using OpenccJiebaLib;

using (var openccJieba = new OpenccJieba())
{
    string traditional = openccJieba.Convert("汉字转换测试", "s2t");
    string[] words = openccJieba.JiebaCut("我来到北京清华大学", hmm: true);

    string[] keywords = openccJieba.JiebaKeywordExtractTfidf(
        "这是一个用于关键词提取的测试文本", 5);

    var (kw, weights) = openccJieba.JiebaExtractKeywordsWeights(
        "这是一个用于关键词提取的测试文本", 5, "textrank");
}
```

## Error Handling

If initialization fails or a native error occurs, an `InvalidOperationException` is thrown.

## API Overview

* `Convert(string input, string config, bool punctuation = false)`
* `Convert(string input, OpenccConfig configId, bool punctuation = false)`
* `JiebaCut(string input, bool hmm)`
* `JiebaCutAndJoin(string input, bool hmm, string delimiter)`
* `JiebaKeywordExtractTfidf(string input, int topK)`
* `JiebaKeywordExtractTextRank(string input, int topK)`
* `JiebaExtractKeywordsWeights(string input, int topK, string method)`

## Troubleshooting

### `DllNotFoundException`

* Ensure the native file exists under `runtimes/<RID>/native/`.
* If using NuGet, clean and rebuild the project.

### `BadImageFormatException`

* Architecture mismatch (x86 vs x64).

### Platform Notes

* **Linux**: may require `LD_LIBRARY_PATH`.
* **macOS**: remove Gatekeeper quarantine flags:

```bash
xattr -dr com.apple.quarantine libopencc_jieba_capi.dylib
```

## License

MIT License. See [LICENSE](https://github.com/laisuk/OpenccJiebaLib/blob/master/LICENSE).

## Acknowledgements

* OpenCC
* Jieba
* opencc-jieba-rs
