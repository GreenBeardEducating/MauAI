namespace MauAI;

public sealed class ChatMessage
{
    public ChatMessage(string role, string content)
    {
        Role = role;
        Content = content;
    }

    public string Role { get; }
    public string Content { get; }

    public string BackgroundColor =>
        Role == "You" ? "#E5DFFF" : "#FFFFFF";

    public string RoleColor =>
        Role == "You" ? "#512BD4" : "#16803C";
}
