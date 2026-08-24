namespace Game.Core.Tutorial
{
    public readonly struct TutorialRequest
    {
        public TutorialRequest(
            string stepId,
            string title,
            string message,
            string inputLabel,
            TutorialAction requiredAction)
        {
            StepId = stepId;
            Title = title;
            Message = message;
            InputLabel = inputLabel;
            RequiredAction = requiredAction;
        }

        public string StepId { get; }
        public string Title { get; }
        public string Message { get; }
        public string InputLabel { get; }
        public TutorialAction RequiredAction { get; }
    }
}
