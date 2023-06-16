# Contributing
This file provides general guidance for anyone contributing to IBM® RPA CLI project.

## Commits
Commits must meet the following criteria.

### Messages
Commit messages should follow the [Conventional Commits](https://conventionalcommits.org/) pattern.

#### Signing off
Following [Developer's Certificate of Origin 1.1 (DCO)](https://github.com/hyperledger/fabric/blob/master/docs/source/DCO1.1.txt), commit messages require `Sign-off-by` statements.

This is accomplished by using `-s` flag in the `git commit -s` command.

### Signing
Different from the above, **signed** commits are also required. Understand the difference in the article [Git commit signoff vs signing](https://medium.com/@MarkEmeis/git-commit-signoff-vs-signing-9f37ee272b14).

Learn how to sign commits in github [Signing commits](https://docs.github.com/en/authentication/managing-commit-signature-verification/signing-commits) documentation.

## Pull requests
It's not possible to merge Pull Requests (PR) through Github UI because this project uses a *clean* git history without merged commits and requires signed commits. Therefore, all PRs must be rebased-and-merged, but since Github cannot *sign* the rebased commits on your behalf - because Github does not have your key - this is not supported.

Merging commits manually is fairly easy:

1. `git fetch -p`
2. `git checkout develop`
3. `git pull origin develop`
4. `git merge <pull-request-branch-name>`
5. `git push origin develop`

## Build
Use Visual Studio 2022 or VsCode to build and run the project locally. The main project is `Joba.IBM.RPA.Cli`.

## Test
Use Visual Studio 2022 or VsCode to run the test under [Tests/](src/Tests/) directory. Tests are also run as part of Pull Requests through [build.yml](.github/workflows/build.yml) workflow.

## Release
Create a **signed** tag - using `-s` option in `git tag -s` - and *push* it to the remote. This will start the release process using [release.yml](.github/workflows/release.yml) workflow.

## Documentation
The documentation uses [docsify](https://docsify.js.org/) and it's automatically published whenever files changed within [docs/](docs/) of [gh-pages](https://github.com/IBM/ibm-rpa-cli/tree/gh-pages) branch.