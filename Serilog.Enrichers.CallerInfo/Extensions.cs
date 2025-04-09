using System;
using System.Collections.Immutable;
using System.Diagnostics;

internal static class Extensions
{
	/// <summary>
	/// Determines whether the resolved method originates in one of the allowed assemblies.
	/// </summary>
	/// <param name="method">The method to look up.</param>
	/// <param name="allowedAssemblies">A HashSet of fully qualified assembly names to check against.</param>
	/// <returns>True if the method originates from one of the allowed assemblies, false otherwise.</returns>
	internal static bool IsInAllowedAssembly(this ResolvedMethod method, ImmutableHashSet<string> allowedAssemblies)
	{
		Type type = method.DeclaringType;
		if (type != null)
		{
			string assemblyName = type.Assembly.GetName().Name;
			return allowedAssemblies.Contains(assemblyName);
		}

		return false;
	}
}
