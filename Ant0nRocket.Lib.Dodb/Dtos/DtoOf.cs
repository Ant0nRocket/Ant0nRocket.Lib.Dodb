using Ant0nRocket.Lib.Dodb.Abstractions;

namespace Ant0nRocket.Lib.Dodb.Dtos
{
    /// <summary>
    /// Main element of DTO system. Replace <typeparamref name="T"/> with
    /// any POCO-class and you will get a DTO that could be used inside this
    /// library.
    /// </summary>
    public class DtoOf<T> : Dto where T : class, new()
    {
        private T? _payload;

        /// <summary>
        /// A payload of the document.
        /// </summary>
        public T Payload
        {
            get => _payload ?? throw new NullReferenceException("somehow you made a Payload equals null");
            set
            {
                _payload = value ?? throw new ArgumentNullException("It's forbidden to set Payload as null");

                if (value is IPayload payload)
                {
                    payload.RegisterCarrier(this);
                }
                else
                {
                    throw new ArgumentException("value must be of type IPayload");
                }
            }
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public DtoOf(Guid? userId = default, Guid? requiredDocumentId = default)
        {
            if (typeof(T) == typeof(object))
                // Sync service can use T = object and it will
                // raise an error because object is not of type IPayload.
                // To safe the situation just create instance behind setter.
                _payload = new();
            else
                // ...othervise - create a new payload instance using
                // a setter inside Payload property.
                Payload = new();

            // And anyway, assign values if passed...
            UserId = userId;
            RequiredDocumentId = requiredDocumentId;
        }
    }
}
