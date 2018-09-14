using System.Collections.Generic;
using System.Collections.Immutable;

namespace OmniSharp.Models.V2.QuickInfo
{
    public class QuickInfoResponse
    {
        public static QuickInfoResponse Empty { get; } = new QuickInfoResponse(
            sections: ImmutableList<QuickInfoSection>.Empty,
            tags: ImmutableList<string>.Empty);

        public QuickInfoResponse(IReadOnlyList<QuickInfoSection> sections, IReadOnlyList<string> tags)
        {
            Sections = sections;
            Tags = tags;
        }

        public IReadOnlyList<QuickInfoSection> Sections { get; }
        public IReadOnlyList<string> Tags { get; }
    }
}
