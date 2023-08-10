# Managing projects
Although limited to Control Center, [project](https://www.ibm.com/docs/en/rpa/23.0?topic=interfaces-projects) is a resource defined in IBM® RPA. RPA CLI integrates with projects very well. The tool also introduces the **local** project concept, enabling team collaboration and [GIT integration]() to track project changes. Read the [project concept](concepts.md#projects) explanation to understand the definition of projects in the context of RPA CLI.

## Working directory
Define a working directory to manage your project. This directory is used to store all project assets: configurations, WAL scripts, and any other file. Any RPA CLI commands should be issued within the project working directory.

## Creating a project
Issue the following command to create a local project within the working directory, replacing `[name]` with the project name.

```bash
rpa project new [name]
```

The above command creates a configuration file named `[name].rpa.json`, where `[name]` is the project name.
```json
{
  "description": "[name]"
}
```

The configuration file can be updated without the RPA CLI. Here's an example of the configuration file with all properties populated.
```json
{
  "environments": {
    "prod": {
      "code": 5283,
      "name": "IBM RPA Product Management",
      "region": "US1",
      "address": "https://us1api.wdgautomation.com/v1.0/",
      "x-authentication": "WDG",
      "x-deployment": "SaaS"
    }
  },
  "description": "Assistant",
  "robots": {
    "Assistant": {
      "type": "chatbot",
      "timeout": "00:05:00",
      "description": "none",
      "include": [
        "Assistant_OrangeHRM.wal",
        "Assistant_SmallTalk.wal",
        "Assistant_Thanks.wal",
        "Assistant_Thesaurus.wal",
        "Assistant_Time.wal",
        "Assistant_Weather.wal"
      ]
    },
    "Backoffice": {
      "type": "unattended",
      "timeout": "00:05:00",
      "description": "none",
      "x-properties": {
        "computer-group": "dev computers"
      }
    }
  },
  "packages": {
    "Joba_OrangeHRM": "2",
    "Joba_Security": "1",
    "Joba_System": "1",
    "Joba_AccuWeather": "1"
  },
  "parameters": {
    "Assistant_KbName": "Hero",
    "Assistant_MinimumScore": "450",
    "Assistant_Name": "Hero",
  }
}
```

## Updating a project
You can update any project configuration by changing the `[name].rpa.json` file, where `[name]` is the project name. RPA CLI updates the [Control Center project](https://www.ibm.com/docs/en/rpa/23.0?topic=interfaces-projects) using the configuration file as part of the [project deployment](guide/deploy.md).

# Next steps
* [GIT integration](guide/git.md)
* [Managing robots](guide/robot.md)
* [Managing environments](guide/environment.md)