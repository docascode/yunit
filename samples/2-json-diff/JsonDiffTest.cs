using Microsoft.DocAsTest;
using Xunit;

public class ShoppingCart
{
    public ShoppingCartLine[] Lines { get; set; }
}

public class ShoppingCartLine
{
    public string Name { get; set; }

    public string[] Tags { get; set; }

    public int Quatity { get; set; }
}

public class JsonDiffTest
{
    private readonly ShoppingCart _expectedCart = new ShoppingCart
    {
        Lines = new ShoppingCartLine[]
        {
            new ShoppingCartLine
            {
                Name = "Transformers: The Last Knight Bumblebee Voice Changer Mask",
                Tags = new[]
                {
                    "4.6 x 8.5 x 11 inches",
                    "1.1 pounds",
                    "B01N1834JJ",
                    "C1324",
                    "5 years and up",
                    "3 AA batteries required. (included)",
                },
                Quatity = 1,
            }
        }
    };

    private readonly ShoppingCart _actualCart = new ShoppingCart
    {
        Lines = new ShoppingCartLine[]
        {
            new ShoppingCartLine
            {
                Name = "Transformers: The Last Knight Bumblebee Voice Changer Mask",
                Tags = new[]
                {
                    "4.6 x 8.5 x 11 inches",
                    "1.1 pounds",
                    "B01N1834JJ",
                    "C1324",
                    "5 years and up",
                    "2 AA batteries required. (included)",
                },
                Quatity = 1,
            }
        },
    };

    [Fact]
    public void XunitEquals()
    {
        Assert.Equal(_expectedCart.Lines.Length, _actualCart.Lines.Length);

        for (var i = 0; i < _expectedCart.Lines.Length; i++)
        {
            Assert.Equal(_expectedCart.Lines[i].Name, _actualCart.Lines[i].Name);
            Assert.Equal(_expectedCart.Lines[i].Tags, _actualCart.Lines[i].Tags);
            Assert.Equal(_expectedCart.Lines[i].Quatity, _actualCart.Lines[i].Quatity);
        }
    }

    [Fact]
    public void JsonDiffEquals()
    {
        new JsonDiff().Verify(_expectedCart, _actualCart);
    }

    [Fact]
    public void JsonDiffPipelineEquals()
    {
        var jsonDiff = new JsonDiffBuilder()
            .UseAdditionalProperties()
            .UseWildcard()
            .Build();

        // Allow additional properties
        jsonDiff.Verify(
            expected: new { foo = "bar" }, 
            actual: new { foo = "bar", additionalProperty = "baz" });

        // Allow wildcard match
        jsonDiff.Verify(
            expected: new { foo = "this * sentence" },
            actual: new { foo = "this is a whole sentence" });
    }
}
