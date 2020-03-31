
1. Create a new class library

```cmd
dotnet new console
```

2. Add a pre-release NuGet feed to `NuGet.config`

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
    <packageSources>
        <add key="docfx-v3" value="https://www.myget.org/F/docfx-v3/api/v3/index.json" />
    </packageSources>
</configuration>
```

3. Add NuGet reference to `yunit`

```cmd
dotnet add package yunit --version 3.0.0-*
```

4. Create test logic

```csharp
public class HelloTestSpec
{
    public string Input;
    public string Output;
}

public class HelloTest
{
    [YamlTest("~/*.yml")]
    [MarkdownTest("~/README.md")]
    public void Hello(HelloTestSpec spec)
    {
        Assert.Equal(spec.Output, $"Hello {spec.Input}");
    }
}

```

5. Create YAML test data

```yml
# Hello YAML Test!  <-- hello.yml
input: YAML Test!
output: Hello YAML Test!
```

6. Create Markdown test data

``````````markdown
You can _also_ write test data in markdown files using YAML code blocks that starts with 6 or more backticks.

``````test
input: Markdown Test!
output: Hello Markdown Test!
``````
``````````
