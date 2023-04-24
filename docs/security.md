# Security in the RPA Command Line Interface

## Authentication
RPA CLI commands that exchange information with IBM® RPA server needs authentication to function. Credentials are requested by either adding [environments](guide/environment.md) or [package sources](guide/package-source.md).

The password is **not** stored anywhere. RPA CLI stores the *bearer token* in the `%localappdata%\rpa\[project_name]\settings.json` file. This file is not encrypted **yet** (*coming soon*). Any command that needs authentication to the environment will use the *bearer token* configured in the aforementioned file. Also, RPA CLI handles token expiration seamlessly. Once the token is expired, RPA CLI will prompt the user to provide the **password** again to exchange for another *bearer token*.

## Authorization
Since RPA CLI uses your own credentials, it is authorized to perform the same actions as you can. This allows teams to configure *dev*, *test*, and *prod* [environments](guide/environment.md) and only permit certain people to [deploy]() to each environment. That is, although all three environments are defined in the `[project_name].rpa.json` configuration file, only people that are authorized to access each environment will be able to deploy the project there.

## Permissions
The following table describes the permissions needed by RPA CLI for each command.

*in progress...*