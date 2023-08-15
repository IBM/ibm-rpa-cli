# Creating packages
Package is not a concept defined in IBM® RPA. Packages are reusable libraries that you can add as a dependency to your RPA projects. Please, read the [package concept](concepts.md#package) explanation to understand the definition of packages in the context of RPA CLI.

## Prerequisites
To issue `rpa bot` commands you need to have a project configuration file within the working directory. See [managing projects](guide/project.md) section.

## Creating a package
Creating packages are similar to [creating robots](guide/robot.md). The only difference is that the `--template` parameter should be `package`.

```bash
rpa bot new [name] --template package
```

## Package template
*in progress...*