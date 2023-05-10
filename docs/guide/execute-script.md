# Using executeScript commands
This guide describes how to use [executeScript](https://www.ibm.com/docs/en/rpa/23.0?topic=general-execute-script) commands so you can take advantage of the [build](reference.md#rpa-build) process to deploy [robots](guide/robot.md) - and not WAL scripts.

## Context
Currently, IBM® RPA does not group WAL files into projects. The Studio treats each WAL file independently and you can only publish/download one WAL file at a time. Studio treats WAL files independently because it treats them as robots, but WAL files are usually used as source code files from RPA developers, and a robot is usually composed of multiple WAL files.

RPA CLI changes that concept. For RPA CLI, WAL files are source code files, not robots. Robots are the output of the build process. The build process outputs one or more robots, dependending on the *robots* property in the configuration file `[name].rpa.json`, where `[name]` is the project name.

## Referencing WAL files
The [build](reference.md#rpa-build) process expects [executeScript](https://www.ibm.com/docs/en/rpa/23.0?topic=general-execute-script) commands to reference WAL files like so: `executeScript --name "${workingDirectory}/[path_of_the_file]"`.

### `${workingDirectory}`
 `${workingDirectory}` is a *String* WAL variable type and its value should be the current local working directory. The build process injects code to the **main** WAL file to provide the mechanism **at runtime** to change the value of `${workingDirectory}` accordingly.

When you create [robots](guide/robot.md) using *templates*, the initial WAL code structure is defined for you alongside with `${workingDirectory}` variable. The following is a template example for unattended.

```
defVar --name workingDirectory --type String
//************************************
//Template for a unattended
//- Continue your logic from line 16.
//************************************
//Set the working directory where all local .wal files are located. Always use 'workingDirectory' variable in the 'executeScript' to reference the scripts.
//- The build process will ignore 'executeScripts' that do not reference local .wal files using 'workingDirectory'.
//- DO NOT publish or use published scripts in the 'executeScript'. The build process will buil ONE script with all the local referenced dependencies.
//- Also, this will ensure your dev team only has to change one variable (workingDirectory) to run the project.
//- If you call an 'executeScript', always pass the 'workingDirectory' variable as a parameter, so you do not need to set in every script.
//- To store the source code .wal files, use GIT. Do not use the Control Center to store the .wal files.
setVarIf --variablename "${workingDirectory}" --value "C:\\Users\\002742631\\Desktop\\Assistant" --left "${workingDirectory}" --operator "Is_Null_Or_Empty" --comment "The build process will inject the correct value for \'workingDirectory\' at runtime."
onError --label HandleErrorWrapper
//TODO: write the rest of your logic from here
beginSub --name HandleErrorWrapper
	goSub --label HandleError
	recover
endSub
beginSub --name HandleError
//TODO: handle the error
endSub
```
<img src="_assets/template.png"/>

### Path of the WAL file
After the `${workingDirectory}` variable, you need to specify the file path within the local working directory. The path can contain child directories as well. Here are a few examples.

The *Assistant* working directory structure:
```
├── Assistant
│   ├── packages
│   │   ├── Joba_Security.wal
│   ├── skills
│   │   ├── Assistant_OrangeHRM.wal
│   ├── Assistant.wal
│   └── Assistant_Thanks.wal
└──
```
* Assistant_Thanks.wal: `executeScript --name "${workingDirectory}/Assistant_Thanks.wal"`
* Assistant_OrangeHRM.wal: `executeScript --name "${workingDirectory}/skills/Assistant_OrangeHRM.wal"`
* Joba_Security.wal: `executeScript --name "${workingDirectory}/packages/Joba_Security.wal"`

## Ignored references
Any [executeScript](https://www.ibm.com/docs/en/rpa/23.0?topic=general-execute-script) that does not follow the above pattern is ignored by the [build](reference.md#rpa-build) process. Which might mean that the deployed robot is not self-contained, requiring you to manually publish the other WAL scripts. Examples of ignored references.

* Full path of the wal file: `executeScript --name "c:/Asssistant/packages/Joba_Security.wal"`
* Only variable: `executeScript --name "${filePath}"`
* Only script name without file extension: `executeScript --name Joba_Security`

## Performance
A common practice established for RPA developers using IBM® RPA is to reference scripts using **only** the `--name` parameter - and not specifying the `--version` parameter. This means that, at runtime, IBM® RPA fetches the script version marked as *production*. Since developers can change the *production* version at any time, even during bot execution, IBM® RPA **always** makes a HTTP request to fetch the script. The consequence of this behavior is fragile and slow robots.

Imagine the following robot
```
defVar --name i --type Numeric
for --variable ${i} --from 1 --to 1000 --step 1
	executeScript --name Joba_Security
next
```

It will make 1000 HTTP requests to fech *Joba_Security* script. It does not cache the script because the *production* version can be changed at any point by the developer. Obviously the above example is an exageration, but how many loops do you have that reference [executeScript](https://www.ibm.com/docs/en/rpa/23.0?topic=general-execute-script) commands without `--version`?

?> RPA CLI solves the performance problem by bundling WAL script reference into the robot - the **main** WAL file - through the [build](reference.md#rpa-build) process. At runtime, IBM® RPA only fetches 1 script, the **main** one. The other references are already available and do not need to be fetched.