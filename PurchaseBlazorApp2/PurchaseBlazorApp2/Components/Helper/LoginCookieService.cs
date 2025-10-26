using PurchaseBlazorApp2.Components.Data;
using System.Text.Json;

namespace PurchaseBlazorApp2.Components.Helper
{
    public class LoginCookieService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LoginCookieService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void SetCookie(string key, string value)
        {
            _httpContextAccessor.HttpContext?.Response.Cookies.Append(key, value, new CookieOptions
            {
                Expires = DateTimeOffset.Now.AddDays(7),
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax
            });
        }

        public void SetCookie(UserName Info)
        {
            var json = JsonSerializer.Serialize(Info);
            _httpContextAccessor.HttpContext?.Response.Cookies.Append("UserInfo", json, new CookieOptions
            {
                Expires = DateTimeOffset.Now.AddDays(7),
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax
            });
        }


        public UserName? GetUserName()
        {
            try
            {
                var cookie = _httpContextAccessor.HttpContext?.Request.Cookies["UserInfo"];

                if (string.IsNullOrWhiteSpace(cookie))
                    return null;

                return JsonSerializer.Deserialize<UserName>(cookie);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
