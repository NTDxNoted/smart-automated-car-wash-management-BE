// Application/DTOs/CreateServiceRequest.cs
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class CreateServiceRequest
    {
        [Required(ErrorMessage = "ServiceName is required.")]
        public string ServiceName { get; set; } = string.Empty;

        [Required(ErrorMessage = "ServiceCategory is required.")]
        public string ServiceCategory { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        public string Description { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0.")]
        public decimal Price { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Duration must be at least 1 minute.")]
        public int Duration { get; set; }
    }
}