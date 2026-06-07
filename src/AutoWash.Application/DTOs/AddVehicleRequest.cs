namespace AutoWash.Application.DTOs
{
    public class AddVehicleRequest
    {
        public string LicensePlate { get; set; } = string.Empty;
        public string OtpCode { get; set; } = string.Empty;
    }
}
