using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Reinforced.Typings.Cli
{
    /// <summary>
    /// Class for CLI typescript typings utility
    /// </summary>
    public static class Bootstrapper
    {
        private static ExporterConsoleParameters _parameters;
        private static Dictionary<string, string> _referencesCache = new Dictionary<string, string>();
        private static string _lastAssemblyLocalDir;
        private static int _totalLoadedAssemblies;


        /// <summary>
        /// Usage: rtcli.exe Assembly.dll [Assembly2.dll Assembly3.dll ... etc] file.ts
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            Console.WriteLine("Reinforced.Typings CLI generator � 2015 Pavel B. Novikov");

            if (args.Length == 0)
            {
                PrintHelp();
                return;
            }
            _parameters = ExtractParametersFromArgs(args);
            if (_parameters == null)
            {
                Console.WriteLine("No valid parameters found. Exiting.");
                Environment.Exit(0);
            }
            var settings = InstantiateExportSettings();
            
            TsExporter exporter = new TsExporter(settings);
            exporter.Export();

            
            Console.WriteLine("Reinforced.Typings generation finished with total {0} assemblies loaded",_totalLoadedAssemblies);
            
            Console.WriteLine("Please build CompileTypeScript task to update javascript sources");
            
        }

        public static ExportSettings InstantiateExportSettings()
        {
            ExportSettings settings = new ExportSettings
            {
                ExportPureTypings = _parameters.ExportPureTypings,
                Hierarchical = _parameters.Hierarchy,
                TargetDirectory = _parameters.TargetDirectory,
                TargetFile = _parameters.TargetFile,
                WriteWarningComment = _parameters.WriteWarningComment,
                SourceAssemblies = GetAssembliesFromArgs(),
                RootNamespace = _parameters.RootNamespace,
                CamelCaseForMethods = _parameters.CamelCaseForMethods,
                CamelCaseForProperties = _parameters.CamelCaseForProperties,
                DocumentationFilePath = _parameters.DocumentationFilePath,
                GenerateDocumentation = _parameters.GenerateDocumentation
            };
            return settings;
        }

        public static void BuildReferencesCache()
        {
            _referencesCache.Clear();
            foreach (var reference in _parameters.References)
            {
                _referencesCache.Add(Path.GetFileName(reference), reference);
            }
        }

        public static string LookupAssemblyPath(string assemblyNameOrFullPath, bool storeIfFullName = true)
        {
            if (!assemblyNameOrFullPath.EndsWith(".dll")) assemblyNameOrFullPath = assemblyNameOrFullPath + ".dll";
            //Console.WriteLine("Looking up for assembly {0}",assemblyNameOrFullPath);

            if (Path.IsPathRooted(assemblyNameOrFullPath))
            {
                if (storeIfFullName)
                {
                    _lastAssemblyLocalDir = Path.GetDirectoryName(assemblyNameOrFullPath) + "\\";
                }
              //  Console.WriteLine("Already have full path to assembly {0}",assemblyNameOrFullPath);
                return assemblyNameOrFullPath;
            }

            if (_referencesCache.ContainsKey(assemblyNameOrFullPath))
            {
                var rf = _referencesCache[assemblyNameOrFullPath];
                //Console.WriteLine("Assembly {0} found at {1}", assemblyNameOrFullPath, rf);
                return rf;
            }
            var p = Path.Combine(_lastAssemblyLocalDir, assemblyNameOrFullPath);
            if (File.Exists(p))
            {
                //Console.WriteLine("Assembly {0} found at {1}", assemblyNameOrFullPath, p);
                return p;
            }

            Console.WriteLine("Warning! Probably assembly {0} not found", assemblyNameOrFullPath, p);
            return assemblyNameOrFullPath;
        }

        public static Assembly[] GetAssembliesFromArgs()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;
            BuildReferencesCache();

            List<Assembly> assemblies = new List<Assembly>();

            for (int i = 0; i < _parameters.SourceAssemblies.Length; i++)
            {
                var assemblyPath = _parameters.SourceAssemblies[i];
                var path = LookupAssemblyPath(assemblyPath);
                var a = Assembly.LoadFrom(path);
                _totalLoadedAssemblies++;
                
                assemblies.Add(a);
            }

            return assemblies.ToArray();
        }

        public static Assembly CurrentDomainOnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (args.Name.StartsWith("Reinforced.Typings.XmlSerializers")) return Assembly.GetExecutingAssembly();
            AssemblyName nm = new AssemblyName(args.Name);
            string path = LookupAssemblyPath(nm.Name + ".dll",false);
            Assembly a = Assembly.LoadFrom(path);
            _totalLoadedAssemblies++;
            Console.WriteLine("{0} additionally resolved", nm);
            return a;
        }

        public static void PrintHelp()
        {
            Console.WriteLine("Available parameters:");
            Console.WriteLine();

            var t = typeof(ExporterConsoleParameters);
            var props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var propertyInfo in props)
            {
                var attr = propertyInfo.GetCustomAttribute<ConsoleHelpAttribute>();
                if (attr != null)
                {
                    var req = attr.RequiredType;
                    string requiredText = null;
                    switch (req)
                    {
                        case Required.NotReuired:
                            requiredText = "(not requred)";
                            break;
                        case Required.Reuired:
                            requiredText = "(requred)";
                            break;
                        case Required.Partially:
                            requiredText = "(sometimes requred)";
                            break;
                    }
                    Console.WriteLine(propertyInfo.Name + " " + requiredText);

                    var s = "\t" + attr.HelpText.Replace("\r",String.Empty).Replace("\n", "\n\t").Replace("\n", Environment.NewLine);
                    Console.WriteLine(s);

                    Console.WriteLine();
                }
            }
        }

        public static ExporterConsoleParameters ExtractParametersFromArgs(string[] args)
        {
            var t = typeof(ExporterConsoleParameters);
            var instance = new ExporterConsoleParameters();
            foreach (var s in args)
            {
                var trimmed = s.TrimStart('-');
                var kv = trimmed.Split('=');
                if (kv.Length != 2)
                {
                    Console.WriteLine("Unrecognized parameter: {0}", s);
                    continue;
                }

                var key = kv[0].Trim();
                var value = kv[1].Trim().Trim('"');

                var prop = t.GetProperty(key);
                if (prop == null)
                {
                    Console.WriteLine("Unrecognized parameter: {0}", key);
                    continue;
                }

                if (prop.PropertyType == typeof(bool))
                {
                    bool parsedValue = Boolean.Parse(value);
                    prop.SetValue(instance, parsedValue);
                    continue;
                }

                if (prop.PropertyType == typeof(string))
                {
                    prop.SetValue(instance, value);
                    continue;
                }

                if (prop.PropertyType == typeof(string[]))
                {
                    var parsedValue = value.Split(';');
                    prop.SetValue(instance, parsedValue);
                    continue;
                }

                Console.WriteLine("Cannot parse parameter for source property {0}", key);
            }

            try
            {
                instance.Validate();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Parameters validation error: {0}", ex.Message);
                PrintHelp();
                return null;
            }
            return instance;
        }
    }
}