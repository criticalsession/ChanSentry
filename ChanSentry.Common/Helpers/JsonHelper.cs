using System;
using System.Collections.Generic;
using System.Text.Json;

namespace ChanSentry.Common.Helpers;

public static class JsonHelper
{
    public static T Deserialize<T>(this T model, string json) where T : Models.IApiModel
    {
        return JsonSerializer.Deserialize<T>(json)
            ?? throw new InvalidOperationException("Deserialization resulted in null object.");
    }
}
