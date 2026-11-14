# Security Policy

## Supported Versions

Security fixes are provided for the latest released version.

| Version | Supported |
|:--------|:----------|
| 2.9.x (latest release) | :white_check_mark: |
| Older versions | :x: |

## Reporting a Vulnerability

**Please do not report security vulnerabilities through public GitHub issues,
discussions, or pull requests.**

Report vulnerabilities privately through GitHub's private vulnerability reporting:

<https://github.com/purelogiccode/RomValidator/security/advisories/new>

You can also open the repository's **Security** tab and choose
**Report a vulnerability**.

Please include:

- the affected version and Windows architecture (x64 or ARM64),
- a description of the issue and its security impact,
- step-by-step reproduction instructions or a proof of concept,
- any relevant logs from `%LOCALAPPDATA%\ROM Validator\Logs`, and
- a suggested fix or mitigation, if you have one.

Do not include secrets, personal data, or ROM files you are not allowed to share.

## Scope

In scope:

- the ROM Validator desktop application and its installer/release packages,
- DAT, XML, ZIP, 7z and RAR parsing and archive extraction,
- hash calculation and file handling,
- the update checker and the bug-reporting pipeline,
- the bundled 7-Zip executables and release artifacts.

Out of scope:

- vulnerabilities in third-party dependencies that are already fixed upstream
  (please report those upstream, and let us know so we can update),
- issues that require physical access to a machine or an already-compromised system,
- social engineering and denial-of-service attacks against GitHub infrastructure,
- problems in unsupported or modified builds.

## Response Process

- We aim to acknowledge reports within **7 days**.
- We aim to provide an initial assessment and remediation plan within **14 days**.
- We will keep you informed while a fix is developed and coordinate disclosure with you.
- Please allow up to **90 days** for a fix before public disclosure.

Credit will be given in the advisory and release notes unless you ask to remain
anonymous.

## User Guidance

- Download releases only from the
  [official releases page](https://github.com/purelogiccode/RomValidator/releases).
- Verify the published checksums when they are provided.
- Keep the application and Windows up to date.
- ROM Validator processes files locally; only diagnostic bug reports are sent to the
  developer, and those should not contain ROM contents.
