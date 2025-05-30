namespace FarGuard.Core.Messaging;

public class SecureMessage
{
    public Guid Sender { get; set; }
    public Guid Recipient { get; set; }
    public byte[] EncryptedData { get; set; } = [];
    public byte[] Key { get; set; } = [];
    public byte[] IV { get; set; } = [];
    public DateTime Timestamp { get; set; }
}