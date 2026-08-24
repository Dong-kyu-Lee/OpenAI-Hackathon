namespace Game.Core.Tutorial
{
    public readonly struct TutorialPresentation
    {
        public TutorialPresentation(bool isVisible, string title, string message, string inputLabel)
        {
            IsVisible = isVisible;
            Title = title;
            Message = message;
            InputLabel = inputLabel;
        }

        public bool IsVisible { get; }
        public string Title { get; }
        public string Message { get; }
        public string InputLabel { get; }

        public static TutorialPresentation Hidden => new(false, string.Empty, string.Empty, string.Empty);
    }
}
