# Changelog

All notable changes to this project will be documented in this file.

This project adheres to [Semantic Versioning](https://semver.org/).

---

## [1.2.0] - 2026-02-02

### Added

- Added `OpenccConfig` enum for strongly typed OpenCC conversion configuration.
- Added `Convert(string, OpenccConfig, bool)` overload for type-safe conversion calls.
- Added `GetNativeAbiNumber` and `GetNativeVersionString`

### Changed

- Refactored native P/Invoke bindings into a dedicated native interop class.
- Simplified conversion API by removing internal config state (`SetConfig` / `GetConfig`);
  configuration is now provided per `Convert()` call.
- Improved UTF-8 interop by centralizing null-terminated encoding helpers (`*Utf8Z`)
  and reusing pooled buffers where appropriate.
- Updated `opencc-jieba-rs` C API to v0.7.3.

### Fixed

- Fixed ABI mismatch in C# P/Invoke by using `UnmanagedType.I1` for Rust `bool` parameters
  (`OpenccFmmsegLib` / `OpenccJiebaLib`).
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
