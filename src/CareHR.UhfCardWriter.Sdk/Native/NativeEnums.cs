namespace CareHR.UhfCardWriter.Sdk.Native;

/// <summary>
/// NOT defined in CFApi.h (no C enum).
/// Values = EPC Gen2 memory bank IDs used as WriteTag/ReadTag <c>memBank</c> byte.
/// Desk Reader sample names: FileType/Reserve=0, UII/EPC=1, TID=2, User=3.
/// </summary>
internal enum NativeMemBank : byte
{
    /// <summary>Gen2 Reserved (Desk Reader: FileType / Reserve).</summary>
    Reserved = 0x00,

    /// <summary>Gen2 EPC bank (Desk Reader: MemBank.UII).</summary>
    Epc = 0x01,

    /// <summary>Gen2 TID bank (Desk Reader: MemBank.TID).</summary>
    Tid = 0x02,

    /// <summary>Gen2 User bank (Desk Reader: MemBank.User).</summary>
    User = 0x03,
}

/// <summary>Aliases matching Desk Reader <c>MemBank</c> naming.</summary>
internal static class NativeMemBankNames
{
    public const NativeMemBank FileType = NativeMemBank.Reserved;
    public const NativeMemBank Uii = NativeMemBank.Epc;
}
