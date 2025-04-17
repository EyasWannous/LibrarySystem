using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace LibrarySystem.BusinessLogic.DTOs;

public class GetPaginateListDto
{
    [DefaultValue(0)]
    public int Skip { get; set; }

    [Range(0, 100)]
    [DefaultValue(100)]
    public int Take { get; set; }

}
