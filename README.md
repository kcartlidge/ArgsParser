# ArgsParser

**Easy argument parsing** for .Net applications.
Handles **options** (arguments with parameters) and **flags** (simple switches), with the facility to show **automatically generated** usage details and errors.

Compatible with NetStandard 2.0 for use in Core 3 or later (tested to Net 8).
Available as a [nuget package](https://www.nuget.org/packages/ArgsParser/).

## Contents

- [Supported Features](#supported-features)
- [**Example Usage**](#example-usage)
  - [Example Output](#example-output)
- [Custom Validation for Options](#custom-validation-for-options)
- [Showing Helpful Information to the User](#showing-helpful-information-to-the-user)
- [Getting the Provided Options and Flags](#getting-the-provided-options-and-flags)
- [Checking for Errors Manually](#checking-for-errors-manually)
- [Examples of Argument Errors](#examples-of-argument-errors)

---

## Supported Features

- Supports *required* named *options*
- Supports *optional* named *options*
- Supports *defaults* for option values
- Supports *optional* named *flags*
- Display *automatically formatted help/usage text* showing supported flags/options
  - Also shows argument types, defaults, and an optional legend
- Display all *errors* (automatically formatted)
- Display all *provided arguments* (automatically formatted, including any defaults)
- Allows *custom validators* for options
  - For example enforcing that a provided string is a CSV filename
- Option data types support any `IConvertable`, including `int`, `bool`, `DateTime`
- Arguments can use either `-` or `--` prefixes
- Helpful errors for a variety of situations
  - Missing required options
  - Unknown options or flags
  - Custom validator errors
  - Option values of incorrect type
  - Unexpected values (not following an option)

## Example Usage

1. Set up the options and flags
2. Optionally show automatically-formatted instructions to your callers
3. Get *ArgsParser* to deal with the provided arguments
4. Show any errors (also automatically formatted)
5. Use the options and flags in your own code

There's a tiny [example console app](./ArgsParser.Example/Program.cs), the contents of which are basically as follows:

``` csharp
using ArgsParser;
...

// Define the options and flags, including whether required and any default values.
var indent = 2;
var parser = new Parser(args)
  .SupportsOption<int>("port", "Port to start the dev server on", 1337)    // Optional, with default.
  .RequiresOption<string>("read", "Folder to read the site from", "site")  // Required, with default.
  .RequiresOption<string>("write", "CSV file to write the result to")      // Required, no default.
  .SupportsFlag("serve", "Start the site going in a dev server")           // Optional flag.
  .SupportsFlag("force", "Overwrite any destination content")              // Optional flag.
  .AddCustomValidator("write", IsCSV)  // Automatic extra check.
  .ShowHelpLegend(true)  // Include explanatory notes in Help text?
  .Help(indent, "Usage:")  // Show instructions to the user.
  .Parse()  // Check the provided input arguments.
  .ShowProvided(indent, "Provided:");  // Summarise the provided options and flags.

// Show any errors, and abort.
if (parser.HasErrors)
{
    parser.ShowErrors(indent, "Issues:");
    return;
}

// Examples of accessing the options/flags.
var shouldServe = parser.IsFlagProvided("serve");
var port = parser.GetOption<int>("port");
var readFromFolder = parser.GetOption<string>("read");
var writeToFolder = parser.GetOption<string>("write");
```

The methods used, including `AddCustomValidator()`, are detailed further on.

### Example Output

User command:

``` shell
MyApp -serve -verbose true
```

When called with the above (and using the example configuration just discussed) the default values for `-port` and `-read` will be included automatically and the user will see the following:

``` text
Usage:
  -port   integer    Port to start the dev server on  [1337]
  -read   text     * Folder to read the site from  [site]
  -write  text     * Folder to write the result to
  -serve             Start the site going in a dev server
  -force             Overwrite any destination content

  * is required, values in square brackets are defaults

Provided:
  -port  1337
  -read  site
  -serve

Issues:
  -write is required
  -verbose is an unknown option
```

(A fuller explanation of the *Issues:* appears shortly in the [Examples of Argument Errors](#examples-of-argument-errors) section below.)

## Custom Validation for Options

Flags don't need custom validation; they are either provided or they're not. For options you can add custom validation beyond the built-in 'required' setting and the user-provided value's conversion to the expected data type.

Standard validation is concerned with the *presence/absence* of arguments.
Custom option validators allow you to also check their *contents*.

For example, here's a custom validator function that checks an option contains a CSV filename. This same function can be assigned to multiple options (eg both an input filename and an output filename). You can also use inline lambda but a full function is both clearer for explanatory purposes and also reusable.

```csharp
/// <summary>Sample validator function which checks for a CSV filename.</summary>
/// <param name="key">Name of the argument.</param>
/// <param name="value">Content passed in.</param>
/// <returns>A list of any errors to be added to the parser's automatic ones.</returns>
private List<string> IsCSV(string key, object value)
{
    // In reality we would also need null checks etc.
    var errs = new List<string>();
    var ext = Path.GetExtension($"{value}").ToLowerInvariant();
    if (ext != ".csv") errs.Add($"{key} does not hold a CSV filename");
    return errs;
}
```

A custom validator always receives an option name and any value provided. It should return a list of zero or more error messages which will be automatically included alongside the standard automatically generated ones.

The incoming value is an `object` as the incoming data type may be one of a variety. The signature could be made generic, but validators are *your* code so you know what types your validator can expect and can safely cast as appropriate.

Once you have a validator you need to register it for any options requiring the check. You do this by calling `AddCustomValidator` with the option name and the validation function.

```csharp
var parser = new Parser(args)
    .SupportsOption<string>("filename", "A CSV filename")
    .AddCustomValidator("filename", IsCSV);
parser.Parse();
```

## Showing Helpful Information to the User

Most of the helpful text methods discussed below take two parameters:

- `int indent = 0`
  - allows the lines of text to be shifted to the right
- `string heading = ""`
  - any heading to show *above* the text (not indented)

In general an `indent` of `0` is fine *without* headings, and `2` works well *with* headings.

### `Parser.Help(int indent = 0, string heading = "");`

This is the typical 'usage' information users might expect to see.
Options and flags are displayed in the order they were added to the parser instance in your code, and whilst it's entirely up to you it's usually clearer if you therefore register all your options before adding any flags.

Here's an example help output showing a variety of options/flags:

``` text
-port    integer       Port to start the dev server on  [1337]
-read    text        * Folder to read the site from  [site]
-write   text        * Folder to write the result to
-apr     number        Annual interest  [3.596]
-fee     number        Monthly charge  [19.50]
-secure  true/false    Serve on HTTPS?  [True]
-until   datetime      When to stop serving  [22/08/2023 06:28:13]
-force                 Overwrite any destination content
-serve                 Start the site going in a dev server

* is required, values in square brackets are defaults
```

The last line is optional and can be deactivated by `.ShowHelpLegend(false)` when configuring the `Parser` instance (the default is `true`).

### `Parser.ShowErrors(int indent = 0, string heading = "")`

All issues are displayed in a list. There are two distinct types of error and all errors of the first type will appear before any of the second type.

- The first type are errors with *known* options/flags and they will all show first in the order the options/flags were registered in the parser
- The second type are errors where the thing provided by the user isn't recognised (eg an unknown flag) and these appear in the order they were provided in the arguments

``` text
-write is required
-run is an unknown flag
```

### `Parser.ShowProvided(int indent = 0, string heading = "");`

This shows all the provided arguments in the order the options/flags were added to the parser instance.
Any options which weren't provided by the user will also appear here if a default was applied.

``` text
-port  3000
-read  in.txt
-force
-serve
```

## Getting the Provided Options and Flags

### `bool IsOptionProvided(string optionName)` and `bool IsFlagProvided(string flagName)`

These return `true` if the option or flag was provided.

``` csharp
if (parser.IsFlagProvided("serve")) ...
```

### `T GetOption<T>(string optionName)`

Returns the generically typed value provided.

``` csharp
var port = parser.GetOption<int>("port");
```

- If the option provided was the wrong type an `InvalidCastException` is thrown
- If the option was *not provided* but there *is a default* configured that is returned
- If the option was *not provided* and there *is no default* then the default value for the .Net type is returned (eg `0` for an `Int32` or `false` for a `bool`)

### `Dictionary<string, object> Parser.GetProvided()`

This is a helper method; checks for specific options and flags are simpler using the type-aware `IsOptionProvided()`, `IsFlagProvided()`, and `GetOption()` as detailed above.
This method returns a dictionary of key/value pairs for the provided arguments in the order they were created on the parser instance in your code.

For each entry the `key` is the name of the matching option or flag and the `value` (returned as an `object`) contains the type-converted value for an option or `null` for a flag (as flags don't have values).
You can easily isolate options and flags using something like `.Where(x => x.Value == null)`.

Example usage:

```csharp
// Display arguments as either `-flag` or `-option value`.
Console.Write("MyApp");
foreach (var item in parser.GetProvided())
{
    if (item.Value == null) Console.Write($" -{item.Key}");
    else Console.Write($" -{item.Key} '{item.Value}'");
}
```

Assuming `MyApp` was the name of your application, this would recreate the command used when it was called. For example:

``` shell
MyApp -port "3000" -read "in.txt" -force -serve
```

That's a contrived example usage, though, as the next command does this for you anyway ...

### `string GetProvidedAsCommandArgs()`

This automatically wraps up the result of `Parser.GetProvided()` (detailed above) as a space-delimited command argument string.  In other words, it returns all the provided options/flags in the ideal format (minus the leading application name).

This can be used for example to include the command the user ran within the output their command generated.

``` csharp
Console.WriteLine("MyApp " + parser.GetProvidedAsCommandArgs());
```

This would output something like:

``` text
MyApp -port "3000" -read "in.txt" -force -serve
```

## Checking for Errors Manually

*Usually all you need to do is check one flag and use the built-in helper to show the issues:*

``` csharp
if (parser.HasErrors)
{
  parser.ShowErrors(indent, "Issues:");
  return;
}
```

If you want more granular access to the errors there are two collections:

- `Dictionary<string, string> ExpectationErrors`
  - Contains any errors where option/flag expectations are not met by the arguments
  - Keyed by alphabetical option/flag name
  - Example: `"write", "-write does not hold a CSV filename"`
- `SortedList<int, string> parser.ArgumentErrors`
  - Contains errors relating to unknown arguments, indexed by order of discovery
  - Keyed by order of discovery
  - Example: `2, "-verbose is an unknown option"`

## Examples of Argument Errors

These examples assume the `Parser` was defined as shown in the [Example usage](#example-usage) section previously detailed.

Example user input:

``` shell
MyApp -run data "Site Title" --serve -ignore -port 3000
```

There are a few things wrong with this input in relation to the setup of the options/flags in the example usage code:

- The `-write` option is *required but not provided*
- The provided `-run` option is *not known*
- The `"Site Title"` argument is *orphan text* with *no option name* preceeding it
- The provided `-ignore` flag is *not known*

Here's what that looks like when run:

``` text
Issues:
  -write is required
  -run is an unknown option
  Unexpected value: Site Title
  -ignore is an unknown flag
```

Whilst the `-read` option is missing there is no error logged as it was defined with a default value of `site` and so the requirement is automatically met. The value `data` is not treated as an error because even though `-run` is an unknown option *ArgsParser* knows that `data` would have belonged to `-run` so it isn't another error.

Errors come in two collections (the property `Parser.HasErrors` will be `true` if either has entries). These correspond to the two blocks of error messages described in the `ShowErrors()` method.

- `ExpectationErrors` are where specific expectations are not met (eg a missing required option), which means the relevant option/flag whose expectations are not being met is known
  - Custom option validator errors will also be in here as they apply to known options
- `ArgumentErrors` are where something was provided but the nature of the issue means we can't be sure which option/flag it relates to - for example a value was provided without an option name preceeding it

Based on the example above the errors (as key/value pairs) will be as follows:

- `ExpectationErrors` keyed by the name of the related option/flag
  - `write` => `-write is required`
- `ArgumentErrors` keyed by the 0-based offset within the arguments provided
  - `0` => `-run is an unknown option`
    - This is assumed to be an option not a flag as it is followed by a value
  - `2` => `Unexpected value: Site Title`
    - This is unexpected because `run` was not recognised as a valid option
  - `4` => `-ignore is an unknown flag`
    - This is assumed to be a flag because it is followed by `-port` rather than a value

---

Copyright K Cartlidge 2020-2024.

Licensed under [GNU AGPLv3](./LICENSE) ([see here for more details](https://choosealicense.com/licenses/agpl-3.0/)).
See the [CHANGELOG](./CHANGELOG.md) for current status.
