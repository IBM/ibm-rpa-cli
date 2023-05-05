# Managing bots
Although limited to Control Center, [bot](https://www.ibm.com/docs/en/rpa/23.0?topic=scripts-bots) is a resource defined in IBM® RPA. Read the [robot concept](concepts.md#robots) explanation to understand the definition of robots in the context of RPA CLI.

## Prerequisites
To issue `rpa bot` commands you need to have a project configuration file within the working directory. See [managing projects](guide/project.md) section.

## Creating a robot
Issue the following command to create a robot within the working directory, replacing `[name]` with the robot name and `[template]` with one of the available templates: *unattended*, *attended*, *chatbot*, *excel*.

```bash
rpa bot new [name] --template [template]
```

The above command updates a configuration file named `[project_name].rpa.json` adding a *robots* property, where `[project_name]` is the project name, `[name]` is the specified robot name, and `[type]` is derived from the specified `[template]`.
```json
{
  "description": "[project_name]",
  "robots": {
    "[name]": {
      "type": "[type]",
      "timeout": "00:05:00",
      "description": "[name]"
    }
  }
}
```

The command also creates a WAL file named `[name].wal` within the working directory, where `[name]` is the specified robot name. This WAL file is the **entry point** or **main** file of the robot. If your code references other WAL files using the [executeScript](https://www.ibm.com/docs/en/rpa/23.0?topic=general-execute-script) command, those references will be embedded into the main file through the [build]() proccess.

### Unattended
Unattended robot type has one extra **required** configuration for deployment of the project: `computer-group`. You need to specify a valid [computer group](https://www.ibm.com/docs/en/rpa/23.0?topic=computers-managing-computer-groups) **name** manually in the configuration file.

Example:
```json
{
  ...
  "robots": {
    "[name]": {
      "type": "[type]",
      ...
      "computer-group": "[computer_group_name]"
    }
  }
}
```

When you [deploy]() the project with unattended robots, Control Center [bots](https://www.ibm.com/docs/en/rpa/23.0?topic=scripts-bots) are created or updated accordingly.

### Attended
When you [deploy]() the project with attended robots, only WAL scripts are published. Support for creating Control Center [launchers](https://www.ibm.com/docs/en/rpa/23.0?topic=interfaces-launchers) is coming soon.

### Chatbot
When you [deploy]() the project with chatbot robots, only WAL scripts are published. Support for creating Control Center [chat mappings](https://www.ibm.com/docs/en/rpa/23.0?topic=chatbots-chats-mappings) is coming soon.

## Updating a robot
You can update any robot configuration by changing the `[name].rpa.json` file, where `[name]` is the project name. RPA CLI updates the [Control Center bots](https://www.ibm.com/docs/en/rpa/23.0?topic=scripts-bots) using the configuration file as part of the [project deployment](guide/deploy.md).

## Deleting a robot
!> **Warning**: RPA CLI does not delete resources previously deployed if you remove robots from the configuration file.

## Dependencies
The [build]() proccess looks for [executeScript](https://www.ibm.com/docs/en/rpa/23.0?topic=general-execute-script) commands and embed them as dependencies in the **main** file.

For cases where you *dynamicaly* reference a WAL file, you can explicitly define dependencies in the configuration file by adding the `include` property in the `robot` property. The `include` property expects an array of WAL script paths relative to the working directory.

Example:
```json
{
  ...
  "robots": {
    "[name]": {
      "type": "[type]",
      ...
      "include": ["path_wal_file_1.wal", "packages/path_wal_file_2.wal", "..."]
    }
  }
}
```

[Package](guide/package.md) references are also automatically embedded by the [build]() process.

# Next steps
* [Managing environments](guide/environment.md)