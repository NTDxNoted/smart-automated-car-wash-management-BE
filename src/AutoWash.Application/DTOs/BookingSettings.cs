namespace AutoWash.Application.DTOs
{
    public class BookingSettings
    {
        public int MaxParallelSlots { get; set; } = 1;

        // BR-19: khi slot thường (MaxParallelSlots) đã đầy, khách có tier ưu tiên cao hơn
        // mức thấp nhất (Member/Guest) vẫn được đặt thêm vào số buffer này.
        public int PriorityBufferSlots { get; set; } = 1;
    }
}
