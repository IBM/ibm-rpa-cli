# Managing packages
Package is not a concept defined in IBM® RPA. Packages are reusable libraries that you can add as a dependency to your RPA projects. Please, read the [package concept](concepts.md#package) explanation to understand the definition of packages in the context of RPA CLI.

## Prerequisites
To issue `rpa package` commands you need to have
* a project configuration file within the working directory - see [managing projects](guide/project.md) section.
* a package source configuration file within the working directory - see [managing package sources](guide/package-source.md).

## Installing packages
Issue the following command to install the **latest** version of a package, replacing `[name]` with the package name.
```bash
rpa package install [name]
```

The command also support *wildcard* to install several packages at once, for example `rpa package install Security*` installs every package that starts with *Security*.

To install a specific version, use the `--version` option.
```bash
rpa package install [name] --version [version]
```

By default, the command searches for packages in every configured [package source](guide/package-source.md). To install from a specific package source, use the `--source` option.
```bash
rpa package install [name] --source [source]
```

The above commands update the configuration file named `[project_name].rpa.json` adding a *packages* property, where `[project_name]` is the project name, `[package_name]` is the name of the package, and `[package_version]` is the version of the package.

```json
...
"packages": {
    "[package_name]": [package_version]
  },
```

The commands also creates a `/packages/` directory within the working directory and downloads the WAL file named `[name].wal`, where `[name]` is the package name.

## Uninstalling packages
Issue the following command to uninstall a package, replacing `[name]` with the package name.

```bash
rpa package uninstall [name]
```

The command also support *wildcard* to uninstall several packages at once, for example `rpa package uninstall Security*` uninstalls every package that starts with *Security*.

The above command updates the configuration file named `[project_name].rpa.json` removing the package from the *packages* property, where `[project_name]` is the project name.

The command also deletes the WAL file named `[name].wal` from the `/packages/` directory, where `[name]` is the package name.

> References to the package are not removed from WAL files.

## Updating packages
Issue the following command to update the package to its **latest** version, replacing `[name]` with the package name.
```bash
rpa package update [name]
```

To update to a specific version, use the `--version` option.
```bash
rpa package update [name] --version [version]
```

By default, the command searches for packages' updates in every configured [package source](guide/package-source.md). To update from a specific package source, use the `--source` option.
```bash
rpa package update [name] --source [source]
```

The above command updates the configuration file named `[project_name].rpa.json`, where `[project_name]` is the project name.

## Restoring packages
Issue the following command to restore packages.
```bash
rpa package restore
```

The above command creates a `/packages/` directory within the working directory and downloads all packages specified in the configuration file named `[project_name].rpa.json` *packages* property where `[project_name]` is the project name.

## GIT
Packages do not need to be committed to the source code since you can [restore](guide/package.md#restoring-packages) them at any point. It's recommended that you add the following line to the [.gitignore](https://git-scm.com/docs/gitignore) file.

```
packages/
```

> You can issue the [rpa git config](reference.md#rpa-git) command to configure that automatically.

# Next steps
* [Referencing packages](guide/execute-script.md) in WAL files.