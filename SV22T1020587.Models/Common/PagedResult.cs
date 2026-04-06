using System;
using System.Collections.Generic;
using System.Linq;

namespace SV22T1020587.Models.Common
{
    /// <summary>
    /// Cấu trúc kết quả phân trang dùng chung
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PagedResult<T> where T : class
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int RowCount { get; set; }
        public List<T> DataItems { get; set; } = new List<T>();

        /// <summary>
        /// Tính tổng số trang
        /// </summary>
        public int PageCount
        {
            get
            {
                if (PageSize <= 0) return 1;
                int n = RowCount / PageSize;
                if (RowCount % PageSize > 0) n += 1;
                return n;
            }
        }

        /// <summary>
        /// Logic tính toán các số trang cần hiển thị trên giao diện (Pager)
        /// Dựa trên lớp PageItem (Page = 0 là dấu ba chấm)
        /// </summary>
        /// <param name="delta">Số lượng trang hiển thị xung quanh trang hiện tại</param>
        /// <returns></returns>
        public List<PageItem> GetDisplayPages(int delta = 2)
        {
            var result = new List<PageItem>();
            int current = Page;
            int last = PageCount;

            for (int i = 1; i <= last; i++)
            {
                // Điều kiện hiển thị: Trang đầu, Trang cuối, hoặc các trang xung quanh trang hiện tại
                if (i == 1 || i == last || (i >= current - delta && i <= current + delta))
                {
                    // Sử dụng Constructor: new PageItem(pageNumber, isCurrent)
                    result.Add(new PageItem(i, i == current));
                }
                // Nếu có khoảng trống và phần tử trước đó chưa phải là dấu ba chấm
                else if (result.Count > 0 && !result.Last().IsEllipsis)
                {
                    // Theo logic của bạn: Page = 0 thì IsEllipsis sẽ là true
                    result.Add(new PageItem(0, false));
                }
            }
            return result;
        }
    }
}