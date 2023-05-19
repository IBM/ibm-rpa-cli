# RPA CLI concepts
To fully leverage the IBM® RPA Command Line Interface (RPA CLI), you need to understand how RPA CLI treats WAL scripts, projects, robots, environments, and how RPA CLI introduces concepts not part of IBM® RPA offering, such as packages and package sources.

## Scripts
WAL scripts are treated as source code files. They are not treated as robots. You cannot deploy scripts to environments. RPA CLI deploys [robots](#robots). This is fundamentally different from IBM® RPA.

## Robots
Robots are the result of the [build](reference.md#rpa-build) process. Robots are created using source code files such as WAL scripts, but they are *not* source code files. Although a robot can be composed by several WAL scripts, a robot always has one **main**, also known as **entry point**, WAL script. The **main** script is what gets executed by the runtime engine. The other scripts are just dependencies. The **main** script cannot function without the dependencies. Fetching dependencies *on demand*, a common construct used by RPA developers, has a major [performance](guide/execute-script.md#performance) penalty. Therefore, RPA CLI does not publish every script to Control Center. RPA CLI embeds dependencies into the **main** script, creating a new script, and that is what gets deployed.

## Projects
Although limited to Control Center, [projects](https://www.ibm.com/docs/en/rpa/23.0?topic=interfaces-projects) are a concept in IBM® RPA. But you cannot open or create projects within Studio or locally in you laptop. You cannot group source code files - WAL scripts - locally as part of a project.

RPA CLI introduces **local** projects. Local projects are enabled by defining a file structure such as the *configuration file* (see [managing projects](guide/project.md)) where developers can manually (or through RPA CLI) change project configurations without relying on UI. Changes that can be tracked, merged, and reviewed by 3rd-party versioning source code tools such as GIT.

With RPA CLI, you can start your project locally and only deploy it when needed. You do not need to create a [Control Center project](https://www.ibm.com/docs/en/rpa/23.0?topic=interfaces-projects) to start working on your automation idea. Work locally and [deploy](guide/deploy.md) later. RPA CLI takes care of creating or updating the Control Center resources.

## Environments
*in progress...*

## Package sources
*in progress...*

## Packages
*in progress...*