using NUnit.Framework;
using System;
using System.Linq;

namespace ArgsParser.Tests
{
    class ArgsParserTests
    {
        private const string UnknownKey = "N/A";

        [Test]
        public void NothingAllowed_NothingProvided_HasNoErrors()
        {
            var result = new ArgsParser(new string[] { })
                .Parse();

            Assert.IsEmpty(result.Errors);
        }

        /* OPTIONS */

        [Test]
        public void OptionAllowed_NothingProvided_HasNoErrors()
        {
            var parser = new ArgsParser(new string[] { })
                .SupportsOption<string>("opt", "An option");

            var result = parser.Parse();

            Assert.IsEmpty(result.Errors);
        }

        [Test]
        public void OptionAllowed_OptionProvided_HasNoErrors()
        {
            var parser = new ArgsParser(new string[] { "-opt", "1" })
                .SupportsOption<string>("opt", "An option");

            var result = parser.Parse();

            Assert.IsEmpty(result.Errors);
        }

        [Test]
        public void OptionRequired_NothingProvided_HasErrors()
        {
            var parser = new ArgsParser(new string[] { })
                .RequiresOption<string>("opt", "An option");

            var result = parser.Parse();

            Assert.Contains("opt", result.Errors.Keys.ToList());
            Assert.AreEqual(1, result.Errors.Count);
        }

        [Test]
        public void OptionRequired_NothingProvided_WithDefaultValue_HasNoErrors()
        {
            var parser = new ArgsParser(new string[] { })
                .RequiresOption<string>("opt", "An option", "default-value");

            var result = parser.Parse();

            Assert.IsEmpty(result.Errors);
        }

        [Test]
        public void OptionRequired_OptionProvided_HasNoErrors()
        {
            var parser = new ArgsParser(new string[] { "-opt", "1" })
                .RequiresOption<string>("opt", "An option");

            var result = parser.Parse();

            Assert.IsEmpty(result.Errors);
        }

        [Test]
        public void OptionRequired_OptionProvided_WithDefaultValue_OverridesDefault()
        {
            var parser = new ArgsParser(new string[] { "-opt", "actual-value" })
                .RequiresOption<string>("opt", "An option", "default-value");

            var result = parser.Parse();

            Assert.IsEmpty(result.Errors);
            Assert.AreEqual("actual-value", parser.ParsedOptions["opt"]);
        }

        [Test]
        public void UnknownOptionProvided_HasErrors()
        {
            var result = new ArgsParser(new string[] { "-a", "b", "c", "-serve" })
                .Parse();

            var errorKeys = result.Errors.Keys.ToList();
            Assert.Contains("a", errorKeys);
            Assert.Contains("serve", errorKeys);
            Assert.Contains(UnknownKey, errorKeys);
            Assert.AreEqual(3, result.Errors.Count);
        }

        [Test]
        public void Options_RetainCaseOfValues()
        {
            var parser = new ArgsParser(new string[] { "-opt", "MyValue" })
                .SupportsOption<string>("opt", "An option");

            var result = parser.Parse();

            Assert.AreEqual("MyValue", result.ParsedOptions["opt"]);
        }

        /* FLAGS */

        [Test]
        public void HasFlag_NothingProvided_HasNoErrors()
        {
            var parser = new ArgsParser(new string[] { })
                .HasFlag("serve", "A flag");

            var result = parser.Parse();

            Assert.IsEmpty(result.Errors);
        }

        [Test]
        public void HasFlag_FlagProvided_HasNoErrors()
        {
            var parser = new ArgsParser(new string[] { "-serve" })
                .HasFlag("serve", "A flag");

            var result = parser.Parse();

            Assert.IsEmpty(result.Errors);
        }

        [Test]
        public void UnknownFlagProvided_HasErrors()
        {
            var parser = new ArgsParser(new string[] { "-serve" })
                .HasFlag("not-serve", "A flag");

            var result = parser.Parse();

            Assert.Contains("serve", result.Errors.Keys.ToList());
            Assert.AreEqual(1, result.Errors.Count);
        }

        /* GENERAL */

        [Test]
        public void ComplexOptionsAndFlags_ConditionsMet_HasNoErrors()
        {
            var parser = new ArgsParser(new string[] { "-opt2", "2", "-serve", "-opt1", "1" })
                .HasFlag("serve", "A flag")
                .SupportsOption<string>("opt1", "An option")
                .RequiresOption<string>("opt2", "Another option");

            var result = parser.Parse();

            Assert.IsEmpty(result.Errors);
        }

        [Test]
        public void OptionsWithTypes_ValuesCannotBeConverted_HasErrors()
        {
            var parser = new ArgsParser(new string[] { "-dtm", "a", "-f", "b" })
                .RequiresOption<DateTime>("dtm", "A datetime value")
                .RequiresOption<float>("f", "A float value");

            var result = parser.Parse();

            Assert.AreEqual(2, result.Errors.Count);
            Assert.Contains("dtm", result.Errors.Keys.ToList());
            StringAssert.Contains("value of type", result.Errors["dtm"].First());
            Assert.Contains("f", result.Errors.Keys.ToList());
            StringAssert.Contains("value of type", result.Errors["f"].First());
        }

        [Test]
        public void OptionsWithTypes_TypeConversionOccurs()
        {
            var parser = new ArgsParser(new string[] {
                    "-s", "a", "-i", "1", "-n", "1.2", "-d", "2020-10-03", "-b", "true" })
                .RequiresOption<string>("s", "A string value")
                .RequiresOption<int>("i", "An integer value")
                .RequiresOption<decimal>("n", "A numeric value")
                .RequiresOption<DateTime>("d", "A datetime value")
                .RequiresOption<bool>("b", "A boolean value");

            var result = parser.Parse();

            Assert.IsEmpty(result.Errors);
            Assert.IsInstanceOf<string>(result.ParsedOptions["s"]);
            Assert.IsInstanceOf<int>(result.ParsedOptions["i"]);
            Assert.IsInstanceOf<decimal>(result.ParsedOptions["n"]);
            Assert.IsInstanceOf<DateTime>(result.ParsedOptions["d"]);
            Assert.IsInstanceOf<bool>(result.ParsedOptions["b"]);
            Assert.AreEqual("a", result.ParsedOptions["s"]);
            Assert.AreEqual(1, result.ParsedOptions["i"]);
            Assert.AreEqual(1.2D, result.ParsedOptions["n"]);
            Assert.AreEqual(new DateTime(2020, 10, 3), result.ParsedOptions["d"]);
            Assert.AreEqual(true, result.ParsedOptions["b"]);
        }

        [Test]
        public void UnexpectedValueProvided_HasErrors()
        {
            var parser = new ArgsParser(new string[] { "-opt", "1", "what?" })
                .RequiresOption<string>("opt", "An option");

            var result = parser.Parse();

            Assert.Contains(UnknownKey, result.Errors.Keys.ToList());
            Assert.AreEqual(1, result.Errors.Count);
        }

        [Test]
        public void OptionsAndFlagsProvided_JustDashes_HasErrors()
        {
            var parser = new ArgsParser(new string[] { "-", "1", "-a", "-" })
                .HasFlag("a", "A flag");

            var result = parser.Parse();

            Assert.AreEqual(1, result.Errors.Count);
            var err = result.Errors.First();
            Assert.AreEqual(UnknownKey, err.Key);
            Assert.Contains("Argument received with no name", err.Value);
            Assert.Contains("Unexpected value: 1", err.Value);
        }

        [Test]
        public void OptionsAndFlags_AreCaseInsensitive()
        {
            var parser = new ArgsParser(new string[] { "-oPT", "1", "-SerVe" })
                .HasFlag("sErve", "A flag")
                .SupportsOption<string>("Opt", "An option");

            var result = parser.Parse();

            Assert.IsEmpty(result.Errors);
        }

        /* OTHER */

        [Test]
        public void HasErrors_WithoutError_ReturnsFalse()
        {
            var parser = new ArgsParser(new string[] { });

            var result = parser.Parse();

            Assert.IsEmpty(result.Errors);
            Assert.IsFalse(result.HasErrors);
        }

        [Test]
        public void HasErrors_WithError_ReturnsTrue()
        {
            var parser = new ArgsParser(new string[] { "-" });

            var result = parser.Parse();

            Assert.AreEqual(1, result.Errors.Count);
            Assert.IsTrue(result.HasErrors);
        }

        /* FETCHING */

        [Test]
        public void FlagProvided_UnknownFlag_ThrowsArgumentException()
        {
            var parser = new ArgsParser(new string[] { })
                .HasFlag("f", "A flag");
            parser.Parse();

            Action action = () => parser.FlagProvided("unknown-flag");

            Assert.Throws<ArgumentException>(action.Invoke);
        }

        [Test]
        public void FlagProvided_WithoutFlag_ReturnsFalse()
        {
            var parser = new ArgsParser(new string[] { })
                .HasFlag("f", "A flag");

            parser.Parse();

            Assert.IsFalse(parser.FlagProvided("f"));
        }

        [Test]
        public void FlagProvided_WithFlag_ReturnsTrue()
        {
            var parser = new ArgsParser(new string[] { "-f" })
                .HasFlag("f", "A flag");

            parser.Parse();

            Assert.IsTrue(parser.FlagProvided("f"));
        }

        [Test]
        public void OptionProvided_UnknownOption_ThrowsArgumentException()
        {
            var parser = new ArgsParser(new string[] { })
                .SupportsOption<string>("o", "An option");
            parser.Parse();

            Action action = () => parser.OptionProvided("unknown-option");

            Assert.Throws<ArgumentException>(action.Invoke);
        }

        [Test]
        public void OptionProvided_WithoutOption_ReturnsFalse()
        {
            var parser = new ArgsParser(new string[] { })
                .SupportsOption<string>("o", "An option");

            parser.Parse();

            Assert.IsFalse(parser.OptionProvided("o"));
        }

        [Test]
        public void OptionProvided_WithOption_ReturnsTrue()
        {
            var parser = new ArgsParser(new string[] { "-o", "value" })
                .SupportsOption<string>("o", "An option");

            parser.Parse();

            Assert.IsTrue(parser.OptionProvided("o"));
        }

        [Test]
        public void GetOption_UnknownOption_ReturnsArgumentException()
        {
            var parser = new ArgsParser(new string[] { })
                .SupportsOption<int>("o", "An option");
            parser.Parse();

            Action action = () => parser.GetOption<int>("unknown-option");

            Assert.Throws<ArgumentException>(action.Invoke);
        }

        [Test]
        public void GetOption_IncorrectOptionType_ReturnsInvalidCastException()
        {
            var parser = new ArgsParser(new string[] { })
                .SupportsOption<int>("o", "An option");
            parser.Parse();

            Action action = () => parser.GetOption<int?>("o");

            Assert.Throws<InvalidCastException>(action.Invoke);
        }

        [Test]
        public void GetOption_NothingProvided_NoDefault_ReturnsTypeDefault()
        {
            var parser = new ArgsParser(new string[] { })
                .SupportsOption<int>("o", "An option");
            parser.Parse();

            var result = parser.GetOption<int>("o");

            Assert.AreEqual(default(int), result);
        }

        [Test]
        public void GetOption_NothingProvided_NoDefault_ReturnsNullableTypeDefault()
        {
            var parser = new ArgsParser(new string[] { })
                .SupportsOption<int?>("o", "An option");
            parser.Parse();

            var result = parser.GetOption<int?>("o");

            Assert.AreEqual(default(int?), result);
        }

        [Test]
        public void GetOption_NothingProvided_WithDefault_ReturnsSpecifiedDefault()
        {
            var parser = new ArgsParser(new string[] { })
                .SupportsOption<int>("o", "An option", 42);
            parser.Parse();

            var result = parser.GetOption<int>("o");

            Assert.AreEqual(42, result);
        }

        [Test]
        public void GetOption_ValueProvided_WithDefault_ReturnsActualValue()
        {
            var parser = new ArgsParser(new string[] { "-o", "1337" })
                .SupportsOption<int>("o", "An option", 42);
            parser.Parse();

            var result = parser.GetOption<int>("o");

            Assert.AreEqual(1337, result);
        }
    }
}
