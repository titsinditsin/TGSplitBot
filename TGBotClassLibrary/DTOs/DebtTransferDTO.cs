namespace TGBotClassLibrary
{
    public class DebtTransfer
    {
        public string FromUserName { get; set; } = string.Empty;
        public string ToUserName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}