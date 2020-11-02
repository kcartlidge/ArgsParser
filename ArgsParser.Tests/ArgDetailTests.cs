using NUnit.Framework;

namespace ArgsParser.Tests
{
    public class ArgDetailTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Constructor_SetsFields()
        {
            var type = typeof(string);
            var info = "info";
            var defaultValue = "default";

            var d = new ArgDetail(type, true, info, defaultValue);

            Assert.AreEqual(type, d.ArgType);
            Assert.AreEqual(true, d.IsRequired);
            Assert.AreEqual(info, d.Info);
            Assert.AreEqual(defaultValue, d.DefaultValue);
        }

        [TestCase(true, false)]
        [TestCase(false, true)]
        public void IsOptional_InvertsIsRequired(bool isRequired, bool expected)
        {
            var type = typeof(string);
            var info = "info";
            var defaultValue = "default";

            var d = new ArgDetail(type, isRequired, info, defaultValue);

            Assert.AreNotEqual(isRequired, d.IsOptional);
            Assert.AreEqual(expected, d.IsOptional);
        }
    }
}
