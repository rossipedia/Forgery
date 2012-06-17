using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using Moq;
using ObjectMapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ObjectMapperTests
{
    [TestClass]
    public class ObjectMapperTest
    {
        #region Types for testing

        // Resharper disables
        // ReSharper disable MemberCanBePrivate.Global
        // ReSharper disable UnusedMember.Global
        // ReSharper disable UnusedAutoPropertyAccessor.Global

        #region Enums

        public enum TestEnum
        {
            Value1, Value2, Value3
        }

        [DbEnum(EnumSaveType.String)]
        public enum StringEnum
        {
            Value1, Value2
        }

        #endregion

        #region Classes

        public class TestObject
        {
            [DbKey]
            [DbIdentity]
            public int Id { get; set; }
            public string Name { get; set; }
            public TestEnum TestValue1 { get; set; }
            [DbEnum(EnumSaveType.String)]
            public TestEnum TestValue2 { get; set; }
        }

        public class UnmappableObject
        {
            public int FooBar { get; set; }
        }

        public class MultipleIdentityObject
        {
            [DbIdentity]
            public int Id1 { get; set; }
            [DbIdentity]
            public int Id2 { get; set; }
        }

        public class MultiKeyObject
        {
            [DbKey]
            public int Key1 { get; set; }
            [DbKey]
            public int Key2 { get; set; }
            public string Name { get; set; }
        }

        public class ObjectWithNonKeyIdentity
        {
            [DbKey]
            public string Code { get; set; }
            [DbIdentity]
            public int Ident { get; set; }

            public string Name { get; set; }
        }

        public class NonIdentityObject
        {
            [DbKey]
            public int Id { get; set; }
            [DbKey]
            public string Name { get; set; }
        }

        public class TimestampedObject
        {
            [DbKey]
            public int Id { get; set; }
            public string Name { get; set; }

            [DbCreatedTimestamp]
            public DateTime Created { get; set; }
            [DbModifiedTimestamp]
            public DateTime Modified { get; set; }
        }

        public class ObjectWithIntEnumProperty
        {
            [DbKey]
            [DbIdentity]
            public int Id { get; set; }
            public string Name { get; set; }

            public StringEnum EnumVal1 { get; set; }
            [DbEnum(EnumSaveType.Numeric)]
            public StringEnum EnumVal2 { get; set; }
        }

        [DbTable(Name = "SomeTable")]
        public class ObjectWithSpecificTableName
        {
            [DbKey]
            [DbIdentity]
            public int Id { get; set; }
            public string Name { get; set; }
        }

        [DbTable(Prefix = "test")]
        public class ObjectWithPrefixedTableName
        {
            [DbKey]
            [DbIdentity]
            public int Id { get; set; }
            public string Name { get; set; }
        }

        #endregion

        // Resharper restores
        // ReSharper restore UnusedAutoPropertyAccessor.Global
        // ReSharper restore UnusedMember.Global
        // ReSharper restore MemberCanBePrivate.Global

        #endregion

        #region Objects for Testing
        private TestObject _testObject;
        #endregion

        #region Constants
        private const string ExpectedUpdateStatement = "UPDATE TestObject SET Name = @Name, TestValue1 = @TestValue1, TestValue2 = @TestValue2 WHERE Id = @Id;";
        private const string ExpectedInsertStatement = "INSERT INTO TestObject (Name, TestValue1, TestValue2) VALUES (@Name, @TestValue1, @TestValue2); SELECT SCOPE_IDENTITY() as Id;";
        private const string ExpectedDeleteStatement = "DELETE FROM TestObject WHERE Id = @Id;";
        private const string ExpectedSelectStatement = "SELECT Id, Name, TestValue1, TestValue2 FROM TestObject ";
        #endregion

        #region Mocks
        MockRepository _mockRepository;

        class TestMocks
        {
            public Mock<IDbConnection> Connection;
            public Mock<IDbCommand> Command;
            public Mock<IDataParameterCollection> Parameters;
            public List<IDataParameter> RealParameters;

            public Mock<IDataReader> Reader;
        }

        TestMocks CreateTestMocks()
        {
            var mocks = new TestMocks
            {
                Connection = _mockRepository.Create<IDbConnection>(),
                Command = _mockRepository.Create<IDbCommand>(),
                Parameters = _mockRepository.Create<IDataParameterCollection>(),
                RealParameters = new List<IDataParameter>(),
                Reader = new Mock<IDataReader>()
            };

            SetupMockConnection(mocks);
            SetupMockCommand(mocks);
            SetupMockParameters(mocks);

            return mocks;
        }

        void SetupMockRepository()
        {
            _mockRepository = new MockRepository(MockBehavior.Strict);
        }

        void SetupMockConnection(TestMocks mocks)
        {
            mocks.Connection.Setup(c => c.CreateCommand()).Returns(mocks.Command.Object);
        }

        void SetupMockCommand(TestMocks mocks)
        {
            mocks.Command.SetupProperty(cmd => cmd.CommandText);
            mocks.Command.SetupProperty(cmd => cmd.CommandType);

            mocks.Command.SetupGet(cmd => cmd.Parameters).Returns(mocks.Parameters.Object);

            mocks.Command.Setup(cmd => cmd.ExecuteReader()).Returns(mocks.Reader.Object);
            
            mocks.Command.Setup(cmd => cmd.CreateParameter()).Returns(() => {
                var p = _mockRepository.Create<IDbDataParameter>();
                p.SetupAllProperties();
                return p.Object;
            });
        }

        void SetupMockParameters(TestMocks mocks)
        {
            mocks.Parameters
                .Setup(p => p.Add(It.IsAny<IDataParameter>()))
                .Returns<IDataParameter>(p =>
                {
                    mocks.RealParameters.Add(p);
                    return mocks.RealParameters.Count - 1;
                });

            mocks.Parameters.SetupGet(p => p.Count).Returns(() => mocks.RealParameters.Count);

            mocks.Parameters
                .Setup(p => p.Contains(It.IsAny<string>()))
                .Returns<string>(s => mocks.RealParameters.Any(p => p.ParameterName == s));

            mocks.Parameters
                .Setup(p => p[It.IsAny<string>()])
                .Returns<string>(s => mocks.RealParameters.Single(p => p.ParameterName == s));

            mocks.Parameters
                .Setup(p => p[It.IsAny<int>()])
                .Returns<int>(i => mocks.RealParameters[i]);
        }

        IDataReader CreateMockReader<T>(TestMocks mocks, params T[] objects)
        {
            var type = typeof(T);

            var reader = mocks.Reader;

            var records = objects.ToList();
            var row = -1;

            // ReSharper disable AccessToModifiedClosure
            // ReSharper disable ImplicitlyCapturedClosure
            reader.Setup(r => r.Read())
                .Callback(() => row++)
                .Returns(() => row < records.Count);

            // ReSharper restore ImplicitlyCapturedClosure
            // ReSharper restore AccessToModifiedClosure

            var propertyOrdinal = 0;
            var properties = type.GetProperties().ToList();
            foreach (var prop in properties)
            {
                var ord = propertyOrdinal;
                reader.Setup(r => r.GetBoolean(ord)).Returns(() => (bool)prop.GetValue(records[row], null));
                reader.Setup(r => r.GetByte(ord)).Returns(() => (byte)prop.GetValue(records[row], null));
                reader.Setup(r => r.GetChar(ord)).Returns(() => (char)prop.GetValue(records[row], null));
                reader.Setup(r => r.GetDateTime(ord)).Returns(() => (DateTime)prop.GetValue(records[row], null));
                reader.Setup(r => r.GetDecimal(ord)).Returns(() => (decimal)prop.GetValue(records[row], null));
                reader.Setup(r => r.GetDouble(ord)).Returns(() => (double)prop.GetValue(records[row], null));
                reader.Setup(r => r.GetFloat(ord)).Returns(() => (float)prop.GetValue(records[row], null));
                reader.Setup(r => r.GetGuid(ord)).Returns(() => (Guid)prop.GetValue(records[row], null));
                reader.Setup(r => r.GetInt16(ord)).Returns(() => (short)prop.GetValue(records[row], null));
                reader.Setup(r => r.GetInt32(ord)).Returns(() => (int)prop.GetValue(records[row], null));
                reader.Setup(r => r.GetInt64(ord)).Returns(() => (long)prop.GetValue(records[row], null));

                reader.Setup(r => r.GetOrdinal(prop.Name)).Returns(ord);
                reader.Setup(r => r[prop.Name]).Returns(() => prop.GetValue(records[row], null));

                reader.Setup(r => r.GetString(ord)).Returns(() => (string)prop.GetValue(records[row], null));

                propertyOrdinal++;
            }
            return reader.Object;
        }
        #endregion



        [TestInitialize]
        public void Setup()
        {
            SetupTestObject();
            SetupMockRepository();
        }

        void SetupTestObject()
        {
            _testObject = new TestObject
            {
                Id = 1,
                Name = "John",
                TestValue1 = TestEnum.Value3,
                TestValue2 = TestEnum.Value1
            };
        }




        #region Helper Assert / Verification Methods

        static void AssertParameter(IDbCommand cmd, string name, object value)
        {
            Assert.IsTrue(cmd.Parameters.Contains(name));
            var p = (IDataParameter)cmd.Parameters[name];
            Assert.AreEqual(value, p.Value);
        }

        #endregion

        #region Tests

        [TestMethod]
        public void TestMapper()
        {
            var mocks = CreateTestMocks();
            var reader = CreateMockReader(mocks, _testObject);
            reader.Read();
            var result = reader.MapObject<TestObject>();
            Assert.AreEqual(_testObject.Id, result.Id);
            Assert.AreEqual(_testObject.Name, result.Name);
            Assert.AreEqual(_testObject.TestValue1, result.TestValue1);
            Assert.AreEqual(_testObject.TestValue2, result.TestValue2);
        }

        [TestMethod]
        [ExpectedException(typeof(IndexOutOfRangeException))]
        public void TestMapperFieldNotFound()
        {
            var reader = _mockRepository.Create<IDataReader>();
            reader.Setup(r => r.GetOrdinal("FooBar")).Throws<IndexOutOfRangeException>();
            reader.Object.MapObject<UnmappableObject>();
        }

        [TestMethod]
        public void TestEnumerableMapper()
        {
            var objects = Enumerable.Range(0, 5).Select(i => new TestObject
            {
                Id = 0,
                Name = "Object" + i.ToString()
            }).ToArray();

            var mocks = CreateTestMocks();
            var reader = CreateMockReader(mocks, objects);

            var results = reader.MapToEnumerable<TestObject>().ToList();
            Assert.AreEqual(objects.Length, results.Count);
            for (var i = 0; i < objects.Length; ++i)
            {
                var o = objects[i];
                var r = results[i];
                Assert.AreEqual(o.Id, r.Id);
                Assert.AreEqual(o.Name, r.Name);
                Assert.AreEqual(o.TestValue1, r.TestValue1);
                Assert.AreEqual(o.TestValue2, r.TestValue2);
            }
        }

        [TestMethod]
        public void TestCreateInsertStatement()
        {
            Assert.AreEqual(ExpectedInsertStatement, ObjectMapper<TestObject>.InsertStatement);
        }

        [TestMethod]
        public void TestSetInsertParameters()
        {
            var mocks = CreateTestMocks();
            var cmd = mocks.Command.Object;

            cmd.SetMappedInsertParameters(_testObject);
            Assert.AreEqual(3, cmd.Parameters.Count);

            AssertParameter(cmd, "@Name", _testObject.Name);
            AssertParameter(cmd, "@TestValue1", _testObject.TestValue1);
            AssertParameter(cmd, "@TestValue2", _testObject.TestValue2.ToString());
        }

        [TestMethod]
        public void TestCreateUpdateStatement()
        {
            Assert.AreEqual(ExpectedUpdateStatement, ObjectMapper<TestObject>.UpdateStatement);
        }

        [TestMethod]
        public void TestSetUpdateParameters()
        {
            //var cmd = new SqlCommand();
            var mocks = CreateTestMocks();
            var cmd = mocks.Command.Object;
            cmd.SetMappedUpdateParameters(_testObject);

            Assert.AreEqual(4, cmd.Parameters.Count);

            AssertParameter(cmd, "@Id", _testObject.Id);
            AssertParameter(cmd, "@Name", _testObject.Name);
            AssertParameter(cmd, "@TestValue1", _testObject.TestValue1);
            AssertParameter(cmd, "@TestValue2", _testObject.TestValue2.ToString());
        }

        [TestMethod]
        public void TestCreateInsertCommand()
        {
            var mocks = CreateTestMocks();
            var conn = mocks.Connection.Object;
            var cmd = conn.CreateMappedInsertCommand(_testObject);

            Assert.AreEqual(ExpectedInsertStatement, cmd.CommandText);
            Assert.AreEqual(CommandType.Text, cmd.CommandType);

            Assert.AreEqual(3, cmd.Parameters.Count);

            AssertParameter(cmd, "@Name", _testObject.Name);
            AssertParameter(cmd, "@TestValue1", _testObject.TestValue1);
            AssertParameter(cmd, "@TestValue2", _testObject.TestValue2.ToString());
        }

        [TestMethod]
        public void TestCreateUpdateCommand()
        {
            var conn = new SqlConnection();
            var cmd = conn.CreateMappedUpdateCommand(_testObject);

            Assert.AreEqual(ExpectedUpdateStatement, cmd.CommandText);
            Assert.AreEqual(CommandType.Text, cmd.CommandType);

            Assert.AreEqual(4, cmd.Parameters.Count);

            AssertParameter(cmd, "@Id", _testObject.Id);
            AssertParameter(cmd, "@Name", _testObject.Name);
            AssertParameter(cmd, "@TestValue1", _testObject.TestValue1);
            AssertParameter(cmd, "@TestValue2", _testObject.TestValue2.ToString());
        }

        [TestMethod]
        public void TestCreateDeleteStatement()
        {
            Assert.AreEqual(ExpectedDeleteStatement, ObjectMapper<TestObject>.DeleteStatement);
        }

        [TestMethod]
        public void TestCreateDeleteCommand()
        {
            var mocks = CreateTestMocks();
            var conn = mocks.Connection.Object;
            var cmd = conn.CreateMappedDeleteCommand(_testObject);
            Assert.AreEqual(ExpectedDeleteStatement, cmd.CommandText);
            Assert.AreEqual(CommandType.Text, cmd.CommandType);
            AssertParameter(cmd, "@Id", _testObject.Id);
        }

        [TestMethod]
        public void TestCreateInsertCommandWithDefaults()
        {
            var mocks = CreateTestMocks();
            var conn = mocks.Connection.Object;
            var cmd = conn.CreateMappedInsertCommand<TestObject>();

            Assert.AreEqual(3, cmd.Parameters.Count);

            AssertParameter(cmd, "@Name", null);
            AssertParameter(cmd, "@TestValue1", TestEnum.Value1);
            AssertParameter(cmd, "@TestValue2", "Value1");
        }

        [TestMethod]
        public void TestCreateUpdateCommandWithDefaults()
        {
            var mocks = CreateTestMocks();
            var conn = mocks.Connection.Object;
            var cmd = conn.CreateMappedUpdateCommand<TestObject>();

            Assert.AreEqual(4, cmd.Parameters.Count);

            AssertParameter(cmd, "@Id", 0);
            AssertParameter(cmd, "@Name", null);
            AssertParameter(cmd, "@TestValue1", TestEnum.Value1);
            AssertParameter(cmd, "@TestValue2", "Value1");
        }

        [TestMethod]
        public void TestSetMappedInsertParametersDoesntCreateMultiple()
        {
            var obj2 = new TestObject
            {
                Name = "Bob",
                TestValue1 = TestEnum.Value2,
                TestValue2 = TestEnum.Value1
            };

            var mocks = CreateTestMocks();
            var conn = mocks.Connection.Object;
            var cmd = conn.CreateMappedInsertCommand<TestObject>();

            Assert.AreEqual(3, cmd.Parameters.Count);

            cmd.SetMappedInsertParameters(_testObject);
            AssertParameter(cmd, "@Name", _testObject.Name);
            var p1 = cmd.Parameters["@Name"];
            
            cmd.SetMappedInsertParameters(obj2);
            AssertParameter(cmd, "@Name", obj2.Name);
            var p2 = cmd.Parameters["@Name"];
            
            Assert.AreSame(p1, p2);
        }

        [TestMethod]
        public void TestSetDeleteParameters()
        {
            var mocks = CreateTestMocks();
            var cmd = mocks.Command.Object;
            cmd.SetMappedDeleteParameters(_testObject);

            Assert.AreEqual(1, cmd.Parameters.Count);
            AssertParameter(cmd, "@Id", _testObject.Id);
        }

        [TestMethod]
        [ExpectedException(typeof(MetadataValidationException))]
        public void TestMultipleIdentityThrows()
        {
            // ReSharper disable UnusedVariable
            var metaData = ObjectMapper<MultipleIdentityObject>.Metadata;
            // ReSharper restore UnusedVariable
        }

        [TestMethod]
        public void TestMultipleKeysOnUpdate()
        {
            Assert.AreEqual(
                "UPDATE MultiKeyObject SET Name = @Name WHERE Key1 = @Key1 AND Key2 = @Key2;",
                ObjectMapper<MultiKeyObject>.UpdateStatement
            );
        }

        [TestMethod]
        public void TestNonKeyIdentityDoesntUpdate()
        {
            Assert.AreEqual(
                "UPDATE ObjectWithNonKeyIdentity SET Name = @Name WHERE Code = @Code;",
                ObjectMapper<ObjectWithNonKeyIdentity>.UpdateStatement
            );
        }

        [TestMethod]
        public void TestNonKeyIdentityDoesntInsert()
        {
            Assert.AreEqual(
                "INSERT INTO ObjectWithNonKeyIdentity (Code, Name) VALUES (@Code, @Name); SELECT SCOPE_IDENTITY() as Ident;",
                ObjectMapper<ObjectWithNonKeyIdentity>.InsertStatement
            );
        }

        [TestMethod]
        public void TestInsertMappedObjectWithIdentity()
        {
            var mocks = CreateTestMocks();
            var cmd = mocks.Command;
            cmd.Setup(c => c.ExecuteScalar()).Returns(123);
            var conn = mocks.Connection.Object;
            
            conn.InsertMappedObject(_testObject);
            Assert.AreEqual(123, _testObject.Id);
        }

        [TestMethod]
        public void TestInsertMappedObjectWithoutIdentity()
        {
            var mocks = CreateTestMocks();
            var conn = mocks.Connection.Object;
            var obj = new NonIdentityObject { Id = 123, Name = "Bob" };
            mocks.Command.Setup(cmd => cmd.ExecuteNonQuery()).Returns(1);
            Assert.AreEqual(1, conn.InsertMappedObject(obj));
        }

        [TestMethod]
        [ExpectedException(typeof(MetadataValidationException))]
        public void TestUpdateWithoutNonKeyColumnsThrows()
        {
            Assert.AreNotEqual(null, ObjectMapper<NonIdentityObject>.UpdateStatement);
        }

        [TestMethod]
        public void TestCreatedAndModifiedTimestampWithInsert()
        {
            var testObject = new TimestampedObject { Id = 123, Name = "Bob" };
            var begin = DateTime.Now;
            var mocks = CreateTestMocks();
            var cmd = mocks.Command.Object;
            cmd.SetMappedInsertParameters(testObject);
            var end = DateTime.Now;
            
            Assert.IsTrue(cmd.Parameters.Contains("@Created"));
            Assert.IsTrue(cmd.Parameters.Contains("@Modified"));

            var createdParam = (IDataParameter)cmd.Parameters["@Created"];
            var updatedParam = (IDataParameter)cmd.Parameters["@Modified"];

            Assert.AreSame(typeof(DateTime), createdParam.Value.GetType());
            Assert.AreSame(typeof(DateTime), updatedParam.Value.GetType());

            Assert.IsTrue(begin <= (DateTime)createdParam.Value && end >= (DateTime)createdParam.Value);
            Assert.IsTrue(begin <= (DateTime)updatedParam.Value && end >= (DateTime)updatedParam.Value);
        }

        [TestMethod]
        public void TestCreatedTimestampDoesntUpdate()
        {
            Assert.AreEqual(
                "UPDATE TimestampedObject SET Name = @Name, Modified = @Modified WHERE Id = @Id;",
                ObjectMapper<TimestampedObject>.UpdateStatement
            );
        }

        [TestMethod]
        public void TestCreatedTimestampDoesntGetUpdateParam()
        {
            var test = new TimestampedObject { Id = 123, Name = "Bob", Created = new DateTime(2012, 1, 1) };
            var mocks = CreateTestMocks();
            var cmd = mocks.Command.Object;
            cmd.SetMappedUpdateParameters(test);
            Assert.IsFalse(cmd.Parameters.Contains("@Created"));
        }

        [TestMethod]
        public void TestModifiedTimestampGetsOverwrittenOnUpdate()
        {
            var test = new TimestampedObject { Id = 123, Name = "Bob", Created = new DateTime(2012, 1, 1), Modified = new DateTime(2012, 1, 2) };
            var originalModified = test.Modified;
            var mocks = CreateTestMocks();
            var cmd = mocks.Command.Object;
            cmd.SetMappedUpdateParameters(test);
            Assert.AreNotEqual(originalModified, ((IDataParameter)cmd.Parameters["@Modified"]).Value);
        }

        [TestMethod]
        public void TestPropertyEnumSaveTypeOverridesEnumSaveType()
        {
            var test = new ObjectWithIntEnumProperty
            {
                Id = 1,
                Name = "Bob",
                EnumVal1 = StringEnum.Value1,
                EnumVal2 = StringEnum.Value2
            };

            var mocks = CreateTestMocks();
            var cmd = mocks.Command.Object;
            cmd.SetMappedInsertParameters(test);

            Assert.AreEqual(3, cmd.Parameters.Count);
            AssertParameter(cmd, "@EnumVal1", test.EnumVal1.ToString());
            AssertParameter(cmd, "@EnumVal2", test.EnumVal2);
        }

        [TestMethod]
        public void TestObjectWithSpecificTableName()
        {
            Assert.AreEqual(
                "INSERT INTO SomeTable (Name) VALUES (@Name); SELECT SCOPE_IDENTITY() as Id;",
                ObjectMapper<ObjectWithSpecificTableName>.InsertStatement
            );

            Assert.AreEqual(
                "UPDATE SomeTable SET Name = @Name WHERE Id = @Id;",
                ObjectMapper<ObjectWithSpecificTableName>.UpdateStatement
            );

            Assert.AreEqual(
                "DELETE FROM SomeTable WHERE Id = @Id;",
                ObjectMapper<ObjectWithSpecificTableName>.DeleteStatement
            );
        }

        [TestMethod]
        public void TestObjectWithPrefixedTableName()
        {
            Assert.AreEqual(
                "INSERT INTO testObjectWithPrefixedTableName (Name) VALUES (@Name); SELECT SCOPE_IDENTITY() as Id;",
                ObjectMapper<ObjectWithPrefixedTableName>.InsertStatement
            );

            Assert.AreEqual(
                "UPDATE testObjectWithPrefixedTableName SET Name = @Name WHERE Id = @Id;",
                ObjectMapper<ObjectWithPrefixedTableName>.UpdateStatement
            );

            Assert.AreEqual(
                "DELETE FROM testObjectWithPrefixedTableName WHERE Id = @Id;",
                ObjectMapper<ObjectWithPrefixedTableName>.DeleteStatement
            );
        }

        [TestMethod]
        public void TestAssemblyWidePrefixedTableName()
        {
            // TODO: Gotta think about how to pull this one off
        }

        [TestMethod]
        public void TestCreateSelectStatement()
        {
            Assert.AreEqual(ExpectedSelectStatement, ObjectMapper<TestObject>.SelectStatement);
        }

        [TestMethod]
        public void TestCreateMappedSelectCommand()
        {
            var mocks = CreateTestMocks();
            var conn = mocks.Connection.Object;
            var cmd = conn.CreateMappedSelectCommand<TestObject>();
            Assert.AreEqual(0, cmd.Parameters.Count);
            Assert.AreEqual(ExpectedSelectStatement, cmd.CommandText);
            Assert.AreEqual(CommandType.Text, cmd.CommandType);
        }

        [TestMethod]
        public void TestCreateMappedSelectCommandWithCriteria()
        {
            var mocks = CreateTestMocks();
            var conn = mocks.Connection.Object;
            var cmd = conn.CreateMappedSelectCommand<TestObject>("WHERE Id <> 0");
            Assert.AreEqual(0, cmd.Parameters.Count);
            Assert.AreEqual(
                "SELECT Id, Name, TestValue1, TestValue2 FROM TestObject WHERE Id <> 0",
                cmd.CommandText
            );
            Assert.AreEqual(CommandType.Text, cmd.CommandType);
        }

        [TestMethod]
        public void TestCreateMappedSelectCommandWithCriteriaAndIndexedParameters()
        {
            var mocks = CreateTestMocks();
            var conn = mocks.Connection.Object;
            var cmd = conn.CreateMappedSelectCommand<TestObject>("WHERE Id <> @0", 1);
            Assert.AreEqual(
                "SELECT Id, Name, TestValue1, TestValue2 FROM TestObject WHERE Id <> @0",
                cmd.CommandText
            );
            Assert.AreEqual(CommandType.Text, cmd.CommandType);
            Assert.AreEqual(1, cmd.Parameters.Count);
            AssertParameter(cmd, "@0", 1);
        }

        [TestMethod]
        public void TestExecuteNonQueryText()
        {
            var mocks = CreateTestMocks();
            var conn = mocks.Connection.Object;
            mocks.Command.Setup(cmd => cmd.ExecuteNonQuery()).Returns(1);
            
            var result = conn.ExecuteNonQueryText("UPDATE TestObject SET Name = @0", "Bob");
            
            Assert.AreEqual(1, result);
            mocks.Parameters.Verify(p => p.Add(It.IsAny<IDataParameter>()));
        }

        [TestMethod]
        public void TestExecuteReaderText()
        {
            var mocks = CreateTestMocks();
            var conn = mocks.Connection.Object;

            var reader = conn.ExecuteReaderText("SELECT * FROM TestObject WHERE Name = @0;", "Bob");
            
            Assert.IsNotNull(reader);
            mocks.Parameters.Verify(p => p.Add(It.IsAny<IDataParameter>()));
        }

        #endregion
    }
}
