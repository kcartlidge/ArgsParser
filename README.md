# ArgsParser v4.0.3

Easy argument parsing for .Net applications (Core 3 or later).
Full unit test coverage. Compatible with NetStandard 2.0.
Available as [a nuget package](https://www.nuget.org/packages/ArgsParser/).

## Contents

- [Example usage](#example-usage)
- [Auto-generated helper text](#auto-generated-helper-text)
- [Supported features](#supported-features)
- [Example input and errors](#example-input-and-errors)
- [A more detailed example](#a-more-detailed-example)

## Example usage

``` csharp
using ArgsParser;

var indent = 2;
var parser = new Parser(args)
    .SupportsOption<int>("port", "Port to start the dev server on", 1337)
    .RequiresOption<string>("read", "Folder to read the site from", "site")
    .RequiresOption<string>("write", "Folder to write the result to")
    .SupportsFlag("serve", "Start the site going in a dev server")
    .SupportsFlag("force", "Overwrite any destination content");

parser.Help(indent);
parser.Parse();

if (parser.HasErrors)
{
    parser.ShowErrors(indent);
    return;
}
parser.ShowProvidedArguments(indent);

var startServing = parser.IsFlagProvided("serve");
var port = parser.GetOption<int>("port");
var read = parser.GetOption<string>("read");
```

## Auto-generated helper text

In the examples below, `2` is a left indent of two spaces.

#### `Parser.Help(2);`

*(Required options come first, then optional options, then flags.)*

``` text
  -read  <value>   Folder to read the site from (required)
  -write <value>   Folder to write the result to (required)
  -port  <value>   Port to start the dev server on
  -force           Overwrite any destination content
  -serve           Start the site going in a dev server
```

#### `Parser.ShowErrors(2)`

``` text
  Option missing: write
  Unknown flag: run
```

#### `Parser.ShowProvidedArguments(2);`

``` text
  -port  3000
  -read  in.txt
  -serve
  -force
```

## Supported features

- Display help showing supported flags/options
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

There are a few things wrong here with this input:

- The `-write` option is required but not provided
- The provided `-run` option is not defined
- The `"Site Title"` argument has no option name preceeding it
- The provided `-ignore` flag is not defined

Whilst the `-read` option is missing there is no error logged - it was defined with a default value of `site` and so the requirement is automatically met.

Errors come in two collections (the property `Parser.HasErrors` will be `true` if either has entries):

- `ExpectationErrors` are where specific expectations are not met (eg a missing required option) so the relevant option/flag whose expectations are not being met is known
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

Copyright K Cartlidge 2020-2023.

Licensed under [GNU AGPLv3](./LICENSE) ([see here for more details](https://choosealicense.com/licenses/agpl-3.0/)).
See the [CHANGELOG](./CHANGELOG.md) for current status.
