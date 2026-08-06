using System;
using System.Runtime.InteropServices;
class P {
  [StructLayout(LayoutKind.Sequential)] struct PARA { public byte RFIDPRO; public ushort STRATFREI,STRATFRED,STEPFRE; public byte CN,POWER,ANTENNA,REGION,RESERVED; }
  [StructLayout(LayoutKind.Sequential)] struct DeviceInfo { [MarshalAs(UnmanagedType.ByValArray,SizeConst=32)] public byte[] a; [MarshalAs(UnmanagedType.ByValArray,SizeConst=32)] public byte[] b; [MarshalAs(UnmanagedType.ByValArray,SizeConst=12)] public byte[] c; [MarshalAs(UnmanagedType.ByValArray,SizeConst=12)] public byte[] d; }
  [StructLayout(LayoutKind.Sequential)] struct DeviceFullInfo { [MarshalAs(UnmanagedType.ByValArray,SizeConst=32)] public byte[] a; [MarshalAs(UnmanagedType.ByValArray,SizeConst=32)] public byte[] b; [MarshalAs(UnmanagedType.ByValArray,SizeConst=12)] public byte[] c; [MarshalAs(UnmanagedType.ByValArray,SizeConst=32)] public byte[] d; [MarshalAs(UnmanagedType.ByValArray,SizeConst=32)] public byte[] e; [MarshalAs(UnmanagedType.ByValArray,SizeConst=12)] public byte[] f; }
  [StructLayout(LayoutKind.Sequential)] struct DevicePara { public byte DEVICEARRD,RFIDPRO,WORKMODE,INTERFACE,BAUDRATE,WGSET,ANT,REGION; [MarshalAs(UnmanagedType.ByValArray,SizeConst=2)] public byte[] STRATFREI; [MarshalAs(UnmanagedType.ByValArray,SizeConst=2)] public byte[] STRATFRED; [MarshalAs(UnmanagedType.ByValArray,SizeConst=2)] public byte[] STEPFRE; public byte CN,RFIDPOWER,INVENTORYAREA,QVALUE,SESSION,ACSADDR,ACSDATALEN,FILTERTIME,TRIGGLETIME,BUZZERTIME,INTENERLTIME; }
  [StructLayout(LayoutKind.Sequential)] struct PermissonPara { public byte CodeEn; [MarshalAs(UnmanagedType.ByValArray,SizeConst=4)] public byte[] Code; public byte MaskEn,StartAdd,MaskLen; [MarshalAs(UnmanagedType.ByValArray,SizeConst=12)] public byte[] MaskData; public byte MaskCondition; }
  [StructLayout(LayoutKind.Sequential)] struct LongPermissonPara { public byte CodeEn; [MarshalAs(UnmanagedType.ByValArray,SizeConst=4)] public byte[] Code; public byte MaskEn,StartAdd,MaskLen; [MarshalAs(UnmanagedType.ByValArray,SizeConst=31)] public byte[] MaskData; public byte MaskCondition; }
  [StructLayout(LayoutKind.Sequential)] struct GpioPara { public byte KCEn,RelayTime,KCPowerEn,TriggleMode,BufferEn,ProtocolEn,ProtocolType; [MarshalAs(UnmanagedType.ByValArray,SizeConst=10)] public byte[] ProtocolFormat; }
  [StructLayout(LayoutKind.Sequential)] struct RssiPara { public short BasciRssi; [MarshalAs(UnmanagedType.ByValArray,SizeConst=16)] public byte[] AntDelta; }
  [StructLayout(LayoutKind.Sequential)] struct WiFiPara { public byte wifiEn; [MarshalAs(UnmanagedType.ByValArray,SizeConst=32)] public byte[] SSID; [MarshalAs(UnmanagedType.ByValArray,SizeConst=64)] public byte[] PASSWORD; [MarshalAs(UnmanagedType.ByValArray,SizeConst=4)] public byte[] IP; [MarshalAs(UnmanagedType.ByValArray,SizeConst=2)] public byte[] PORT; }
  [StructLayout(LayoutKind.Sequential)] struct NetInfo { [MarshalAs(UnmanagedType.ByValArray,SizeConst=4)] public byte[] IP; [MarshalAs(UnmanagedType.ByValArray,SizeConst=6)] public byte[] MAC; [MarshalAs(UnmanagedType.ByValArray,SizeConst=2)] public byte[] PORT; [MarshalAs(UnmanagedType.ByValArray,SizeConst=4)] public byte[] NetMask; [MarshalAs(UnmanagedType.ByValArray,SizeConst=4)] public byte[] Gateway; }
  [StructLayout(LayoutKind.Sequential)] struct RemoteNetInfo { public byte Enable; [MarshalAs(UnmanagedType.ByValArray,SizeConst=4)] public byte[] IP; [MarshalAs(UnmanagedType.ByValArray,SizeConst=2)] public byte[] PORT; public byte HeartTime; }
  [StructLayout(LayoutKind.Sequential)] struct FreqInfo { public byte region; public ushort StartFreq,StopFreq,StepFreq; public byte cnt; }
  [StructLayout(LayoutKind.Sequential)] struct RFIcRegs { public ushort addr; public byte val; }
  [StructLayout(LayoutKind.Sequential)] struct GBRFParam { public byte tc,blf,miller,trext,modu; }
  [StructLayout(LayoutKind.Sequential)] struct GBSortParam { public byte target,action,memBank; public ushort maskPtr; public byte maskLen; [MarshalAs(UnmanagedType.ByValArray,SizeConst=255)] public byte[] maskData; }
  [StructLayout(LayoutKind.Sequential)] struct QueryParam { public byte condition,session,target; }
  [StructLayout(LayoutKind.Sequential)] struct TagInfo { public ushort NO; public short rssi; public byte antenna,channel; [MarshalAs(UnmanagedType.ByValArray,SizeConst=2)] public byte[] crc; [MarshalAs(UnmanagedType.ByValArray,SizeConst=2)] public byte[] pc; public byte codeLen; [MarshalAs(UnmanagedType.ByValArray,SizeConst=255)] public byte[] code; }
  [StructLayout(LayoutKind.Sequential)] struct TagResp { public byte tagStatus,antenna; [MarshalAs(UnmanagedType.ByValArray,SizeConst=2)] public byte[] crc; [MarshalAs(UnmanagedType.ByValArray,SizeConst=2)] public byte[] pc; public byte codeLen; [MarshalAs(UnmanagedType.ByValArray,SizeConst=255)] public byte[] code; }
  [StructLayout(LayoutKind.Sequential)] struct ISORFParam { public float tari,rtcal,trcal; public byte dr,miller,trext,modu; }
  [StructLayout(LayoutKind.Sequential)] struct ISOSelectParam { public byte resv,trucate,target,action,membank; public ushort ptr; public byte len; [MarshalAs(UnmanagedType.ByValArray,SizeConst=240)] public byte[] mask; }
  [StructLayout(LayoutKind.Sequential)] struct ISOQueryParam { public byte sel,session,target; }
  [StructLayout(LayoutKind.Sequential)] struct ISOPermalockParam { public byte readlock,membank; public ushort blockPtr; public byte blockRange; [MarshalAs(UnmanagedType.ByValArray,SizeConst=247)] public byte[] mask; }
  [StructLayout(LayoutKind.Sequential)] struct SelectSortParam { public byte target,trucate,action,membank; public ushort m_ptr; public byte len; [MarshalAs(UnmanagedType.ByValArray,SizeConst=31)] public byte[] mask; }
  [StructLayout(LayoutKind.Sequential)] struct AntPower { public byte Enable; [MarshalAs(UnmanagedType.ByValArray,SizeConst=8)] public byte[] AntPowerArr; }
  [StructLayout(LayoutKind.Sequential)] struct GPIOWorkParam { public byte Mode,GPIEnable,InLevel,GPOEnable,PutLevel; [MarshalAs(UnmanagedType.ByValArray,SizeConst=8)] public byte[] PutTime; }
  [StructLayout(LayoutKind.Sequential)] struct GateWorkParam { public byte GateMode,GateGPI1,GateGPI2,GatePower,GateRead,EASMode,EASGPO; }
  [StructLayout(LayoutKind.Sequential)] struct GateParam { public byte DIR,GPI; [MarshalAs(UnmanagedType.ByValArray,SizeConst=4)] public byte[] SYSTIME; }
  [StructLayout(LayoutKind.Sequential)] struct EASMask { public byte Addr,Len; [MarshalAs(UnmanagedType.ByValArray,SizeConst=32)] public byte[] Data; }
  [StructLayout(LayoutKind.Sequential)] struct Heartbeat { public byte Enable,Time,Len; [MarshalAs(UnmanagedType.ByValArray,SizeConst=32)] public byte[] Data; }
  [StructLayout(LayoutKind.Sequential)] struct AccessInfo { public byte STATE; public ushort CUSTOMERCOUNT; }
  [StructLayout(LayoutKind.Sequential)] struct WhiteList { public byte STATUS; public ushort FRAMENUM; public byte INFOCOUNT; [MarshalAs(UnmanagedType.ByValArray,SizeConst=4096)] public byte[] WHITELIST; }
  [StructLayout(LayoutKind.Sequential)] struct AccessOperateParam { public byte LISTENABLE,READGPIFUNC,FRONTGPIFUNC,BACKGPIFUNC,BUTTONGPIFUNC; [MarshalAs(UnmanagedType.ByValArray,SizeConst=4)] public byte[] RECVGPIEXEFUNC; public byte ACCESSCTRLEXEPART; [MarshalAs(UnmanagedType.ByValArray,SizeConst=7)] public byte[] RECVACTIONEXEPART; }
  static void Main(){
    void S(string n,Type t){ Console.WriteLine(n+"="+Marshal.SizeOf(t)); }
    S("PARA",typeof(PARA)); S("DeviceInfo",typeof(DeviceInfo)); S("DeviceFullInfo",typeof(DeviceFullInfo)); S("DevicePara",typeof(DevicePara));
    S("PermissonPara",typeof(PermissonPara)); S("LongPermissonPara",typeof(LongPermissonPara)); S("GpioPara",typeof(GpioPara)); S("RssiPara",typeof(RssiPara));
    S("WiFiPara",typeof(WiFiPara)); S("NetInfo",typeof(NetInfo)); S("RemoteNetInfo",typeof(RemoteNetInfo)); S("FreqInfo",typeof(FreqInfo));
    S("RFIcRegs",typeof(RFIcRegs)); S("GBRFParam",typeof(GBRFParam)); S("GBSortParam",typeof(GBSortParam)); S("QueryParam",typeof(QueryParam));
    S("TagInfo",typeof(TagInfo)); S("TagResp",typeof(TagResp)); S("ISORFParam",typeof(ISORFParam)); S("ISOSelectParam",typeof(ISOSelectParam));
    S("ISOQueryParam",typeof(ISOQueryParam)); S("ISOPermalockParam",typeof(ISOPermalockParam)); S("SelectSortParam",typeof(SelectSortParam));
    S("AntPower",typeof(AntPower)); S("GPIOWorkParam",typeof(GPIOWorkParam)); S("GateWorkParam",typeof(GateWorkParam)); S("GateParam",typeof(GateParam));
    S("EASMask",typeof(EASMask)); S("Heartbeat",typeof(Heartbeat)); S("AccessInfo",typeof(AccessInfo)); S("WhiteList",typeof(WhiteList)); S("AccessOperateParam",typeof(AccessOperateParam));
  }
}
