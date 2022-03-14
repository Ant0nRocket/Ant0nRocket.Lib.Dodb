namespace Ant0nRocket.Lib.Dodb
{
    public static class DodbLibConfig
    {
        /// <summary>
        /// Indicates should we check AuthorId field in <see cref="Dtos.DtoOf{T}"/>.<br />
        /// Actually, the only scenario when you need it - is a first DTO push.
        /// </summary>
        public static bool ValidateAuthToken { get; set; } = true;
    }
}
