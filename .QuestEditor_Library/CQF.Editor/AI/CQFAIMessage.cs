namespace QuestEditor_Library
{
    public sealed class CQFAIMessage
    {
        public CQFAIMessage(string role, string content, string? displayContent = null, bool? visible = null, IReadOnlyList<CQFAIToolCall>? toolCalls = null, string? toolCallId = null)
        {
            Role = role;
            Content = content;
            DisplayContent = displayContent ?? content;
            IsVisible = visible ?? (role != "system" && role != "tool");
            ToolCalls = toolCalls ?? Array.Empty<CQFAIToolCall>();
            ToolCallId = toolCallId;
        }
        public string Role { get; }
        public string Content { get; }
        public string DisplayContent { get; }
        public bool IsVisible { get; }
        public IReadOnlyList<CQFAIToolCall> ToolCalls { get; }
        public string? ToolCallId { get; }
    }
}
