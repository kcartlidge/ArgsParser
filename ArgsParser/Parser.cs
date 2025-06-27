using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArgsParser
{
    public class Parser
    {
        #region Public Properties

        /// <summary>Were there errors generated during parsing?</summary>
        public bool HasErrors { get => ArgumentErrors.Any() || ExpectationErrors.Any(); }

        /// <summary>Any argument errors gathered during parsing, indexed by order of discovery.</summary>
        public SortedList<int, string> ArgumentErrors = new SortedList<int, string>();

        /// <summary>Any errors where expectations are not met by the arguments.</summary>
        public Dictionary<string, string> ExpectationErrors = new Dictionary<string, string>();

        #endregion

        #region Private Fields

        /// <summary>Have the args been parsed yet?</summary>
        private bool parsed = false;

        /// <summary>Positional offset within args.</summary>
        private int nextSequence = 0;

        /// <summary>The unparsed arguments as originally provided.</summary>
        private readonly string[] rawArgs;

        /// <summary>Options provided by (user) input args.</summary>
        private Dictionary<string, object> options = new Dictionary<string, object>();

        /// <summary>Flags provided by (user) input args.</summary>
        private List<string> flags = new List<string>();

        /// <summary>Show explanatory text below options/flags in Help()?</summary>
        private bool showHelpLegend = true;

        /// <summary>Registered custom validators.</summary>
        private readonly Dictionary<string, Func<string, object, List<string>>> validators = new Dictionary<string, Func<string, object, List<string>>>();

        /// <summary>The definitive list of known options and flags.</summary>
        private readonly List<ArgDetail> known = new List<ArgDetail>();

        /// <summary>Options from the 'known' list.</summary>
        private List<ArgDetail> knownOptions => known.Where(x => x.IsOption).ToList();

        /// <summary>Flags from the 'known' list.</summary>
        private List<ArgDetail> knownFlags => known.Where(x => x.IsFlag).ToList();

        /// <summary>Number of characters in the longest option name.</summary>
        private int maxOptionWidth =>
            knownOptions.Any() ? knownOptions.Max(x => x.Name.Length) : 1;

        /// <summary>Number of characters in the longest flag name.</summary>
        private int maxFlagWidth =>
            knownFlags.Any() ? knownFlags.Max(x => x.Name.Length) : 1;

        /// <summary>
        /// Title displayed before additional help or notes in generated help documentation.
        /// </summary>
        private string extraHelpTitle = "Notes:";

        /// <summary>
        /// Additional text to append to the generated help output. This is typically used
        /// for providing extra clarification, examples, or custom notes beyond the standard
        /// options and usage descriptions.
        /// </summary>
        private string[] extraHelp = new string[0];

        #endregion

        /// <summary>
        /// Create a new Parser based on the given arguments collection.
        /// Usually, but not necessarily, this is the args provided at app start.
        /// </summary>
        public Parser(string[] args)
        {
            rawArgs = args;
        }

        #region Define Options and Flags

        /// <summary>Registers a supported non-compulsory option.</summary>
        /// <param name="optionName">The option name. Any leading dash prefix is ignored.</param>
        /// <param name="info">Descriptive text for the generated Help().</param>
        /// <param name="defaultValue">Optional default value.</param>
        public Parser SupportsOption<T>(string optionName, string info, object defaultValue = null)
        {
            known.Add(new ArgDetail(
                Normalise(optionName),
                nextSequence++,
                typeof(T),
                false,
                true,
                info,
                defaultValue));
            return this;
        }

        /// <summary>Registers a compulsory option.</summary>
        /// <param name="optionName">The option name. Any leading dash prefix is ignored.</param>
        /// <param name="info">Descriptive text for the generated Help().</param>
        /// <param name="defaultValue">Optional default value.</param>
        public Parser RequiresOption<T>(string optionName, string info, object defaultValue = null)
        {
            known.Add(new ArgDetail(
                Normalise(optionName),
                nextSequence++,
                typeof(T),
                true,
                true,
                info,
                defaultValue));
            return this;
        }

        /// <summary>Registers a supported non-compulsory flag.</summary>
        /// <param name="optionName">The flag name. Any leading dash prefix is ignored.</param>
        /// <param name="info">Descriptive text for the generated Help().</param>
        public Parser SupportsFlag(string flagName, string info)
        {
            known.Add(new ArgDetail(
                Normalise(flagName),
                nextSequence++,
                typeof(bool),
                false,
                false,
                info,
                null));
            return this;
        }

        /// <summary>
        /// Adds a custom validator that will be called if the specified
        /// option has a value set. All built-in argument parsing and
        /// validation will occur first, then custom validators will be
        /// called in the order registered.
        /// The validator receives the option name (allowing the same
        /// validator to be used for multiple arguments) and the value
        /// provided. It must return a list of strings with any error
        /// messages that need to be added to the `ExpectationErrors`.
        /// Only one validator can be added per option.
        /// </summary>
        public Parser AddCustomValidator(
            string option,
            Func<string, object, List<string>> validate)
        {
            var key = Normalise(option ?? "");
            if (KnowsOption(key) == false)
                throw new ArgumentException($"Cannot add a validator for unknown option '{key}'.");
            if (validators.ContainsKey(key))
                throw new ArgumentException($"A validator for {key} has already been added.");

            validators.Add(key, validate);
            return this;
        }

        #endregion

        /// <summary>Parse the initial args according to the defined flags and options.</summary>
        public Parser Parse()
        {
            // Can only parse once.
            if (parsed) return this;
            parsed = true;

            // Get the sequenced arguments.
            var args = new SortedList<int, ArgValue>();
            var inDash = false;
            for (var i = 0; i < rawArgs.Length; i++)
            {
                // Assume nothing is known about the current argument.
                var rawArg = rawArgs[i];
                var arg = new ArgValue(i + 1, rawArg, ArgType.Unknown);

                if (arg.HasDash)
                {
                    // This and previous both dashes? Previous must be a flag.
                    if (inDash) args[i - 1].ArgType = ArgType.Flag;

                    // Assume is a flag (no lookahead; patched to option below).
                    arg.ArgType = ArgType.Flag;
                    inDash = true;
                }
                else
                {
                    if (inDash)
                    {
                        // Not a dash, previous one was, must an option's value.
                        args[i - 1].ArgType = ArgType.Option;
                        args[i - 1].Value = arg.Original;
                        arg.ArgType = ArgType.Skip;
                    }
                    inDash = false;
                }

                // Build up the collection of arguments.
                if (arg.ArgType != ArgType.Skip) args.Add(i, arg);
            }

            // Check for argument errors (things provided but wrong).
            // Populate flags and options in passing.
            foreach (var arg in args)
            {
                switch (arg.Value.ArgType)
                {
                    case ArgType.Unknown:
                        AddArgumentError(arg.Key, $"Unexpected value: {arg.Value.Original}");
                        break;
                    case ArgType.Flag:
                        if (arg.Value.Name.Length == 0)
                            AddArgumentError(arg.Key, $"Flag received with no name");
                        else if (KnowsFlag(arg.Value.Name) == false)
                            AddArgumentError(arg.Key, $"-{arg.Value.Name} is an unknown flag");
                        else
                            AddFlag(arg.Value.Name);
                        break;
                    case ArgType.Option:
                        if (arg.Value.Name.Length == 0)
                            AddArgumentError(arg.Key, $"Option received with no name");
                        else if (KnowsOption(arg.Value.Name) == false)
                            AddArgumentError(arg.Key, $"-{arg.Value.Name} is an unknown option");
                        else
                        {
                            var o = GetOption(arg.Value.Name);
                            AddOption(arg.Value.Name);
                            try
                            {
                                options[arg.Value.Name] = Convert.ChangeType(arg.Value.Value, o.ArgType);
                            }
                            catch
                            {
                                AddArgumentError(arg.Value.Sequence, $"-{arg.Value.Name} should be a value of type {o.ArgType.Name}");
                            }
                        }
                        break;
                    case ArgType.Skip:
                    default:
                        break;
                }
            }

            // Populate any defaults that were not provided.
            foreach (var opt in known.Where(x => x.IsOption && x.HasDefault))
            {
                if (options.ContainsKey(opt.Name) == false)
                {
                    AddOption(opt.Name);
                    options[opt.Name] = Convert.ChangeType(opt.DefaultValue, opt.ArgType);
                }
            }

            // Check for expectation errors (things expected but missing).
            // In brief, enforce any options requirements.
            // No required flags, as that would force the value to always be true.
            foreach (var option in knownOptions.Where(x => x.IsRequired).OrderBy(x => x.Sequence))
                if (options.ContainsKey(option.Name) == false)
                    // Apply a default if provided, else it's an error.
                    if (option.DefaultValue != null)
                        options.Add(option.Name, option.DefaultValue);
                    else
                        AddExpectationError(option.Name, $"-{option.Name} is required");

            // Apply any custom validators to the options with values.
            foreach (var option in options.Where(x => validators.ContainsKey(x.Key)))
            {
                // Apply the validator and merge in any errors.
                var v = option.Value;
                var errs = validators[option.Key](option.Key, v);
                foreach (var err in errs) AddExpectationError(option.Key, err);
            }

            return this;
        }

        #region Visual Outputs

        /// <summary>
        /// Displays informative help for all flags and options via the Console (stdout).
        /// Groups into sections for clarity, and sorts within those sections.
        /// </summary>
        public Parser Help(int indent = 0, string heading = "")
        {
            // Sanity checks.
            if (indent < 0) throw new Exception($"A negative indent ({indent}) is not allowed.");
            if (knownOptions.Count + knownFlags.Count == 0) return this;

            // Layout calculations.
            var pad = "".PadLeft(indent);
            var req = "*";
            var width = Math.Max(maxOptionWidth, maxFlagWidth);
            var typeWidth = knownOptions.Any() ? knownOptions.Max(x => x.ArgTypeName.Length) : 0;
            var flagWidth = width + (knownOptions.Any() ? typeWidth + 2 : 0);

            if (heading.Any())
            {
                Console.WriteLine();
                Console.WriteLine(heading);
            }

            // Show options and flags by sequence added.
            foreach (var item in known.OrderBy(x => x.Sequence).ThenBy(x => x.Name))
            {
                if (item.IsOption)
                {
                    var typename = item.ArgTypeName.PadRight(typeWidth);
                    var required = item.IsRequired ? req : " ";
                    var key = item.Name.PadRight(width);
                    var q = item.IsQuoted ? "\"" : "";
                    var def = item.HasDefault
                        ? $"[{q}{item.DefaultValue}{q}]"
                        : "";

                    Console.WriteLine($"{pad}-{key}  {typename}  {required} {item.Info}  {def}");
                }
                else if (item.IsFlag)
                {
                    var required = item.IsRequired ? req : " ";
                    var key = item.Name.PadRight(flagWidth);
                    Console.WriteLine($"{pad}-{key}  {required} {item.Info}");
                }
            }

            // Add legend if any items are required or have a default.
            if (showHelpLegend)
            {
                var hasRequired = known.Any(x => x.IsRequired);
                var hasDefaults = known.Any(x => x.HasDefault);
                if (hasRequired || hasDefaults)
                {
                    Console.WriteLine();
                    if (hasRequired) Console.WriteLine($"{pad}(*) denotes a required option");
                    if (hasDefaults) Console.WriteLine($"{pad}[ ] is a default value");
                }
            }

            // Show any extra help details.
            if (extraHelp.Any())
            {
                Console.WriteLine();
                Console.WriteLine(extraHelpTitle);
                foreach (var line in extraHelp)
                {
                    Console.WriteLine($"{pad}{line.Trim()}");
                }
            }

            return this;
        }

        /// <summary>Show explanatory text below options/flags in Help()?</summary>
        public Parser ShowHelpLegend(bool show = true)
        {
            showHelpLegend = show;
            return this;
        }

        /// <summary>
        /// Adds additional lines of help information to the parser's output.
        /// Shows just below the options and flags.
        /// </summary>
        /// <param name="title">The heading to show above the text (eg `Notes:`).</param>
        /// <param name="lines">Lines of text containing the extra stuff to display.</param>
        /// <returns>The current instance of the Parser to allow method chaining.</returns>
        public Parser AddExtraHelp(string title, params string[] lines)
        {
            extraHelpTitle = title.Trim();
            extraHelp = lines;
            return this;
        }

        /// <summary>Displays a list of key/value argument error(s).</summary>
        public Parser ShowErrors(int indent = 0, string heading = "")
        {
            if (HasErrors == false) return this;
            if (indent < 0) throw new Exception($"A negative indent ({indent}) is not allowed.");
            var pad = "".PadLeft(indent);

            if (heading.Any())
            {
                Console.WriteLine();
                Console.WriteLine(heading);
            }

            foreach (var item in known.OrderBy(x => x.Sequence))
            {
                foreach (var error in ExpectationErrors.Where(x => x.Key == item.Name))
                    Console.WriteLine($"{pad}{error.Value}");
            }
            foreach (var error in ArgumentErrors)
                Console.WriteLine($"{pad}{error.Value}");
            return this;
        }

        /// <summary>
        /// Display a list of MATCHED key/value arguments provided, based on
        /// those returned by GetProvided().
        /// </summary>
        /// <remarks>Does NOT include unknown options/flags.</remarks>
        public Parser ShowProvided(int indent = 0, string heading = "")
        {
            if (indent < 0) throw new Exception($"A negative indent ({indent}) is not allowed.");

            var pad = "".PadLeft(indent);
            var width = Math.Max(maxOptionWidth, maxFlagWidth);

            if (heading.Any())
            {
                Console.WriteLine();
                Console.WriteLine(heading);
            }

            var provided = GetProvided();
            foreach (var item in provided)
            {
                var (key, value) = (item.Key.PadRight(width), item.Value);
                if (value == null)
                    Console.WriteLine($"{pad}-{key}");
                else
                {
                    var m = known.FirstOrDefault(x=>x.Name == item.Key);
                    var q = m.IsQuoted ? "\"" : " ";
                    Console.WriteLine($"{pad}-{key} {q}{value}{q}".TrimEnd());
                }
            }
            return this;
        }

        #endregion

        #region Accessing Options and Flags

        /// <summary>
        /// Fetch a list of MATCHED key/value arguments provided, including
        /// flags - which are in the result but with a null value (matched
        /// options always have values).
        /// </summary>
        /// <remarks>Does NOT include unknown options/flags.</remarks>
        public Dictionary<string, object> GetProvided()
        {
            var result = new Dictionary<string, object>();
            foreach (var item in known.OrderBy(x => x.Sequence))
            {
                foreach (var option in this.options)
                    if (option.Key == item.Name) result.Add(option.Key, option.Value);
                foreach (var flag in this.flags)
                    if (flag == item.Name) result.Add(flag, null);
            }
            return result;
        }

        /// <summary>
        /// Returns the MATCHED key/value arguments provided, based on
        /// those returned by GetProvided(), as a string in the form of
        /// command line arguments.
        /// </summary>
        /// <remarks>Does NOT include unknown options/flags.</remarks>
        public string GetProvidedAsCommandArgs()
        {
            var result = new StringBuilder();
            foreach (var item in GetProvided())
            {
                if (result.Length > 0) result.Append(' ');
                if (item.Value == null) result.Append($"-{item.Key}");
                else result.Append($"-{item.Key} \"{item.Value}\"");
            }
            return result.ToString();
        }

        /// <summary>Is the named option present?</summary>
        public bool IsOptionProvided(string optionName)
        {
            optionName = Normalise(optionName);
            if (KnowsOption(optionName) == false)
                throw new ArgumentException($"Unknown option: {optionName}");
            return (options.ContainsKey(optionName));
        }

        /// <summary>Is the named flag present?</summary>
        public bool IsFlagProvided(string flagName)
        {
            flagName = Normalise(flagName);
            if (KnowsFlag(flagName) == false)
                throw new ArgumentException($"Unknown flag: {flagName}");
            return (flags.Contains(flagName));
        }

        /// <summary>
        /// Gets the named option if present.
        /// If the type differs from expected, an InvalidCastException is thrown.
        /// Where not provided, the original DefaultValue is used.
        /// The fallback is the default value for the type itself.
        /// </summary>
        public T GetOption<T>(string optionName)
        {
            optionName = Normalise(optionName);

            // Unknown option.
            if (KnowsOption(optionName) == false)
                throw new ArgumentException($"Unknown option: {optionName}");

            // Option type and generic type on this call differ.
            if (typeof(T) != GetOption(optionName).ArgType)
                throw new InvalidCastException($"Incorrect type getting option {optionName}");

            // Option value was provided (default is already applied if needed).
            if (IsOptionProvided(optionName)) return (T)options[optionName];

            // No option provided, but there is a default given.
            if (GetOption(optionName).DefaultValue != null) return (T)GetOption(optionName).DefaultValue;

            // Fallback on the default value for the return type.
            return default;
        }

        #endregion

        #region Internals

        /// <summary>Tidies up a name to ensure consistent behaviour.</summary>
        private string Normalise(string original)
        {
            return original.ToLowerInvariant().Trim().TrimStart('-').Trim();
        }

        /// <summary>Do we already know about this option?</summary>
        private bool KnowsOption(string name)
        {
            name = Normalise(name);
            return known.Any(x => x.Name == name && x.IsOption);
        }

        /// <summary>Do we already know about this flag?</summary>
        private bool KnowsFlag(string name)
        {
            name = Normalise(name);
            return known.Any(x => x.Name == name && x.IsFlag);
        }

        /// <summary>
        /// Fetch a named option (assumes KnowsOption checked already).
        /// </summary>
        private ArgDetail GetOption(string name)
        {
            name = Normalise(name);
            return known.First(x => x.Name == name && x.IsOption);
        }

        /// <summary>
        /// Fetch a named flag (assumes KnowsFlag checked already).
        /// </summary>
        private ArgDetail GetFlag(string name)
        {
            name = Normalise(name);
            return known.First(x => x.Name == name && x.IsFlag);
        }

        /// <summary>Add a non-specific error related to a passed argument.</summary>
        private void AddArgumentError(int sequence, string message)
        {
            if (ArgumentErrors.ContainsKey(sequence))
                ArgumentErrors[sequence] = ArgumentErrors[sequence] + $"\n{message}";
            else
                ArgumentErrors.Add(sequence, message);
        }

        /// <summary>Add an error for an argument for a specific option.</summary>
        private void AddExpectationError(string key, string message)
        {
            if (ExpectationErrors.ContainsKey(key))
                ExpectationErrors[key] = ExpectationErrors[key] + $"\n{message}";
            else
                ExpectationErrors.Add(key, message);
        }

        /// <summary>Add a flag as provided in the arguments.</summary>
        private void AddFlag(string flagName)
        {
            flagName = Normalise(flagName);
            if (flags.Contains(flagName)) return;
            flags.Add(flagName);
        }

        /// <summary>Add an option as provided in the arguments.</summary>
        private void AddOption(string optionName)
        {
            optionName = Normalise(optionName);
            if (options.ContainsKey(optionName)) return;
            options.Add(optionName, null);
        }

        #endregion
    }
}
