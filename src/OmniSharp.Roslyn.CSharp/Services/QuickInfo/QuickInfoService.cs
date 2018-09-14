using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions;
using OmniSharp.Mef;
using OmniSharp.Models.V2.QuickInfo;
using CSharpQuickInfoService = Microsoft.CodeAnalysis.QuickInfo.QuickInfoService;

namespace OmniSharp.Roslyn.CSharp.Services.QuickInfo
{
    [OmniSharpHandler(OmniSharpEndpoints.V2.QuickInfo, LanguageNames.CSharp)]
    public class QuickInfoService : BaseService<QuickInfoRequest, QuickInfoResponse>
    {
        [ImportingConstructor]
        public QuickInfoService(ILoggerFactory loggerFactory, OmniSharpWorkspace workspace)
            : base(loggerFactory, workspace)
        {
        }

        protected override async Task<QuickInfoResponse> HandleRequestAsync(QuickInfoRequest request)
        {
            var document = GetDocument(request);
            if (document == null)
            {
                return QuickInfoResponse.Empty;
            }

            var sourceText = await document.GetTextAsync();
            var position = sourceText.GetPositionFromLineAndOffset(request.Line, request.Column);

            var service = CSharpQuickInfoService.GetService(document);
            var quickInfoItem = await service.GetQuickInfoAsync(document, position);
            if (quickInfoItem == null)
            {
                return QuickInfoResponse.Empty;
            }

            return new QuickInfoResponse(
                sections: quickInfoItem.Sections.ToImmutableList().ConvertAll(
                    s => new QuickInfoSection(s.Kind, s.Text)),
                tags: ImmutableArray.CreateRange(quickInfoItem.Tags.Select(t => t.ToLowerInvariant())));
        }
    }
}
