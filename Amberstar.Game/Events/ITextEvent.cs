namespace Amberstar.Game.Events
{
	internal interface ITextEvent
    {
        /// <summary>
        /// Index of the map text block which is shown before the event is executed.
        /// Like in the original, values of 26 and above mean that there is no text.
        /// </summary>
        byte TextIndex { get; }
    }

    internal static class TextEventExtensions
    {
        public const byte NoText = 26;

        public static bool HasText(this ITextEvent textEvent) => textEvent.TextIndex < NoText;
    }
}
