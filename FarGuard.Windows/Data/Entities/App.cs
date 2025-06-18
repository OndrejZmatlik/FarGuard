using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FarGuard.Windows.Data.Entities;

public class App
{
    [Key]
    public Guid UserId { get; set; } = Guid.NewGuid();
    public string UserName { get; set; } = "Anonymous";
}