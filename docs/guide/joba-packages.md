# Joba packages
The RPA CLI author has created a [package source](guide/package-source.md) named *Joba Packages* where there are published packages for anybody to use.

## Access
There isn't an automatic way to request access to the *Joba Packages* package source yet. Please reach out to the author requesting access to the tenant.

After you were granted access, issue the following command to configure it as a [package source](guide/package-source.md) for your project.

```bash
rpa package source Joba --url "https://us1api.wdgautomation.com/v1.0/" --region US1 --tenant 5547 --username [username] --password [password]
```

## Packages
These are the current packages available.

### Joba_AccuWeather
Provides methods to use the [Accu Weather API](https://developer.accuweather.com/).

#### DailyForecasts
Returns the next 5 days of forecasts for a particular location.

**Inputs**
* `String` apiKey (**required**)
* `String` locationKey (**required**) - get from *SearchCities* method.

**Outputs**
* `Table` forecasts - a table with the following columns: `Date` Date | `Integer` Minimum | `Integer` Maximum | `String` Unit | `String` Icon | `String` Text

#### SearchCities
Returns the cities metadata for a particular search term.

**Inputs**
* `String` apiKey (**required**)
* `String` offset (**required**) - pagination.
* `String` query (**required**) - the name of city to search.

**Outputs**
* `Table` locations - a table with the following columns: `String` Key | `Integer` Rank | `String` Region | `String` Country | `String` City

### Joba_OrangeHRM
**in progress...*

### Joba_Security
**in progress...*

### Joba_System
**in progress...*

# Seting up your own
The way *Joba Packages* tenant is configured is particular interesting and might serve as a guide on how to create *private* package sources.

1. Create a tenant to act as a package source.
2. Create a [role](https://www.ibm.com/docs/en/rpa/23.0?topic=teams-managing-roles) named *Package downloader* with *View scripts* permission.
3. Create users that want to install packages and assign the *Package downloader* role.

<img src="_assets/package-downloader.png"/>

## Package template
The author has a template to create WAL scripts to act as packages. This template allows one WAL script to have multiple methods (or functions). The following is the *main* routine of a package.
```
defVar --name methodNames --type List --innertype String --parameter
defVar --name methodName --type String --parameter
#region validating
case --switches "CaseSwitchesAll"
	when --left "${methodName}" --operator "Is_Null_Or_Empty"
	when --left "${methodNames}" --operator "Is_Empty"
then
	failTest --message "The parameter \'methodName\' or \'methodNames\' is required and was not specified"
endCase
#endregion

if --left "${methodName}" --operator "Is_Null_Or_Empty" --negate
	add --collection "${methodNames}" --value "${methodName}"
endIf
foreach --collection "${methodNames}" --variable "${methodName}" --distinct
	goSub --label "${methodName}"
endFor
```

You define a **public** method by creating a routine. You define a **private** method by creating a routine with prefixing `__` in its name, for example:
* Public: `DailyForecasts`
* Private: `__BuildForecasts`

The above pattern also applies to variables.

Here's a full example of `Joba_OrangeHRM` package:
```
defVar --name methodNames --type List --innertype String --parameter
defVar --name methodName --type String --parameter
defVar --name employeeName --type String --parameter
defVar --name leavesReport --type DataTable --output
defVar --name leaveType --type String
defVar --name leavesReportImage --type Image --output
defVar --name leaveEntitlements --type Numeric
defVar --name leavePendingApproval --type Numeric
defVar --name leaveScheduled --type Numeric
defVar --name leaveTaken --type Numeric
defVar --name leaveBalance --type Numeric
defVar --name leaveEntitlementsText --type String
defVar --name leavePendingApprovalText --type String
defVar --name leaveScheduledText --type String
defVar --name leaveTakenText --type String
defVar --name leaveBalanceText --type String
defVar --name isEmployeeInvalid --type Boolean
defVar --name currentRow --type Numeric
defVar --name webTable --type DataTable
defVar --name loginFailedMessage --type String
defVar --name ownsBrowser --type Boolean
defVar --name userName --type String --parameter
defVar --name userPassword --type String --parameter
defVar --name browser --type Browser --parameter
defVar --name browserName --type String --value chrome
defVar --name isLoggedIn --type Boolean
defVar --name timesheetReport --type DataTable --output
defVar --name timesheetStatus --type String --output
defVar --name url --type String --parameter
defVar --name workingDirectory --type String --parameter
#region debug data
setVarIf --variablename "${methodName}" --value Timesheet --left "${rpa:runtimeEnvironment}" --operator "Equal_To" --right development
setVarIf --variablename "${url}" --value "http://orange.wdgautomation.com/symfony/web/index.php" --left "${rpa:runtimeEnvironment}" --operator "Equal_To" --right development
setVarIf --variablename "${userName}" --value sysadmin --left "${rpa:runtimeEnvironment}" --operator "Equal_To" --right development
setVarIf --variablename "${userPassword}" --value "tr@klwuS2OWLs#ebiB-t" --left "${rpa:runtimeEnvironment}" --operator "Equal_To" --right development
setVarIf --variablename "${employeeName}" --value "Garry White" --left "${rpa:runtimeEnvironment}" --operator "Equal_To" --right development
#endregion

#region validating
case --switches "CaseSwitchesAll"
	when --left "${methodName}" --operator "Is_Null_Or_Empty"
	when --left "${methodNames}" --operator "Is_Empty"
then
	failTest --message "The parameter \'methodName\' or \'methodNames\' is required and was not specified"
endCase
#endregion

if --left "${methodName}" --operator "Is_Null_Or_Empty" --negate
	add --collection "${methodNames}" --value "${methodName}"
endIf
foreach --collection "${methodNames}" --variable "${methodName}" --distinct
	goSub --label "${methodName}"
endFor

//before finalizing the script, close the browser if we own it
gosubIf --label __CloseBrowser --left "${ownsBrowser}" --operator "Is_True"
beginSub --name Timesheet
	goSub --label __StartBrowserAndLoginIfNeeded
	
	webNavigate --url "${url}/time/viewEmployeeTimesheet"
	webWait --timeout "00:00:05"
	webWaitElement --selector "Id" --id employee --timeout "00:00:05"
	webSet --value "${employeeName}" --selector "Id" --id employee --simulatehuman
	webWaitElement --selector "CssSelector" --css "#employeeSelectForm > fieldset > ol > li:nth-child(1) > span" --timeout "00:00:03" isEmployeeInvalid=value
	assert --message "Employee does not exist: ${employeeName}" --left "${isEmployeeInvalid}" --operator "Is_True" --negate
	
	webClick --selector "Id" --id btnView
	webWait --timeout "00:00:05"
	webSetComboBox --selectoptionby "MatchByText" --matchbytext "2020-08-31 to 2020-09-06" --selector "Id" --id startDates
	webWait --timeout "00:00:05"
	webGetTable --removehtml  --selector "CssSelector" --css "#timesheet > div > div.tableWrapper > table" timesheetReport=value
	
	webGet --selector "Id" --id timesheet_status timesheetStatus=value
	replaceText --texttoparse "${timesheetStatus}" --textpattern "Status:" timesheetStatus=value
	trimString --text "${timesheetStatus}" --trimoption "TrimStartAndEnd" timesheetStatus=value
	
	gosubIf --label __LogOutput --left "${rpa:runtimeEnvironment}" --operator "Equal_To" --right development
endSub
beginSub --name __StartBrowserAndLoginIfNeeded
	setVarIf --variablename "${ownsBrowser}" --value true --left "${browser}" --operator "Is_Null"
	if --left "${browser}" --operator "Is_Null"
		webStart --name "${browserName}" --type "Chrome" browser=value
	else
		webAttach --browser ${browser} --name "${browserName}"
	endIf
	goSub --label Login
	assert --message "System Error: ${loginFailedMessage}" --left "${loginFailedMessage}" --operator "Is_Null_Or_Empty"
endSub
beginSub --name __CloseBrowser
	if --left "${rpa:runtimeEnvironment}" --operator "Equal_To" --right development
		webClose --name "${browserName}" --leavebrowseropen
	else
		webClose --name "${browserName}"
	endIf
endSub
beginSub --name __BuildLeavesReport
//Since the 'webGetTable' command doesn't do a good job getting the table from this page, we need to build the table ourselves
	
	addColumn --dataTable ${leavesReport} --columnname "Leave Type" --type String
	addColumn --dataTable ${leavesReport} --columnname "Leave Entitlements" --type Numeric
	addColumn --dataTable ${leavesReport} --columnname "Leave Pending Approval" --type Numeric
	addColumn --dataTable ${leavesReport} --columnname "Leave Scheduled" --type Numeric
	addColumn --dataTable ${leavesReport} --columnname "Leave Taken" --type Numeric
	addColumn --dataTable ${leavesReport} --columnname "Leave Balance" --type Numeric
	for --variable ${currentRow} --from 1 --to ${webTable.Rows} --step 1
		mapTableRow --dataTable ${webTable} --row ${currentRow} --mappings "number=1=${leaveType},number=2=${leaveEntitlementsText},number=3=${leavePendingApprovalText},number=4=${leaveScheduledText},number=5=${leaveTakenText},number=6=${leaveBalanceText}"
		convertStringToNumber --culture "en-US" --text "${leaveBalanceText}" --allowleadingsign  --allowdecimalpoint  leaveBalance=value
		convertStringToNumber --culture "en-US" --text "${leaveEntitlementsText}" --allowleadingsign  --allowdecimalpoint  leaveEntitlements=value
		convertStringToNumber --culture "en-US" --text "${leavePendingApprovalText}" --allowleadingsign  --allowdecimalpoint  leavePendingApproval=value
		convertStringToNumber --culture "en-US" --text "${leaveScheduledText}" --allowleadingsign  --allowdecimalpoint  leaveScheduled=value
		convertStringToNumber --culture "en-US" --text "${leaveTakenText}" --allowleadingsign  --allowdecimalpoint  leaveTaken=value
		trimString --text "${leaveType}" --trimoption "TrimStartAndEnd" leaveType=value
		case --switches "Any"
			when --left "${leaveBalance}" --operator "Greater_Than" --right 0
			when --left "${leaveEntitlements}" --operator "Greater_Than" --right 0
			when --left "${leavePendingApproval}" --operator "Greater_Than" --right 0
			when --left "${leaveScheduled}" --operator "Greater_Than" --right 0
			when --left "${leaveTaken}" --operator "Greater_Than" --right 0
		then
			addRow --valuesmapping "Leave Type=${leaveType},Leave Entitlements=${leaveEntitlements},Leave Pending Approval=${leavePendingApproval},Leave Scheduled=${leaveScheduled},Leave Taken=${leaveTaken},Leave Balance=${leaveBalance}" --dataTable ${leavesReport}
		endCase
	next
endSub
beginSub --name __LogOutput
	logMessage --message "Leaves Report:\r\n${leavesReport}\r\n" --type "Info"
	logMessage --message "Timesheet:\r\n${timesheetStatus}\r\n${timesheetReport}\r\n" --type "Info"
endSub
beginSub --name LeavesReport
	goSub --label __StartBrowserAndLoginIfNeeded
	
	webNavigate --url "${url}/leave/viewLeaveBalanceReport"
	webWait --timeout "00:00:05"
	webWaitElement --selector "Id" --id leave_balance_report_type --timeout "00:00:05"
	webSetComboBox --selectoptionby "MatchByText" --matchbytext Employee --selector "Id" --id leave_balance_report_type
	webWaitElement --selector "Id" --id leave_balance_employee_empName --timeout "00:00:05"
	webSet --value "${employeeName}" --selector "Id" --id leave_balance_employee_empName --simulatehuman
	webClick --selector "CssSelector" --css "body > div.ac_results > ul > li > strong"
	webSetComboBox --selectoptionby "Value" --value "2022-01-01$$2022-12-31" --selector "Id" --id period --comment "2022-01-01 - 2022-12-31"
	webClick --selector "Id" --id viewBtn
	webWait --timeout "00:00:05"
	webWaitElement --selector "CssSelector" --css "#frmLeaveBalanceReport > fieldset > ol > li:nth-child(2) > span" --timeout "00:00:01" isEmployeeInvalid=value
	assert --message "Employee does not exist: ${employeeName}" --left "${isEmployeeInvalid}" --operator "Is_True" --negate
	webGetTable --removehtml  --selector "CssSelector" --css "#report-results > div > table" webTable=value
	webGetImage --selector "CssSelector" --css "#report-results > div > table" leavesReportImage=value
	
	goSub --label __BuildLeavesReport
	gosubIf --label __LogOutput --left "${rpa:runtimeEnvironment}" --operator "Equal_To" --right development
endSub
beginSub --name Login
	setVarIf --variablename "${ownsBrowser}" --value true --left "${browser}" --operator "Is_Null"
	
	if --left "${browser}" --operator "Is_Null"
		webStart --name "${browserName}" --type "Chrome" browser=value
	else
		webAttach --browser ${browser} --name "${browserName}"
	endIf
	goSub --label __IsLoggedIn --comment "check if the user is already logged in"
	if --left "${isLoggedIn}" --operator "Is_True" --negate
		webNavigate --url "${url}"
		webWaitElement --selector "Id" --id txtUsername --timeout "00:00:05"
		webSet --value "${userName}" --selector "Id" --id txtUsername
		webSet --value "${userPassword}" --selector "Id" --id txtPassword
		webClick --selector "Id" --id btnLogin
		webWait --timeout "00:00:05"
		
		goSub --label __IsLoggedIn --comment "verify if the user was successfully logged in"
		if --left "${isLoggedIn}" --operator "Is_True" --negate
			webGet --selector "CssSelector" --css "#divLoginButton > span" loginFailedMessage=value
		endIf
	endIf
endSub
beginSub --name __IsLoggedIn
	webWaitElement --selector "Id" --id welcome --timeout "00:00:01" --comment "verify whether the user is already logged in" isLoggedIn=value
endSub
```