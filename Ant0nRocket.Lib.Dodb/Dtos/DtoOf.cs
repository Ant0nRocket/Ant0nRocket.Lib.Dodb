namespace Ant0nRocket.Lib.Dodb.Dtos
{
    public class DtoOf<T> : Dto where T : class, new()
    {
        /// <summary>
        /// A payload of the document. Everything could be here,
        /// no limitations.
        /// </summary>
        public T Payload { get; set; } = new();
    }
}
