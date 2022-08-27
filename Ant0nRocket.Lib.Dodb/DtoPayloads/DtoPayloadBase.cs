using Ant0nRocket.Lib.Dodb.Abstractions;
using Ant0nRocket.Lib.Dodb.Dtos;

namespace Ant0nRocket.Lib.Dodb.DtoPayloads
{
    /// <summary>
    /// Helper class for payloads. Inherit if you need no boilerplate code.<br />
    /// Or mark your class with <see cref="IPayload"/> and do all by your hands :)
    /// </summary>
    public abstract class DtoPayloadBase : IPayload
    {
        /// <summary>
        /// Usually, it's a DtoOf{[current class]}.<br />
        /// Or simplier: our carrier, parent.
        /// </summary>
        protected Dto _dtoCarrier;

        /// <inheritdoc />
        public Dto GetCarrier() => _dtoCarrier;

        /// <inheritdoc />
        public void RegisterCarrier(Dto dtoCarrier) => _dtoCarrier = dtoCarrier;
    }
}
