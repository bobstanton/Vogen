﻿#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vogen.IntegrationTests.SerializationAndConversionTests.Types;
using Xunit;
using NewtonsoftJsonSerializer = Newtonsoft.Json.JsonConvert;
using SystemTextJsonSerializer = System.Text.Json.JsonSerializer;

namespace Vogen.IntegrationTests.SerializationAndConversionTests
{
    [ValueObject(underlyingType: typeof(decimal))]
    public partial struct AnotherDecimalVo { }

    public class DecimalVoTests
    {
        [Fact]
        public void equality_between_same_value_objects()
        {
            DecimalVo.From(18).Equals(DecimalVo.From(18)).Should().BeTrue();
            (DecimalVo.From(18) == DecimalVo.From(18)).Should().BeTrue();

            (DecimalVo.From(18) != DecimalVo.From(19)).Should().BeTrue();
            (DecimalVo.From(18) == DecimalVo.From(19)).Should().BeFalse();

            DecimalVo.From(18).Equals(DecimalVo.From(18)).Should().BeTrue();
            (DecimalVo.From(18) == DecimalVo.From(18)).Should().BeTrue();

            var original = DecimalVo.From(18);
            var other = DecimalVo.From(18);

            ((original as IEquatable<DecimalVo>).Equals(other)).Should().BeTrue();
            ((other as IEquatable<DecimalVo>).Equals(original)).Should().BeTrue();
        }

        [Fact]
        public void equality_between_different_value_objects()
        {
            DecimalVo.From(18).Equals(AnotherDecimalVo.From(18)).Should().BeFalse();
        }

        [Fact]
        public void CanSerializeToLong_WithNewtonsoftJsonProvider()
        {
            var foo = NewtonsoftJsonDecimalVo.From(123D);

            string serializedFoo = NewtonsoftJsonSerializer.SerializeObject(foo);
            string serializedLong = NewtonsoftJsonSerializer.SerializeObject(foo.Value);

            Assert.Equal(serializedFoo, serializedLong);
        }

        [Fact]
        public void CanSerializeToNullableLong_WithNewtonsoftJsonProvider()
        {
            var entity = new EntityWithNullableId { Id = null };

            var json = NewtonsoftJsonSerializer.SerializeObject(entity);
            var deserialize = NewtonsoftJsonSerializer.DeserializeObject<EntityWithNullableId>(json);

            deserialize.Should().NotBeNull();
            deserialize.Id.Should().BeNull();
        }

        [Fact]
        public void CanSerializeToLong_WithSystemTextJsonProvider()
        {
            var foo = SystemTextJsonDecimalVo.From(123D);

            string serializedFoo = SystemTextJsonSerializer.Serialize(foo);
            string serializedLong = SystemTextJsonSerializer.Serialize(foo.Value);

            serializedFoo.Equals(serializedLong).Should().BeTrue();
        }

        [Fact]
        public void CanDeserializeFromLong_WithNewtonsoftJsonProvider()
        {
            var value = 123D;
            var foo = NewtonsoftJsonDecimalVo.From(value);
            var serializedLong = NewtonsoftJsonSerializer.SerializeObject(value);

            var deserializedFoo = NewtonsoftJsonSerializer.DeserializeObject<NewtonsoftJsonDecimalVo>(serializedLong);

            Assert.Equal(foo, deserializedFoo);
        }

        [Fact]
        public void CanDeserializeFromLong_WithSystemTextJsonProvider()
        {
            var value = 123D;
            var foo = SystemTextJsonDecimalVo.From(value);
            var serializedLong = SystemTextJsonSerializer.Serialize(value);

            var deserializedFoo = SystemTextJsonSerializer.Deserialize<SystemTextJsonDecimalVo>(serializedLong);

            Assert.Equal(foo, deserializedFoo);
        }

        [Fact]
        public void CanSerializeToLong_WithBothJsonConverters()
        {
            var foo = BothJsonDecimalVo.From(123D);

            var serializedFoo1 = NewtonsoftJsonSerializer.SerializeObject(foo);
            var serializedLong1 = NewtonsoftJsonSerializer.SerializeObject(foo.Value);

            var serializedFoo2 = SystemTextJsonSerializer.Serialize(foo);
            var serializedLong2 = SystemTextJsonSerializer.Serialize(foo.Value);

            Assert.Equal(serializedFoo1, serializedLong1);
            Assert.Equal(serializedFoo2, serializedLong2);
        }

        [Fact]
        public void WhenNoJsonConverter_SystemTextJsonSerializesWithValueProperty()
        {
            var foo = NoJsonDecimalVo.From(123D);

            var serialized = SystemTextJsonSerializer.Serialize(foo);

            var expected = "{\"Value\":" + foo.Value + "}";

            Assert.Equal(expected, serialized);
        }

        [Fact]
        public void WhenNoJsonConverter_NewtonsoftSerializesWithoutValueProperty()
        {
            var foo = NoJsonDecimalVo.From(123D);

            var serialized = NewtonsoftJsonSerializer.SerializeObject(foo);

            var expected = $"\"{foo.Value}\"";

            Assert.Equal(expected, serialized);
        }

        [Fact]
        public void WhenNoTypeConverter_SerializesWithValueProperty()
        {
            var foo = NoConverterDecimalVo.From(123D);

            var newtonsoft = SystemTextJsonSerializer.Serialize(foo);
            var systemText = SystemTextJsonSerializer.Serialize(foo);

            var expected = "{\"Value\":" + foo.Value + "}";

            Assert.Equal(expected, newtonsoft);
            Assert.Equal(expected, systemText);
        }

        [Fact]
        public void WhenEfCoreValueConverterUsesValueConverter()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(connection)
                .Options;

            var original = new TestEntity { Id = EfCoreDecimalVo.From(123D) };
            using (var context = new TestDbContext(options))
            {
                context.Database.EnsureCreated();
                context.Entities.Add(original);
                context.SaveChanges();
            }
            using (var context = new TestDbContext(options))
            {
                var all = context.Entities.ToList();
                var retrieved = Assert.Single(all);
                Assert.Equal(original.Id, retrieved.Id);
            }
        }

        [Fact]
        public async Task WhenDapperValueConverterUsesValueConverter()
        {
            using var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            IEnumerable<DapperDecimalVo> results = await connection.QueryAsync<DapperDecimalVo>("SELECT 123");

            var value = Assert.Single(results);
            Assert.Equal(DapperDecimalVo.From(123D), value);
        }

        [Theory]
        [InlineData(123D)]
        [InlineData("123")]
        public void TypeConverter_CanConvertToAndFrom(object value)
        {
            var converter = TypeDescriptor.GetConverter(typeof(NoJsonDecimalVo));
            var id = converter.ConvertFrom(value);
            Assert.IsType<NoJsonDecimalVo>(id);
            Assert.Equal(NoJsonDecimalVo.From(123D), id);

            var reconverted = converter.ConvertTo(id, value.GetType());
            Assert.Equal(value, reconverted);
        }

        public class TestDbContext : DbContext
        {
            public DbSet<TestEntity> Entities { get; set; }

            public TestDbContext(DbContextOptions options) : base(options)
            {
            }

             protected override void OnModelCreating(ModelBuilder modelBuilder)
             {
                 modelBuilder
                     .Entity<TestEntity>(builder =>
                     {
                         builder
                             .Property(x => x.Id)
                             .HasConversion(new EfCoreDecimalVo.EfCoreValueConverter())
                             .ValueGeneratedNever();
                     });
             }
        }

        public class TestEntity
        {
            public EfCoreDecimalVo Id { get; set; }
        }

        public class EntityWithNullableId
        {
            public NewtonsoftJsonDecimalVo? Id { get; set; }
        }
    }
}