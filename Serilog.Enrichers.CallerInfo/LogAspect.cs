using AspectInjector.Broker;
using System;
using System.Reflection;
using System.Text;
using System.Text.Json;

[Aspect(Scope.Global)]
[Injection(typeof(LogAspect))]
public class LogAspect : Attribute
{
	private readonly string propertyName = "MethodParametersValues";
	[Advice(Kind.Around, Targets = Target.Method)]
	public object LogParameters(
		[Argument(Source.Instance)] object instance,
		[Argument(Source.Metadata)] MethodBase method,
		[Argument(Source.Arguments)] object[] args,
		[Argument(Source.Target)] Func<object[], object> target)
	{
		ParameterInfo[] paramInfos = method.GetParameters();

		StringBuilder sb = new StringBuilder();
		int i = 0;
		foreach (ParameterInfo param in paramInfos)
		{
			_ = sb.Append($@"{param.Name}: [{JsonSerializer.Serialize(args[i])}];");
		}

		LogMethodContext.Add(this.propertyName, sb.ToString());

		try
		{
			return target(args);
		}
		finally
		{
			LogMethodContext.Clear(); // Important to avoid leaking across calls
		}
	}
}
