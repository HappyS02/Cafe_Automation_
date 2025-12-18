using Microsoft.AspNetCore.Http;
using Newtonsoft.Json; // Paket yoksa: Install-Package Newtonsoft.Json
using System.Text.Json;

namespace CafeOtomasyon.Extensions
{
    public static class SessionExtensions
    {
        // Nesneyi JSON'a çevirip kaydeder
        public static void SetObject(this ISession session, string key, object value)
        {
            var json = JsonConvert.SerializeObject(value);
            session.SetString(key, json);
        }

        // JSON'u tekrar Nesneye çevirip okur
        public static T GetObject<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default : JsonConvert.DeserializeObject<T>(value);
        }
    }
}