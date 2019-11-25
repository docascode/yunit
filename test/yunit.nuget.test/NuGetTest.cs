using System;
using System.IO;

namespace yunit.NuGetTest
{
    public class NuGetTest
    {
        [MarkdownTest("~/test/yunit.nuget.test/**/*.md")]
        public void Foo(string filename)
        {
            File.WriteAllText(filename, "");
        }
    }
}
