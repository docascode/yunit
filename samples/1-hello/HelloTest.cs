using Microsoft.DocAsTest;
using Xunit;

public class HelloTestSpec
{
    public string Input;
    public string Output;
}

public class HelloTest
{
    [YamlTest("~/1-hello/**/*.yml")]
    [MarkdownTest("~/1-hello/**/*.md", FenceTip="test")]
    public void Hello(HelloTestSpec spec)
    {
        Assert.Equal(spec.Output, $"Hello {spec.Input}");
    }
}
