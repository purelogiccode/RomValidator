---
title: FAQ
nav_order: 5
description: "Frequently asked questions about ROM Validator."
---

# Frequently Asked Questions

## General

**Is ROM Validator free?**
Yes. It is open source under the GPLv3 license.

**Which platforms are supported?**
Windows 10 and Windows 11 on x64 and ARM64. The user interface is WPF, so Linux and macOS are
not supported.

**Does it modify my files?**
Only when you ask it to. Validation reads files by default. Files are moved, renamed or deleted
only when the corresponding option is enabled. Permanent deletion requires an explicit
confirmation.

**Does it download ROMs?**
No. ROM Validator never downloads ROMs or DAT files. The **Download Dat Files** button only opens
[no-intro.org](https://no-intro.org/) in your browser.

## Validation

**Why is a correct file reported as failed?**
Open the log line for the mismatch reason. Common causes are a different dump (size or hash
mismatch), a corrupt download, or a cloud-only placeholder that is not fully available locally.

**Why are archives renamed?**
When the hash matches a DAT entry but the name differs, the file is renamed to the DAT name.
Inside archives the entry is renamed and the archive is repacked. RAR archives become zip files
because RAR creation is not supported.

**What are the `_success`, `_fail` and `_duplicate` folders?**
They organize the results of a validation run. See
[Validating ROMs](validating-roms#result-folders).

**Can I stop a run?**
Yes, click **Cancel** during validation or **Stop** during hashing. Files already processed keep
their results.

## Data and privacy

**What data does the application send?**
Anonymous usage statistics (application id and version) and bug reports for Warning+ events.
Bug reports contain environment details and exception information, never your ROM files or file
contents. See [Screenshots and Application Data](screenshots-and-data).

**Where are the logs?**
`%LOCALAPPDATA%\ROM Validator\Logs`, retained for seven days. Click **App Data** to open the
folder.

**How do I take a screenshot?**
Press **F8** anywhere in the application.

## Development

**How do I build the application?**
See [Build from Source](build-from-source).

**How do releases and documentation get published?**
See [CI/CD](ci-cd).

**How can I contribute?**
See [Contributing](contributing).
