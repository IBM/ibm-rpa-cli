# Referencing files
This guide describes how to reference files within the working directory, so they are [deployed](guide/deploy.md) as dependencies.

## Wal
Read [using executeScript](guide/execute-script.md) guide.

## Excel
Supported commands
- [excelOpen](https://www.ibm.com/docs/en/rpa/23.0?topic=office-open-excel-file)

The [build](reference.md#rpa-build) process looks for [excelOpen](https://www.ibm.com/docs/en/rpa/23.0?topic=office-open-excel-file) commands that reference Excel files within the working directory. If the `excelOpen` command follows this pattern `excelOpen --file "${workingDirectory}/[path_of_the_file]"`, the build process will embed it as a dependency.

## `${workingDirectory}`
 `${workingDirectory}` is a *String* WAL variable type and its value should be the current local working directory. The build process injects code to the **main** WAL file to provide the mechanism **at runtime** to change the value of `${workingDirectory}` accordingly.

 When you create [robots](guide/robot.md) using *templates*, the initial WAL code structure is defined for you alongside with `${workingDirectory}` variable.

## Path of the file
After the `${workingDirectory}` variable, you need to specify the file path within the local working directory. The path can contain child directories as well. Here are a few examples.

The *Assistant* working directory structure:
```
├── Assistant
│   ├── packages
│   │   ├── Joba_Security.wal
│   ├── spreadsheets
│   │   ├── expense-report.xlsx
│   └── Assistant.wal
└──
```
* expense-report.xlsx: `excelOpen --file "${workingDirectory}/spreadsheets/expense-report.xlsx"`
* Joba_Security.wal: `executeScript --name "${workingDirectory}/packages/Joba_Security.wal"`