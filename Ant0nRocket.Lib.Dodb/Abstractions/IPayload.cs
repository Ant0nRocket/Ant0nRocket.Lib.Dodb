using Ant0nRocket.Lib.Dodb.Dtos;

namespace Ant0nRocket.Lib.Dodb.Abstractions
{
    /// <summary>
    /// Describes a Payload class. Every class that pretending
    /// to a payload must fit this interface.
    /// </summary>
    public interface IPayload
    {
        /// <summary>
        /// This method will be called when Payload property
        /// of DtoOf{T} will be set.
        /// </summary>
        /// <param name="dtoCarrier"></param>
        void RegisterCarrier(Dto dtoCarrier);

        /// <summary>
        /// Call this if you need to get carrier instance.
        /// </summary>
        /// <returns></returns>
        Dto GetCarrier();
    }
}
