# Frequently asked questions

## Does it support SaaS?
Yes.

## Does it support On-Premise?
Yes. RPA CLI supports On-Premise servers with version `23.0.3` and later.

## Does it support Red Hat® OpenShift®?
Yes. RPA CLI supports OpenShift® Container Platform (OCP) servers with version `23.0.3` and later.

## Does it support Single Sign-On?
Yes. RPA CLI follows the [IBM RPA documentation](https://www.ibm.com/docs/en/rpa/23.0?topic=call-authenticating-rpa-api#authenticating-to-the-api-through-zen) to support Single Sign-On for Red Hat® OpenShift®.

## Which versions of IBM® RPA are supported?
Read the [prerequisites](guide/getting-started.md#prerequisites).

## Does the client software needs to be installed?
RPA CLI does not required the *client software* to be installed in the machine.

## Does it support running on Continuous Deployment (CD) platforms?
Yes. On CD platforms, RPA CLI pick ups credentials from the system environment variables. Read more [here](https://ibm.github.io/ibm-rpa-cli/#/guide/environment?id=user-name-and-password). 

## Does it support running on Linux?
RPA CLI was built on top of [.NET 7](https://dotnet.microsoft.com/en-us/download/dotnet/7.0) which is supported on Linux. But the distributed executable only runs on Windows. *Support for Linux is coming*.