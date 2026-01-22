using System.ComponentModel.DataAnnotations;

namespace HuonevarausAPI.Models;

public class ReservationCreateDto
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string RoomName { get; set; } = string.Empty;
    [Required]
    [RegularExpression(@"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}$",
        ErrorMessage = "Aikamuoto on virheellinen. Käytä muotoa yyyy-MM-dd HH:mm.")]
    public string StartTime { get; set; } = string.Empty; // "yyyy-MM-dd HH:mm"
    [Required]
    [RegularExpression(@"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}$",
        ErrorMessage = "Aikamuoto on virheellinen. Käytä muotoa yyyy-MM-dd HH:mm.")]
    public string EndTime { get; set; } = string.Empty; // "yyyy-MM-dd HH:mm"
}