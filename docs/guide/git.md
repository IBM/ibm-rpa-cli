# Git integration
RPA CLI provides functionalities to integrate with [Git](https://git-scm.com/), enabling RPA developers to **compare** and **merge** WAL files through Git.

!> [Git is different from Github](https://stackoverflow.com/a/13321586/1830639) as Git is a revision control system, whereas Github is hosting service for git repositories. RPA CLI does not offer functionalities specific to Github platform.

The **compare** and **merge** features only works in **local git**, that is, platforms as Github does not know how to handle binary files such as WAL. However, pull request reviews can be used normally.

## Prerequisites
[VsCode](https://code.visualstudio.com/) needs to be installed as RPA CLI launches it for comparing and merging operations. In the future, RPA CLI might enable developers to choose which editor to use, but as of today, RPA CLI only supports VsCode.

## WAL is binary
Git does not understand binary files. Since WAL files are binary files, Git does not know how to handle them. If you do not tell Git how to handle WAL files, it will handle them as *text* files by default, and therefore, it will **corrupt** the files when saving them on *merging* and *committing* operations.

## The solution
Running the following command on a git repository will configure git to handle WAL files appropriately:

```bash
rpa git config
```

That's everything you need to do. If you want to understand more of what the above command does, keep reading.

!> If you already have a git repository where you've already committed WAL files without the above configuration, those WAL files are already *"corrupted"* by git because git indexed those files as text. Therefore, the git *merge* operation that RPA CLI hooks into will fail with *System.IO.EndOfStreamException: Failed to read past end of stream* error.

## How does the solution work?
RPA CLI uses a combination of Git features to deliver **compare** and **merge** functionalities integrated with Git.

### .gitattributes file
The `rpa git config` command creates (or updates) the [.gitattributes](https://www.git-scm.com/docs/gitattributes) file within the git repository. It adds the following line: `*.wal merge=rpa diff=rpa -text`. This tells git to use the `rpa` for merging and comparing operations on WAL files. Note that the `rpa` means nothing to git yet.

### .gitconfig file
The `rpa git config` command updates the **global** git configuration file appending the following:

```
[diff "rpa"]
	textconv = wal2txt
	cachetextconv = true
[merge "rpa"]
	name = rpa cli merge driver
	driver = rpa git merge %O %A %B -v
```

This tells git what `rpa` means in the *.gitattribute* file. Basically, this configuration proxies git events to RPA CLI commands, like `rpa git merge [params]`.

## .gitignore file
The `rpa git config` command creates (or updates) the [.gitignore](https://www.git-scm.com/docs/gitignore) file within the git repository. It adds the following lines:

```
.rpa
*.wad
packages
```

This tells git to ignore those paths.