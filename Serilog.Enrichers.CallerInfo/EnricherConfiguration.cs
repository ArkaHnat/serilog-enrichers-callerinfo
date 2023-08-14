﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Serilog.Configuration;

namespace Serilog.Enrichers.CallerInfo
{
    public static class EnricherConfiguration
    {
        /// <summary>
        /// Enrich log events with information about the calling method.
        /// </summary>
        /// <param name="enrichmentConfiguration">The enrichment configuration.</param>
        /// <param name="includeFileInfo">Whether to include the caller's file information (file name, line number, column number).</param>
        /// <param name="allowedAssemblies">Which assemblies to consider when finding the calling method in the stack trace.</param>
        /// <param name="prefix">An optional prefix to prepend to all property values.</param>
        /// <returns>The modified logger configuration.</returns>
        public static LoggerConfiguration WithCallerInfo(
            this LoggerEnrichmentConfiguration enrichmentConfiguration,
            bool includeFileInfo,
            IEnumerable<string> allowedAssemblies,
            string prefix = "")
        {
            return enrichmentConfiguration.With(new Enricher(includeFileInfo, allowedAssemblies, prefix));
        }

        /// <summary>
        /// Enrich log events with information about the calling method. If using from appsettings.json, also provide the startingAssembly as it will otherwise be a Serilog assembly that is inferred as the calling assembly.
        /// </summary>
        /// <param name="enrichmentConfiguration">The enrichment configuration.</param>
        /// <param name="includeFileInfo">Whether to include the caller's file information (file name, line number, column number).</param>
        /// <param name="assemblyPrefix">The prefix of assemblies to allow when finding the calling method in the stack trace.</param>
        /// <param name="prefix">An optional prefix to prepend to all property values.</param>
        /// <param name="startingAssembly">The optional name of the assembly from which to discover other related ones with the given prefix. If not provided, the calling assembly of this method is used as the starting point.</param>
        /// <returns>The modified logger configuration.</returns>
        public static LoggerConfiguration WithCallerInfo(
            this LoggerEnrichmentConfiguration enrichmentConfiguration,
            bool includeFileInfo,
            string assemblyPrefix,
            string prefix = "",
            string startingAssembly = "")
        {
            var startAssembly = string.IsNullOrWhiteSpace(startingAssembly) ? Assembly.GetCallingAssembly() : Assembly.Load(startingAssembly);
            var referencedAssemblies = GetAssemblies(startAssembly, asm => asm.Name?.StartsWith(assemblyPrefix, StringComparison.OrdinalIgnoreCase) ?? false);
            return enrichmentConfiguration.WithCallerInfo(includeFileInfo, referencedAssemblies, prefix);
        }

        /// <summary>
        /// Find the assemblies that a starting Assembly references, filtering with some predicate.<br/>
        /// Adapted from <see href="https://stackoverflow.com/a/10253634/2102106"/>
        /// </summary>
        /// <param name="start">The starting assembly.</param>
        /// <param name="filter">A filtering predicate based on the AssemblyName</param>
        /// <returns>The list of referenced Assembly names</returns>
        private static IEnumerable<string> GetAssemblies(Assembly start, Func<AssemblyName, bool> filter)
        {
            var asmNames = new List<string>();
            var stack = new Stack<Assembly>();
            stack.Push(start);

            do
            {
                var asm = stack.Pop();
                if (!filter(asm.GetName()))
                {
                    continue;
                }

                asmNames.Add(asm.GetName().Name);
                foreach (var reference in asm.GetReferencedAssemblies())
                {
                    if (!filter(reference))
                    {
                        continue;
                    }

                    if (!asmNames.Contains(reference.Name))
                    {
                        stack.Push(Assembly.Load(reference));
                        asmNames.Add(reference.Name);
                    }
                }
            } while (stack.Count > 0);

            return asmNames;
        }
    }
}
