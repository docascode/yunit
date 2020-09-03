// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using GlobExpressions;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Newtonsoft.Json;

namespace Yunit
{
#pragma warning disable CA1812 // avoid uninstantiated internal classes

    // See https://github.com/microsoft/vstest-docs/blob/master/RFCs/0004-Adapter-Extensibility.md
    // for more details on how to write a vstest adapter
    [FileExtension(".dll")]
    [FileExtension(".exe")]
    [DefaultExecutorUri("executor://yunit")]
    [ExtensionUri("executor://yunit")]
    [Category("managed")]
    internal class TestAdapter : ITestDiscoverer, ITestExecutor
    {
        private static readonly object s_lock = new object();

        private static readonly Lazy<string> s_repositoryRoot = new Lazy<string>(FindRepositoryPath);

        private static readonly JsonSerializer s_jsonSerializer = new JsonSerializer { NullValueHandling = NullValueHandling.Ignore };

        private static readonly ConcurrentDictionary<(string, string), (Type, MethodInfo)> s_methodInfo
                          = new ConcurrentDictionary<(string, string), (Type, MethodInfo)>();

        private static readonly TestProperty s_ordinalProperty = TestProperty.Register(
            "yunit.Ordinal", "Ordinal", typeof(int), TestPropertyAttributes.Hidden, typeof(TestCase));

        private static readonly TestProperty s_MatrixProperty = TestProperty.Register(
            "yunit.Matrix", "Matrix", typeof(string), TestPropertyAttributes.Hidden, typeof(TestCase));

        private static readonly TestProperty s_attributeIndexProperty = TestProperty.Register(
            "yunit.AttributeIndex", "AttributeIndex", typeof(int), TestPropertyAttributes.Hidden, typeof(TestCase));

        private static readonly string[] s_filteringProperties = { "DisplayName", "Summary", "FullyQualifiedName" };

        private volatile bool _canceled = false;

        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            DiscoverTests(
                sources,
                testCase => discoverySink.SendTestCase(testCase),
                message => logger.SendMessage(TestMessageLevel.Warning, message));
        }

        public void Cancel() => _canceled = true;

        public void DiscoverTests(IEnumerable<string> sources, Action<TestCase> sendTestCase, Action<string> log)
        {
            // Looking for public methods in public types
            foreach (var source in sources)
            {
                var assembly = Assembly.LoadFrom(source);
                var sourcePath = Path.GetDirectoryName(Path.GetFullPath(source));

                foreach (var type in assembly.GetExportedTypes())
                {
                    foreach (var method in type.GetRuntimeMethods())
                    {
                        var attributes = method.GetCustomAttributes(typeof(ITestAttribute), inherit: false);

                        for (var i = 0; i < attributes.Length; i++)
                        {
                            var attribute = (ITestAttribute)attributes[i];
                            var (expandMethodType, expandMethod) = string.IsNullOrEmpty(attribute.ExpandTest)
                                ? default
                                : GetMethodInfo(source, $"{type.FullName}.{attribute.ExpandTest}");

                            DiscoverTests(attribute, source, ExpandTest, log);

                            void ExpandTest(TestData data)
                            {
                                if (expandMethodType is null || expandMethod is null)
                                {
                                    sendTestCase(CreateTestCase(data, type, method, source, i));
                                }
                                else
                                {
                                    var metrices = InvokeMethod(expandMethodType, expandMethod, data) as IEnumerable<string>;
                                    if (metrices != null)
                                    {
                                        foreach (var metrix in metrices)
                                        {
                                            var metrixData = data.Clone();
                                            data.Matrix = metrix;
                                            sendTestCase(CreateTestCase(data, type, method, source, i));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            var testRuns = new ConcurrentBag<Task>();
            Parallel.ForEach(tests, test => testRuns.Add(RunTest(frameworkHandle, test)));
            Task.WhenAll(testRuns).GetAwaiter().GetResult();
        }

        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            var testRuns = new ConcurrentBag<Task>();

            var filterExpression = runContext.GetTestCaseFilter(s_filteringProperties, null);

            new TestAdapter().DiscoverTests(
                sources,
                test =>
                {
                    if (filterExpression == null || filterExpression.MatchTestCase(test, name => GetPropertyValue(test, name)))
                    {
                        testRuns.Add(RunTest(frameworkHandle, test));
                    }
                },
                message => frameworkHandle.SendMessage(TestMessageLevel.Warning, message));

            Task.WhenAll(testRuns).GetAwaiter().GetResult();
        }

        private async Task RunTest(ITestExecutionRecorder log, TestCase test)
        {
            if (_canceled)
            {
                return;
            }

            var result = new TestResult(test);

            try
            {
                lock (s_lock)
                {
                    log.RecordStart(test);
                }
                result.StartTime = DateTime.UtcNow;

                await RunTest(test);

                result.Outcome = TestOutcome.Passed;
            }
            catch (TestNotFoundException)
            {
                result.Outcome = TestOutcome.NotFound;
            }
            catch (TestSkippedException ex)
            {
                result.ErrorMessage = ex.Reason;
                result.Outcome = TestOutcome.Skipped;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                result.ErrorStackTrace = ex.StackTrace;
                result.Outcome = TestOutcome.Failed;
            }
            finally
            {
                result.EndTime = DateTime.UtcNow;

                lock (s_lock)
                {
                    log.RecordResult(result);
                    log.RecordEnd(test, result.Outcome);
                }
            }
        }

        private object GetPropertyValue(TestCase test, string name)
        {
            if (string.Equals(name, "FullyQualifiedName", StringComparison.OrdinalIgnoreCase))
                return test.FullyQualifiedName;
            if (string.Equals(name, "DisplayName", StringComparison.OrdinalIgnoreCase))
                return test.DisplayName;
            return null;
        }

        private static TestCase CreateTestCase(TestData data, Type type, MethodInfo method, string source, int attributeIndex)
        {
            var displayName = string.IsNullOrEmpty(data.Matrix)
                ? $"{Path.GetFileName(data.FilePath)}/{data.Ordinal:D2}: {data.Summary}"
                : $"{Path.GetFileName(data.FilePath)}/{data.Ordinal:D2}: [{data.Matrix}] {data.Summary}";

            var result = new TestCase
            {
                LocalExtensionData = data,
                FullyQualifiedName = $"{type.FullName}.{method.Name}({Path.GetFileName(data.FilePath).Replace('.', '-')}-{data.Ordinal})",
                Source = source,
                ExecutorUri = new Uri("executor://yunit"),
                Id = CreateGuid($"{attributeIndex}/{displayName}"),
                DisplayName = displayName,
                CodeFilePath = data.FilePath,
                LineNumber = data.LineNumber,
            };

            result.SetPropertyValue(s_ordinalProperty, data.Ordinal);
            result.SetPropertyValue(s_MatrixProperty, data.Matrix);
            result.SetPropertyValue(s_attributeIndexProperty, attributeIndex);

            return result;
        }

        private static Guid CreateGuid(string displayName)
        {
#pragma warning disable CA5350 // Do Not Use Weak Cryptographic Algorithms
            using var md5 = SHA1.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(displayName));
            var buffer = new byte[16];
            Array.Copy(hash, 0, buffer, 0, 16);
            return new Guid(buffer);
#pragma warning restore CA5350 // Do Not Use Weak Cryptographic Algorithms
        }

        private static void DiscoverTests(ITestAttribute attribute, string sourcePath, Action<TestData> report, Action<string> log)
        {
            var glob = attribute.Glob ?? "**";
            if (glob.StartsWith("~/") || glob.StartsWith("~\\"))
            {
                glob = glob.Substring(2);
                sourcePath = s_repositoryRoot.Value ?? sourcePath;
            }

            var files = Glob.Files(sourcePath, glob, GlobOptions.CaseInsensitive).ToArray();
            if (files.Length == 0)
            {
                log($"No file is found in directory '{sourcePath}' using glob pattern '{glob}'");
            }

            Parallel.ForEach(files, file => attribute.DiscoverTests(Path.Combine(sourcePath, file), report));
        }

        private Task RunTest(TestCase test)
        {
            if (test.DisplayName.IndexOf("[skip]", 0, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                throw new TestSkippedException("Marked as [skip]");
            }

            var (type, method) = GetMethodInfo(test.Source, test.FullyQualifiedName);

            var data = test.LocalExtensionData as TestData;
            if (data is null)
            {
                // LocalExtensionData is lost when running a selected test from Visual Studio test explorer.
                var ordinal = test.GetPropertyValue<int?>(s_ordinalProperty, null) ?? throw new TestNotFoundException(s_ordinalProperty.ToString());
                var attributeIndex = test.GetPropertyValue<int?>(s_attributeIndexProperty, null) ?? throw new TestNotFoundException(s_attributeIndexProperty.ToString());
                var metrix = test.GetPropertyValue<string>(s_MatrixProperty, null) ?? throw new TestNotFoundException(s_MatrixProperty.ToString());
                var attributes = method.GetCustomAttributes(typeof(ITestAttribute), inherit: false);

                if (attributeIndex >= attributes.Length)
                {
                    throw new TestNotFoundException("attributeIndex >= attributes.Length");
                }

                ((ITestAttribute)attributes[attributeIndex]).DiscoverTests(test.CodeFilePath, currentData =>
                {
                    if (currentData.Ordinal == ordinal)
                    {
                        data = currentData;
                    }
                });

                if (data != null)
                {
                    data.Matrix = metrix;
                }
            }

            if (data is null)
            {
                throw new TestNotFoundException();
            }

            return InvokeMethod(type, method, data) as Task ?? Task.CompletedTask;
        }

        private static object InvokeMethod(Type type, MethodInfo method, TestData data)
        {
            var instance = method.IsStatic ? null : Activator.CreateInstance(type);
            var parameters = method.GetParameters();
            var args = new object[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
            {
                args[i] = parameters[i].ParameterType == typeof(TestData)
                    ? data
                    : YamlUtility.ToJToken(data.Content).ToObject(parameters[i].ParameterType, s_jsonSerializer);
            }

            try
            {
                return method.Invoke(instance, args);
            }
            catch (TargetInvocationException tie)
            {
                ExceptionDispatchInfo.Capture(tie.InnerException).Throw();
                throw;
            }
        }

        private static (Type type, MethodInfo method) GetMethodInfo(string source, string fullyQualifiedName)
        {
            var i = fullyQualifiedName.IndexOf('(');
            if (i >= 0)
            {
                fullyQualifiedName = fullyQualifiedName.Substring(0, i);
            }
            return s_methodInfo.GetOrAdd((source, fullyQualifiedName), GetMethodInfoCore);
        }

        private static (Type type, MethodInfo method) GetMethodInfoCore((string, string) key)
        {
            try
            {
                var (source, fullyQualifiedName) = key;
                var assembly = Assembly.LoadFrom(source);
                var methodNameIndex = fullyQualifiedName.LastIndexOf(".");
                var typeName = fullyQualifiedName.Substring(0, methodNameIndex);
                var methodName = fullyQualifiedName.Substring(methodNameIndex + 1);

                var type = assembly.GetType(typeName);
                var method = type.GetMethod(methodName);

                return (type, method);
            }
            catch
            {
                throw new TestNotFoundException();
            }
        }

        private static string FindRepositoryPath()
        {
            var repo = Directory.GetCurrentDirectory();
            while (!string.IsNullOrEmpty(repo))
            {
                var gitPath = Path.Combine(repo, ".git");
                if (Directory.Exists(gitPath) || File.Exists(gitPath))
                {
                    return repo;
                }

                repo = Path.GetDirectoryName(repo);
            }

            return string.IsNullOrEmpty(repo) ? null : repo;
        }
    }
}
