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
            var name = "name";
            var seq = 93;
            var type = typeof(string);
            var info = "info";
            var defaultValue = "default";

            var d = new ArgDetail(name, seq, type, true, true, info, defaultValue);

            Assert.AreEqual(name, d.Name);
            Assert.AreEqual(seq, d.Sequence);
            Assert.AreEqual(type, d.ArgType);
            Assert.AreEqual(true, d.IsRequired);
            Assert.AreEqual(true, d.IsOption);
            Assert.AreEqual(info, d.Info);
            Assert.AreEqual(defaultValue, d.DefaultValue);
        }

        [TestCase(true, false)]
        [TestCase(false, true)]
        public void IsOptional_InvertsIsRequired(bool isRequired, bool expected)
        {
            var name = "name";
            var seq = 93;
            var type = typeof(string);
            var info = "info";
            var defaultValue = "default";

            var d = new ArgDetail(name, seq, type, isRequired, true, info, defaultValue);

            Assert.AreNotEqual(isRequired, d.IsOptional);
            Assert.AreEqual(expected, d.IsOptional);
        }
    }
}
