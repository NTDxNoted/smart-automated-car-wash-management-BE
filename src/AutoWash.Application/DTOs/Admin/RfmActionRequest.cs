using System.ComponentModel.DataAnnotations;

namespace AutoWash.Application.DTOs.Admin
{
    public class RfmActionRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "CustomerId không hợp lệ")]
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "ActionType không được để trống")]
        public string ActionType { get; set; } = string.Empty;
    }
}
