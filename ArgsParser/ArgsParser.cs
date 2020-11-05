using System;
using System.Collections.Generic;
using System.Linq;

namespace ArgsParser
{
    public class ArgsParser
    {
        public List<string> ParsedFlags = new List<string>();
        public SortedList<string, object> ParsedOptions = new SortedList<string, object>();
        public bool HasErrors { get => Errors.Any(); }
        public SortedList<string, List<string>> Errors = new SortedList<string, List<string>>();

        private readonly string UnknownKey = "N/A";
        private readonly string[] RawArgs;
        private readonly Dictionary<string, ArgDetail> Flags = new Dictionary<string, ArgDetail>();
        private readonly Dictionary<string, ArgDetail> Options = new Dictionary<string, ArgDetail>();
        private int MaxOptionWidth => Options.Max(x => x.Key.Length);
        private int MaxFlagWidth => Flags.Max(x => x.Key.Length);

        public ArgsParser(string[] args)
        {
            RawArgs = args;
        }

        public ArgsParser SupportsOption<T>(string optionName, string info, object defaultValue = null)
        {
            Options.Add(optionName.ToLower(), new ArgDetail(typeof(T), false, info, defaultValue));
            return this;
        }

        public ArgsParser RequiresOption<T>(string optionName, string info, object defaultValue = null)
        {
            Options.Add(optionName.ToLower(), new ArgDetail(typeof(T), true, info, defaultValue));
            return this;
        }

        public ArgsParser HasFlag(string flagName, string info)
        {
            Flags.Add(flagName.ToLower(), new ArgDetail(typeof(bool), false, info, null));
            return this;
        }

        public ArgsParser Help()
        {
            var req = "(required)";
            var width = Math.Max(MaxOptionWidth, MaxFlagWidth);
            var optionWidth = width;
            if (Options.Any()) Console.WriteLine();
            foreach (var option in Options.OrderByDescending(x => x.Value.IsRequired).ThenBy(y => y.Key))
            {
                var required = option.Value.IsRequired ? req : "";
                var key = option.Key.PadRight(optionWidth);
                Console.WriteLine($"-{key} <value>   {option.Value.Info} {required}");
            }
            var flagWidth = width + (Options.Any() ? " <value>".Length : 0);
            if (Flags.Any()) Console.WriteLine();
            foreach (var flag in Flags.OrderByDescending(x => x.Value.IsRequired).ThenBy(y => y.Key))
            {
                var required = flag.Value.IsRequired ? req : "";
                var key = flag.Key.PadRight(flagWidth);
                Console.WriteLine($"-{key}   {flag.Value.Info} {required}");
            }
            Console.WriteLine();
            return this;
        }

        public ArgsParser Parse()
        {
            // Extract all the flags and options.
            // In this initial splitting process, all option values are treated as strings.
            var currentName = (string)null;
            foreach (var arg in RawArgs)
            {
                // Should never be the case, but ...
                if (string.IsNullOrWhiteSpace(arg)) continue;

                if (arg.StartsWith("-"))
                {
                    // Starts an option or a flag.
                    if (currentName == null)
                    {
                        // Not currently in one, so start a new one.
                        currentName = arg.TrimStart('-');
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
                        ParsedFlags.Add(currentName.ToLower());
                        currentName = arg.TrimStart('-');
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
                        ParsedOptions.Add(currentName.ToLower(), arg);
                        currentName = null;
                    }
                }
            }

            // Final trailing option/flag.
            if (currentName != null)
                ParsedFlags.Add(currentName.ToLower());

            // Convert any non-string options.
            foreach (var option in Options.Where(x => x.Value.ArgType != typeof(string)))
                if (ParsedOptions.ContainsKey(option.Key))
                {
                    try
                    {
                        ParsedOptions[option.Key] = Convert.ChangeType(ParsedOptions[option.Key], option.Value.ArgType);
                    }
                    catch
                    {
                        AddError(option.Key, $"Expected a value of type {option.Value.ArgType}: {option.Key}");
                    }
                }

            // Enforce any options requirements.
            // No required flags, as that would force the value to always be true.
            foreach (var option in Options.Where(x => x.Value.IsRequired))
                if (ParsedOptions.ContainsKey(option.Key) == false)
                    // Apply a default if provided, else it's an error.
                    if (option.Value.DefaultValue != null)
                        ParsedOptions.Add(option.Key, option.Value.DefaultValue);
                    else
                        AddError(option.Key, $"Option missing: {option.Key}");

            // Check for unsupported.
            foreach (var flag in ParsedFlags)
                if (Flags.ContainsKey(flag) == false)
                    AddError(flag, $"Unknown flag: {flag}");
            foreach (var option in ParsedOptions)
                if (Options.ContainsKey(option.Key) == false)
                    AddError(option.Key, $"Unknown option: {option.Key}");

            return this;
        }

        public bool FlagProvided(string flagName)
        {
            if (Flags.ContainsKey(flagName) == false)
                throw new ArgumentException($"Unknown flag: {flagName}");

            return (ParsedFlags.Contains(flagName));
        }

        public bool OptionProvided(string optionName)
        {
            if (Options.ContainsKey(optionName) == false)
                throw new ArgumentException($"Unknown option: {optionName}");

            return (ParsedOptions.ContainsKey(optionName));
        }

        public T GetOption<T>(string optionName)
        {
            // Unknown option.
            if (Options.ContainsKey(optionName) == false)
                throw new ArgumentException($"Unknown option: {optionName}");

            // Option type and generic type on this call differ.
            if (typeof(T) != Options[optionName].ArgType)
                throw new InvalidCastException($"Incorrect type getting option {optionName}");

            // Option value was provided (default is already applied if needed).
            if (OptionProvided(optionName))
                return (T)ParsedOptions[optionName];

            // No option provided, but there is a default given.
            if (Options[optionName].DefaultValue != null)
                return (T)Options[optionName].DefaultValue;

            // Fallback on the default value for the return type.
            return default;
        }

        private void AddError(string key, string message)
        {
            key = key.Trim();
            if (Errors.ContainsKey(key) == false)
                Errors.Add(key, new List<string>());

            if (Errors[key].Contains(message)) return;
            Errors[key].Add(message);
        }
    }
}
