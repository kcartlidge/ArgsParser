using NUnit.Framework;
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
        public void OptionRequired_OptionProvided_HasNoErrors()
        {
            var parser = new ArgsParser(new string[] { "-opt", "1" })
                .RequiresOption<string>("opt", "An option");

            var result = parser.Parse();

            Assert.IsEmpty(result.Errors);
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
    }
}
