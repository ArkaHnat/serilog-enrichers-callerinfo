using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Serilog.Enrichers.CallerInfo
{
    public class Enricher : ILogEventEnricher
    {
        private readonly bool _includeFileInfo;
        private readonly int _filePathDepth;
        private readonly ImmutableHashSet<string> _allowedAssemblies;
        private readonly string _prefix;
		private readonly bool _includeMethodParameterTypes;
		private readonly bool _includeMethodParametersNames;
		private readonly bool _includeMethodParametersValues;

		private readonly bool _includeMethodReturnType;
		private readonly string _ignoredMethodTypeCreatedByAspectInjector = "__a$";
		public Enricher(bool includeFileInfo, 
            IEnumerable<string> allowedAssemblies, 
            string prefix = "", 
            int filePathDepth = 0, 
            bool includeMethodParamtereTypes = false,
			bool includeMethodParametersNames = false,
            bool includeMethodParametersValues = false,
			bool includeMethodReturnType = false)
		{
			_includeFileInfo = includeFileInfo;
			_filePathDepth = filePathDepth;
			_allowedAssemblies = allowedAssemblies.ToImmutableHashSet(equalityComparer: StringComparer.OrdinalIgnoreCase);
			_prefix = prefix ?? string.Empty;

			_includeMethodParameterTypes = includeMethodParamtereTypes;
			_includeMethodParametersNames = includeMethodParametersNames;
			_includeMethodParametersValues = includeMethodParametersValues;
            _includeMethodReturnType = includeMethodReturnType;
			_includeFileInfo = includeFileInfo;
            _filePathDepth = filePathDepth;
            _allowedAssemblies = allowedAssemblies.ToImmutableHashSet(equalityComparer: StringComparer.OrdinalIgnoreCase);
            _prefix = prefix ?? string.Empty;
        }

        /// <summary>
        /// Add information about the origin of the logged message, such as method, namespace, and file information (from debugging symbols).
        /// </summary>
        /// <param name="logEvent">The logged event.</param>
        /// <param name="propertyFactory">The property factory</param>
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var st = EnhancedStackTrace.Current();

            var frame = st.FirstOrDefault(x => x.HasMethod() && x.MethodInfo.IsInAllowedAssembly(_allowedAssemblies) && !x.MethodInfo.Name.Contains(_ignoredMethodTypeCreatedByAspectInjector));
            var method = frame?.MethodInfo.MethodBase;
            var type = method?.DeclaringType;

            if (!string.IsNullOrWhiteSpace(_prefix))
            {
                logEvent.AddPropertyIfAbsent(new LogEventProperty("Serilog.Enrichers.CallerInfo_Prefix", new ScalarValue(_prefix)));
            }

            if (type != null)
            {
                logEvent.AddPropertyIfAbsent(new LogEventProperty($"{_prefix}Method", new ScalarValue(method.Name)));
                logEvent.AddPropertyIfAbsent(new LogEventProperty($"{_prefix}Namespace", new ScalarValue(type.FullName)));

                if (_includeFileInfo)
                {
                    var fullFileName = frame.GetFileName();
                    var fileName = GetCleanFileName(fullFileName, _filePathDepth);
                    if (fileName != null)
                    {
                        logEvent.AddPropertyIfAbsent(new LogEventProperty($"{_prefix}SourceFile", new ScalarValue(fileName)));
                        logEvent.AddPropertyIfAbsent(new LogEventProperty($"{_prefix}LineNumber", new ScalarValue(frame.GetFileLineNumber())));
                        logEvent.AddPropertyIfAbsent(new LogEventProperty($"{_prefix}ColumnNumber", new ScalarValue(frame.GetFileColumnNumber())));
                    }
                }
				if (_includeMethodParameterTypes)
				{
					ParameterInfo[] methodParameters = method.GetParameters();
					StringBuilder sb = new StringBuilder();
                    foreach (ParameterInfo parameter in methodParameters)
                    {
                        sb.Append(parameter.ParameterType.Name);
                        sb.Append(" ");
                        if (_includeMethodParametersNames)
                        {
                            sb.Append(parameter.Name);
                        }

                        sb.Append(", ");

                        if (_includeMethodParametersValues)
						{
							var values = LogMethodContext.Get();

							if (values != null)
							{
								foreach (var kvp in values)
								{
									logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(kvp.Key, kvp.Value));
								}
							}
						}
					}

					logEvent.AddPropertyIfAbsent(new LogEventProperty($"{this._prefix}MethodParameters", new ScalarValue(sb.ToString())));
				}
                if (_includeMethodReturnType)
                {
					if (method is MethodInfo methodInfo)
					{
						logEvent.AddPropertyIfAbsent(new LogEventProperty($"{this._prefix}ReturnType", new ScalarValue(methodInfo.ReturnType.FullName)));
					}
					else
					{
						logEvent.AddPropertyIfAbsent(new LogEventProperty($"{this._prefix}ReturnType", new ScalarValue("Unknown")));
					}
				}
			}
        }

        /// <summary>
        /// Gets a clean file name from a full file path, optionally including a specified number of parent directories.
        /// </summary>
        /// <param name="fullFileName">The full file path.</param>
        /// <param name="depth">The number of parent directories to include in the file name. If zero or negative, the full path is returned. If larger than the actual depth of the file, the full path is also returned.</param>
        /// <returns>A string representing the clean file name, or null if the full file path is null or whitespace.</returns>
        private static string GetCleanFileName(string fullFileName, int depth=0)
        {
            if (string.IsNullOrWhiteSpace(fullFileName))
            {
                return null;
            }

            if (depth <= 0) // if the depth is zero or negative, return the full path
            {
                return fullFileName;
            }

            var fileName = Path.GetFileName(fullFileName); // get the file name
            var dirName = Path.GetDirectoryName(fullFileName); // get the directory name

            if (string.IsNullOrWhiteSpace(dirName))
            {
                return fileName;
            }

            var pathSegments = new List<string> { fileName }; // create a list to store the path segments and add the file name to the list
            for (var i = 0; i < depth - 1; i++) // loop until the desired depth is reached or there are no more parent directories
            {
                var parentDirName = Path.GetFileName(dirName); // get the parent directory name
                if (string.IsNullOrWhiteSpace(parentDirName)) // if there is no parent directory, break the loop
                {
                    break;
                }

                pathSegments.Add(parentDirName); // add the parent directory name to the list
                dirName = Path.GetDirectoryName(dirName); // get the grandparent directory name
            }

            pathSegments.Reverse(); // reverse the order of the list to get the correct path order
            return Path.Combine(pathSegments.ToArray()); // join the path segments with the appropriate path separator and return the result
        }
    }

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
            var type = method.DeclaringType;
            if (type != null)
            {
                var assemblyName = type.Assembly.GetName().Name;
                return allowedAssemblies.Contains(assemblyName);
            }

            return false;
        }
    }
}
