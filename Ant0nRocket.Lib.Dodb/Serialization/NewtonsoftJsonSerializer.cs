using Ant0nRocket.Lib.Serialization;

namespace Ant0nRocket.Lib.Dodb.Serialization
{
    internal class NewtonsoftJsonSerializer : IJsonSerializer
    {
        public T Deserialize<T>(string contents, bool throwExceptions = false) where T : class, new() =>
            (T)Deserialize(contents, typeof(T), throwExceptions);

        public object Deserialize(string contents, Type type, bool throwExceptions = false)
        {
            try
            {
                var instance = Newtonsoft.Json.JsonConvert.DeserializeObject(contents, type);
                if (instance == null)
                {
                    if (throwExceptions)
                    {
                        throw new InvalidOperationException("Can't deserialize string");
                    }
                }
                else
                {
                    return instance;
                }
            }
            catch (Exception ex)
            {
                if (throwExceptions)
                    throw new Exception("See inner exception", ex);
            }

            return Activator.CreateInstance(type)!;
        }

        public string Serialize(object obj, bool pretty = false)
        {
            var formatting = pretty ? Newtonsoft.Json.Formatting.Indented : Newtonsoft.Json.Formatting.None;
            return Newtonsoft.Json.JsonConvert.SerializeObject(obj, formatting);
        }
    }
}
