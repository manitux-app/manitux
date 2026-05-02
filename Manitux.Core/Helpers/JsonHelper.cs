using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Manitux.Core.Helpers
{
    public class JsonHelper
    {
        public Dictionary<string, object>? JsonParse(string key, string json)
        {
            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                // Console.WriteLine(data["name"]);
            }
            catch { return null; }
        }


        public string? GetJsonValue(string key, string json)
        {
            try
            {
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    JsonElement root = doc.RootElement;
                    //string name = root.GetProperty("Name").GetString();
                    //int number = root.GetProperty("Number").GetInt32();

                    return root.GetProperty(key).GetString();
                }
            }
            catch { return null; }   
        }

        public dynamic? GetJsonNodeValue(string key, string json)
        {
            try
            {
                var node = JsonNode.Parse(json);
                return node?[key];

                //var name = GetValue("name", json).ToString();
                //var number = (int)GetValue("number", json);
            }
            catch { return null; }
        }

        public Task<T?>? GetJsonValue<T>(string key, string json)
        {
            try
            {
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    JsonElement element = doc.RootElement.GetProperty(key);
                    T? value = JsonSerializer.Deserialize<T>(element.GetRawText());
                    return Task.FromResult(value);
                }
            }
            catch { return null; }
            
        }

    }
}
