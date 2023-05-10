# Managing environments
Environment is not a concept defined in IBM® RPA. Tenants have been used as the alternative to provide environments to customers. RPA CLI also uses tenants as environments. The purpose of configuring environments for RPA CLI is to enable deployment to the environment. Please, read the [environment concept](concepts.md#environment) explanation to understand the definition of environments in the context of RPA CLI.

## Prerequisites
To issue `rpa env` commands you need to have a project configuration file within the working directory. See [managing projects](guide/project.md) section.

## Configuring an environment
Issue the following command to configure an environment, replacing `[alias]` with any name you like to call this environment.

```bash
rpa env new [alias]
```
The above command will prompt you for additional information about the environment, such as *url*, *region*, *tenant*, *user name*, and *password*.
<img src="_assets/rpa-env-new.gif"/>

These prompts can be bypassed if you provide all the options upfront, such as the following command.
```bash
rpa env new [alias] --url [url] --region [region] --tenant [tenant] --username [username] --password [password]
```

The above command updates the configuration file named `[project_name].rpa.json` adding an *environments* property, where `[project_name]` is the project name, `[alias]` is the specified environment alias.
```json
{
  ...
  "environments": {
    "[alias]": {
      "code": [tenant_code],
      "name": "[tenant_name]",
      "region": "[region]",
      "address": "[url]"
    }
  }
}
```

!> **Warning**: the `[alias]` should be unique among [package source](guide/package-source.md) aliases and environment aliases.

### Options
#### Url
The IBM® RPA API URL of the server. 

##### SaaS
The URL option is usually `https://[region]api.[domain]/v1.0/`, for example `https://us1api.wdgautomation.com/v1.0/`. But RPA CLI already knows the SaaS APIs and you can just provide `--url default`.

##### On-Premise
The URL option is your API server URL followed by `/v1.0/`. For example, if your API server is `https://192.123.654`, then the URL should be `https://192.123.654/v1.0/`.

#### Region
The region option is only applicable in SaaS. It's one of the following values: *AP1*, *BR1*, *BR2*, *EU1*, *UK1*, *US1*. See [understanding tenants and regions](https://www.ibm.com/docs/en/rpa/23.0?topic=client-prerequisites-install#understanding-tenants-and-regions) for more details.

#### Tenant
The tenant option is the tenant **code**. See [getting your tenant code](https://www.ibm.com/docs/en/rpa/23.0?topic=client-prerequisites-install#getting-your-tenant-code) for more details.

#### User name and password
The credentials to establish a connection to the environment. These are not stored in the `[project_name].rpa.json` configuration file. The credentials are not stored anywhere. Please refer to the [security](security.md) section for more information.

# Next steps
* [Managing package sources](guide/package-source.md)
* [Deploying](guide/deploy.md) your project to an environment.