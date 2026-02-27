using DTPortal.Core.Domain.Services.Communication;

using DTPortal.Core.Utilities;

using Microsoft.AspNetCore.Http;

using System.Globalization;

using System.Linq;

public class MessageLocalizer : IMessageLocalizer

{

    private readonly IHttpContextAccessor _httpContextAccessor;

    public MessageLocalizer(IHttpContextAccessor httpContextAccessor)

    {

        _httpContextAccessor = httpContextAccessor;

    }

    public string GetMessage(LocalizedMessage message)

    {

        var context = _httpContextAccessor.HttpContext;

        if (context == null || message == null)

            return message?.En;

        var acceptLanguage = context.Request.Headers["Accept-Language"].ToString();

        if (string.IsNullOrEmpty(acceptLanguage))

            return message.En;

        var language = acceptLanguage.Split(',').FirstOrDefault()?.Trim().ToLower();

        if (language != null && language.StartsWith("ar"))

            return message.Ar ?? message.En;

        return message.En;

    }

}
