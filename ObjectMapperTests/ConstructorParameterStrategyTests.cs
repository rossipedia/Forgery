namespace ObjectMapperTests
{
    using System.Linq;

    using NUnit.Framework;

    using ObjectMapper;
    using ObjectMapper.ColumnStrategies;
    using ObjectMapper.FieldStrategies;

    [TestFixture]
    public class ConstructorParameterStrategyTests
    {
        private class TestObject
        {
            public TestObject(int id, string name, decimal value)
            {
            }
        }

        private IFieldResolutionStrategy strategy;

        [SetUp]
        public void SetUp()
        {
            this.strategy = new ConstructorParametersStrategy(new MostSpecificConstructorResolutionStrategy());
        }

        [Test]
        public void ConstructorParametersStrategy_Should_EnumerateSameCountAsConstructorParameters()
        {
            var fields = this.strategy.EnumerateFields(typeof(TestObject));
            Assert.AreEqual(3, fields.Count());
        }

        [Test]
        public void ConstructorParametersStrategy_Should_ReturnListOfConstructorParameterNames()
        {
            var fields = this.strategy.EnumerateFields(typeof(TestObject)).ToList();
            var expected = new[]
                {
                    new DataField("id", typeof(int)),
                    new DataField("name", typeof(string)),
                    new DataField("value", typeof(decimal))
                };

            Assert.IsTrue(expected.SequenceEqual(fields));
        }
    }
}