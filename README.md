# Blobber
Merge or embed .NET assemblies

## How to use it

Install the [NuGet package](https://www.nuget.org/packages/Blobber/) in the target project.
By default, referenced private assemblies (the ones with 'copy local' set) will be embedded and loaded when trying to be resolved.

## Tuning with the `Blobber` file

However, the Blobber does much more.  
It can also **merge** instead of embedding.  
Also, assemblies referenced as not private ('copy local' not set) can be processed.

In order to make fine tuning, simply add a file named `Blobber` at the projet root (it can be of any type, so the `none` is recommended).
The syntax is:  
`[<scope>]<name>:<action>`
Where:  
  - `<scope>` can be `+`, `-` or left empty (in which case it is equivalent to `-`). `-` addresses private assemblies (the 'copy local') and `+` specifies public assemblies (I am honestly not sure this feature is useful)
  - `<name>` is the name of the assembly to match. Wildcard are supported, so `SQLlite` or `Microsoft.*` will work as expected
  - `<action>` is `none`, `merge` or `embed`.

Note: all lines are always processed, so you need to start from less specific to most specific.

So if no `Blobber` file is found in project it will behave as if this line was specified:  
`*: embed`
(even if a `Blobber` file is used, the line above is always used before any other line).
