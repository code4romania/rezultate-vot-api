using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ElectionResults.Core.Scheduler
{

    public class JsonResultsModel
    {
        public Stages Stages { get; set; }
    }

    public class Stages
    {
        public PROV PROV { get; set; }
        public PROV PART { get; set; }
        public PROV FINAL { get; set; }
    }

    public class PROV
    {
        public bool enabled { get; set; }
        public Scopes scopes { get; set; }
    }

    public class Scopes
    {
        public Scope Scope { get; set; }
        public Scope UAT { get; set; }
        public Scope CNTY { get; set; }
    }

    public class Scope
    {
        public Categories Categories { get; set; }
    }

    public class Categories
    {
        public Category P { get; set; }
        public Category CL { get; set; }
        public Category CJ { get; set; }
        public Category PCJ { get; set; }
    }

    public class Category
    {
        [JsonConverter(typeof(CandidateModelConverter))]
        public List<JsonCandidateModel> Table { get; set; }
    }

    public class CandidateModelConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartArray();
            foreach (var player in (List<JsonCandidateModel>)value)
            {
                writer.WriteRawValue(JsonConvert.SerializeObject(player));
            }
            writer.WriteEndArray();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
                var response = new List<JsonCandidateModel>();
            try
            {
                if (reader.TokenType == JsonToken.StartArray)
                {
                    
                    JArray.Load(reader);
                    return response;
                }

                JObject players = JObject.Load(reader);
                foreach (var player in players)
                {
                    var p = JsonConvert.DeserializeObject<JsonCandidateModel>(player.Value.ToString());
                    response.Add(p);
                }

                return response;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return response;
            }
        }

        public override bool CanConvert(Type objectType) => objectType == typeof(List<JsonCandidateModel>);
    }
}