using Ant0nRocket.Lib.Dodb.Dto.Payloads.Abstractions;

using Newtonsoft.Json;

namespace Ant0nRocket.Lib.Dodb.Dto.Payloads
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
        protected DtoBase? _dtoCarrier;

        /// <inheritdoc />
        public DtoBase? GetCarrier() => _dtoCarrier;

        /// <inheritdoc />
        public void RegisterCarrier(DtoBase dtoCarrier) => _dtoCarrier = dtoCarrier;

        /// <summary>
        /// Short-hand for <see cref="GetCarrier()"/>.Id 
        /// </summary>
        [JsonIgnore]
        public Guid __CarrierId => _dtoCarrier?.Id ?? default;
    }
}
