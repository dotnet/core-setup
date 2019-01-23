// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Microsoft.Extensions.DependencyModel
{
    internal static class JsonTextReaderExtensions
    {
        internal static bool TryReadStringProperty(this ref Utf8JsonReader reader, out string name, out string value)
        {
            name = null;
            value = null;
            if (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                name = reader.GetString();

                if (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.String)
                    {
                        value = reader.GetString();
                    }
                    else
                    {
                        reader.Skip();
                    }
                }

                return true;
            }

            return false;
        }

        internal static void Skip(this ref Utf8JsonReader jsonReader)
        {
            if (jsonReader.TokenType == JsonTokenType.PropertyName)
            {
                jsonReader.Read();
            }

            if (jsonReader.TokenType == JsonTokenType.StartObject || jsonReader.TokenType == JsonTokenType.StartArray)
            {
                int depth = jsonReader.CurrentDepth;
                while (jsonReader.Read() && depth <= jsonReader.CurrentDepth)
                {
                }
            }
        }

        internal static void ReadStartObject(this ref Utf8JsonReader reader)
        {
            reader.Read();
            CheckStartObject(ref reader);
        }

        internal static void CheckStartObject(ref Utf8JsonReader reader)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw CreateUnexpectedException(ref reader, "{");
            }
        }

        internal static void CheckEndObject(this ref Utf8JsonReader reader)
        {
            if (reader.TokenType != JsonTokenType.EndObject)
            {
                throw CreateUnexpectedException(ref reader, "}");
            }
        }

        internal static string[] ReadStringArray(this ref Utf8JsonReader reader)
        {
            reader.Read();
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw CreateUnexpectedException(ref reader, "[");
            }

            var items = new List<string>();

            while (reader.Read() && reader.TokenType == JsonTokenType.String)
            {
                items.Add(reader.GetString());
            }

            if (reader.TokenType != JsonTokenType.EndArray)
            {
                throw CreateUnexpectedException(ref reader, "]");
            }

            return items.ToArray();
        }

        internal static string ReadAsString(this ref Utf8JsonReader reader)
        {
            reader.Read();
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }
            if (reader.TokenType != JsonTokenType.String)
            {
                throw CreateUnexpectedException(ref reader, "a JSON string token");
            }
            return reader.GetString();
        }

        internal static bool ReadAsBoolean(this ref Utf8JsonReader reader)
        {
            reader.Read();
            if (reader.TokenType != JsonTokenType.True && reader.TokenType != JsonTokenType.False)
            {
                throw CreateUnexpectedException(ref reader, "a JSON true or false literal token");
            }
            return reader.GetBoolean();
        }

        internal static bool ReadAsNullableBoolean(this ref Utf8JsonReader reader, bool defaultValue)
        {
            reader.Read();
            if (reader.TokenType != JsonTokenType.True && reader.TokenType != JsonTokenType.False)
            {
                if (reader.TokenType == JsonTokenType.Null)
                {
                    return defaultValue;
                }
                throw CreateUnexpectedException(ref reader, "a JSON true or false literal token");
            }
            return reader.GetBoolean();
        }

        internal static Exception CreateUnexpectedException(ref Utf8JsonReader reader, string expected)
        {
            return new FormatException($"Unexpected character encountered, excepted '{expected}' " +
                                       $"at line {reader.CurrentState._lineNumber} position {reader.CurrentState._bytePositionInLine}");
        }
    }
}
