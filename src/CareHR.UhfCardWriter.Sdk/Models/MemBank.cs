namespace CareHR.UhfCardWriter.Sdk.Models;

/// <summary>
/// EPC Gen2 memory bank identifiers used by write/read APIs.
/// </summary>
/// <remarks>Values match Desk Reader sample naming (UII = EPC bank).</remarks>
public enum MemBank : byte
{
    /// <summary>Gen2 Reserved bank.</summary>
    Reserved = 0x00,

    /// <summary>Gen2 EPC / UII bank.</summary>
    Epc = 0x01,

    /// <summary>Gen2 TID bank.</summary>
    Tid = 0x02,

    /// <summary>Gen2 User bank.</summary>
    User = 0x03,
}
