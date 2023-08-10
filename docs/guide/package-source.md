# Managing package sources
Package source is not a concept defined in IBM® RPA. Package sources are package repositories from where you can install packages to your project. RPA CLI uses tenants as package sources. Please, read the [package source concept](concepts.md#package-source) explanation to understand the definition of package sources in the context of RPA CLI.

## Prerequisites
To issue `rpa package source` commands you need to have a project configuration file within the working directory. See [managing projects](guide/project.md) section.

## Configuring a package source
Issue the following command to configure a package source, replacing `[alias]` with any name you like to call this package source.

```bash
rpa package source [alias]
```
The above command will prompt you for additional information about the package source, such as *url*, *region*, *tenant*, *user name*, and *password*.
<img src="_assets/rpa-package-source.gif"/>

These prompts can be bypassed if you provide all the options upfront, such as the following command.
```bash
rpa package source [alias] --url [url] --region [region] --tenant [tenant] --username [username] --password [password]
```

The above command creates a package source configuration file named `[project_name].sources.json`, where `[project_name]` is the project name, `[alias]` is the specified package source alias.
```json
{
  "[alias]": {
    "code": [tenant_code],
    "name": "[tenant_name]",
    "region": "[region]",
    "address": "[url]",
    "x-authentication": "[server_authentication]",
    "x-deployment": "[server_deployment]"
  }
}
```

!> **Warning**: the `[alias]` should be unique among [environment](guide/environment.md) aliases and package source aliases.

### Options
#### Url
The IBM® RPA API URL of the server. 

##### SaaS
The URL option is usually `https://[region]api.[domain]/v1.0/`, for example `https://us1api.wdgautomation.com/v1.0/`. But RPA CLI already knows the SaaS APIs and you can just provide `--url default`.

##### On-Premise
The URL option is your API server URL followed by `/v1.0/`. For example, if your API server is `https://192.123.654`, then the URL should be `https://192.123.654/v1.0/`.

##### OpenShift® Container Platform
The URL option is your API server URL followed by `/v1.0/`. Usually follows this format: `https://[ocp_domain]/rpa/api/v1.0/`.

#### Region
The region option is only applicable in SaaS. It's one of the following values: *AP1*, *BR1*, *BR2*, *EU1*, *UK1*, *US1*. See [understanding tenants and regions](https://www.ibm.com/docs/en/rpa/23.0?topic=client-prerequisites-install#understanding-tenants-and-regions) for more details.

#### Tenant
The tenant option is the tenant **code**. See [getting your tenant code](https://www.ibm.com/docs/en/rpa/23.0?topic=client-prerequisites-install#getting-your-tenant-code) for more details.

#### User name and password
The credentials to establish a connection to the package source. These are not stored in the `[project_name].sources.json` configuration file. The credentials are not stored anywhere. Please refer to the [security](security.md) section for more information.

When the **password** is not provided, RPA CLI tries to find it from the *Operating System Environment Variables*, and if it does not find it, RPA CLI wil prompt the user to type it. This is useful for *Continuous Deployment* platforms, such as *Github Actions*. You can set the password as an environment variable and RPA CLI will pick it up. The environment variable should be in the following format: `RPA_SECRET_{alias}`. Example: `RPA_SECRET_joba` should be the environment variable key if you want to provide the password for `joba` package source.

### Red Hat® OpenShift® support with Single Sign-On
Red Hat® OpenShift® Container Platform (OCP) is usually configured with Single Sign-On (SSO). When SSO is enabled, you must pass another parameter specifying the **Cloud Pak Console URL**, so RPA CLI can perform the authentication according to the [Authenticating to the API through Zen](https://www.ibm.com/docs/en/rpa/23.0?topic=call-authenticating-rpa-api#authenticating-to-the-api-through-zen) documentation.

Use `-p:cpconsole=[url]`. Here's the full command line format:
```bash
rpa env new [alias] --url [url] --region [region] --tenant [tenant] --username [username] --password [password] -p:cpconsole=[url]
```

RPA CLI saves this parameter in the `x-properties`, like so:
```json
{
  "[alias]": {
    "code": [tenant_code],
    "name": "[tenant_name]",
    "region": "[region]",
    "address": "[url]",
    "x-authentication": "[server_authentication]",
    "x-deployment": "[server_deployment]",
    "x-properties": {
      "cpconsole": "[cloud_pak_console_url]"
    }
  }
}
```

# Next Steps
* Install, uninstall, update, and restore [packages](guide/package.md).
* Use the [author's packages](guide/joba-packages.md).