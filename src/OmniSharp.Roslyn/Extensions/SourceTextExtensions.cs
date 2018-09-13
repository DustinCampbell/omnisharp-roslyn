using Microsoft.CodeAnalysis.Text;
using OmniSharp.Models;
using OmniSharp.Models.V2;

namespace OmniSharp
{
    public static class SourceTextExtensions
    {
        /// <summary>
        /// Converts a zero-based position in a <see cref="SourceText"/> to an OmniSharp <see cref="Point"/>.
        /// </summary>
        public static Point GetOmniSharpPoint(this SourceText text, int position)
        {
            var line = text.Lines.GetLineFromPosition(position);

            return new Point
            {
                Line = line.LineNumber,
                Column = position - line.Start
            };
        }

        /// <summary>
        /// Converts a line number and offset to a zero-based position within a <see cref="SourceText"/>.
        /// </summary>
        public static int GetPosition(this SourceText text, int lineNumber, int offset)
            => text.Lines[lineNumber].Start + offset;

        /// <summary>
        /// Converts an OmniSharp <see cref="Point"/> to a zero-based position within a <see cref="SourceText"/>.
        /// </summary>
        public static int GetPosition(this SourceText text, Point point)
            => text.GetPosition(point.Line, point.Column);

        /// <summary>
        /// Converts an OmniSharp <see cref="IPointLike"/> to a zero-based position within a <see cref="SourceText"/>.
        /// </summary>
        public static int GetPosition(this SourceText text, IPointLike point)
            => text.GetPosition(point.Line, point.Column);

        /// <summary>
        /// Converts a <see cref="TextSpan"/> in a <see cref="SourceText"/> to an OmniSharp <see cref="Range"/>.
        /// </summary>
        public static Range GetOmniSharpRange(this SourceText text, TextSpan span)
            => new Range
            {
                Start = text.GetOmniSharpPoint(span.Start),
                End = text.GetOmniSharpPoint(span.End)
            };

        /// <summary>
        /// Converts an OmniSharp <see cref="Range"/> to a <see cref="TextSpan"/> within a <see cref="SourceText"/>.
        /// </summary>
        public static TextSpan GetSpan(this SourceText text, Range range)
            => TextSpan.FromBounds(
                start: text.GetPosition(range.Start),
                end: text.GetPosition(range.End));
    }
}
