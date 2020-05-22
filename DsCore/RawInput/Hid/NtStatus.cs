
namespace DsCore.RawInput
{
    /// <summary>
    /// NTSTATUS
    /// </summary>
    public enum NtStatus : uint
    {
        Success = 0x00110000,
        Null = 0x80110001,
        InvalidPreparsedData = 0xC0110001,
        InvalidReportType = 0xC0110002,
        InvalidReportLength = 0xC0110003,
        UsageNotFound = 0xC0110004,
        ValueOutOfRange = 0xC0110005,
        BadLogPhyValues = 0xC0110006,
        BufferTooSmall = 0xC0110007,
        InternalError = 0xC0110008,
        I8042TransUnknown = 0xC0110009,
        IncompatibleReportId = 0xC011000A,
        NotValueArray = 0xC011000B,
        IsValueArray = 0xC011000C,
        DataIndexNotFound = 0xC011000D,
        DataIndexOutOfRange = 0xC011000E,
        ButtonNotPressed = 0xC011000F,
        ReportDoesNotExist = 0xC0110010,
        NotImplemented = 0xC0110020,
    }
}
