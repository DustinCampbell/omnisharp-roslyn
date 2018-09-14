namespace OmniSharp.Models.V2.QuickInfo
{
    public class QuickInfoSection
    {
        public QuickInfoSection(string kind, string text)
        {
            Kind = kind;
            Text = text;
        }

        public string Kind { get; }
        public string Text { get; }
    }
}
