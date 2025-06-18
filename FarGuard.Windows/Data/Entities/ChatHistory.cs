using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FarGuard.Windows.Data.Entities;

public class ChatHistory
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, StringLength(1024)]
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(Client))]
    public Guid ClientId { get; set; }

    [ForeignKey(nameof(ClientId))]
    public Client Client { get; set; } = null!;

    [ForeignKey(nameof(Sender))]
    public Guid SenderId { get; set; }

    [ForeignKey(nameof(SenderId))]
    public Client Sender { get; set; } = null!;
}