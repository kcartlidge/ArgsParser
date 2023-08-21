using System;
using System.Collections.Generic;
using System.Linq;

namespace ArgsParser
{
    public class Parser
    {
        /// <summary>Were there errors generated during parsing?</summary>
        public bool HasErrors { get => ArgumentErrors.Any() || ExpectationErrors.Any(); }

        /// <summary>Any argument errors gathered during parsing, indexed by order of discovery.</summary>
        public SortedList<int, string> ArgumentErrors = new SortedList<int, string>();

        /// <summary>Any errors where expectations are not met by the arguments.</summary>
        public SortedList<string, string> ExpectationErrors = new SortedList<string, string>();

        private bool parsed = false;
        private readonly string[] rawArgs;
        private List<string> Flags = new List<string>();
        private SortedList<string, object> Options = new SortedList<string, object>();
        private readonly Dictionary<string, ArgDetail> knownFlags = new Dictionary<string, ArgDetail>();
        private readonly Dictionary<string, ArgDetail> knownOptions = new Dictionary<string, ArgDetail>();
        private int maxOptionWidth =>
            knownOptions.Any() ? knownOptions.Max(x => x.Key.Length) : 1;
        private int maxFlagWidth =>
            knownFlags.Any() ? knownFlags.Max(x => x.Key.Length) : 1;

        /// <summary>
        /// Create a new Parser based on the given arguments collection.
        /// Usually, but not necessarily, this is the args provided when an app starts up.
        /// </summary>
        public Parser(string[] args)
        {
            rawArgs = args;
        }

        /// <summary>Registers a supported non-compulsory option.</summary>
        /// <param name="optionName">The option name. Any leading dash prefix is ignored.</param>
        /// <param name="info">Descriptive text for the generated Help().</param>
        /// <param name="defaultValue">Optional default value.</param>
        public Parser SupportsOption<T>(string optionName, string info, object defaultValue = null)
        {
            optionName = optionName.ToLowerInvariant().Trim().TrimStart('-').Trim();

            knownOptions.Add(optionName, new ArgDetail(typeof(T), false, info, defaultValue));
            return this;
        }

        /// <summary>Registers a compulsory option.</summary>
        /// <param name="optionName">The option name. Any leading dash prefix is ignored.</param>
        /// <param name="info">Descriptive text for the generated Help().</param>
        /// <param name="defaultValue">Optional default value.</param>
        public Parser RequiresOption<T>(string optionName, string info, object defaultValue = null)
        {
            optionName = optionName.ToLowerInvariant().Trim().TrimStart('-').Trim();

            knownOptions.Add(optionName, new ArgDetail(typeof(T), true, info, defaultValue));
            return this;
        }

        /// <summary>Registers a supported non-compulsory flag.</summary>
        /// <param name="optionName">The flag name. Any leading dash prefix is ignored.</param>
        /// <param name="info">Descriptive text for the generated Help().</param>
        public Parser SupportsFlag(string flagName, string info)
        {
            flagName = flagName.ToLowerInvariant().Trim().TrimStart('-').Trim();

            knownFlags.Add(flagName, new ArgDetail(typeof(bool), false, info, null));
            return this;
        }

        /// <summary>
        /// Displays informative help for all flags and options via the Console (stdout).
        /// Groups into sections for clarity, and sorts within those sections.
        /// </summary>
        public Parser Help(int indent = 0)
        {
            if (indent < 0) throw new Exception($"A negative indent ({indent}) is not allowed.");
            if (knownOptions.Count + knownFlags.Count == 0) return this;

            var pad = "".PadLeft(indent);
            var req = "(required)";
            var width = Math.Max(maxOptionWidth, maxFlagWidth);
            foreach (var option in knownOptions.OrderByDescending(x => x.Value.IsRequired).ThenBy(y => y.Key))
            {
                var required = option.Value.IsRequired ? req : "";
                var key = option.Key.PadRight(width);
                Console.WriteLine($"{pad}-{key} <value>   {option.Value.Info} {required}");
            }
            var flagWidth = width + (knownOptions.Any() ? " <value>".Length : 0);
            foreach (var flag in knownFlags.OrderByDescending(x => x.Value.IsRequired).ThenBy(y => y.Key))
            {
                var required = flag.Value.IsRequired ? req : "";
                var key = flag.Key.PadRight(flagWidth);
                Console.WriteLine($"{pad}-{key}   {flag.Value.Info} {required}");
            }
            return this;
        }

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
                    // This and previous both dashes, so previous must be a flag.
                    if (inDash)
                        args[i - 1].ArgType = ArgType.Flag;

                    // Assume this will just be a flag (options are patched up below).
                    arg.ArgType = ArgType.Flag;
                    inDash = true;
                }
                else
                {
                    if (inDash)
                    {
                        // Not a dash, but previous one was, so must be the value for an option.
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
                        else if (knownFlags.ContainsKey(arg.Value.Name) == false)
                            AddArgumentError(arg.Key, $"Unknown flag: {arg.Value.Name}");
                        else
                            AddFlag(arg.Value.Name);
                        break;
                    case ArgType.Option:
                        if (arg.Value.Name.Length == 0)
                            AddArgumentError(arg.Key, $"Option received with no name");
                        else if (knownOptions.ContainsKey(arg.Value.Name) == false)
                            AddArgumentError(arg.Key, $"Unknown option: {arg.Value.Name}");
                        else
                        {
                            var o = knownOptions[arg.Value.Name];
                            AddOption(arg.Value.Name);
                            try
                            {
                                Options[arg.Value.Name] = Convert.ChangeType(arg.Value.Value, o.ArgType);
                            }
                            catch
                            {
                                AddArgumentError(arg.Value.Sequence, $"Expected a value of type {o.ArgType}: {arg.Value.Name}");
                            }
                        }
                        break;
                    case ArgType.Skip:
                    default:
                        break;
                }
            }

            // Check for expectation errors (things expected but missing).
            // In brief, enforce any options requirements.
            // No required flags, as that would force the value to always be true.
            foreach (var option in knownOptions.Where(x => x.Value.IsRequired))
                if (Options.ContainsKey(option.Key) == false)
                    // Apply a default if provided, else it's an error.
                    if (option.Value.DefaultValue != null)
                        Options.Add(option.Key, option.Value.DefaultValue);
                    else
                        AddExpectationError(option.Key, $"Option missing: {option.Key}");

            return this;
        }

        /// <summary>Displays a list of key/value argument error(s).</summary>
        public void ShowErrors(int indent = 0)
        {
            if (HasErrors == false) return;
            if (indent < 0) throw new Exception($"A negative indent ({indent}) is not allowed.");

            var pad = "".PadLeft(indent);
            foreach (var error in ExpectationErrors)
                Console.WriteLine($"{pad}{error.Value}");
            foreach (var error in ArgumentErrors)
                Console.WriteLine($"{pad}{error.Value}");
            return;
        }

        /// <summary>Displays a list of key/value arguments provided.</summary>
        /// <remarks>Does NOT include unknown ones.</remarks>
        public void ShowProvidedArguments(int indent = 0)
        {
            if (HasErrors == false) return;
            if (indent < 0) throw new Exception($"A negative indent ({indent}) is not allowed.");

            var pad = "".PadLeft(indent);
            var width = Math.Max(maxOptionWidth, maxFlagWidth);

            foreach (var item in this.Options)
            {
                var key = item.Key.PadRight(width);
                Console.WriteLine($"{pad}-{key} {item.Value}");
            }
            foreach (var item in this.Flags)
                Console.WriteLine($"{pad}-{item}");
            return;
        }

        /// <summary>Is the named flag present?</summary>
        public bool IsFlagProvided(string flagName)
        {
            flagName = flagName.ToLowerInvariant().Trim();

            if (knownFlags.ContainsKey(flagName) == false)
                throw new ArgumentException($"Unknown flag: {flagName}");

            return (Flags.Contains(flagName));
        }

        /// <summary>Is the named option present?</summary>
        public bool IsOptionProvided(string optionName)
        {
            optionName = optionName.ToLowerInvariant().Trim();

            if (knownOptions.ContainsKey(optionName) == false)
                throw new ArgumentException($"Unknown option: {optionName}");

            return (Options.ContainsKey(optionName));
        }

        /// <summary>
        /// Gets the named option if present.
        /// If the type differs from expected, an InvalidCastException is thrown.
        /// Where not provided, the original DefaultValue is used.
        /// The fallback is the default value for the type itself.
        /// </summary>
        public T GetOption<T>(string optionName)
        {
            optionName = optionName.ToLowerInvariant().Trim();

            // Unknown option.
            if (knownOptions.ContainsKey(optionName) == false)
                throw new ArgumentException($"Unknown option: {optionName}");

            // Option type and generic type on this call differ.
            if (typeof(T) != knownOptions[optionName].ArgType)
                throw new InvalidCastException($"Incorrect type getting option {optionName}");

            // Option value was provided (default is already applied if needed).
            if (IsOptionProvided(optionName))
                return (T)Options[optionName];

            // No option provided, but there is a default given.
            if (knownOptions[optionName].DefaultValue != null)
                return (T)knownOptions[optionName].DefaultValue;

            // Fallback on the default value for the return type.
            return default;
        }

        /* SUPPORT */

        private void AddArgumentError(int sequence, string message)
        {
            if (ArgumentErrors.ContainsKey(sequence))
                ArgumentErrors[sequence] = ArgumentErrors[sequence] + $"\n{message}";
            else
                ArgumentErrors.Add(sequence, message);
        }

        private void AddExpectationError(string key, string message)
        {
            if (ExpectationErrors.ContainsKey(key))
                ExpectationErrors[key] = ExpectationErrors[key] + $"\n{message}";
            else
                ExpectationErrors.Add(key, message);
        }

        private void AddFlag(string flagName)
        {
            if (Flags.Contains(flagName)) return;
            Flags.Add(flagName);
        }

        private void AddOption(string optionName)
        {
            if (Options.ContainsKey(optionName)) return;
            Options.Add(optionName, null);
        }
    }
}
