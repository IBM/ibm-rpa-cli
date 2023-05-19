# Getting started with the RPA CLI

## Prerequisites
To access IBM® RPA platform with the RPA CLI, you need a IBM® RPA tenant and credentials to access it. When running RPA CLI commands, the RPA CLI access the tenant resources using the provided credentials. Therefore, the RPA CLI is limited by the provided credentials' permissions and roles. To increase the security of your RPA account, we recommend that you do not use your tenant administrator account credentials. You should create a user with least privilege to provide access credentials to the tasks you'll be running in. See the required [permissions](security.md#permissions) RPA CLI needs and optionally create a custom role for it.

### IBM® RPA version
RPA CLI supports servers with `23.0.3` versions and later. SaaS and On-Prem deployments are supported. RPA CLI does not require the *client software* to be installed.

## Install or Update
This topic describes how to install or update the latest release of the RPA CLI on supported operating systems.

### Windows
We support the RPA CLI on Microsoft-supported versions of 64-bit Windows. We recommend that you install RPA CLI per Windows user-basis instead of installing system-wide. Currently, the installation is a manual procedure as follows:

#### 1. Download 
Find the latest version in the [releases](https://github.com/IBM/ibm-rpa-cli/releases) page. Expand the *Assets* section and click `rpa cli` to download it.

#### 2. Create the directory
Create the directory named `Joba\rpa cli\` within `%localappdata%` of the current user and copy the `rpa.exe` to it.

From your downloads directory, open the *cmd* and type:

```bash
mkdir "%localappdata%\Joba\rpa cli\"
copy rpa.exe "%localappdata%\Joba\rpa cli\" /y
```

#### 3. Change the user environment PATH variable
Instruct windows to find `rpa.exe` when issuing commands. This will inform Windows to recognize `rpa` as a command in the *cmd*.

You can edit the **user** PATH environment variable through Windows control panel, or using the [setx](https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/setx) command:

```bash
setx PATH "%PATH%;%localappdata%\Joba\rpa cli"
```

#### 4. Confirm the installation
Run the following command to confirm the installation:
```bash
rpa --version
```
This should print the RPA CLI version in the console.

### Linux
*Coming soon...*

## Help
You can get help with any command when using the RPA CLI. To do so, simply type `--help` at the end of a command name.

For example, the following command displays help for the general RPA CLI options and the available top-level commands.
```bash
rpa --help
```

The following command displays the available *project* specific commands.
```bash
rpa project --help
```

# Next steps
After successful install, you can safely delete the downloaded asset from your downloads directory.
* [Managing projects](guide/project.md).