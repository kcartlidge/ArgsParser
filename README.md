# ArgsParser

Easy argument parsing for .Net applications (Core 3 or later).
Full unit test coverage. Compatible with NetStandard 2.0.
Available as [a nuget package](https://www.nuget.org/packages/ArgsParser/).

## Contents

- [Example usage](#example-usage)
	- [Custom option validators](#custom-option-validators)
- [Auto-generated helper text](#auto-generated-helper-text)
- [Supported features](#supported-features)
- [Example input and errors](#example-input-and-errors)
- [A more detailed example](#a-more-detailed-example)

## Example usage

``` csharp
using ArgsParser;

// Define the options and flags required or supported.
var parser = new Parser(args)
    .SupportsOption<int>("port", "Port to start the dev server on", 1337)
    .RequiresOption<string>("read", "Folder to read the site from", "site")
    .RequiresOption<string>("write", "Folder to write the result to")
    .SupportsFlag("serve", "Start the site going in a dev server")
    .SupportsFlag("force", "Overwrite any destination content");

// Show it to the user.
parser.Help();

// Check their input.
parser.Parse();

// Show any errors, and abort.
if (parser.HasErrors)
{
    parser.ShowErrors();
    return;
}

// Summarise what they chose.
parser.ShowProvidedArguments();

// Make use of the options/flags.
var startServing = parser.IsFlagProvided("serve");
var port = parser.GetOption<int>("port");
var read = parser.GetOption<string>("read");
```

### Custom option validators

Standard validation is concerned with the presence/absence of arguments.
Custom option validators allow you to also check their *contents*.

For example, here's a custom validator function that checks an option contains a CSV filename. This same function can be used repeatedly for multiple options.
You can also declare inline functions using lambda but this is clearer for explanatory purposes.

```cs
/// <summary>Sample validator function which checks for a CSV filename.</summary>
/// <param name="key">Name of the argument.</param>
/// <param name="value">Content passed in.</param>
/// <returns>A list of any errors.</returns>
private List<string> IsCSV(string key, object value)
{
    // In reality we would also need null checks etc.
    var errs = new List<string>();
    var ext = Path.GetExtension($"{value}").ToLowerInvariant();
    if (ext != ".csv") errs.Add($"{key} does not hold a CSV filename.");
    return errs;
}
```

The signature is always the same. Your validator receives an option name and value, then returns a list of zero or more error messages which will be automatically gathered alongside the standard errors. The value is an `object` because your options are generically typed and therefore there is no guarantee what the incoming type will be. (It's your codebase; if you know which options your validator is being registered with you can make casting assumptions.)

Once you have a validator you need to register it:

```cs
var parser = new Parser(args)
    .SupportsOption<string>("filename", "A CSV filename")
    .AddCustomOptionValidator("filename", IsCSV);
parser.Parse();
```

*Accessing errors is described further on.*

## Auto-generated helper text

#### `Parser.Help();`

This method supports an optional parameter to specify an indent when writing to the screen.

*These are displayed in the order they were created on the parser instance in your code. Here's an example.*

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

#### `Parser.ShowErrors()`

This method supports an optional parameter to specify an indent when writing to the screen.

``` text
Option missing: write
Unknown flag: run
```

#### `Parser.ShowProvidedArguments();`

This method supports an optional parameter to specify an indent when writing to the screen.

``` text
-port  3000
-read  in.txt
-force
-serve
```

#### `Parser.GetProvided()`

This doesn't directly generate output text; it returns a dictionary of key/value pairs for the provided arguments.
The `key` is the name of the matching option or flag.
The `value` (returned as an `object`) contains either `null` for a flag or the type-converted input for an option.

Example usage:

```cs
Console.Write("MyApp");
foreach (var item in parser.GetProvided())
{
    if (item.Value == null) Console.Write($" -{item.Key}");
    else Console.Write($" -{item.Key} \"{item.Value}\"");
}
```

Assuming `MyApp` was the name of your application, this would recreate the command used when it was called. For example:

```
MyApp -port "3000" -read "in.txt" -force -serve
```

*These are returned in the order they were created on the parser instance in your code.*
You can easily isolate options and flags using something like `.Where(x => x.Value == null)`.

#### `GetProvidedAsCommandArgs()`

This automatically wraps up the result of `Parser.GetProvided()` as a space-delimited command argument string.  In other words, it returns all the provided options/flags in the ideal format (minus the leading application name).

This can be used to annotate your own app's output with the command needed to produce that output, for example, or to automatically document how to recreate the effect of the current run.

Based on the `GetProvided` example above, it would return:

```
-port "3000" -read "in.txt" -force -serve
```

## Supported features

- Display help showing supported flags/options
  - Also shows argument types, defaults, and optional legend
- Display all errors
- Display all provided input arguments
- Required named option/values
- Optional named option/values
- Optional named flags
- Default option values
- Option types support any `IConvertable`, including `int`, `bool`, `DateTime`
- Accepts either `-` or `--` prefixes
- Provides two collections of error messages
  - Expectation errors
    - Missing required options
    - Custom option validator errors
  - Argument errors
    - Option values of incorrect type
      - This *may* be switched to be an Expectation error in a future change
    - Unexpected values (not with an option)
    - Unknown flags or options

## Example input and errors

These assume the arguments defined in the [Example usage](#example-usage) section above.

Example user input:

``` batch
MyApp -run data "Site Title" --serve -ignore -port 3000
```

There are a few things wrong with this input in relation to the setup of the options/flags in the example usage code:

- The `-write` option is required but not provided
- The provided `-run` option is not defined
- The `"Site Title"` argument has no option name preceeding it
- The provided `-ignore` flag is not defined

Whilst the `-read` option is missing there is no error logged - it was defined with a default value of `site` and so the requirement is automatically met.

Errors come in two collections (the property `Parser.HasErrors` will be `true` if either has entries):

- `ExpectationErrors` are where specific expectations are not met (eg a missing required option) so the relevant option/flag whose expectations are not being met is known
  - Custom option validator errors will also be in here
- `ArgumentErrors` are where something was provided but there were general issues with it (eg a value provided without an option name preceeding it) so there is no certainty as to what was intended by the input given and we cannot definitively tie it to a specific option/flag

Based on the example above the errors (as key/value pairs) will be as follows:

- `ExpectationErrors` keyed by the name of the related option/flag
  - `write` => `Option missing: write`
- `ArgumentErrors` keyed by the 0-based offset into the arguments provided
  - `0` => `Unknown option: run`
  - `2` => `Unexpected value: Site Title`
  - `4` => `Unknown flag: ignore`

## A more detailed example

(The assertions included below use NUnit. See [the test project](./ArgsParser.Tests).)

``` csharp
var args = new string[] { "-run", "data", "Site Title", "--serve", "-ignore", "-port", "3000" };

var parser = new Parser(args)
  .SupportsOption<int>("port", "Port to start the dev server on", 1337)
  .RequiresOption<string>("read", "Folder to read the site from", "site")
  .RequiresOption<string>("write", "Folder to write the result to")
  .SupportsFlag("serve", "Start the site going in a dev server")
  .SupportsFlag("force", "Overwrite any destination content")
  .Help();

var result = parser.Parse();

Assert.AreEqual(4, result.ExpectationErrors.Count + result.ArgumentErrors.Count);
Assert.Contains("Option missing: write", result.ExpectationErrors.Values.ToList());
Assert.Contains("Unknown option: run", result.ArgumentErrors.Values.ToList());
Assert.Contains("Unexpected value: Site Title", result.ArgumentErrors.Values.ToList());
Assert.Contains("Unknown flag: ignore", result.ArgumentErrors.Values.ToList());

Assert.IsTrue(result.IsOptionProvided("port"));
Assert.AreEqual(3000, result.GetOption<int>("port"));

Assert.IsTrue(result.IsOptionProvided("read"));
Assert.AreEqual("site", result.GetOption<string>("read"));

Assert.IsFalse(result.IsOptionProvided("write"));
Assert.AreEqual(null, result.GetOption<string>("write"));

Assert.IsTrue(result.IsFlagProvided("serve"));
Assert.IsFalse(result.IsFlagProvided("force"));
```

---

Copyright K Cartlidge 2020-2024.

Licensed under [GNU AGPLv3](./LICENSE) ([see here for more details](https://choosealicense.com/licenses/agpl-3.0/)).
See the [CHANGELOG](./CHANGELOG.md) for current status.
