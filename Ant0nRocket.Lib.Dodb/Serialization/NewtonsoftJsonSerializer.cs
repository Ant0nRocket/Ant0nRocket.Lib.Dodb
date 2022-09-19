using Ant0nRocket.Lib.Std20.Serialization;

namespace Ant0nRocket.Lib.Dodb.Serialization
{
    internal class NewtonsoftJsonSerializer : IJsonSerializer
    {
        public T Deserialize<T>(string contents, bool throwExceptions = false) where T : class, new()
        {
            try
            {
                var instance = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(contents);
                if (instance == null)
                {
                    if (throwExceptions)
                    {
                        throw new InvalidOperationException("Can't deserialize string");
                    }
                }
                else
                {
                    return instance!;
                }
            }
            catch (Exception ex)
            {
                if (throwExceptions)
                    throw new Exception("See inner exception", ex);
            }

            return Activator.CreateInstance<T>();
        }

        public object Deserialize(string contents, Type type, bool throwExceptions)
        {
            try
            {
                var instance = Newtonsoft.Json.JsonConvert.DeserializeObject(contents, type);
                if (instance == null && throwExceptions)
                    throw new InvalidOperationException("Can't deserialize string");
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
