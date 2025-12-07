using System;
using System.Collections.Generic;
using System.Text;

namespace ChanSentry.Common.Helpers;

public static class JsonHelper
{
    public static T Deserialize<T>(this T model, string json) where T : Models.IApiModel
    {
        return System.Text.Json.JsonSerializer.Deserialize<T>(json);
    }
}
