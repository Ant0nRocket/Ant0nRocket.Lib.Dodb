namespace Ant0nRocket.Lib.Dodb.Enums
{
    /// <summary>
    /// Describes the reason why DTO(s) applying were failed. 
    /// </summary>
    public enum GrPushFailReason
    {
        /// <summary>
        /// Document with <see cref="Dto.DtoBase.Id"/> already exists in database.
        /// </summary>
        DocumentExists,

        /// <summary>
        /// Document with <see cref="Dto.DtoBase.RequiredDocumentId"/> need to
        /// exists in database in order to apply current DTO.
        /// </summary>
        RequiredDocumentNotExists,

        /// <summary>
        /// Every document (except very first one) must have <see cref="Dto.DtoBase.RequiredDocumentId"/>
        /// </summary>
        RequiredDocumentNotSpecified,

        /// <summary>
        /// No handler registred for current <see cref="Dto.DtoOf{T}.Payload"/> type.
        /// </summary>
        PayloadHandlerNotFound,

        /// <summary>
        /// DTO didn't pass the validation.
        /// </summary>
        ValidationFailed,

        /// <summary>
        /// All checks were passed but database doesn'r like the result (usually, 
        /// foreign keys are missed somewhere).
        /// </summary>
        DatabaseError,

        /// <summary>
        /// When response has a type that is not <see cref="Gateway.Responses.GrDtoPushSuccess"/> and not
        /// <see cref="Gateway.Responses.GrDtoPushFailed"/>.
        /// </summary>
        UnknownResultType,

        /// <summary>
        /// Reserved for on-top libraries. See 
        /// </summary>
        OtherReasons,
    }
}
