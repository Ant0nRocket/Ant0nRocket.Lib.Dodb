namespace Ant0nRocket.Lib.Dodb.Abstractions
{
    public interface IValidateablePayload
    {
        void Validate(List<string> errorsList);
    }
}
