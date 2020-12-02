﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;

namespace Yunit
{
    /// <summary>
    /// Provides test data coming from YAML documents.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class YamlTestAttribute : Attribute, ITestAttribute
    {
        private static readonly char[] s_summaryTrimChars = { '#', ' ', '\t' };

        /// <summary>
        /// Gets or sets the glob pattern to search for files.
        /// </summary>
        public string Glob { get; set; }

        /// <summary>
        /// Gets or sets the method name to expand the test case.
        /// </summary>
        public string ExpandTest { get; set; }

        /// <summary>
        /// Gets or sets whether the source YAML fragment should be updated if a test returns an object.
        /// </summary>
        public bool UpdateSource { get; set; }

        public YamlTestAttribute(string glob = null) => Glob = glob;

        void ITestAttribute.DiscoverTests(string path, Action<TestData> report)
        {
            using var reader = File.OpenText(path);
            var ordinal = 0;
            var lineNumber = 0;
            var data = new TestData { LineNumber = 1 };
            var content = new StringBuilder();

            while (true)
            {
                var line = reader.ReadLine();

                lineNumber++;

                if (line is null || line == "---")
                {
                    if (content.Length > 0)
                    {
                        data.Ordinal = ++ordinal;
                        data.Content = content.ToString();
                        data.FilePath = path;
                        data.UpdateSource = UpdateSource;

                        report(data);

                        content.Length = 0;
                        data = new TestData { LineNumber = lineNumber + 1 };
                    }

                    if (line is null)
                    {
                        break;
                    }
                }
                else
                {
                    if (line.StartsWith("#"))
                    {
                        if (data.Summary is null)
                        {
                            data.Summary = line.Trim(s_summaryTrimChars);
                        }
                    }
                    else
                    {
                        if (content.Length == 0)
                        {
                            data.ContentStartLine = lineNumber;
                        }
                        content.Append(line);
                        content.AppendLine();
                    }
                }
            }
        }
    }
}
