# Support
RPA CLI is open sourced and support relies on the contributors.

* Open a [github issue](https://github.com/IBM/ibm-rpa-cli/issues/new?template=bug_report.yml) for support.
* Open a [github request](https://github.com/IBM/ibm-rpa-cli/issues/new?template=feature_request.md) for new features.
* Open a [github question](https://github.com/IBM/ibm-rpa-cli/issues/new?template=question.md) for doubts.

## Troubleshooting
You can troubleshoot errors by specifying the `-v Detailed` (or just `-v`) option to get more logs into the console for any [available commands](reference.md). For example:

```bash
rpa build -v
```

You can also look at the `rpa cli.log` file under the hidden `.rpa\` directory within the *working directory*.

?> Use the `.rpa\rpa cli.log` file contents to submit [github issues](https://github.com/IBM/ibm-rpa-cli/issues/new?template=bug_report.yml).

!> The log file is a *rolling log file* based on file size, that means, when the `rpa cli.log` file reaches 1MB, another `rpa cli1.log` file will be created, and when that new file reaches 1MB, another `rpa cli2.log` will be created, and so forth.