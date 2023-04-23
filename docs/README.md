!> **Warning**: this tool is **unofficial** and it's highly opinionated!

# Get Started
## Install
TODO
## Usage
Create a working directory, for example *Demo*: `mkdir MyProject`.

Open the command line and navigate to the working directory.

Create the RPA project.
```powershell
rpa project new MyProject
```

Create the bot from a template.
```powershell
rpa bot new MyBot --template unattended
```

Add other WAL files to the working directory or pull existing files from an environment.
```powershell
rpa env new dev
rpa pull Demo* --type wal
```

Build the project.
```powershell
rpa build --output "./out"
```

Test the generated WAL file from `out` directory in IBM RPA Studio.

Deploy the project to an environment.
```powershell
rpa env new prod
rpa deploy prod
```


# About
TODO

# Support
TODO

# Author
<img src='https://avatars.githubusercontent.com/u/165290?v=4' alt='Joba' width='75px'/>
<a style='margin-left:10px' href='https://github.com/JobaDiniz' target='_blank'>Joba</a>

# License
Licensed under [MIT](https://getpino.io/#/./LICENSE).