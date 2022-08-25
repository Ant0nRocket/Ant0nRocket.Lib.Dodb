namespace Ant0nRocket.Lib.Dodb.Dtos
{
    /// <summary>
    /// Main element of DTO system. Replace <typeparamref name="T"/> with
    /// any POCO-class and you will get a DTO that could be used inside this
    /// library.
    /// </summary>
    public class DtoOf<T> : Dto where T : class, new()
    {
        /// <summary>
        /// A payload of the document. Everything could be here,
        /// no limitations.
        /// </summary>
        public T Payload { get; set; } = new();
    }
}
