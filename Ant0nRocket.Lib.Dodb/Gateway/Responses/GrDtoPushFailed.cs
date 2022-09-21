using Ant0nRocket.Lib.Dodb.Dto;
using Ant0nRocket.Lib.Dodb.Enums;
using Ant0nRocket.Lib.Dodb.Gateway.Abstractions;
using Ant0nRocket.Lib.Std20.Extensions;

namespace Ant0nRocket.Lib.Dodb.Gateway.Responses
{
    /// <summary>
    /// <see cref="Dodb"/> will return this when something goes wrong.
    /// </summary>
    public class GrDtoPushFailed : IGatewayResponse
    {
        /// <summary>
        /// <inheritdoc cref="Enums.GrPushFailReason"/>
        /// <see cref="Enums.GrPushFailReason"/>
        /// </summary>
        public GrPushFailReason Reason { get; set; } = GrPushFailReason.OtherReasons;

        private DtoBase? _dto;
        /// <summary>
        /// Instance of a problem DTO.
        /// </summary>
        public DtoBase? Dto
        {
            get => _dto;
            set
            {
                _dto = value;
                try
                {
                    var payloadInstance = _dto?.GetPropertyValue("Payload");
                    DtoPayloadTypeName = payloadInstance?.GetType().Name;
                }
                catch
                {
                    DtoPayloadTypeName = "Empty";
                }
            }
        }

        /// <summary>
        /// Id of a DTO.
        /// </summary>
        public Guid? DtoId => Dto?.Id;

        /// <summary>
        /// DTO payload type name.
        /// </summary>
        public string? DtoPayloadTypeName { get; private set; }

        /// <summary>
        /// Comments for fail reason (validation errors, etc.)
        /// </summary>
        public List<string> Messages { get; } = new();

        /// <summary>
        /// Default constructor.
        /// </summary>
        public GrDtoPushFailed() { }

        /// <summary>
        /// Auto-set <see cref="Dto"/> and addes one <paramref name="message"/> to messages list.
        /// </summary>
        public GrDtoPushFailed(
            DtoBase dto, 
            string message, 
            GrPushFailReason reason = GrPushFailReason.ValidationFailed)
        {
            Dto = dto;
            Messages.Add(message);
            Reason = reason;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var messageIndex = 0;
            var messages = string.Join("; ", Messages.Select(m => $"{++messageIndex}. {m}"));
            return $"{nameof(GrDtoPushFailed)}: " +
                $"[{Reason}] Id='{DtoId}', " +
                $"PayloadType='{DtoPayloadTypeName}', " +
                $"Messages: [{messages}]";
        }
    }
}
