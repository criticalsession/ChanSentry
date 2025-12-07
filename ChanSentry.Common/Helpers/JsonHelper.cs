using System;
using System.Collections.Generic;
using System.Text.Json;

namespace ChanSentry.Common.Helpers;

public static class JsonHelper
{
    /// <param name="json">The JSON string to deserialize into an object of type T (IApiModel). Cannot be null.</param>
    /// <returns>An instance of type T that represents the deserialized JSON data.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the JSON string cannot be deserialized into a non-null instance of type T.</exception>
    public static T Deserialize<T>(string json) where T : Models.IApiModel
    {
        return JsonSerializer.Deserialize<T>(json)
            ?? throw new InvalidOperationException("Deserialization resulted in null object.");
    }
}
