using System;
using System.Collections.Generic;

namespace AutoWash.Application.DTOs
{
    public class AvailableSlotResponse
    {
        public string Date { get; set; } = string.Empty;
        public List<TimeSlotDto> Slots { get; set; } = new List<TimeSlotDto>();
    }

    public class TimeSlotDto
    {
        public string Time { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
    }
}
