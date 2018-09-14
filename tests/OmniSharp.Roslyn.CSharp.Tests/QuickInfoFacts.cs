using System.Threading.Tasks;
using OmniSharp.Models.V2.QuickInfo;
using OmniSharp.Roslyn.CSharp.Services.QuickInfo;
using TestUtility;
using Xunit;
using Xunit.Abstractions;

namespace OmniSharp.Roslyn.CSharp.Tests
{
    public class QuickInfoFacts : AbstractSingleRequestHandlerTestFixture<QuickInfoService>
    {
        public QuickInfoFacts(ITestOutputHelper testOutput, SharedOmniSharpHostFixture sharedOmniSharpHostFixture)
            : base(testOutput, sharedOmniSharpHostFixture)
        {
        }

        protected override string EndpointName => OmniSharpEndpoints.V2.QuickInfo;

        [Fact]
        public async Task ClassInNamespace()
        {
            const string code = @"
namespace N
{
    public class $$C { }
}
";

            var response = await GetQuickInfoAsync(code);

            Assert.Equal("class N.C", response.Sections[0].Text);
            Assert.Contains("class", response.Tags);
            Assert.Contains("public", response.Tags);
        }

        [Fact]
        public async Task FieldWithFullyQualifiedNameType()
        {
            const string code = @"
namespace N1.N2.N3
{
    public class C1 { }
}

namespace N4
{
    public class C2
    {
        N1.N2.N3.C1 $$c1;
    }
}
";

            var response = await GetQuickInfoAsync(code);

            Assert.Equal("(field) N1.N2.N3.C1 C2.c1", response.Sections[0].Text);
            Assert.Contains("field", response.Tags);
            Assert.Contains("private", response.Tags);
        }

        [Fact]
        public async Task FieldWithMinimallyQualifiedNameType()
        {
            const string code = @"
namespace N1.N2.N3
{
    public class C1 { }
}

namespace N4
{
    using N1.N2.N3;

    public class C2
    {
        N1.N2.N3.C1 $$c1;
    }
}
";

            var response = await GetQuickInfoAsync(code);

            Assert.Equal("(field) C1 C2.c1", response.Sections[0].Text);
            Assert.Contains("field", response.Tags);
            Assert.Contains("private", response.Tags);
        }

        [Fact]
        public async Task GenericType()
        {
            const string code = @"
using System.Collections.Generic;
namespace N1
{
    class C1
    {
        void M1()
        {
            var$$ d = new Dictionary<string, List<int>>();
        }
    }
}
";

            var response = await GetQuickInfoAsync(code);

            Assert.Equal("class System.Collections.Generic.Dictionary<TKey, TValue>", response.Sections[0].Text);
            Assert.Equal(@"
TKey is string
TValue is List<int>", response.Sections[1].Text);
            Assert.Contains("class", response.Tags);
            Assert.Contains("public", response.Tags);
        }

        [Fact]
        public async Task LocalOfGenericType()
        {
            const string code = @"
using System.Collections.Generic;
namespace N1
{
    class C1
    {
        void M1()
        {
            var $$d = new Dictionary<string, List<int>>();
        }
    }
}
";

            var response = await GetQuickInfoAsync(code);

            Assert.Equal("(local variable) Dictionary<string, List<int>> d", response.Sections[0].Text);
            Assert.Contains("local", response.Tags);
        }

        [Fact]
        public async Task AnonymousTypes()
        {
            const string code = @"
using System.Collections.Generic;
namespace N1
{
    class C1
    {
        List<T> CreateList<T>(T t) => new List<T>();

        void M1()
        {
            var obj1 = new { Text = ""Hello"" };
            var $$obj2 = new { Obj = obj1, List = CreateList(obj1) };
        }
    }
}
";

            var response = await GetQuickInfoAsync(code);

            Assert.Equal("(local variable) 'a obj2", response.Sections[0].Text);
            Assert.Equal(@"
Anonymous Types:
    'a is new { 'b Obj, List<'b> List }
    'b is new { string Text }", response.Sections[1].Text);
            Assert.Contains("local", response.Tags);
        }

        [Fact]
        public async Task XmlDocCommentWithMinimallyQualifiedCref()
        {
            const string code = @"
class C1
{
    /// <summary>
    /// Test <see cref=""System.Text.StringBuilder""/> and stuff.
    /// </summary>
    void M1()
    {
    }
}

namespace N1
{
    using System.Text;

    class C1
    {
        C1() => new global::C1().M$$1();
    }
}
";

            var response = await GetQuickInfoAsync(code);

            Assert.Equal("void global::C1.M1()", response.Sections[0].Text);
            Assert.Equal(@"Test StringBuilder and stuff.", response.Sections[1].Text);
            Assert.Contains("method", response.Tags);
            Assert.Contains("private", response.Tags);
        }

        [Fact]
        public async Task XmlDocCommentWithFullyQualifiedCref()
        {
            const string code = @"
class C1
{
    /// <summary>
    /// Test <see cref=""System.Text.StringBuilder""/> and stuff.
    /// </summary>
    public void M1()
    {
    }
}

namespace N1
{
    class C1
    {
        C1() => new global::C1().M$$1();
    }
}
";

            var response = await GetQuickInfoAsync(code);

            Assert.Equal("void global::C1.M1()", response.Sections[0].Text);
            Assert.Equal(@"Test System.Text.StringBuilder and stuff.", response.Sections[1].Text);
            Assert.Contains("method", response.Tags);
            Assert.Contains("private", response.Tags);
        }

        [Fact]
        public async Task PredefinedKeywords()
        {
            const string code = @"
class C1
{
    void M$$1(System.Boolean b, System.Byte by, System.String s, in System.Int64 l)
    {
    }
}
";

            var response = await GetQuickInfoAsync(code);

            Assert.Equal("void C1.M1(bool b, byte by, string s, in long l)", response.Sections[0].Text);
            Assert.Contains("method", response.Tags);
            Assert.Contains("private", response.Tags);
        }

        [Fact]
        public async Task PredefinedKeywordsFromReference()
        {
            const string code = @"
class C1
{
    void M1(System.Boolean b, System.Byte by, System.String s, in System.Int64 l)
    {
        M$$1(true, 42, ""hello"", 1024);
    }
}
";
            var response = await GetQuickInfoAsync(code);

            Assert.Equal("void C1.M1(bool b, byte by, string s, in long l)", response.Sections[0].Text);
            Assert.Contains("method", response.Tags);
            Assert.Contains("private", response.Tags);
        }

        [Fact]
        public async Task BuiltInOperator()
        {
            const string code = @"
class C1
{
    int M1() => 19 $$+ 23;
}
";
            var response = await GetQuickInfoAsync(code);

            Assert.Equal("int int.operator +(int left, int right)", response.Sections[0].Text);
            Assert.Contains("method", response.Tags);
            Assert.Contains("public", response.Tags);
        }

        [Fact]
        public async Task LambdaCaptures()
        {
            const string code = @"
using System;
class C1
{
    void M1()
    {
        var y = 23;
        var z = 19;
        Func<int, int> l = x =>$$ x + y + z;
    }
}
";
            var response = await GetQuickInfoAsync(code);

            Assert.Equal("lambda expression", response.Sections[0].Text);
            Assert.Equal(@"
Variables captured: y, z", response.Sections[1].Text);
            Assert.Contains("method", response.Tags);
            Assert.Contains("private", response.Tags);
        }

        private Task<QuickInfoResponse> GetQuickInfoAsync(string code)
        {
            var testFile = new TestFile("test.cs", code);
            Assert.True(testFile.Content.HasPosition, "Test does not specify position with $$.");

            SharedOmniSharpTestHost.AddFilesToWorkspace(testFile);

            var point = testFile.Content.GetPointFromPosition();
            var request = new QuickInfoRequest
            {
                FileName = testFile.FileName,
                Line = point.Line,
                Column = point.Offset
            };

            var handler = GetRequestHandler(SharedOmniSharpTestHost);

            return handler.Handle(request);
        }
    }
}
