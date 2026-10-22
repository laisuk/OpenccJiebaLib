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
`t2twp`, `t2hk`, `tw2t`, `tw2tp`, `hk2t`, `t2jp`, `jp2t`, `s2hkp`, `hk2sp`,
`t2hkp`, `hk2tp`

The phrase-aware Hong Kong configurations added in native v0.8.0 are:

- `s2hkp`: Simplified Chinese to Hong Kong Traditional Chinese with phrase conversion.
- `hk2sp`: Hong Kong Traditional Chinese to Simplified Chinese with phrase conversion.
- `t2hkp`: Traditional Chinese to Hong Kong Traditional Chinese with phrase conversion.
- `hk2tp`: Hong Kong Traditional Chinese to Traditional Chinese with phrase conversion.

## Getting Started

### Prerequisites

* .NET Standard 2.0 or higher (.NET Framework, .NET Core/5+/6+, Mono, Xamarin, etc.).
* .NET 6.0 or later recommended.
* Native **`opencc_jieba_capi`** library (must be available to the runtime).

### Installation

#### Option 1 — Project Reference

* Add a project reference to **OpenccJiebaLib** in your solution.
* Ensure the native binary is available at runtime in the standard layout:

```
runtimes/<RID>/native/
```

Expected filenames:

* Windows: `opencc_jieba_capi.dll`
* Linux: `libopencc_jieba_capi.so`
* macOS: `libopencc_jieba_capi.dylib`

> 🧪 **Unit tests** (MSTest/xUnit/nUnit) also need the native binaries in the test project’s output folder.
> Use the same copy strategy (copy `runtimes/**` into `bin/…`) or add an MSBuild `Target` to auto-copy natives after
> build.

---

#### Option 2 — From NuGet

```sh
dotnet add package OpenccJiebaLib
```

* The NuGet package includes platform-specific native runtimes under:

```
runtimes/<RID>/native/
```

**Shipped RIDs:** `win-x64`, `linux-arm64`, `linux-x64`, `osx-arm64`

> When publishing with `-r <RID>`, `dotnet publish` copies only the matching native runtime into the publishing output.

---

## Custom native runtimes (drop-in)

OpenccJiebaLib loads the native library from the standard NuGet layout:

```
runtimes/<RID>/native/<library>
```

To add support for another platform, drop in your own native binary:

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
    string traditional = openccJieba.Convert("汉字转换测试", OpenccConfig.S2T);
    string hongKong = openccJieba.Convert("鼠标", OpenccConfig.S2HKP);

    string[] searchTokens = openccJieba.Segment(
        "我来到北京清华大学",
        SegmentMode.Search,
        hmm: true);

    string tagged = openccJieba.SegmentJoin(
        "我来到北京清华大学",
        SegmentMode.Tag,
        hmm: true,
        delimiter: " ",
        separator: "/");

    string[] keywords = openccJieba.JiebaKeywordExtract(
        "这是一个用于关键词提取的测试文本",
        5,
        JiebaKeywordAlgorithm.Tfidf);

    var (kw, weights) = openccJieba.JiebaExtractKeywordsWeights(
        "这是一个用于关键词提取的测试文本",
        5,
        JiebaKeywordAlgorithm.TextRank);

    int script = openccJieba.ZhoCheck("汉字转换测试");
}
```

String-based overloads are still available for configuration names such as `"s2t"` and keyword methods such as
`"textrank"`, but the enum-based APIs are recommended for new code.

## Custom dictionaries and dynamic Jieba words

These APIs require native `opencc_jieba_capi` **v0.8.1.1 or later**. Windows x64 has been
validated against v0.8.1.1. The other bundled RID binaries are unchanged and await synchronization;
their new APIs have not been validated. An older native library missing a required export raises
`EntryPointNotFoundException` when that API is used.

OpenCC dictionaries change conversion mappings. Jieba user entries change segmentation and POS
tagging. They can be supplied separately or together:

```csharp
using System.Collections.Generic;
using OpenccJiebaLib;

var custom = new[]
{
    new CustomDictSpec(DictSlot.STCharacters, CustomDictMode.Append,
        new[] { new KeyValuePair<string, string>("龙", "龍龍") })
};
using (var converter = new OpenccJieba(custom))
{
    string result = converter.Convert("龙", "s2t"); // 龍龍
}

var users = new[] { new UserDictEntry("帕兰蒂尔", 100000, "nz") };
using (var converter = new OpenccJieba(users))
{
    string[] tokens = converter.JiebaCut("帕兰蒂尔", false); // ["帕兰蒂尔"]
}

var phrases = new[]
{
    new CustomDictSpec(DictSlot.STPhrases, CustomDictMode.Append,
        new[] { new KeyValuePair<string, string>("帕兰蒂尔", "柏蘭蒂爾") })
};
using (var converter = new OpenccJieba(users, phrases))
{
    string result = converter.Convert("帕兰蒂尔是一家公司", "s2t"); // 柏蘭蒂爾是一家公司
    converter.AddWord("星际量子咖啡馆", frequency: 100000, tag: "nz");
    string[] tokens = converter.JiebaCut("星际量子咖啡馆", false); // ["星际量子咖啡馆"]
    converter.AddWord("自动选频词"); // null frequency lets Jieba choose
    converter.AddWord("星际量子咖啡馆", frequency: 0); // explicit zero disables the word
}
```

`CustomDictSpec` takes an immutable, ordered snapshot of mappings. Constructors snapshot the
collections, pin UTF-8 buffers only for the synchronous call, and release them after native code
copies the data. Caller collections can be changed afterward. Collections and entries cannot be
null. Strings must be valid Unicode without embedded NUL characters. Jieba words must also be
nonempty and contain no whitespace; supplied POS tags must contain no whitespace.

`Append = 1` merges into a slot, replacing duplicate keys with the last mapping. `Override = 2`
clears that slot before adding mappings. Specifications apply in order. Empty constructor
collections use the built-in dictionaries; an empty Append mapping list leaves a slot unchanged,
and an empty Override list clears it. Empty mapping keys or values fail native initialization with
an `InvalidOperationException` containing the native error. `DictSlot` exposes only the 19 slots
in the Jieba header, with their exact native IDs; punctuation and Seal slots are not supported.
Select the slot used by the conversion configuration passed to `Convert`.

A custom OpenCC phrase can match only within Jieba's segmentation boundaries. For example,
`HKPhrasesRev` mapping `細路哥 → 小男孩` alone leaves `這個細路哥很靈活` as
`这个细路哥很灵活` under `hk2sp`, because Jieba splits `細路 / 哥`. Also supplying
`new UserDictEntry("細路哥", 3)` yields `這個 / 細路哥 / 很 / 靈活` and
`这个小男孩很灵活`. Calling `AddWord("細路哥", 3)` on that custom instance enables
the same phrase mapping immediately.

`UserDictEntry.Frequency` is required. `AddWord` accepts an optional `ulong?` frequency:
null requests automatic frequency, while zero is passed explicitly. Frequencies must fit the
current process's native `size_t`. A null tag passes native NULL; an empty tag passes an empty
string. In the bundled Jieba, a tag is assigned when a word is first inserted; updating an existing
word changes its frequency and retains its original tag. `AddWord` changes only that instance's
Jieba dictionary and does not change OpenCC slots or recreate the native instance.

Native operations, `AddWord`, and disposal are serialized per instance. Different instances can
run concurrently. Disposal is idempotent, and operations after disposal throw
`ObjectDisposedException`. Constructor and insertion failures preserve native error details.

## Error Handling

Common exception types:

- `DllNotFoundException`: the native OpenCC-Jieba library cannot be found.
- `EntryPointNotFoundException`: the native library does not export a required function.
- `BadImageFormatException`: the native library does not match the current process architecture.
- `InvalidOperationException`: native initialization or native call failures after the library has loaded.
- `ArgumentOutOfRangeException`: invalid enum values passed to `OpenccConfig`, `SegmentMode`, or `JiebaKeywordAlgorithm`
  based APIs.
- `ArgumentException`: unsupported keyword method names. Invalid string configuration names passed to
  `Convert(string, string, bool)` intentionally fall back to `s2t`; use `OpenccConfigExtensions.Parse` for strict
  validation.
- `ArgumentNullException`: `null` keyword method passed to `JiebaExtractKeywordsWeights(..., string method, ...)`.

---

## Public API Overview

### Construction and Lifetime

These constructors create an `OpenccJieba` instance; configuration is selected per conversion call.

- `OpenccJieba()`
  Creates an instance with built-in OpenCC and Jieba dictionaries.
- `OpenccJieba(IEnumerable<UserDictEntry> userDictionaryEntries)`
  Adds initial Jieba segmentation entries while retaining built-in conversion mappings.
- `OpenccJieba(IEnumerable<CustomDictSpec> customDictionarySpecifications)`
  Applies ordered custom OpenCC mappings while retaining built-in Jieba segmentation.
-
`OpenccJieba(IEnumerable<UserDictEntry> userDictionaryEntries, IEnumerable<CustomDictSpec> customDictionarySpecifications)`
Supplies both Jieba entries and custom OpenCC mappings.
- `void AddWord(string word, ulong? frequency = null, string tag = null)`
  Adds or updates a Jieba word on this instance immediately. Null frequency requests automatic selection;
  zero explicitly disables the word. An existing word retains its original POS tag.
- `void Dispose()`
  Implements `IDisposable` for deterministic native resource cleanup. Repeated calls are safe;
  operations after disposal throw `ObjectDisposedException`.

Custom constructors and `AddWord` require native v0.8.1.1 or later. Native operations and disposal
are serialized per instance.
See [Custom dictionaries and dynamic Jieba words](#custom-dictionaries-and-dynamic-jieba-words)
for validation, ownership, merge semantics, and bundled runtime availability.

### Dictionary Types

- `CustomDictSpec(DictSlot slot, CustomDictMode mode, IEnumerable<KeyValuePair<string, string>> entries)`
  Creates an immutable, ordered snapshot of conversion mappings.
- `DictSlot CustomDictSpec.Slot { get; }`
  Gets the native conversion dictionary slot.
- `CustomDictMode CustomDictSpec.Mode { get; }`
  Gets the merge mode.
- `IReadOnlyList<KeyValuePair<string, string>> CustomDictSpec.Entries { get; }`
  Gets the read-only snapshot in input order.
- `UserDictEntry(string word, ulong frequency, string tag = null)`
  Creates an immutable, validated Jieba entry with an explicit frequency that must fit native `size_t`.
- `string UserDictEntry.Word { get; }`, `ulong UserDictEntry.Frequency { get; }`, `string UserDictEntry.Tag { get; }`
  Expose the word, frequency, and optional POS tag. Null and empty tags remain distinct.
- `DictSlot : uint`
  Defines the 19 native conversion slots, from `STCharacters` (1) through `JPSPhrases` (19).
- `CustomDictMode : uint`
  Defines `Append` (1) to merge entries and `Override` (2) to clear the selected slot before applying entries.

---

### Conversion

- `Convert(string input, string config, bool punctuation = false)`
- `Convert(string input, OpenccConfig configId, bool punctuation = false)`
- `OpenccConfig` defines all 20 numeric conversion IDs, including the phrase-aware Hong Kong configurations.
- `OpenccConfigExtensions.DefaultConfig()` returns `OpenccConfig.S2T`.
- `OpenccConfigExtensions.TryParseConfig(string name, out OpenccConfig configId)` returns whether a canonical name is
  supported;
  on failure, `configId` is `default`.
- `OpenccConfigExtensions.TryGetConfigName(OpenccConfig configId, out string name)` returns a canonical lowercase name,
  or `false` with an empty string for an undefined ID.
- `OpenccConfigExtensions.IsValidConfig(string name)`
- `OpenccConfigExtensions.Parse(string name)`
- `OpenccConfigExtensions.ToCanonicalName(this OpenccConfig config)`

`Convert` returns a `string`. Its string overload accepts case-insensitive names and falls back to `s2t`
for unsupported names. The managed configuration helpers do not invoke native code: `Parse` provides strict
validation and throws for null or unsupported names; `ToCanonicalName` throws for undefined enum values.

---

### Segmentation & Tagging

- `Segment(string input, SegmentMode mode, bool hmm = true)`
- `SegmentJoin(string input, SegmentMode mode, bool hmm = true, string delimiter = " ", string separator = "/")`
- `ZhoCheck(string input)`

Legacy (deprecated):

- `JiebaCutAndJoin(string input, bool hmm, string delimiter)`

Low-level methods:

- `JiebaCut(string input, bool hmm)`
- `JiebaCutForSearch(string input, bool hmm)`
- `JiebaCutAll(string input)`
- `JiebaTag(string input, bool hmm)`
- `JiebaTagAsString(string input, bool hmm, string separator = "/")`

`Segment` returns `string[]`; `SegmentJoin` returns a single joined `string`.
`SegmentMode` defines `Cut` (accurate), `Search` (finer-grained), `Full` (all matching tokens),
and `Tag` (tokens formatted as `word/tag`). `hmm` enables Hidden Markov Model support where applicable
and is ignored in `Full` mode. `SegmentJoin` uses `delimiter` between tokens and `separator` between
words and POS tags in `Tag` mode.

`JiebaCut`, `JiebaCutForSearch`, `JiebaCutAll`, and `JiebaTagAsString` return `string[]`.
`JiebaTag` returns `JiebaTagItem[]` for structured POS results:

- `JiebaTagItem(string word, string tag)` creates a readonly token/tag value, converting null arguments to empty
  strings.
- `string JiebaTagItem.Word { get; }` gets the token text.
- `string JiebaTagItem.Tag { get; }` gets the POS tag.
- `string JiebaTagItem.ToString()` formats the value as `word/tag`.

`ZhoCheck` returns an `int`: 0 for other text, 1 for Traditional Chinese, or 2 for Simplified Chinese.
The obsolete `JiebaCutAndJoin` returns a `string`; use `SegmentJoin` with `SegmentMode.Cut` instead.

---

### Keyword Extraction

- `JiebaKeywordAlgorithm` defines `Tfidf` (1) and `TextRank` (2).
- `KeywordAlgorithmExtensions.TryParse(string value, out JiebaKeywordAlgorithm algorithm)`
- `KeywordAlgorithmExtensions.Parse(string value)`
- `KeywordAlgorithmExtensions.ToNativeMethod(this JiebaKeywordAlgorithm algorithm)`
- `JiebaKeywordExtract(string input, int topK, JiebaKeywordAlgorithm algorithm, string allowedPos = "")`
- `JiebaKeywordExtractTfidf(string input, int topK, string allowedPos = "")`
- `JiebaKeywordExtractTextRank(string input, int topK, string allowedPos = "")`

Weighted results:

- `JiebaExtractKeywordsWeights(string input, int topK, string method, string allowedPos = "")`
- `JiebaExtractKeywordsWeights(string input, int topK, JiebaKeywordAlgorithm algorithm, string allowedPos = "")`
- `JiebaKeywordExtractTfidfWeights(string input, int topK, string allowedPos = "")`
- `JiebaKeywordExtractTextRankWeights(string input, int topK, string allowedPos = "")`

Unweighted extractors return `string[]`. Weighted extractors return
`(string[] keywords, double[] weights)`, with weights aligned to keywords by index.
`topK` limits the number of results; nonpositive values return empty results. `allowedPos` is a space-separated
POS filter (for example, `"n nr ns"`); an empty string disables filtering.

`KeywordAlgorithmExtensions` helpers run entirely in managed code. Parsing is case-insensitive, trims surrounding
whitespace, and accepts `tfidf`, `tf-idf`, `tf_idf`, `textrank`, `text-rank`, and `text_rank`.
`TryParse` returns `false` with a default enum value for unsupported input; `Parse` throws `ArgumentException`.
`ToNativeMethod` returns `"tfidf"` or `"textrank"` and throws `ArgumentOutOfRangeException` for undefined values.

---

### Utilities

- `static int GetNativeAbiNumber()` returns the native binary interface version for compatibility diagnostics.
- `static string GetNativeVersionString()` returns the native library version for logging and diagnostics.

---

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

---

## License

MIT License. See [LICENSE](https://github.com/laisuk/OpenccJiebaLib/blob/master/LICENSE).

## Acknowledgements

* OpenCC
* Jieba
* opencc-jieba-rs
