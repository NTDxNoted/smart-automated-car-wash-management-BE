using System.Collections.Generic;

namespace AutoWash.Application.DTOs
{
    public class PagedResponse<T>
    {
        public int Page { get; set; }
        public int Total { get; set; }
        public List<T> Data { get; set; } = new List<T>();
        public int PageSize { get; internal set; }
    }
}