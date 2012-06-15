using System;
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

        public class NonKeyedObject
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

        #region Mocks
        Mock<IDataReader> _mockReader;
        Mock<IDbConnection> _mockConnection;
        Mock<IDbCommand> _mockCommand;
        Mock<IDataParameterCollection> _mockParameters;
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

        #region Test Setup
        [TestInitialize]
        public void Setup()
        {
            SetupTestObject();
            SetupMockReader();
            SetupMockConnection();
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

        void SetupMockReader()
        {
            _mockReader = new Mock<IDataReader>();

            _mockReader.Setup(reader => reader.GetOrdinal("Id")).Returns(0);
            _mockReader.Setup(reader => reader.GetOrdinal("Name")).Returns(1);

            _mockReader.Setup(reader => reader.GetInt32(0)).Returns(123);
            _mockReader.Setup(reader => reader.GetString(1)).Returns("Bob");

            _mockReader.Setup(reader => reader["TestValue1"]).Returns(1);
            _mockReader.Setup(reader => reader["TestValue2"]).Returns("Value3");

            _mockReader.Setup(reader => reader.GetOrdinal("FooBar")).Throws<IndexOutOfRangeException>();

            _mockReader.Setup(reader => reader.Read()).Returns(true);
        }

        void SetupMockConnection()
        {
            _mockCommand = new Mock<IDbCommand>();
            _mockCommand.Setup(c => c.ExecuteScalar()).Returns(123);
            _mockCommand.Setup(c => c.CreateParameter()).Returns(() => new SqlParameter());
            _mockParameters = new Mock<IDataParameterCollection>();
            _mockCommand.Setup(c => c.Parameters).Returns(_mockParameters.Object);
            _mockConnection = new Mock<IDbConnection>();
            _mockConnection.Setup(c => c.CreateCommand()).Returns(_mockCommand.Object);
        }
        #endregion

        #region Helper Methods

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
            var reader = _mockReader.Object;
            var result = reader.MapObject<TestObject>();
            Assert.AreEqual(result.Id, 123);
            Assert.AreEqual(result.Name, "Bob");
            Assert.AreEqual(result.TestValue1, TestEnum.Value2);
            Assert.AreEqual(result.TestValue2, TestEnum.Value3);
        }

        [TestMethod]
        [ExpectedException(typeof(IndexOutOfRangeException))]
        public void TestMapperFieldNotFound()
        {
            var reader = _mockReader.Object;
            reader.MapObject<UnmappableObject>();
        }

        [TestMethod]
        public void TestEnumerableMapper()
        {
            // ReSharper disable AccessToModifiedClosure
            var i = 0;
            _mockReader.Setup(r => r.GetInt32(0)).Returns(() => i);
            _mockReader.Setup(r => r.Read()).Returns(() => i < 5).Callback(() => i++);
            // ReSharper restore AccessToModifiedClosure

            var reader = _mockReader.Object;

            //var mapper = new ObjectMapper<TestObject>(reader);
            var objects = reader.MapToEnumerable<TestObject>().ToList();
            Assert.AreEqual(5, objects.Count);
        }

        [TestMethod]
        public void TestCreateInsertStatement()
        {
            Assert.AreEqual(ExpectedInsertStatement, ObjectMapper<TestObject>.InsertStatement);
        }

        [TestMethod]
        public void TestSetInsertParameters()
        {

            var cmd = new SqlCommand();
            cmd.SetMappedInsertParameters(_testObject);
            Assert.AreEqual(3, cmd.Parameters.Count);

            AssertParameter(cmd, "@Name", "John");
            AssertParameter(cmd, "@TestValue1", TestEnum.Value3);
            AssertParameter(cmd, "@TestValue2", "Value1");
        }

        [TestMethod]
        public void TestCreateUpdateStatement()
        {
            Assert.AreEqual(ExpectedUpdateStatement, ObjectMapper<TestObject>.UpdateStatement);
        }

        [TestMethod]
        public void TestSetUpdateParameters()
        {
            var cmd = new SqlCommand();
            cmd.SetMappedUpdateParameters(_testObject);

            Assert.AreEqual(4, cmd.Parameters.Count);

            AssertParameter(cmd, "@Id", 1);
            AssertParameter(cmd, "@Name", "John");
            AssertParameter(cmd, "@TestValue1", TestEnum.Value3);
            AssertParameter(cmd, "@TestValue2", "Value1");
        }

        [TestMethod]
        public void TestCreateInsertCommand()
        {
            var conn = new SqlConnection();
            var cmd = conn.CreateMappedInsertCommand(_testObject);

            Assert.AreEqual(ExpectedInsertStatement, cmd.CommandText);
            Assert.AreEqual(CommandType.Text, cmd.CommandType);

            Assert.AreEqual(3, cmd.Parameters.Count);

            var parameters = cmd.Parameters.Cast<IDataParameter>().ToList();

            Assert.AreEqual("@Name", parameters[0].ParameterName);
            Assert.AreEqual("John", parameters[0].Value);

            Assert.AreEqual("@TestValue1", parameters[1].ParameterName);
            Assert.AreEqual(TestEnum.Value3, parameters[1].Value);

            Assert.AreEqual("@TestValue2", parameters[2].ParameterName);
            Assert.AreEqual("Value1", parameters[2].Value);
        }

        [TestMethod]
        public void TestCreateUpdateCommand()
        {
            var conn = new SqlConnection();
            var cmd = conn.CreateMappedUpdateCommand(_testObject);

            Assert.AreEqual(ExpectedUpdateStatement, cmd.CommandText);
            Assert.AreEqual(CommandType.Text, cmd.CommandType);

            Assert.AreEqual(4, cmd.Parameters.Count);

            AssertParameter(cmd, "@Id", 1);
            AssertParameter(cmd, "@Name", "John");
            AssertParameter(cmd, "@TestValue1", TestEnum.Value3);
            AssertParameter(cmd, "@TestValue2", "Value1");
        }

        [TestMethod]
        public void TestCreateDeleteStatement()
        {
            Assert.AreEqual(ExpectedDeleteStatement, ObjectMapper<TestObject>.DeleteStatement);
        }

        [TestMethod]
        public void TestCreateDeleteCommand()
        {
            var conn = new SqlConnection();
            var cmd = conn.CreateMappedDeleteCommand(_testObject);
            Assert.AreEqual(ExpectedDeleteStatement, cmd.CommandText);
            Assert.AreEqual(CommandType.Text, cmd.CommandType);

            Assert.AreEqual(1, cmd.Parameters.Count);
            AssertParameter(cmd, "@Id", 1);
        }

        [TestMethod]
        public void TestCreateInsertCommandWithDefaults()
        {
            var conn = new SqlConnection();
            var cmd = conn.CreateMappedInsertCommand<TestObject>();

            Assert.AreEqual(3, cmd.Parameters.Count);

            AssertParameter(cmd, "@Name", null);
            AssertParameter(cmd, "@TestValue1", TestEnum.Value1);
            AssertParameter(cmd, "@TestValue2", "Value1");
        }

        [TestMethod]
        public void TestCreateUpdateCommandWithDefaults()
        {
            var conn = new SqlConnection();
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
            var obj1 = new TestObject
            {
                Name = "John",
                TestValue1 = TestEnum.Value1,
                TestValue2 = TestEnum.Value2
            };

            var obj2 = new TestObject
            {
                Name = "Bob",
                TestValue1 = TestEnum.Value2,
                TestValue2 = TestEnum.Value1
            };

            var conn = new SqlConnection();
            var cmd = conn.CreateMappedInsertCommand<TestObject>();

            Assert.AreNotEqual(0, cmd.Parameters.Count);

            cmd.SetMappedInsertParameters(obj1);
            var p1 = cmd.Parameters[0] as IDataParameter;
            Debug.Assert(p1 != null, "p1 != null");
            Assert.AreEqual(p1.Value, "John");

            cmd.SetMappedInsertParameters(obj2);
            var p2 = cmd.Parameters[0] as IDataParameter;
            Debug.Assert(p2 != null, "p2 != null");
            Assert.AreEqual(p2.Value, "Bob");

            Assert.AreSame(p1, p2);
        }
        
        [TestMethod]
        public void TestSetDeleteParameters()
        {
            var cmd = new SqlCommand();
            cmd.SetMappedDeleteParameters(_testObject);

            Assert.AreEqual(1, cmd.Parameters.Count);
            AssertParameter(cmd, "@Id", 1);
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
            var conn = _mockConnection.Object;
            conn.InsertMappedObject(_testObject);
            Assert.AreEqual(123, _testObject.Id);
        }

        [TestMethod]
        public void TestInsertMappedObjectWithoutIdentity()
        {
            var test = new MultiKeyObject
            {
                Key1 = 4,
                Key2 = 8,
                Name = "Bob"
            };

            var conn = _mockConnection.Object;
            conn.InsertMappedObject(test);
            _mockCommand.Verify(c => c.ExecuteNonQuery());
        }

        [TestMethod]
        [ExpectedException(typeof(MetadataValidationException))]
        public void TestUpdateWithoutNonKeyColumnsThrows()
        {
            Assert.AreNotEqual(null, ObjectMapper<NonKeyedObject>.UpdateStatement);
        }

        [TestMethod]
        public void TestCreatedAndModifiedTimestampWithInsert()
        {
            var test = new TimestampedObject { Id = 123, Name = "Bob" };

            var st = DateTime.Now;
            var cmd = new SqlCommand();
            cmd.SetMappedInsertParameters(test);
            var en = DateTime.Now;

            var createdParam = cmd.Parameters[2];
            var updatedParam = cmd.Parameters[3];

            Assert.IsTrue(cmd.Parameters.Contains("@Created"));
            Assert.IsTrue(cmd.Parameters.Contains("@Modified"));

            Assert.AreSame(typeof(DateTime), createdParam.Value.GetType());
            Assert.AreSame(typeof(DateTime), updatedParam.Value.GetType());

            Assert.IsTrue(st <= (DateTime)createdParam.Value && en >= (DateTime)createdParam.Value);
            Assert.IsTrue(st <= (DateTime)updatedParam.Value && en >= (DateTime)updatedParam.Value);
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
            var cmd = new SqlCommand();
            cmd.SetMappedUpdateParameters(test);

            Assert.IsFalse(cmd.Parameters.Contains("@Created"));
        }

        [TestMethod]
        public void TestModifiedTimestampGetsOverwrittenOnUpdate()
        {
            var test = new TimestampedObject { Id = 123, Name = "Bob", Created = new DateTime(2012, 1, 1), Modified = new DateTime(2012, 1, 2) };
            var originalModified = test.Modified;
            var cmd = new SqlCommand();
            cmd.SetMappedUpdateParameters(test);

            Assert.AreNotEqual(originalModified, cmd.Parameters["@Modified"].Value);
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

            var cmd = new SqlCommand();
            cmd.SetMappedInsertParameters(test);

            Assert.AreEqual(3, cmd.Parameters.Count);
            Assert.AreEqual("Value1", cmd.Parameters["@EnumVal1"].Value);
            Assert.AreEqual(StringEnum.Value2, cmd.Parameters["@EnumVal2"].Value);
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
            var conn = new SqlConnection();
            var cmd = conn.CreateMappedSelectCommand<TestObject>();
            Assert.AreEqual(0, cmd.Parameters.Count);
            Assert.AreEqual(ExpectedSelectStatement, cmd.CommandText);
            Assert.AreEqual(CommandType.Text, cmd.CommandType);
        }

        [TestMethod]
        public void TestCreateMappedSelectCommandWithCriteria()
        {
            var conn = new SqlConnection();
            var cmd = conn.CreateMappedSelectCommand<TestObject>("WHERE Id <> 0");
            Assert.AreEqual(0, cmd.Parameters.Count);
            Assert.AreEqual(
                "SELECT Id, Name, TestValue1, TestValue2 FROM TestObject WHERE Id <> 0",
                cmd.CommandText
            );
            Assert.AreEqual(CommandType.Text, cmd.CommandType);
        }

        [TestMethod]
        public void TestCreateMappedSelectCommandWithCriteriaAndParameters()
        {
            var conn = new SqlConnection();
            var cmd = conn.CreateMappedSelectCommand<TestObject>("WHERE Id <> @0", 1);
            Assert.AreEqual(
                "SELECT Id, Name, TestValue1, TestValue2 FROM TestObject WHERE Id <> @0",
                cmd.CommandText
            );
            Assert.AreEqual(CommandType.Text, cmd.CommandType);
            Assert.AreEqual(1, cmd.Parameters.Count);
            AssertParameter(cmd, "@0", 1);
        }

        #endregion
    }
}
