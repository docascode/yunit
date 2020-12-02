using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Yunit.NuGetTest
{
    public class NuGetTest
    {
        [MarkdownTest("~/test/yunit.nuget.test/**/*.md")]
        public void Foo(string filename)
        {
            File.WriteAllText(filename, "");
        }

        [MarkdownTest("~/test/yunit.nuget.test/**/*.md")]
        public void SkipSync(string filename)
        {
            throw new TestSkippedException();
        }

        [MarkdownTest("~/test/yunit.nuget.test/**/*.md")]
        public async Task SkipAsync(TestData data, string filename)
        {
            await Task.Delay(1);
            throw new TestSkippedException();
        }

        [MarkdownTest("~/test/yunit.nuget.test/**/*.md", ExpandTest = "ExpandTest")]
        public void ExpandTestMethod(TestData data, string filename)
        {
            throw new TestSkippedException(data.Matrix);
        }

        public string[] ExpandTest(string filename)
        {
            return new [] { "", "metrix 1", "metrix 2" };
        }

        [YamlTest("~/test/yunit.nuget.test/**/*.yml", UpdateSource = true)]
        public Task<JToken> TestUpdateSource(JToken input)
        {
            return Task.FromResult(input);
        }
    }
}
