using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace SV22T1020587.Admin
{
    public static class SessionExtension
    {
        // Ghi dữ liệu vào Session dưới dạng chuỗi JSON
        public static void SetObjectAsJson(this ISession session, string key, object value)
        {
            session.SetString(key, JsonSerializer.Serialize(value));
        }

        // Đọc dữ liệu từ Session và chuyển ngược lại thành Object
        public static T? GetObjectFromJson<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default : JsonSerializer.Deserialize<T>(value);
        }
    }
}