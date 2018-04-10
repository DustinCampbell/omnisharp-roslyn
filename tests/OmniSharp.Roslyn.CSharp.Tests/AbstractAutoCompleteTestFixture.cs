﻿using System.Collections.Generic;
using System.Threading.Tasks;
using OmniSharp.Models.AutoComplete;
using OmniSharp.Roslyn.CSharp.Services.IntelliSense;
using TestUtility;
using Xunit.Abstractions;

namespace OmniSharp.Roslyn.CSharp.Tests
{
    public class AbstractAutoCompleteTestFixture : AbstractSingleRequestHandlerTestFixture<AutoCompleteService>
    {
        protected AbstractAutoCompleteTestFixture(ITestOutputHelper output, SharedOmniSharpHostFixture sharedOmniSharpHostFixture)
            : base(output, sharedOmniSharpHostFixture)
        {
        }

        protected override string EndpointName => OmniSharpEndpoints.AutoComplete;

        protected async Task<IEnumerable<AutoCompleteResponse>> FindCompletionsAsync(string filename, string source, bool wantSnippet = false, string triggerChar = null)
        {
            var testFile = new TestFile(filename, source);
            SharedOmniSharpTestHost.AddFilesToWorkspace(testFile);
            var point = testFile.Content.GetPointFromPosition();

            var request = new AutoCompleteRequest
            {
                Line = point.Line,
                Column = point.Offset,
                FileName = testFile.FileName,
                Buffer = testFile.Content.Code,
                WordToComplete = GetPartialWord(testFile.Content),
                WantMethodHeader = true,
                WantSnippet = wantSnippet,
                WantReturnType = true,
                TriggerCharacter = triggerChar
            };

            var requestHandler = GetRequestHandler(SharedOmniSharpTestHost);

            return await requestHandler.Handle(request);
        }

        private static string GetPartialWord(TestContent testConnect)
        {
            if (!testConnect.HasPosition || testConnect.Position == 0)
            {
                return string.Empty;
            }

            var index = testConnect.Position;
            while (index >= 1)
            {
                var ch = testConnect.Code[index - 1];
                if (ch != '_' && !char.IsLetterOrDigit(ch))
                {
                    break;
                }

                index--;
            }

            return testConnect.Code.Substring(index, testConnect.Position - index);
        }
    }
}
