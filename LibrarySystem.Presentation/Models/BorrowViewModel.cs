using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Presentation.Models;


public class BorrowViewModel
{
    [Required]
    public Guid BookId { get; set; }
}