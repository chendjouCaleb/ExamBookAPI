using Newtonsoft.Json;
using Traceability.Serializers;

namespace Traceability.JsonSerializer
{
    public class NewtonSoftDataSerializer: IDataSerializer
    {
        private readonly JsonSerializerSettings _settings;

        public NewtonSoftDataSerializer(JsonSerializerSettings settings)
        {
            _settings = settings;
        }

        public string Serialize(object data)
        {
            return JsonConvert.SerializeObject(data, _settings);
        }

        public TData Deserialize<TData>(string data)
        {
            return JsonConvert.DeserializeObject<TData>(data)!;
        }
    }
}