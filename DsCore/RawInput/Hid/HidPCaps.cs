using System.Runtime.InteropServices;

namespace DsCore.RawInput
{
    /// <summary>
    /// The HIDP_CAPS structure contains information about a top-level collection's capability.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct HidPCaps
    {
        /// <summary>
        /// Specifies a top-level collection's usage ID.
        /// </summary>
        public ushort Usage;
        /// <summary>
        /// Specifies the top-level collection's usage page.
        /// </summary>
        public ushort UsagePage;
        /// <summary>
        /// Specifies the maximum size, in bytes, of all the input reports 
        /// (including the report ID, if report IDs are used, which is prepended to the report data).
        /// </summary>
        public ushort InputReportByteLength;
        /// <summary>
        /// Specifies the maximum size, in bytes, of all the output reports 
        /// (including the report ID, if report IDs are used, which is prepended to the report data).
        /// </summary>
        public ushort OutputReportByteLength;
        /// <summary>
        /// Specifies the maximum length, in bytes, of all the feature reports 
        /// (including the report ID, if report IDs are used, which is prepended to the report data).
        /// </summary>
        public ushort FeatureReportByteLength;
        /// <summary>
        /// Reserved for internal system use.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
        public ushort[] Reserved;
        /// <summary>
        /// Specifies the number of HIDP_LINK_COLLECTION_NODE structures that are returned for this 
        /// top-level collection by HidP_GetLinkCollectionNodes.
        /// </summary>
        public ushort NumberLinkCollectionNodes;
        /// <summary>
        /// Specifies the number of input HIDP_BUTTON_CAPS structures that HidP_GetButtonCaps returns.
        /// </summary>
        public ushort NumberInputButtonCaps;
        /// <summary>
        /// Specifies the number of input HIDP_VALUE_CAPS structures that HidP_GetValueCaps returns.
        /// </summary>
        public ushort NumberInputValueCaps;
        /// <summary>
        /// Specifies the number of data indices assigned to buttons and values in all input reports.
        /// </summary>
        public ushort NumberInputDataIndices;
        /// <summary>
        /// Specifies the number of output HIDP_BUTTON_CAPS structures that HidP_GetButtonCaps returns.
        /// </summary>
        public ushort NumberOutputButtonCaps;
        /// <summary>
        /// Specifies the number of output HIDP_VALUE_CAPS structures that HidP_GetValueCaps returns.
        /// </summary>
        public ushort NumberOutputValueCaps;
        /// <summary>
        /// Specifies the number of data indices assigned to buttons and values in all output reports.
        /// </summary>
        public ushort NumberOutputDataIndices;
        /// <summary>
        /// Specifies the total number of feature HIDP_BUTTONS_CAPS structures that HidP_GetButtonCaps returns.
        /// </summary>
        public ushort NumberFeatureButtonCaps;
        /// <summary>
        /// Specifies the total number of feature HIDP_VALUE_CAPS structures that HidP_GetValueCaps returns.
        /// </summary>
        public ushort NumberFeatureValueCaps;
        /// <summary>
        /// Specifies the number of data indices assigned to buttons and values in all feature reports.
        /// </summary>
        public ushort NumberFeatureDataIndices;

        public override string ToString()
        {
            return string.Format("Usage: {0}, UsagePage: {1}, InputReportByteLength: {2}, NumberInputButtonCaps: {3}, NumberInputValueCaps: {4}",
                                         Usage, UsagePage, InputReportByteLength, NumberInputButtonCaps, NumberInputValueCaps);
           
        }
    }

}
