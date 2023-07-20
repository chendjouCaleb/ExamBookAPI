namespace Traceability.Serializers
{
    public interface IDataSerializer
    {
        string Serialize(object data);

        TData Deserialize<TData>(string data);
    }
}