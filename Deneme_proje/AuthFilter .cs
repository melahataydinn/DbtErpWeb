using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using Dapper;
using System;
using System.Linq;
using Deneme_proje;

public class AuthFilter : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var controller = context.RouteData.Values["controller"]?.ToString();
        var action = context.RouteData.Values["action"]?.ToString();

        // Login ve Home controller'ları için kontrol yapma
        if (string.Equals(controller, "login", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(controller, "home", StringComparison.OrdinalIgnoreCase))
        {
            base.OnActionExecuting(context);
            return;
        }

        // AllowAnonymous attribute kontrolü
        var actionDescriptor = context.ActionDescriptor as Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor;
        if (actionDescriptor != null)
        {
            // Controller üzerinde AllowAnonymous var mı?
            var controllerHasAllowAnonymous = actionDescriptor.ControllerTypeInfo
                .GetCustomAttributes(typeof(AllowAnonymousAttribute), true)
                .Any();

            // Method üzerinde AllowAnonymous var mı?
            var methodHasAllowAnonymous = actionDescriptor.MethodInfo
                .GetCustomAttributes(typeof(AllowAnonymousAttribute), true)
                .Any();

            // Eğer Controller veya Method üzerinde AllowAnonymous varsa, yetki kontrolü yapma
            if (controllerHasAllowAnonymous || methodHasAllowAnonymous)
            {
                base.OnActionExecuting(context);
                return;
            }
        }

        var session = context.HttpContext.Session;
        var username = session.GetString("Username");
        var userNo = session.GetString("UserNo");
        var isAuthenticated = session.GetString("IsAuthenticated");

        // Kullanıcı giriş yapmamışsa
        if (string.IsNullOrEmpty(username) || isAuthenticated != "true" || string.IsNullOrEmpty(userNo))
        {
            context.Result = new RedirectToActionResult("Index", "Login", null);
            return;
        }

        try
        {
            var configuration = context.HttpContext.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration;
            if (configuration == null)
            {
                throw new Exception("IConfiguration servisine erişilemedi.");
            }

            var connectionString = configuration.GetConnectionString("ERPDatabase");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new Exception("ERPDatabase bağlantı dizesi bulunamadı.");
            }

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Menü kaydını kontrol et
                // NOT: Veritabanındaki değerlerle tam eşleşme için LOWER fonksiyonlarını kaldırdık
                var checkQuery = @"
SELECT Yetki 
FROM MenuYonetim 
WHERE ControllerAdi = @controller 
AND ActionAdi = @action";

                var parameters = new
                {
                    controller = controller,
                    action = action
                };

                // Yetki değerini al
                var yetkiDegeri = connection.QueryFirstOrDefault<string>(checkQuery, parameters);

                // Eğer yetki değeri null ise, kaydımız yok demektir
                if (yetkiDegeri == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Controller: {controller}, Action: {action} için menü kaydı bulunamadı.");

                    if (!string.Equals(controller, "home", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Result = new RedirectToActionResult("Index", "Home", null);
                    }
                    return;
                }

                // Yetki kontrolü yapılıyor
                // Eğer Yetki alanı "1" gibi bir değer ise, tüm kullanıcılara izin ver
                if (yetkiDegeri == "1")
                {
                    // "1" değeri herkes için erişim anlamına geliyorsa, devam et
                    base.OnActionExecuting(context);
                    return;
                }

                // Yetki değeri, kullanıcı numarası listesi olarak kullanılıyorsa
                bool yetkiVar = yetkiDegeri == userNo ||
                       yetkiDegeri.StartsWith($"{userNo},") ||
                       yetkiDegeri.Contains($",{userNo},") ||
                       yetkiDegeri.EndsWith($",{userNo}");

                System.Diagnostics.Debug.WriteLine($"Controller: {controller}, Action: {action}, UserNo: {userNo}, Yetki: {yetkiDegeri}, YetkiVar: {yetkiVar}");

                if (!yetkiVar)
                {
                    if (!string.Equals(controller, "home", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Result = new RedirectToActionResult("Index", "Home", null);
                    }
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Yetki kontrolü sırasında hata: {ex.Message}");
            if (!string.Equals(controller, "home", StringComparison.OrdinalIgnoreCase))
            {
                context.Result = new RedirectToActionResult("Index", "Home", null);
            }
            return;
        }

        base.OnActionExecuting(context);
    }
}