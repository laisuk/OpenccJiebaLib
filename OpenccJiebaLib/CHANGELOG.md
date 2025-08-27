# Changelog

All notable changes to this project will be documented in this file.

This project adheres to [Semantic Versioning](https://semver.org/).

---

## [1.0.0] – 2025-08-28

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
