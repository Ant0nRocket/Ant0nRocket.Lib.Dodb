namespace Ant0nRocket.Lib.Dodb.Gateway.Responses.Attributes
{
    /// <summary>
    /// Purpose: say we take two reponses, how to know was operation completed
    /// successfully or not? Right, apply this attribute :)
    /// </summary>
    public class IsSuccessAttribute : Attribute
    {
        public bool IsSuccess { get; set; }

        public IsSuccessAttribute(bool value) => IsSuccess = value;
    }
}
