using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace ChanSentry.Common.Helpers;

public static class JsonHelper
{
    /// <summary>
    /// Deserializes the specified JSON string into an instance of type T (IApiModel).
    /// </summary>
    /// <typeparam name="T">The type of the model to deserialize. Must implement the IApiModel interface.</typeparam>
    /// <param name="model">The model instance used to call this extension method. This parameter is not used.</param>
    /// <param name="json">The JSON string to deserialize into an object of type T. Cannot be null.</param>
    /// <returns>An instance of type T that represents the deserialized JSON data.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the JSON string cannot be deserialized into a non-null instance of type T.</exception>
    public static T Deserialize<T>(this T model, string json) where T : Models.IApiModel
    {
        return JsonSerializer.Deserialize<T>(json)
            ?? throw new InvalidOperationException("Deserialization resulted in null object.");
    }
}
