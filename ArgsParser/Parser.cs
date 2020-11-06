using System;
using System.Collections.Generic;
using System.Linq;

namespace ArgsParser
{
    public class Parser
    {
        /// <summary>The key used in Errors if a flag/option could not be determined.</summary>
        public static readonly string UnknownKey = "n/a";

        /// <summary>FNames of flags discovered via parsing (if parsed, else empty).</summary>
        public List<string> Flags = new List<string>();

        /// <summary>
        /// Options discovered via parsing (if parsed, else empty).
        /// Objects keyed by name. GetOption return typed entries.
        /// </summary>
        public SortedList<string, object> Options = new SortedList<string, object>();

        /// <summary>Were there errors generated during parsing?</summary>
        public bool HasErrors { get => Errors.Any(); }

        /// <summary>
        /// Any errors gathered during parsing.
        /// Keyed by flag/option name, or UnknownKey if that wasn't known.
        /// Contains lists of 1+ error messages per entry.
        /// </summary>
        public SortedList<string, List<string>> Errors = new SortedList<string, List<string>>();

        private readonly string[] rawArgs;
        private readonly Dictionary<string, ArgDetail> knownFlags = new Dictionary<string, ArgDetail>();
        private readonly Dictionary<string, ArgDetail> knownOptions = new Dictionary<string, ArgDetail>();
        private int maxOptionWidth => knownOptions.Max(x => x.Key.Length);
        private int maxFlagWidth => knownFlags.Max(x => x.Key.Length);
        private bool parsed = false;

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
        public Parser HasFlag(string flagName, string info)
        {
            flagName = flagName.ToLowerInvariant().Trim().TrimStart('-').Trim();

            knownFlags.Add(flagName, new ArgDetail(typeof(bool), false, info, null));
            return this;
        }

        /// <summary>
        /// Displays informative help for all flags and options via the Console (stdout).
        /// Groups into sections for clarity, and sorts within those sections.
        /// </summary>
        public Parser Help()
        {
            var req = "(required)";
            var width = Math.Max(maxOptionWidth, maxFlagWidth);
            var optionWidth = width;
            if (knownOptions.Any()) Console.WriteLine();
            foreach (var option in knownOptions.OrderByDescending(x => x.Value.IsRequired).ThenBy(y => y.Key))
            {
                var required = option.Value.IsRequired ? req : "";
                var key = option.Key.PadRight(optionWidth);
                Console.WriteLine($"-{key} <value>   {option.Value.Info} {required}");
            }
            var flagWidth = width + (knownOptions.Any() ? " <value>".Length : 0);
            if (knownFlags.Any()) Console.WriteLine();
            foreach (var flag in knownFlags.OrderByDescending(x => x.Value.IsRequired).ThenBy(y => y.Key))
            {
                var required = flag.Value.IsRequired ? req : "";
                var key = flag.Key.PadRight(flagWidth);
                Console.WriteLine($"-{key}   {flag.Value.Info} {required}");
            }
            Console.WriteLine();
            return this;
        }

        /// <summary>Parse the initial args according to the defined flags and options.</summary>
        public Parser Parse()
        {
            // Can only parse once.
            if (parsed) return this;
            parsed = true;

            // Extract all the flags and options.
            // In this initial splitting process, all option values are treated as strings.
            var currentName = (string)null;
            foreach (var arg in rawArgs)
            {
                // Should never be the case, but ...
                if (string.IsNullOrWhiteSpace(arg)) continue;

                if (arg.StartsWith("-"))
                {
                    // Starts an option or a flag.
                    if (currentName == null)
                    {
                        // Not currently in one, so start a new one.
                        currentName = arg.TrimStart('-').Trim();
                        if (string.IsNullOrWhiteSpace(currentName))
                        {
                            AddError(UnknownKey, $"Argument received with no name");
                            currentName = null;
                        }
                    }
                    else
                    {
                        // Currently in one, so previous one must be a flag.
                        // Add it, and start a new one.
                        Flags.Add(currentName.ToLowerInvariant());
                        currentName = arg.TrimStart('-').Trim();
                        if (string.IsNullOrWhiteSpace(currentName))
                        {
                            AddError(UnknownKey, $"Argument received with no name");
                            currentName = null;
                        }
                    }
                }
                else
                {
                    // Simple argument.
                    if (currentName == null)
                    {
                        // Not currently in an option.
                        AddError(UnknownKey, $"Unexpected value: {arg}");
                    }
                    else
                    {
                        // In an option, so add it with the value and start a new one.
                        Options.Add(currentName.ToLowerInvariant(), arg);
                        currentName = null;
                    }
                }
            }

            // Final trailing option/flag.
            if (currentName != null)
                Flags.Add(currentName.ToLowerInvariant());

            // Convert any non-string options.
            foreach (var option in knownOptions.Where(x => x.Value.ArgType != typeof(string)))
                if (Options.ContainsKey(option.Key))
                {
                    try
                    {
                        Options[option.Key] = Convert.ChangeType(Options[option.Key], option.Value.ArgType);
                    }
                    catch
                    {
                        AddError(option.Key, $"Expected a value of type {option.Value.ArgType}: {option.Key}");
                    }
                }

            // Enforce any options requirements.
            // No required flags, as that would force the value to always be true.
            foreach (var option in knownOptions.Where(x => x.Value.IsRequired))
                if (Options.ContainsKey(option.Key) == false)
                    // Apply a default if provided, else it's an error.
                    if (option.Value.DefaultValue != null)
                        Options.Add(option.Key, option.Value.DefaultValue);
                    else
                        AddError(option.Key, $"Option missing: {option.Key}");

            // Check for unsupported.
            foreach (var flag in Flags)
                if (knownFlags.ContainsKey(flag) == false)
                    AddError(flag, $"Unknown flag: {flag}");
            foreach (var option in Options)
                if (knownOptions.ContainsKey(option.Key) == false)
                    AddError(option.Key, $"Unknown option: {option.Key}");

            return this;
        }

        /// <summary>Is the named flag present?</summary>
        public bool FlagProvided(string flagName)
        {
            flagName = flagName.ToLowerInvariant().Trim();

            if (knownFlags.ContainsKey(flagName) == false)
                throw new ArgumentException($"Unknown flag: {flagName}");

            return (Flags.Contains(flagName));
        }

        /// <summary>Is the named option present?</summary>
        public bool OptionProvided(string optionName)
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
            if (OptionProvided(optionName))
                return (T)Options[optionName];

            // No option provided, but there is a default given.
            if (knownOptions[optionName].DefaultValue != null)
                return (T)knownOptions[optionName].DefaultValue;

            // Fallback on the default value for the return type.
            return default;
        }

        private void AddError(string key, string message)
        {
            key = key.ToLowerInvariant().Trim();

            if (Errors.ContainsKey(key) == false)
                Errors.Add(key, new List<string>());

            if (Errors[key].Contains(message)) return;
            Errors[key].Add(message);
        }
    }
}
