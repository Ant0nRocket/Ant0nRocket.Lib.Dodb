namespace Ant0nRocket.Lib.Dodb.Attributes
{
    /// <summary>
    /// Purpose: say we take two reponses, how to know was operation completed
    /// successfully or not? Right, apply this attribute :)
    /// </summary>
    public class IsSuccessAttribute : Attribute
    {
        public bool IsSuccess { get; }

        public IsSuccessAttribute(bool isSuccess) => IsSuccess = isSuccess;
    }
}
