namespace TGBotClassLibrary
{
    public class ParsedCommand
    {
        public string CommandType { get; set; } = "unknown";
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

}