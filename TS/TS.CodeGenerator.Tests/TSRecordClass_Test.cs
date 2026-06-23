using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace TS.CodeGenerator.tests
{
    public record SimpleRecord(string Name, int Age);

    public record RecordWithComplexProps(string Name, List<int> Numbers, DateTime? OptionalDate);

    public record DerivedRecord(string Extra) : SimpleRecord("default", 0);

    public record GenericRecord<T>(T Value, string Label);

    public class TSRecordClass_Test
    {
        private readonly ITestOutputHelper _output;

        public TSRecordClass_Test(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void DiagnoseRecordReflection()
        {
            var ti = typeof(SimpleRecord).GetTypeInfo();

            _output.WriteLine("=== Properties ===");
            foreach (var p in ti.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
                _output.WriteLine($"  {p.Name}: {p.PropertyType}");

            _output.WriteLine("=== All Public Properties (including inherited) ===");
            foreach (var p in ti.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                _output.WriteLine($"  {p.Name}: {p.PropertyType} (Declaring: {p.DeclaringType})");

            _output.WriteLine("=== Methods (DeclaredOnly, not special) ===");
            foreach (var m in ti.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly).Where(x => !x.IsSpecialName))
                _output.WriteLine($"  {m.Name}({string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name))}) -> {m.ReturnType.Name}  [CompilerGenerated={m.GetCustomAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>() != null}]");

            _output.WriteLine($"=== IsInterface: {ti.IsInterface} ===");
            _output.WriteLine($"=== BaseType: {ti.BaseType} ===");
            _output.WriteLine($"=== Interfaces: {string.Join(", ", ti.GetInterfaces().Select(i => i.Name))} ===");
        }

        [Fact]
        public void TestSimpleRecordGeneratesInterface()
        {
            var gen = new TSGenerator(typeof(SimpleRecord).GetTypeInfo().Assembly);
            gen.AddInterface(typeof(SimpleRecord));

            var res = gen.ToTSString();

            _output.WriteLine(res);

            Assert.Contains("interface", res);
            Assert.Contains("Name:", res);
            Assert.Contains("Age:", res);
        }

        [Fact]
        public void TestSimpleRecordNoCompilerGeneratedMethods()
        {
            var gen = new TSInterface(typeof(SimpleRecord), (t) =>
            {
                if (Settings.StartingTypeMap.ContainsKey(t))
                    return Settings.StartingTypeMap[t];
                return Types.Any;
            });
            gen.Initialize();

            var res = gen.ToTSString();
            _output.WriteLine(res);

            Assert.DoesNotContain("<Clone>", res);
            Assert.DoesNotContain("Deconstruct", res);
            Assert.DoesNotContain("PrintMembers", res);
        }

        [Fact]
        public void TestRecordWithComplexProps()
        {
            var gen = new TSGenerator(typeof(RecordWithComplexProps).GetTypeInfo().Assembly);
            gen.AddInterface(typeof(RecordWithComplexProps));

            var res = gen.ToTSString();

            _output.WriteLine(res);

            Assert.Contains("Name:", res);
            Assert.Contains("Numbers:", res);
            Assert.Contains("OptionalDate", res);
        }

        [Fact]
        public void TestGenericRecord()
        {
            var gen = new TSInterface(typeof(GenericRecord<>), (t) =>
            {
                if (t.IsGenericParameter) return t.Name;
                if (Settings.StartingTypeMap.ContainsKey(t))
                    return Settings.StartingTypeMap[t];
                return Types.Any;
            });
            gen.Initialize();

            var res = gen.ToTSString();
            _output.WriteLine(res);

            Assert.Contains("interface", res);
            Assert.Contains("<T>", res);
            Assert.Contains("Value: T", res);
            Assert.Contains("Label:", res);
        }
    }
}
