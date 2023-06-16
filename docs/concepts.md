# RPA CLI concepts
To fully leverage the IBM® RPA Command Line Interface (RPA CLI), you need to understand how RPA CLI treats WAL scripts, projects, robots, environments, and how RPA CLI introduces concepts not part of IBM® RPA offering, such as packages and package sources.

## Scripts
WAL scripts are treated as source code files. They are not treated as robots. You cannot deploy scripts to environments. RPA CLI deploys [robots](#robots). This is fundamentally different from IBM® RPA.

## Robots
Robots are the result of the [build](reference.md#rpa-build) process. Robots are created using source code files such as WAL scripts, but they are *not* source code files. Although a robot can be composed by several WAL scripts, a robot always has one **main**, also known as **entry point**, WAL script. The **main** script is what gets executed by the runtime engine. The other scripts are just dependencies. The **main** script cannot function without the dependencies. Fetching dependencies *on demand*, a common construct used by RPA developers, has a major [performance](guide/execute-script.md#performance) penalty. Therefore, RPA CLI does not publish every script to Control Center. RPA CLI embeds dependencies into the **main** script, creating a new script, and that is what gets deployed.

See [managing robots](guide/robot.md).

## Projects
Although limited to Control Center, [projects](https://www.ibm.com/docs/en/rpa/23.0?topic=interfaces-projects) are a concept in IBM® RPA. But you cannot open or create projects within Studio or locally in you laptop. You cannot group source code files - WAL scripts - locally as part of a project.

RPA CLI introduces **local** projects. Local projects are enabled by defining a file structure such as the *configuration file* (see [managing projects](guide/project.md)) where developers can manually (or through RPA CLI) change project configurations without relying on UI. Changes that can be tracked, merged, and reviewed by 3rd-party versioning source code tools such as GIT.

With RPA CLI, you can start your project locally and only deploy it when needed. You do not need to create a [Control Center project](https://www.ibm.com/docs/en/rpa/23.0?topic=interfaces-projects) to start working on your automation idea. Work locally and [deploy](guide/deploy.md) later. RPA CLI takes care of creating or updating the Control Center resources.

See [managing projects](guide/project.md).

## Environments
Environment is not a concept defined in IBM® RPA. Tenants have been used as the alternative to provide environments to customers. RPA CLI also uses tenants as environments. The purpose of configuring environments for RPA CLI is to enable project deployment to the environment.

RPA CLI treats environments solely as buckets to deploy projects. When you issue [rpa deploy](reference.md#rpa-deploy) command, you must specify an environment. RPA CLI creates or updates the resources accordingly in the environment. If resources already exist, RPA CLI overwrites them. If resources do not exist, RPA CLI creates them.

!> **Warning**: RPA CLI never deletes resources.

See [managing environments](guide/environment.md).

## Package sources
Package source is not a concept defined in IBM® RPA. Package sources are package repositories from where you can install packages to your project. RPA CLI uses tenants as package sources. This means that you can create a tenant solely to act as a package source, that is, a tenant responsible to host packages.

Package sources bring somewhat extensibility to IBM® RPA. Although you won't see the package commands in the Studio toolbox, RPA CLI provides the mechanism to host and install packages. Package sources are defined in the *package configuration file*.

The author has create a package source named [Joba Packages](guide/joba-packages.md) where you can request access to and start using its packages. You can also create your own package source within your organization and upload your organization packages there - this is usually called as *private package source*.

See [managing package sources](guide/package-source.md).

## Packages
Package is not a concept defined in IBM® RPA. Packages are reusable libraries that you can add as a **dependency** to your RPA projects. They are downloaded from package sources.

Packages are just WAL scripts defined in a particular format. RPA CLI provides a [guideline and template](guide/joba-packages.md#package-template) to create packages. To use them you still rely on the [executeScript](https://www.ibm.com/docs/en/rpa/23.0?topic=general-execute-script) command. Packages have versions - the WAL script versions. When you install packages, you install specific versions of them. This is important to manage project changes.

See [managing](guide/package.md) and [referencing](guide/execute-script.md#referencing-wal-files) packages.

# Next steps
* [Install](guide/getting-started.md) RPA CLI.