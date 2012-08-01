namespace ObjectMapperTests
{
    using System;

    using NUnit.Framework;

    using ObjectMapper.Attributes;
    using ObjectMapper.ColumnStrategies;

    [TestFixture]
    public class ConstructorResolutionTests
    {
        private class TestClass
        {
            public TestClass()
            {
            }

            [DbConstruct]
            public TestClass(int a)
            {
            }

            public TestClass(int a, int b)
            {
            }
        }

        private class InvalidType
        {
            private InvalidType()
            {
            }
        }

        [Test]
        public void MostSpecificConstructorResolutionStrategy_Should_ResolveConstructorWithMostNumberOfArguments()
        {
            var selector = new MostSpecificConstructorResolutionStrategy();
            var constructor = typeof(TestClass).GetConstructor(new[] { typeof(int), typeof(int) });

            Assert.AreSame(constructor, selector.SelectConstructor(typeof(TestClass)));
        }

        [Test]
        public void MostSpecificConstructorResolutionStrategy_Should_ThrowWithNoPublicConstructors()
        {
            var selector = new MostSpecificConstructorResolutionStrategy();
            Assert.Throws<InvalidOperationException>(() => selector.SelectConstructor(typeof(InvalidType)));
        }

        [Test]
        public void DbConstructResolutionStrategy_Should_ResolveConstructorMarkedWithDbConstructAttribute()
        {
            var selector = new DbConstructResolutionStrategy();
            var constructor = typeof(TestClass).GetConstructor(new[] { typeof(int) });

            Assert.AreSame(constructor, selector.SelectConstructor(typeof(TestClass)));
        }

        [Test]
        public void DbConstructResolutionStrategy_Should_ThrowWithNoMarkedConstructor()
        {
            var selector = new DbConstructResolutionStrategy();
            Assert.Throws<InvalidOperationException>(() => selector.SelectConstructor(typeof(InvalidType)));
        }
    }
}
