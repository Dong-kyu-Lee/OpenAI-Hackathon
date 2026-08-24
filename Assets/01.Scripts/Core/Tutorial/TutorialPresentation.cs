namespace Game.Core.Tutorial
{
    public readonly struct TutorialPresentation
    {
        public TutorialPresentation(
            bool isVisible,
            string title,
            string message,
            string inputLabel,
            TutorialInputHint[] inputHints)
        {
            IsVisible = isVisible;
            Title = title;
            Message = message;
            InputLabel = inputLabel;
            InputHints = inputHints ?? System.Array.Empty<TutorialInputHint>();
        }

        public bool IsVisible { get; }
        public string Title { get; }
        public string Message { get; }
        public string InputLabel { get; }
        public TutorialInputHint[] InputHints { get; }

        public static TutorialPresentation Hidden =>
            new(false, string.Empty, string.Empty, string.Empty, System.Array.Empty<TutorialInputHint>());
    }
}
