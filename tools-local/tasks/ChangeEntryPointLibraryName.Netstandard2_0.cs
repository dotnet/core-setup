// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Build.Framework;
using System.IO;
using System.Text.Json;
using System.Diagnostics;

namespace Microsoft.DotNet.Build.Tasks
{
    public class ChangeEntryPointLibraryName : BuildTask
    {
        [Required]
        public string DepsFile { get; set; }

        public string NewName { get; set; }

        private string _version;

        public override bool Execute()
        {
            bool retVal = false;

            byte[] utf8Json = File.ReadAllBytes(DepsFile);

            // TODO: Replace the use of the Utf8JsonReader with the JsonDocument once it is available.

            var reader = new Utf8JsonReader(utf8Json, isFinalBlock: true, state: default);

            using (var filestream = new FileStream(DepsFile, FileMode.Create, FileAccess.Write))
            {
                using (var bufferWriter = new StreamBufferWriter(filestream))
                {
                    var state = new JsonWriterState(options: new JsonWriterOptions { Indented = true });
                    var jsonWriter = new Utf8JsonWriter(bufferWriter, state);

                    retVal = reader.Read();

                    if (retVal)
                    {
                        Debug.Assert(reader.TokenType == JsonTokenType.StartObject);
                        jsonWriter.WriteStartObject();
                        ReadWriteObject(ref reader, ref jsonWriter);
                        jsonWriter.Flush();
                    }
                }
            }

            return retVal;
        }

        internal static void Skip(ref Utf8JsonReader jsonReader)
        {
            int depth = jsonReader.CurrentDepth;
            if (jsonReader.TokenType == JsonTokenType.PropertyName)
            {
                jsonReader.Read();
            }

            if (jsonReader.TokenType == JsonTokenType.StartObject || jsonReader.TokenType == JsonTokenType.StartArray)
            {
                while (jsonReader.Read() && depth < jsonReader.CurrentDepth)
                {
                }
            }
        }

        internal static Exception CreateUnexpectedException(ref Utf8JsonReader reader, string expected)
        {
            return new FormatException($"Unexpected character encountered, excepted '{expected}' " +
                                       $"at line {reader.CurrentState._lineNumber} position {reader.CurrentState._bytePositionInLine}");
        }

        private void ReadWriteObject(ref Utf8JsonReader reader, ref Utf8JsonWriter writer)
        {
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.PropertyName:
                        ReadWritePropertyName(reader.GetStringValue(), ref reader, ref writer);
                        break;
                    case JsonTokenType.EndObject:
                        writer.WriteEndObject();
                        break;
                }
            }
        }

        private void ReadWritePropertyName(string propertyName, ref Utf8JsonReader reader, ref Utf8JsonWriter writer)
        {
            if (propertyName == "targets")
            {
                ReadWritePropertyNameTargets(ref reader, ref writer);
            }
            else if (propertyName == "libraries" && !string.IsNullOrEmpty(_version))
            {
                ReadWritePropertyNameLibraries(ref reader, ref writer);
            }
            else
            {
                if (reader.Read())
                {
                    switch (reader.TokenType)
                    {
                        case JsonTokenType.True:
                            writer.WriteBoolean(propertyName, true);
                            break;
                        case JsonTokenType.False:
                            writer.WriteBoolean(propertyName, false);
                            break;
                        case JsonTokenType.Number:
                            throw CreateUnexpectedException(ref reader, "Expect non-numeric JSON tokens only within deps.json.");
                        case JsonTokenType.String:
                            writer.WriteString(propertyName, reader.ValueSpan);
                            break;
                        case JsonTokenType.Null:
                            writer.WriteNull(propertyName);
                            break;
                        case JsonTokenType.StartObject:
                            writer.WriteStartObject(propertyName);
                            ReadWriteObject(ref reader, ref writer);
                            break;
                        case JsonTokenType.StartArray:
                            writer.WriteStartArray(propertyName);
                            ReadWriteArray(ref reader, ref writer);
                            break;
                    }
                }
            }
        }

        private void ReadWritePropertyNameTargets(ref Utf8JsonReader reader, ref Utf8JsonWriter writer)
        {
            Debug.Assert(reader.GetStringValue() == "targets");

            reader.Read();
            Debug.Assert(reader.TokenType == JsonTokenType.StartObject);

            writer.WriteStartObject("targets", escape: false);

            reader.Read();
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string propertyName = reader.GetStringValue();
                reader.Read();
                Debug.Assert(reader.TokenType == JsonTokenType.StartObject);
                writer.WriteStartObject(propertyName);

                reader.Read();
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string targetLibrary = reader.GetStringValue();
                    _version = targetLibrary.Substring(targetLibrary.IndexOf('/') + 1);
                    if (!string.IsNullOrEmpty(NewName))
                    {
                        string newPropertyName = NewName + '/' + _version;
                        ReadWritePropertyName(newPropertyName, ref reader, ref writer);
                    }
                    else
                    {
                        Skip(ref reader);
                    }
                }
                else
                {
                    Debug.Assert(reader.TokenType == JsonTokenType.EndObject);
                    writer.WriteEndObject();
                }
            }
            else
            {
                Debug.Assert(reader.TokenType == JsonTokenType.EndObject);
                writer.WriteEndObject();
            }
        }

        private void ReadWritePropertyNameLibraries(ref Utf8JsonReader reader, ref Utf8JsonWriter writer)
        {
            Debug.Assert(reader.GetStringValue() == "libraries");
            Debug.Assert(!string.IsNullOrEmpty(_version));

            reader.Read();
            Debug.Assert(reader.TokenType == JsonTokenType.StartObject);

            writer.WriteStartObject("libraries", escape: false);

            reader.Read();
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                if (!string.IsNullOrEmpty(NewName))
                {
                    string newPropertyName = NewName + '/' + _version;
                    ReadWritePropertyName(newPropertyName, ref reader, ref writer);
                }
                else
                {
                    Skip(ref reader);
                }
            }
            else
            {
                Debug.Assert(reader.TokenType == JsonTokenType.EndObject);
                writer.WriteEndObject();
            }
        }

        private void ReadWriteArray(ref Utf8JsonReader reader, ref Utf8JsonWriter writer)
        {
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.True:
                        writer.WriteBooleanValue(true);
                        break;
                    case JsonTokenType.False:
                        writer.WriteBooleanValue(false);
                        break;
                    case JsonTokenType.Number:
                        throw CreateUnexpectedException(ref reader, "Expect non-numeric JSON tokens only within deps.json.");
                    case JsonTokenType.String:
                        writer.WriteStringValue(reader.ValueSpan);
                        break;
                    case JsonTokenType.Null:
                        writer.WriteNullValue();
                        break;
                    case JsonTokenType.StartObject:
                        writer.WriteStartObject();
                        ReadWriteObject(ref reader, ref writer);
                        break;
                    case JsonTokenType.StartArray:
                        writer.WriteStartArray();
                        ReadWriteArray(ref reader, ref writer);
                        break;
                    case JsonTokenType.EndArray:
                        writer.WriteEndArray();
                        return;
                }
            }
        }
    }
}
