using CareHR.UhfCardWriter.Sdk.Models;

namespace CareHR.UhfCardWriter.Sdk;

/// <summary>
/// Select / lock / kill primitives (single Driver calls).
/// </summary>
public interface IUhfTagControl
{
    /// <summary>Sets the Gen2 select mask.</summary>
    /// <param name="maskPtr">Mask bit pointer.</param>
    /// <param name="maskBits">Number of mask bits.</param>
    /// <param name="mask">Mask bytes.</param>
    /// <returns>SDK result.</returns>
    SdkResult Select(ushort maskPtr, byte maskBits, byte[] mask);

    /// <summary>Issues a lock command.</summary>
    /// <param name="accessPassword">Exactly four bytes.</param>
    /// <param name="area">Lock area per vendor SDK.</param>
    /// <param name="action">Lock action per vendor SDK.</param>
    /// <returns>SDK result.</returns>
    SdkResult Lock(byte[] accessPassword, byte area, byte action);

    /// <summary>Issues a kill command.</summary>
    /// <param name="accessPassword">Exactly four bytes.</param>
    /// <returns>SDK result.</returns>
    SdkResult Kill(byte[] accessPassword);
}
