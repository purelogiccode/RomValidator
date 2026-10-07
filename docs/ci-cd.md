---
title: CI/CD
parent: Reference
nav_order: 5
description: "GitHub Actions workflows: continuous integration, releases, GitHub Pages and wiki."
---

# CI/CD

All automation lives in `.github/workflows`. Three workflows keep the project healthy and
publish its artifacts and documentation.

## Continuous integration - `ci.yml`

| Property | Value |
|:---------|:------|
| Triggers | Push to `master`, pull requests, manual dispatch |
| Runner | `windows-latest` |
| Steps | Restore, build (Release), test, upload test results and coverage |

The build runs the Meziantou and Roslynator analyzers with warnings treated as errors, so a
green CI run means zero compiler and analyzer warnings.

Test results (`*.trx`) and coverage output are uploaded as the `test-results` artifact.

## Release - `release.yml`

| Property | Value |
|:---------|:------|
| Triggers | Push of a `release_*` or `v*` tag, or manual dispatch with a tag input |
| Runner | `windows-latest` (verify + package), `ubuntu-latest` (publish) |
| Permissions | `contents: write` |

Flow:

1. **verify** - builds and runs the full test suite.
2. **package** - publishes `win-x64` and `win-arm64` builds as single-file, framework-dependent
   executables and creates:
   - `release_<version>_win-x64.zip`
   - `release_<version>_win-arm64.zip`
   - a `.sha256` checksum file for each zip.

   Every zip contains `RomValidator.exe`, the bundled 7-Zip fallback executables
   (`7za.exe` / `7za_arm64.exe`), and `LICENSE.txt`, `ReadMe.md` and `WhatsNew.md`.
3. **release** - checks out the repository history, then creates a GitHub release for the tag and
   attaches all zips and checksums, with automatically generated release notes.

### Creating a release

```powershell
git tag release_2.9.0
git push origin release_2.9.0
```

The version in the zip names comes from the tag (`release_` or a leading `v` is stripped). This
naming convention is compatible with the in-app update check, which parses tags such as
`release_2.9.0` and `v2.9.0`. Keep the tag in sync with `AssemblyVersion` / `FileVersion` in
`RomValidator.csproj`.

A manual dispatch of the workflow with the tag input can be used to re-publish an existing tag.

## Documentation - `docs.yml`

| Property | Value |
|:---------|:------|
| Triggers | Push to `master` that changes `docs/**` or the workflow itself, manual dispatch |
| Permissions | `contents: write`, `pages: write`, `id-token: write` |

### GitHub Pages

The `pages` job builds this documentation with Jekyll (using the
[just-the-docs](https://just-the-docs.github.io/just-the-docs/) theme) and deploys it with the
official GitHub Pages actions:

1. `actions/configure-pages` - enables Pages with the "GitHub Actions" source when needed.
2. `actions/jekyll-build-pages` - builds `docs/` into `_site/`.
3. `actions/upload-pages-artifact` + `actions/deploy-pages` - publishes the site.

The site is available at <https://purelogiccode.github.io/RomValidator/>.

> **One-time setup**
>
> If the Pages site has never been enabled, open **Settings > Pages** and select
> **GitHub Actions** as the source, or let `configure-pages` enable it on the first run.

### GitHub Wiki

The `wiki` job runs `.github/scripts/publish-wiki.sh`, which:

1. clones `<repository>.wiki.git`,
2. strips YAML front matter from every `docs/*.md` page and copies the content to the wiki
   (`index.md` becomes `Home.md`),
3. generates `_Sidebar.md` from the page titles and sections,
4. commits and pushes only when something changed.

The wiki clone and push require a token that is allowed to write to the wiki repository. Create a
repository secret named **`WIKI_TOKEN`** (classic personal access token with the `repo` scope) to
guarantee this. If the secret is absent, the workflow falls back to `GITHUB_TOKEN`; on
repositories where `GITHUB_TOKEN` cannot push to the wiki the job emits a warning and skips the
sync instead of failing.

> **First wiki page**
>
> A wiki repository does not exist until the first page has been created in the GitHub UI. Open
> the **Wiki** tab once and save a page, then run the workflow again.

## Related pages

- [Build from Source](build-from-source)
- [Contributing](contributing)
