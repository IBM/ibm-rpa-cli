# Deploying projects
Deploying projects is not a concept defined in IBM® RPA. In IBM® RPA you *publish* scripts, one by one. The RPA CLI deploys projects to [environments](guide/environment.md), creating or updating [robots](guide/robot.md), creating or updating parameters, publishing *compiled* WAL scripts.

## Prerequisites
To issue `rpa deploy` command you need to
* have a project configuration file within the working directory - see [managing projects](guide/project.md) section.
* have an environment configured - see [managing environment](guide/environment.md) section.
* follow the [executeScript](https://www.ibm.com/docs/en/rpa/23.0?topic=general-execute-script) usage [guidelines](guide/execute-script.md) so the build process sucessfully creates self-contained robots.

## Deploying
Issue the following command to deploy the project to an environment, replacing `[alias]` with the environment name.
```bash
rpa deploy [alias]
```

The command automatically [restore packages](guide/package.md#restoring-packages), [builds](reference.md#rpa-build), and deploys robots and [RPA parameters](https://www.ibm.com/docs/en/rpa/23.0?topic=scripts-parameters) to the specified environment.

### Overriding parameters
By default, the `rpa deploy` command deploys the [RPA parameters](https://www.ibm.com/docs/en/rpa/23.0?topic=scripts-parameters) specified in the configuration file named `[project_name].rpa.json` - in the *parameters* property - where `[project_name]` is the project name.

You can override this behavior by creating another file named `[alias].json` to specify different [RPA parameters](https://www.ibm.com/docs/en/rpa/23.0?topic=scripts-parameters) to be used when deploying. For example, if you are deploying to the `dev` environment and a file `dev.json` exists in the working directory, the *parameters* specified in the `dev.json` file will be used instead of `[project_name].rpa.json`.

The following is an example of `dev.json` file structure
```json
"parameters": {
    "Assistant_KbName": "Hero",
    "Assistant_MinimumScore": "450",
    "Assistant_Name": "Hero",
}
```