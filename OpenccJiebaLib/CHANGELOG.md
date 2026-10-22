# Changelog

All notable changes to this project will be documented in this file.

This project adheres to [Semantic Versioning](https://semver.org/).

---

[1.4.0] - Unreleased

### Changed

- Windows x64 native `opencc-jieba-capi` updated and validated against v0.8.1.1.
  Other RID binaries remain unchanged and await synchronization; new APIs require native v0.8.1.1 or later.
- Serialized native instance operations and disposal to protect dynamic Jieba mutation.
- Native version regression checks accept the optional C API revision component.

### Added

- Immutable `CustomDictSpec`, `CustomDictMode`, and the 19 native `DictSlot` values.
- Immutable `UserDictEntry` and constructor overloads for Jieba user entries, OpenCC conversion mappings, or both.
- Exception-safe pinned UTF-8 constructor marshaling, strict Unicode/NUL validation, and native error propagation.
- Dynamic `AddWord(word, frequency, tag)` with automatic/zero frequency semantics and disposal protection.
  Existing words retain their original POS tag according to bundled Jieba behavior.
- Regression coverage for dictionary ordering, Append/Override, Unicode, ownership, native failures,
  concurrent mutation/disposal, and Jieba/OpenCC phrase interaction.

---

## [1.3.0] - 2026-08-01

### Changed

- Updated `opencc-jieba-capi` native to v0.8.0.
- Strongly typed keyword extraction now rejects invalid `JiebaKeywordAlgorithm` values consistently,
  including when the input is empty or `topK` is not positive.

### Added

- Added the phrase-aware Hong Kong configurations `s2hkp`, `hk2sp`, `t2hkp`, and
  `hk2tp` to `OpenccConfig` and its parsing/canonical-name helpers.
- Documented the new Hong Kong conversion configurations and enum usage.

### Fixed

- Corrected public XML documentation for native instance use, native-loading exceptions,
  segmentation failures, and keyword extraction exceptions.
- Ensured tests dispose each native `OpenccJieba` instance deterministically.

---

## [1.2.2] - 2026-04-21

### Added

- Update native `opencc-jieba-capi` to v0.7.5
- Added param `separator` in segment mode `tag` when joined as string

---

## [1.2.1] - 2026-03-24

### Fixed

- Added missing XML documentation comments for several public APIs.
- Improved IntelliSense descriptions for segmentation and tagging methods.

---

## [1.2.0] - 2026-03-23

### Added

- Added `OpenccConfig` enum for strongly typed OpenCC conversion configuration.
- Added `Convert(string, OpenccConfig, bool)` overload for type-safe conversion calls.
- Added `GetNativeAbiNumber` and `GetNativeVersionString`.
- Added `linux-arm64` runtimes.
- Added `JiebaCutForSearch(string, bool)` for search-mode segmentation.
- Added `JiebaCutAll(string)` for full-mode segmentation.
- Added `JiebaTag(string, bool)` for part-of-speech tagging.
- Added `JiebaTagAsString(string, bool)` for tag output in `"word/tag"` string form.
- Added unified `Segment(string, SegmentMode, bool)` API for cut, search, full, and tag modes.
- Added unified `SegmentJoin(string, SegmentMode, bool, string)` API for joined segmentation/tagging output.
- Added `JiebaKeywordAlgorithm` enum for strongly typed Jieba keyword extraction algorithm selection.
- Added `JiebaKeywordExtract(string, int, JiebaKeywordAlgorithm, string)` unified keyword extraction API with optional
  POS filtering.
- Added `JiebaExtractKeywordsWeights(string, int, JiebaKeywordAlgorithm, string)` overload for weighted keyword
  extraction with strongly typed algorithm selection and optional POS filtering.
- Added support for native `_pos()` keyword extraction APIs, including `opencc_jieba_keywords_pos` and
  `opencc_jieba_keywords_and_weights_pos`.

### Changed

- Deprecated `JiebaCutAndJoin(string, bool, string)` in favor of `SegmentJoin(string, SegmentMode, bool, string)`.
- Refactored native P/Invoke bindings into a dedicated native interop class.
- Simplified conversion API by removing internal config state (`SetConfig` / `GetConfig`);
  configuration is now provided per `Convert()` call.
- Improved UTF-8 interop by centralizing null-terminated encoding helpers (`*Utf8Z`)
  and reusing pooled buffers where appropriate.
- Refactored keyword extraction APIs to use shared internal helpers and support optional POS filtering via
  space-separated `allowedPos` strings.
- Updated `opencc-jieba-rs` C API to v0.7.4.

### Fixed

- Fixed ABI mismatch in C# P/Invoke by using `UnmanagedType.I1` for Rust `bool` parameters (`OpenccFmmsegLib` /
  `OpenccJiebaLib`).
- Improved robustness of native memory cleanup in keyword extraction APIs.

---

## [1.1.1] - 2025-10-30

### Changed

- Added detail documentation

---

## [1.1.0] - 2025-10-07

### Changed

- Update opencc-jieba-rs C API to v0.7.1
- Inline code optimization

---

## [1.0.1] – 2025-08-28

### Added

- First official Nuget release of `OpenccJiebaLib`.
- Built with **Rust** and a **Jieba-style lexicon segmenter**, powered by **OpenCC lexicons** for Chinese text
  conversion.
- Support for:
    - Simplified ↔ Traditional (ST, TS)
    - Taiwan, Hong Kong, and Japanese variants
    - Phrase and character dictionaries
    - Punctuation conversion
- `Jieba` default to use **Large Dictionary** which supports both **Simplified and Traditional Chinese** text *
  *segmentation**.
- `Dictionary` structure to preload dictionaries for Jieba.
- Utility for UTF-8 script detection (`zho_check`).
