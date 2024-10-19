using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ArgsParser.Tests
{
    class ParserTests
    {
        [Test]
        public void README_Example()
        {
            var args = new string[] { "-run", "data", "Site Title", "--serve", "-ignore", "-port", "3000" };
            var parser = new Parser(args)
                .SupportsOption<int>("port", "Port to start the dev server on", 1337)
                .RequiresOption<string>("read", "Folder to read the site from", "site")
                .RequiresOption<string>("write", "Folder to write the result to")
                .SupportsFlag("serve", "Start the site going in a dev server")
                .SupportsFlag("force", "Overwrite any destination content");

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
        }

        [Test]
        public void NothingAllowed_NothingProvided_HasNoErrors()
        {
            var result = new Parser(new string[] { })
                .Parse();

            Assert.IsFalse(result.HasErrors);
        }

        /* OPTIONS */

        [Test]
        public void OptionAllowed_NothingProvided_HasNoErrors()
        {
            var parser = new Parser(new string[] { })
                .SupportsOption<string>("opt", "An option");

            var result = parser.Parse();

            Assert.IsFalse(result.HasErrors);
        }

        [Test]
        public void OptionAllowed_OptionProvided_HasNoErrors()
        {
            var parser = new Parser(new string[] { "-opt", "1" })
                .SupportsOption<string>("opt", "An option");

            var result = parser.Parse();

            Assert.IsFalse(result.HasErrors);
        }

        [Test]
        public void OptionRequired_NothingProvided_HasErrors()
        {
            var parser = new Parser(new string[] { })
                .RequiresOption<string>("opt", "An option");

            var result = parser.Parse();

            Assert.Contains("Option missing: opt", result.ExpectationErrors.Values.ToList());
            Assert.IsEmpty(result.ArgumentErrors);
        }

        [Test]
        public void OptionRequired_NothingProvided_WithDefaultValue_HasNoErrors()
        {
            var parser = new Parser(new string[] { })
                .RequiresOption<string>("opt", "An option", "default-value");

            var result = parser.Parse();

            Assert.IsFalse(result.HasErrors);
        }

        [Test]
        public void OptionRequired_OptionProvided_HasNoErrors()
        {
            var parser = new Parser(new string[] { "-opt", "1" })
                .RequiresOption<string>("opt", "An option");

            var result = parser.Parse();

            Assert.IsFalse(result.HasErrors);
        }

        [Test]
        public void OptionRequired_OptionProvided_WithDefaultValue_OverridesDefault()
        {
            var parser = new Parser(new string[] { "-opt", "actual-value" })
                .RequiresOption<string>("opt", "An option", "default-value");

            var result = parser.Parse();

            Assert.IsFalse(result.HasErrors);
            Assert.AreEqual("actual-value", parser.GetOption<string>("opt"));
        }

        [Test]
        public void UnknownOptionProvided_HasErrors()
        {
            var result = new Parser(new string[] { "-a", "b" })
                .Parse();

            Assert.IsEmpty(result.ExpectationErrors);
            Assert.Contains("Unknown option: a", result.ArgumentErrors.Values.ToList());
        }

        [Test]
        public void Options_RetainCaseOfValues()
        {
            var parser = new Parser(new string[] { "-opt", "MyValue" })
                .SupportsOption<string>("opt", "An option");

            var result = parser.Parse();

            Assert.AreEqual("MyValue", result.GetOption<string>("opt"));
        }

        /* FLAGS */

        [Test]
        public void HasFlag_NothingProvided_HasNoErrors()
        {
            var parser = new Parser(new string[] { })
                .SupportsFlag("serve", "A flag");

            var result = parser.Parse();

            Assert.IsFalse(result.HasErrors);
        }

        [Test]
        public void HasFlag_FlagProvided_HasNoErrors()
        {
            var parser = new Parser(new string[] { "-serve" })
                .SupportsFlag("serve", "A flag");

            var result = parser.Parse();

            Assert.IsFalse(result.HasErrors);
        }

        [Test]
        public void UnknownFlagProvided_HasErrors()
        {
            var parser = new Parser(new string[] { "-serve" });

            var result = parser.Parse();

            Assert.IsEmpty(result.ExpectationErrors);
            Assert.Contains("Unknown flag: serve", result.ArgumentErrors.Values.ToList());
        }


        /* ERROR CHECKS */

        [Test]
        public void HasErrors_WithoutError_ReturnsFalse()
        {
            var parser = new Parser(new string[] { });

            var result = parser.Parse();

            Assert.IsEmpty(result.ExpectationErrors);
            Assert.IsEmpty(result.ArgumentErrors);
            Assert.IsFalse(result.HasErrors);
        }

        [Test]
        public void HasErrors_WithExpectationError_ReturnsTrue()
        {
            var parser = new Parser(new string[] { })
                .RequiresOption<string>("a", "An option");

            var result = parser.Parse();

            Assert.AreEqual(1, result.ExpectationErrors.Count);
            Assert.IsEmpty(result.ArgumentErrors);
            Assert.IsTrue(result.HasErrors);
        }

        [Test]
        public void HasErrors_WithArgumentError_ReturnsTrue()
        {
            var parser = new Parser(new string[] { "-" });

            var result = parser.Parse();

            Assert.IsEmpty(result.ExpectationErrors);
            Assert.AreEqual(1, result.ArgumentErrors.Count);
            Assert.IsTrue(result.HasErrors);
        }


        /* EXISTENCE CHECKING */

        [Test]
        public void FlagProvided_CheckingUnknownFlag_ThrowsArgumentException()
        {
            var parser = new Parser(new string[] { })
                .SupportsFlag("f", "A flag");
            parser.Parse();

            Action action = () => parser.IsFlagProvided("unknown-flag");

            Assert.Throws<ArgumentException>(action.Invoke);
        }

        [Test]
        public void FlagProvided_WithoutFlag_ReturnsFalse()
        {
            var parser = new Parser(new string[] { })
                .SupportsFlag("f", "A flag");

            parser.Parse();

            Assert.IsFalse(parser.IsFlagProvided("f"));
        }

        [Test]
        public void FlagProvided_WithFlag_ReturnsTrue()
        {
            var parser = new Parser(new string[] { "-f" })
                .SupportsFlag("f", "A flag");

            parser.Parse();

            Assert.IsTrue(parser.IsFlagProvided("f"));
        }

        [Test]
        public void OptionProvided_CheckingUnknownOption_ThrowsArgumentException()
        {
            var parser = new Parser(new string[] { })
                .SupportsOption<string>("o", "An option");
            parser.Parse();

            Action action = () => parser.IsOptionProvided("unknown-option");

            Assert.Throws<ArgumentException>(action.Invoke);
        }

        [Test]
        public void OptionProvided_WithoutOption_ReturnsFalse()
        {
            var parser = new Parser(new string[] { })
                .SupportsOption<string>("o", "An option");

            parser.Parse();

            Assert.IsFalse(parser.IsOptionProvided("o"));
        }

        [Test]
        public void OptionProvided_WithOption_ReturnsTrue()
        {
            var parser = new Parser(new string[] { "-o", "value" })
                .SupportsOption<string>("o", "An option");

            parser.Parse();

            Assert.IsTrue(parser.IsOptionProvided("o"));
        }


        /* CUSTOM VALIDATION */

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

        [Test]
        public void AddCustomOptionValidator_KnownOption_GoodValue_NoValidationError()
        {
            var argName = "filename";
            var parser = new Parser(new string[] { $"-{argName}", "FILE.CSV" })
                .SupportsOption<string>(argName, "A CSV filename")
                .AddCustomValidator(argName, IsCSV);

            parser.Parse();

            Assert.IsFalse(parser.HasErrors);
        }

        [Test]
        public void AddCustomOptionValidator_KnownOption_BadValue_AddsValidationError()
        {
            var argName = "filename";
            var parser = new Parser(new string[] { $"-{argName}", "FILE.TXT" })
                .SupportsOption<string>(argName, "A CSV filename")
                .AddCustomValidator(argName, IsCSV);

            parser.Parse();

            Assert.IsTrue(parser.HasErrors);
            Assert.AreEqual(1, parser.ExpectationErrors.Count);
            Assert.AreEqual(argName, parser.ExpectationErrors.First().Key);
            StringAssert.Contains(argName, parser.ExpectationErrors.First().Value);
        }

        [Test]
        public void AddCustomOptionValidator_KnownOption_NoValue_ValidatorNotCalled()
        {
            var argName = "filename";
            var parser = new Parser(new string[] { }) // No value given.
                .SupportsOption<string>(argName, "A CSV filename")
                .AddCustomValidator(argName, IsCSV);

            parser.Parse();

            Assert.IsFalse(parser.HasErrors);
        }

        [Test]
        public void AddCustomOptionValidator_KnownOption_AddsTwice_ThrowsArgumentException()
        {
            var parser = new Parser(new string[] { })
                .SupportsOption<string>("o", "An option");

            Action action = () =>
            {
                parser.AddCustomValidator("o", null);
                parser.AddCustomValidator("o", null);
            };

            Assert.Throws<ArgumentException>(action.Invoke);
        }

        [Test]
        public void AddCustomOptionValidator_UnknownOption_ThrowsArgumentException()
        {
            var parser = new Parser(new string[] { })
                .SupportsOption<string>("o", "An option");

            Action action = () => parser.AddCustomValidator("bad", null);

            Assert.Throws<ArgumentException>(action.Invoke);
        }


        /* FETCHING */

        [Test]
        public void GetOption_UnknownOption_ReturnsArgumentException()
        {
            var parser = new Parser(new string[] { })
                .SupportsOption<int>("o", "An option");
            parser.Parse();

            Action action = () => parser.GetOption<int>("unknown-option");

            Assert.Throws<ArgumentException>(action.Invoke);
        }

        [Test]
        public void GetOption_IncorrectOptionType_ReturnsInvalidCastException()
        {
            var parser = new Parser(new string[] { })
                .SupportsOption<int>("o", "An option");
            parser.Parse();

            Action action = () => parser.GetOption<int?>("o");

            Assert.Throws<InvalidCastException>(action.Invoke);
        }

        [Test]
        public void GetOption_NothingProvided_NoDefault_ReturnsTypeDefault()
        {
            var parser = new Parser(new string[] { })
                .SupportsOption<int>("o", "An option");
            parser.Parse();

            var result = parser.GetOption<int>("o");

            Assert.AreEqual(default(int), result);
        }

        [Test]
        public void GetOption_NothingProvided_NoDefault_ReturnsNullableTypeDefault()
        {
            var parser = new Parser(new string[] { })
                .SupportsOption<int?>("o", "An option");
            parser.Parse();

            var result = parser.GetOption<int?>("o");

            Assert.AreEqual(default(int?), result);
        }

        [Test]
        public void GetOption_NothingProvided_WithDefault_ReturnsSpecifiedDefault()
        {
            var parser = new Parser(new string[] { })
                .SupportsOption<int>("o", "An option", 42);
            parser.Parse();

            var result = parser.GetOption<int>("o");

            Assert.AreEqual(42, result);
            Assert.IsTrue(parser.GetProvided().ContainsKey("o"));
        }

        [Test]
        public void GetOption_ValueProvided_WithDefault_ReturnsActualValue()
        {
            var parser = new Parser(new string[] { "-o", "1337" })
                .SupportsOption<int>("o", "An option", 42);
            parser.Parse();

            var result = parser.GetOption<int>("o");

            Assert.AreEqual(1337, result);
            Assert.IsTrue(parser.GetProvided().ContainsKey("o"));
        }


        /* CASING */

        [Test]
        public void OptionsAndFlags_AreCaseInsensitive()
        {
            var parser = new Parser(new string[] { "-oPT", "1", "-SerVe" })
                .SupportsFlag("sErve", "A flag")
                .SupportsOption<string>("Opt", "An option");

            var result = parser.Parse();

            Assert.IsFalse(result.HasErrors);
        }

        [Test]
        public void Options_ValuesRetainCase()
        {
            const string value = "LoWeRaNdUpPeR";
            var parser = new Parser(new string[] { "-opt", value })
                .SupportsOption<string>("opt", "An option");

            var result = parser.Parse();

            Assert.AreEqual(value, result.GetOption<string>("opt"));
        }


        /* GENERAL */

        [Test]
        public void ComplexOptionsAndFlags_ConditionsMet_HasNoErrors()
        {
            var parser = new Parser(new string[] { "-opt2", "2", "-serve", "-opt1", "1" })
                .SupportsFlag("serve", "A flag")
                .SupportsOption<string>("opt1", "An option")
                .RequiresOption<string>("opt2", "Another option");

            var result = parser.Parse();

            Assert.IsFalse(result.HasErrors);
        }

        [Test]
        public void OptionsWithTypes_ValuesCannotBeConverted_HasErrors()
        {
            var parser = new Parser(new string[] { "-dtm", "a", "-f", "b" })
                .RequiresOption<DateTime>("dtm", "A datetime value")
                .RequiresOption<float>("f", "A float value");

            var result = parser.Parse();

            Assert.IsEmpty(result.ExpectationErrors);
            Assert.AreEqual(2, result.ArgumentErrors.Count);
            Assert.Contains("Expected a value of type System.DateTime: dtm", result.ArgumentErrors.Values.ToList());
            Assert.Contains("Expected a value of type System.Single: f", result.ArgumentErrors.Values.ToList());
        }

        [Test]
        public void OptionsWithTypes_TypeConversionOccurs()
        {
            var parser = new Parser(new string[] {
                    "-s", "a", "-i", "1", "-n", "1.2", "-d", "2020-10-03", "-b", "true" })
                .RequiresOption<string>("s", "A string value")
                .RequiresOption<int>("i", "An integer value")
                .RequiresOption<decimal>("n", "A numeric value")
                .RequiresOption<DateTime>("d", "A datetime value")
                .RequiresOption<bool>("b", "A boolean value");

            var result = parser.Parse();

            Assert.IsFalse(result.HasErrors);
            Assert.AreEqual("a", result.GetOption<string>("s"));
            Assert.AreEqual(1, result.GetOption<int>("i"));
            Assert.AreEqual(1.2D, result.GetOption<decimal>("n"));
            Assert.AreEqual(new DateTime(2020, 10, 3), result.GetOption<DateTime>("d"));
            Assert.AreEqual(true, result.GetOption<bool>("b"));
        }

        [Test]
        public void UnexpectedValueProvided_HasErrors()
        {
            var parser = new Parser(new string[] { "-opt", "1", "what?" })
                .RequiresOption<string>("opt", "An option");

            var result = parser.Parse();

            Assert.IsEmpty(result.ExpectationErrors);
            Assert.Contains("Unexpected value: what?", result.ArgumentErrors.Values.ToList());
        }

        [Test]
        public void OptionsAndFlagsProvided_JustDashes_HasErrors()
        {
            var parser = new Parser(new string[] { "-", "1", "-a", "-" })
                .SupportsFlag("a", "A flag");

            var result = parser.Parse();

            Assert.IsEmpty(result.ExpectationErrors);
            Assert.AreEqual(2, result.ArgumentErrors.Count);
            Assert.Contains("Option received with no name", result.ArgumentErrors.Values.ToList());
            Assert.Contains("Flag received with no name", result.ArgumentErrors.Values.ToList());
        }

        /// <summary>
        /// GetProvided should be returning in the order the options and flags
        /// were registered with the parser, not the order in the inputs.
        /// </summary>
        [Test]
        public void OptionsAndFlagsProvided_GetProvided_ReturnsCorrectlyOrdered()
        {
            var parser = new Parser(new string[] { "-f2", "-f1", "-s2", "a", "-s1", "b" })
                .RequiresOption<string>("s1", "A string value")
                .RequiresOption<string>("s2", "A string value")
                .SupportsFlag("f1", "A flag")
                .SupportsFlag("f2", "A flag");

            var result = parser.Parse().GetProvided();

            Assert.AreEqual(4, result.Count);
            Assert.AreEqual(parser.GetOption<string>("s1"), result["s1"]);
            Assert.AreEqual(parser.GetOption<string>("s2"), result["s2"]);
            Assert.Contains("f1", result.Keys.ToList());
            Assert.Contains("f2", result.Keys.ToList());
            Assert.AreEqual("s1", result.Keys.ToList()[0]);
            Assert.AreEqual("s2", result.Keys.ToList()[1]);
            Assert.AreEqual("f1", result.Keys.ToList()[2]);
            Assert.AreEqual("f2", result.Keys.ToList()[3]);
        }

        /// <summary>
        /// For ease of development you can uncomment this test
        /// and perform whatever actions are needed. Re-comment
        /// it before committing.
        /// </summary>
        [Test]
        public void TestbedForDevelopment()
        {
            ////var args = new string[] { "-run", "data", "Site Title", "--serve", "-ignore", "-port", "3000" };
            //var args = new string[] { "-run", "-Read", "IN.TXT", "--serVe", "-force", "-port", "3000" };
            //var parser = new Parser(args)
            //    .SupportsOption<int>("port", "Port to start the dev server on", 1337)
            //    .RequiresOption<bool>("secure", "Serve on HTTPS?")
            //    .RequiresOption<string>("read", "Folder to read the site from", "site")
            //    .RequiresOption<string>("write", "Folder to write the result to")
            //    .SupportsOption<decimal>("fee", "Monthly charge", 19.50M)
            //    .SupportsOption<double>("apr", "Annual interest", 3.5960)
            //    .SupportsOption<DateTime>("until", "When to stop serving", DateTime.UtcNow)
            //    .SupportsFlag("serve", "Start the site going in a dev server")
            //    .SupportsFlag("force", "Overwrite any destination content");

            //parser.Help(2);
            //parser.Parse();
            //Console.WriteLine();
            //Console.WriteLine("ShowProvided()");
            //parser.ShowProvided(2);

            //Console.WriteLine();
            //Console.WriteLine("ShowErrors()");
            //parser.ShowErrors(2);

            //Console.WriteLine();
            //Console.WriteLine("GetProvided()");
            //Console.Write("  CreateCover");
            //foreach (var item in parser.GetProvided())
            //{
            //    if (item.Value == null) Console.Write($" -{item.Key}");
            //    else Console.Write($" -{item.Key} \"{item.Value}\"");
            //}
            //Console.WriteLine();

            //Console.WriteLine();
            //Console.WriteLine("GetProvidedAsCommandArgs()");
            //Console.WriteLine(parser.GetProvidedAsCommandArgs());

            Assert.Pass();
        }
    }
}
