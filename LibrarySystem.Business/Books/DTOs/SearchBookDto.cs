using LibrarySystem.BusinessLogic.DTOs;

namespace LibrarySystem.BusinessLogic.Books.DTOs;

public class SearchBookDto : GetPaginateListDto
{
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? ISBN { get; set; }
}
