using Newtonsoft.Json.Linq;
using Serilog.Sinks.InMemory;
using Serilog.Sinks.InMemory.Assertions;

namespace Serilog.Enrichers.CallerInfo.Tests
{
    public class CallerInfoTests
    {
        [Fact]
        public void CanManuallySpecifyAssemblies()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithCallerInfo(includeFileInfo: true, new List<string> { "Serilog.Enrichers.CallerInfo.Tests" })
                .WriteTo.InMemory()
                .CreateLogger();

            Log.Error(new Exception("Error occurred!"), "Test log message");
            InMemorySink.Instance.Should()
                .HaveMessage("Test log message")
                .Appearing().Once()
                .WithProperty("Method").WithValue(nameof(CanManuallySpecifyAssemblies))
                .And.WithProperty("Namespace").WithValue("Serilog.Enrichers.CallerInfo.Tests.CallerInfoTests");
        }
		[Theory]
        [LogAspect]
		[InlineData("TestValue")]
		public void LogAspectShouldNotBreakLogging(string test)
		{
			Log.Logger = new LoggerConfiguration()
				.Enrich.WithCallerInfo(includeFileInfo: true, new List<string> { "Serilog.Enrichers.CallerInfo.Tests" })
				.WriteTo.InMemory()
				.CreateLogger();

			Log.Error(new Exception("Error occurred!"), "Test log message");
			InMemorySink.Instance.Should()
				.HaveMessage("Test log message")
				.Appearing().Once()
				.WithProperty("Method").WithValue(nameof(LogAspectShouldNotBreakLogging))
				.And.WithProperty("Namespace").WithValue("Serilog.Enrichers.CallerInfo.Tests.CallerInfoTests");
		}
		public static IEnumerable<object[]> GetUserChoiceTestData1()
		{
			yield return new object[] { new DemoClass() { Value = "DemoClassValue" }, new DemoClass() { Value = "DemoClassValue" } };
		}

		[Theory]
		[LogAspect]
		[MemberData(nameof(GetUserChoiceTestData1))]
		public void PushedPropertiesShouldContainMethodParametersValues(DemoClass demoClass1, DemoClass demoClass2)
		{
			Log.Logger = new LoggerConfiguration()
				.Enrich.WithCallerInfo(includeFileInfo: true, new List<string> { "Serilog.Enrichers.CallerInfo.Tests" }, includeMethodParametersValues: true, includeMethodParamtereTypes: true, includeMethodParametersNames: true)
				.WriteTo.InMemory()
				.CreateLogger();

			Log.Error(new Exception("Error occurred!"), "Test log message");
            InMemorySink.Instance.Should()
                .HaveMessage("Test log message")
                .Appearing().Once()
                .WithProperty("Method").WithValue(nameof(PushedPropertiesShouldContainMethodParametersValues))
                .And.WithProperty("Namespace").WithValue("Serilog.Enrichers.CallerInfo.Tests.CallerInfoTests")
                .And.WithProperty("MethodParametersValues").WithValue("demoClass1: [{\"Value\":\"DemoClassValue\"}];demoClass2: [{\"Value\":\"DemoClassValue\"}];");
		}

		[Theory]
		[LogAspect]
		[MemberData(nameof(GetUserChoiceTestData1))]
		public void PushedPropertiesShouldContainReturnMethodType(DemoClass demoClass1, DemoClass demoClass2)
		{
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithCallerInfo(includeFileInfo: true, new List<string> { "Serilog.Enrichers.CallerInfo.Tests" }, includeMethodReturnType: true)
				.WriteTo.InMemory()
				.CreateLogger();

			Log.Error(new Exception("Error occurred!"), "Test log message");
			InMemorySink.Instance.Should()
				.HaveMessage("Test log message")
				.Appearing().Once()
				.WithProperty("Method").WithValue(nameof(PushedPropertiesShouldContainReturnMethodType))
				.And.WithProperty("Namespace").WithValue("Serilog.Enrichers.CallerInfo.Tests.CallerInfoTests")
				.And.WithProperty("ReturnType").WithValue("System.Void");
		}
         
		[Fact]
        public void CanAutodetectAssemblies()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithCallerInfo(includeFileInfo: true, "Serilog.Enrichers.CallerInfo")
                .WriteTo.InMemory()
                .CreateLogger();
            
            Log.Error(new Exception("Error occurred!"), "Test log message");
            InMemorySink.Instance.Should()
                .HaveMessage("Test log message")
                .Appearing().Once()
                .WithProperty("Method").WithValue(nameof(CanAutodetectAssemblies))
                .And.WithProperty("Namespace").WithValue("Serilog.Enrichers.CallerInfo.Tests.CallerInfoTests");
        }

        [Fact]
        public void CanAutodetectFromManualStartingAssembly()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithCallerInfo(includeFileInfo: true, "Serilog.Enrichers.CallerInfo", startingAssembly: "Serilog.Enrichers.CallerInfo.Tests")
                .WriteTo.InMemory()
                .CreateLogger();

            Log.Error(new Exception("Error occurred!"), "Test log message");
            InMemorySink.Instance.Should()
                .HaveMessage("Test log message")
                .Appearing().Once()
                .WithProperty("Method").WithValue(nameof(CanAutodetectFromManualStartingAssembly))
                .And.WithProperty("Namespace").WithValue("Serilog.Enrichers.CallerInfo.Tests.CallerInfoTests");
        }

        [Fact]
        public void CanExcludeAssemblies()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithCallerInfo(includeFileInfo: true, "Serilog.Enrichers.CallerInfo", excludedPrefixes: new List<string> { "Serilog.Enrichers.CallerInfo.Tests" })
                .WriteTo.InMemory()
                .CreateLogger();

            Log.Error(new Exception("Error occurred!"), "Test log message");
            InMemorySink.Instance.Should()
                .HaveMessage("Test log message")
                .Appearing().Once()
                .Match(e => !e.Properties.ContainsKey("Method"), "Is excluded")
                .And.Match(e => !e.Properties.ContainsKey("Namespace"), "Is excluded");
        }

        [Fact]
        public void CanRestrictFilePathDepth()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithCallerInfo(includeFileInfo: true, "Serilog.Enrichers.CallerInfo", filePathDepth: 2)
                .WriteTo.InMemory()
                .CreateLogger();

            Log.Error(new Exception("Error occurred!"), "Test log message");
            InMemorySink.Instance.Should()
                .HaveMessage("Test log message")
                .Appearing().Once()
                .WithProperty("SourceFile").WithValue(Path.Combine("Serilog.Enrichers.CallerInfo.Tests", "CallerInfoTests.cs"));
        }

        [Fact]
        public void LocalFunctionsAreNotIncluded()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithCallerInfo(includeFileInfo: true, "Serilog.Enrichers.CallerInfo")
                .WriteTo.InMemory()
                .CreateLogger();

            static void LocalFunction(string arg)
            {
                Log.Information(arg);
            }

            LocalFunction("i like turtles");

            InMemorySink.Instance.Should()
                .HaveMessage("i like turtles")
                .Appearing().Once()
                .WithProperty("Method").WithValue(nameof(LocalFunctionsAreNotIncluded))
                .And.WithProperty("Namespace").WithValue("Serilog.Enrichers.CallerInfo.Tests.CallerInfoTests");
        }

        [Fact]
        public void PrivateFunctionShouldBeAvailable()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithCallerInfo(includeFileInfo: true, "Serilog.Enrichers.CallerInfo")
                .WriteTo.InMemory()
                .CreateLogger();

            PrivateLocalFunction("i like turtles");

            InMemorySink.Instance.Should()
                .HaveMessage("i like turtles")
                .Appearing().Once()
                .WithProperty("Method").WithValue(nameof(PrivateLocalFunction))
                .And.WithProperty("Namespace").WithValue("Serilog.Enrichers.CallerInfo.Tests.CallerInfoTests");
        }

        private static void PrivateLocalFunction(string arg)
        {
            Log.Information(arg);
        }
        public class DemoClass
        {
            public string Value { get; set; }
        }
    }
}