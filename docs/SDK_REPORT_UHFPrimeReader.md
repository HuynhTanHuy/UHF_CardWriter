# Báo cáo SDK UHFPrimeReader — Phase 1

**Nguồn:**
- Binary: `CF600+601+603+SDK\UHF Desk Reader SDK\API\x64\UHFPrimeReader.dll` (+ `hidapi.dll`)
- Header: `CF600+601+603+SDK\UHF Desk Reader SDK\API\Linux\CFApi.h`
- Sample C#: `Sample\C#\WindowsFormsApp1` (`Readercommon.cs`, `Class1.cs`)
- Desk Reader sample: `Source Code\Desk Reader` (flow ghi EPC)

**Ngày kiểm tra:** 2026-08-06  
**DLL đã copy vào:** `CareHR.UhfCardWriter\src\CareHR.UhfCardWriter.Sdk\Native\`

---

## 1. Danh sách API export (đã xác minh bằng `GetProcAddress`)

Tổng: **105** export có trong `API\x64\UHFPrimeReader.dll`.

```
CFHid_GetUsbCount
CFHid_GetUsbInfo
Close_Relay
Close_Relay2
CloseDevice
GET_RGW
Get4GInfo
GetAccessMode
GetAccessOperateParam
GetAntenna
GetAntPower
GetAntThreshold
GetBLEOutputMode
GetCoilPRM
GetDateTime
GetDeviceInfo
GetDevicePara
GetDirection
GetEASMask
GetEncryption
GetFreq
GetGateReponse
GetGateWorkParam
GetGPIOLevel
GetGpioPara
GetGPIOWorkParam
GetInfo
GetLongPermissonPara
GetMemoryData
GetMQTTConnectInfo
GetMQTTTopic1
GetMQTTTopic2
GetMQTTTopic3
GetMQTTTopic4
GetNetInfo
GetNTPInfo
GetOfflineLog
GetPermissonPara
GetPowerDelta
GetReadTagResp
GetRemoteNetInfo
GetRFIDType
GetRFPower
GetScreenParam
GetTagAndCustomUii
GetTagAndCustomUiiStart
GetTagResp
GetTagUii
GetTcpClientCount
GetTcpClientInfo
GetTemperature
GetWhiteList
GetwifiPara
InventoryContinue
InventoryStop
KillTag
LockTag
OpenDevice
OpenHidConnection
OpenNetConnection
OpenTcpServer
ReadTag
Release_Relay
Release_Relay2
SelectOrSortGet
SelectOrSortSet
SET_RGW
Set4GInfo
SetAccessMode
SetAccessOperateParam
SetAntenna
SetAntPower
SetAntThreshold
SetBLEOutputMode
SetCoilPRM
SetDateTime
SetDevicePara
SetDevicePara_J
SetDirection
SetEASMask
SetEncryption
SetFreq
SetGateWorkParam
SetGpioPara
SetGPIOWorkParam
SetGPOOutput
SetLongPermissonPara
SetMQTTConnectInfo
SetMQTTTopic1
SetMQTTTopic2
SetMQTTTopic3
SetMQTTTopic4
SetNetInfo
SetNTPInfo
SetPermissonPara
SetPowerDelta
SetRemoteNetInfo
SetRFIDType
SetRFPower
SetScreenParam
SetSelectMask
SetTemperature
SetWhiteList
SetwifiPara
WriteTag
```

**Ghi chú:** Một số tên xuất hiện trong bản dump chuỗi cũ / tài liệu khác (`GetSelectPRM`, `SetSelectPRM`, `GetRFParam`, `CFHid_OpenDevice`, …) **không có** trong binary `API\x64` hiện tại khi kiểm bằng `GetProcAddress` → **không dùng** cho Phase 2.

**Không có export tên `GetTagInfo`.** API lấy thông tin thẻ inventory là **`GetTagUii`** (trả struct `TagInfo`).

---

## 2. Danh sách struct trong `CFApi.h`

| Struct | Ghi chú |
|--------|---------|
| `PARA` | RF para rút gọn |
| `DeviceInfo` | Firmware/hardware/SN module |
| `DeviceFullInfo` | Device + module versions |
| `DevicePara` | Tham số thiết bị đầy đủ |
| `PermissonPara` | Permission/mask ngắn |
| `LongPermissonPara` | Permission/mask dài |
| `GpioPara` | GPIO/relay/protocol |
| `RssiPara` | RSSI filter |
| `WiFiPara` | WiFi |
| `NetInfo` | LAN |
| `RemoteNetInfo` | Remote net + heartbeat |
| `FreqInfo` | Frequency plan |
| `RFIcRegs` | RF IC register |
| `GBRFParam` | GB RF |
| `GBSortParam` | GB sort/select |
| `QueryParam` | Query |
| `TagInfo` | Kết quả inventory (`GetTagUii`) |
| `TagResp` | Kết quả access (`GetTagResp` / read path) |
| `ISORFParam` | ISO RF |
| `ISOSelectParam` | ISO select |
| `ISOQueryParam` | ISO query |
| `ISOPermalockParam` | Permalock |
| `CP_Sensi_Prm_Typ` | Sensitivity test |
| `CP_Sensi_Result_Typ` | Sensitivity result |
| `IQ_Axial_Typ` | IQ axial |
| `JSC_AUTO_SCAN_PRM_Typ` | Auto scan |
| `JSC_Data_Typ` | JSC data |
| `Read_Write_Reg_Cmd_Item_Typ` | Reg R/W item |
| `Read_Regs_Result_Typ` | Reg read result |
| `Int_Status_Item_Typ` | Interrupt status |
| `CR_Log_Item_Typ` | CR log |
| `SelectSortParam` | Select/sort (`SelectOrSort*`) |
| `AntPower` | Per-antenna power |
| `GPIOWorkParam` | GPIO work |
| `GateWorkParam` | Gate |
| `GateParam` | Gate event |
| `EASMask` | EAS |
| `Heartbeat` | Heartbeat |
| `AccessInfo` | Access info |
| `WhiteList` | Whitelist (lớn) |
| `AccessOperateParam` | Access operate |

---

## 3. Danh sách enum

**Trong `CFApi.h`: không có `enum` C.**

Trạng thái / lệnh / bank được biểu diễn bằng `#define` constants (mục 4).

**Ngoài header (sample Desk Reader / managed API `UHF_Reader_API`):** có `MemBank` (UII/EPC, TID, User, …) — **không nằm trong `CFApi.h`**. Phase 2 sẽ map `memBank` bằng `byte` constant theo Gen2 / sample (`MemBank.UII` khi ghi EPC, `wordPtr = 2`).

---

## 4. Danh sách constant (nhóm chính trong `CFApi.h`)

### 4.1 Status / error (`STAT_*`)
| Constant | Value |
|----------|-------|
| `STAT_OK` | `0x00000000` |
| `STAT_PORT_HANDLE_ERR` | `0xFFFFFF01` |
| `STAT_PORT_OPEN_FAILED` | `0xFFFFFF02` |
| `STAT_DLL_INNER_FAILED` | `0xFFFFFF03` |
| `STAT_CMD_PARAM_ERR` | `0xFFFFFF04` |
| `STAT_CMD_SERIAL_NUM_EXIT` | `0xFFFFFF05` |
| `STAT_CMD_INNER_ERR` | `0xFFFFFF06` |
| `STAT_CMD_INVENTORY_STOP` | `0xFFFFFF07` |
| `STAT_CMD_TAG_NO_RESP` | `0xFFFFFF08` |
| `STAT_CMD_DECODE_TAG_DATA_FAIL` | `0xFFFFFF09` |
| `STAT_CMD_CODE_OVERFLOW` | `0xFFFFFF0A` |
| `STAT_CMD_AUTH_FAIL` | `0xFFFFFF0B` |
| `STAT_CMD_PWD_ERR` | `0xFFFFFF0C` |
| `STAT_CMD_SAM_NO_RESP` | `0xFFFFFF0D` |
| `STAT_CMD_SAM_CMD_FAIL` | `0xFFFFFF0E` |
| `STAT_CMD_RESP_FORMAT_ERR` | `0xFFFFFF0F` |
| `STAT_CMD_HAS_MORE_DATA` | `0xFFFFFF10` |
| `STAT_CMD_BUF_OVERFLOW` | `0xFFFFFF11` |
| `STAT_CMD_COMM_TIMEOUT` | `0xFFFFFF12` |
| `STAT_CMD_COMM_WR_FAILED` | `0xFFFFFF13` |
| `STAT_CMD_COMM_RD_FAILED` | `0xFFFFFF14` |
| `STAT_CMD_NOMORE_DATA` | `0xFFFFFF15` |
| `STAT_DLL_UNCONNECT` | `0xFFFFFF16` |
| `STAT_DLL_DISCONNECT` | `0xFFFFFF17` |
| `STAT_CMD_RESP_CRC_ERR` | `0xFFFFFF18` |
| `STAT_CMD_IAP_CRC_ERR` | `0xFFFFFF21` |
| `STAT_CMD_DOWMLOAD_ERR` | `0xFFFFFF22` |
| `STAT_CMD_DOWM_NONE_ERR` | `0xFFFFFF23` |
| `STAT_GB_TAG_*` | `0xFFFFFF40`–`0xFFFFFF46` |
| `STAT_ISO_TAG_*` | `0xFFFFFF50`–`0xFFFFFF5D` |

### 4.2 Timeout
`DEF_READ_TIMEOUT=50`, `DEF_WRITE_TIMEOUT=1000`, `COMMON_TIMEOUT=2000`, `SPECIAL_TIMEOUT=300`, `TIMEOUT_1500/2000/4000/5000/10000`

### 4.3 ISO access command codes (dùng với `GetTagResp`)
| Constant | Value | Ý nghĩa |
|----------|-------|---------|
| `ISO_INVENTORY_CONTINUE` | `0x0001` | |
| `ISO_INVENTORY_STOP` | `0x0002` | |
| `ISO_READ_TAG` | `0x0003` | |
| `ISO_WRITE_TAG` | `0x0004` | **Write response** |
| `ISO_LOCK_TAG` | `0x0005` | |
| `ISO_KILL_TAG` | `0x0006` | |
| `ISO_SET_SELECTMASK` | `0x0007` | |

### 4.4 Reader response / tag response bytes
`R_RES_OK=0x00`, `R_RES_PARAM_ERR`, `R_RES_OPR_ERR`, `R_RES_TAG_NO_RESP=0x14`, `R_RES_TAG_PWD_ERR=0x17`, `R_RES_NOMORE_DATA=0xFF`, …  
`T_ISO_RES_*`, `T_GB_RES_*`, `HEAD_BYTE=0xCF`, `DEVICE_ADDR=0xFF`, `INVALID_HANDLE_VALUE=-1`

*(Còn nhiều `#define` lệnh reader/IAP/test/GB — xem đầy đủ trong `CFApi.h` dòng ~468–691; Phase 2 chỉ cần nhóm STAT + ISO_* + timeout.)*

---

## 5. CallingConvention

- Header: `extern "C" { int Api(...); }` — **không** ghi `__stdcall` / `__cdecl`.
- Sample C# SDK (`Readercommon.cs`): `[DllImport(...)]` **không** set `CallingConvention` → mặc định **`CallingConvention.Winapi`** (= **StdCall** trên Windows).
- Sample / `CareHR.RfidGateway` đã chạy thực tế với convention này.

**Quyết định Phase 2:** `CallingConvention.Winapi` (StdCall), khớp sample C#.

---

## 6. CharSet

- API có `char*` (`OpenDevice`, `OpenNetConnection`, `CFHid_GetUsbInfo`, …): sample dùng **`CharSet.Ansi`**.
- API chỉ `IntPtr` / số / byte[]: không cần CharSet (hoặc để mặc định).

**Quyết định Phase 2:** `CharSet.Ansi` trên mọi DllImport có chuỗi.

---

## 7. Struct packing

- `CFApi.h`: **không** có `#pragma pack`.
- Sample C#: `[StructLayout(LayoutKind.Sequential)]` — **không** set `Pack` → pack mặc định CLR (tương đương alignment tự nhiên, thường pack 8 trên x64 cho field alignment).
- `CareHR.RfidGateway`: cùng pattern `LayoutKind.Sequential` không `Pack`.

**Quyết định Phase 2:** `LayoutKind.Sequential`, **không** ép `Pack=1` trừ khi verify runtime lệch (ưu tiên khớp sample đang chạy).

---

## 8. Kích thước struct (`Marshal.SizeOf`, `LayoutKind.Sequential` mặc định)

Đo trên .NET (cùng máy build), layout **Sequential không Pack** — khớp sample C# `Class1.cs`.

| Struct | sizeof (bytes) | Phase 2 |
|--------|----------------:|---------|
| `TagInfo` | **266** | Bắt buộc |
| `TagResp` | **262** | Bắt buộc |
| `DevicePara` (sample: 8×byte + 3×ushort + 11×byte) | **26** | Optional diagnostics |
| `DeviceInfo` | 88 | Optional |
| `DeviceFullInfo` | 152 | Optional |
| `PARA` | 14 | Không (padding sau byte đầu) |
| `FreqInfo` | 10 | Không |
| `PermissonPara` | 21 | Không |
| `LongPermissonPara` | 40 | Không |
| `GpioPara` | 17 | Không |
| `RssiPara` | 18 | Không |
| `WiFiPara` | 103 | Không |
| `NetInfo` | 20 | Không |
| `RemoteNetInfo` | 8 | Không |
| `RFIcRegs` | 4 | Không |
| `GBRFParam` | 5 | Không |
| `GBSortParam` | 262 | Không |
| `QueryParam` | 3 | Không |
| `ISORFParam` | 16 | Không |
| `ISOSelectParam` | 250 | Không |
| `ISOQueryParam` | 3 | Không |
| `ISOPermalockParam` | 252 | Không |
| `SelectSortParam` | 38 | Không |
| `AntPower` | 9 | Không |
| `GPIOWorkParam` | 13 | Không |
| `GateWorkParam` | 7 | Không |
| `GateParam` | 6 | Không |
| `EASMask` | 34 | Không |
| `Heartbeat` | 35 | Không |
| `AccessInfo` | 4 | Không |
| `WhiteList` | 4102 | Không |
| `AccessOperateParam` | 17 | Không |
| `CP_Sensi_Prm_Typ` | 20 | Không |
| `CP_Sensi_Result_Typ` | 16 | Không |
| `IQ_Axial_Typ` | 4 | Không |
| `JSC_AUTO_SCAN_PRM_Typ` | 4 | Không |
| `JSC_Data_Typ` | 255 | Không |
| `Read_Write_Reg_Cmd_Item_Typ` | 8 | Không |
| `Read_Regs_Result_Typ` | 256 | Không |
| `Int_Status_Item_Typ` | 8 | Không |
| `CR_Log_Item_Typ` | 8 | Không |

**Lưu ý `DevicePara`:** `CFApi.h` khai `unsigned char STRATFREI[2]`…; sample C# dùng `ushort STRATFREI/STRATFRED/STEPFRE` (cùng 2 byte LE). Phase 2 theo **sample C#** (`ushort`) vì đã chứng minh chạy với DLL.

---

## 9. DLL phụ thuộc

| DLL | Bắt buộc kèm deploy | Vai trò |
|-----|---------------------|---------|
| `UHFPrimeReader.dll` | Có | SDK chính (x64) |
| `hidapi.dll` | Có (cùng thư mục exe) | Phụ thuộc HID; thiếu → `LoadLibrary` Win32 **126** |
| `KERNEL32.dll` | Hệ thống | |
| `WS2_32.dll` | Hệ thống | Network (`OpenNetConnection`) |

Có thể cần **VC++ Redistributable** nếu máy thiếu CRT (không thấy tên `VCRUNTIME140` trong chuỗi import của bản dump nhanh; runtime check trên máy dev hiện **LoadLibrary OK** với `hidapi` cạnh DLL).

---

## 10. Kiến trúc x64 / x86

| Hạng mục | Giá trị |
|----------|---------|
| PE Machine của `API\x64\UHFPrimeReader.dll` | `0x8664` (**x64**) |
| Folder SDK dùng cho project | `API\x64` |
| Solution `CareHR.UhfCardWriter` | `PlatformTarget=x64`, App `RuntimeIdentifier=win-x64` |
| Có sẵn `API\x86` | Có trong SDK vendor — **không** dùng cho build hiện tại |

---

## 11. Callback

**Không tìm thấy** trong `CFApi.h`:
- Không `typedef ... (*Callback)(...)`
- Không tham số function-pointer trên API public
- Inventory dùng **poll** `GetTagUii`, không callback async từ DLL

---

## 12. API sẽ sử dụng ở Phase 2 (Native Layer tối thiểu)

Phase 2 chỉ **khai báo** DllImport/struct/constant (chưa business). Tập tối thiểu cho flow ghi EPC đã thống nhất:

| API | Struct / constant liên quan |
|-----|-----------------------------|
| `OpenDevice` | — |
| `OpenHidConnection` | + `CFHid_GetUsbCount`, `CFHid_GetUsbInfo` |
| `OpenNetConnection` | — |
| `CloseDevice` | — |
| `InventoryContinue` | — |
| `GetTagUii` | `TagInfo` |
| `InventoryStop` | — |
| `SetSelectMask` | — |
| `WriteTag` | `accPwd`, `memBank`, `wordPtr`, `wordCount`, `writeData` |
| `GetTagResp` | `TagResp`, cmd=`ISO_WRITE_TAG` (`0x0004`) |
| `ReadTag` | — |
| `GetReadTagResp` | `TagResp` + buffer data |
| `LockTag` | (khai báo sẵn theo Phase 4 surface; chưa business) |
| `KillTag` | (khai báo sẵn theo Phase 4 surface; chưa business) |

**Constants Phase 2:** `STAT_*` chính, `ISO_*` access cmds, timeout macros.  
**Structs Phase 2 tối thiểu:** `TagInfo`, `TagResp`, (optional) `DevicePara` nếu cần connect diagnostics.

---

## 13. API không sử dụng (trong phạm vi app ghi thẻ CareHR hiện tại)

Toàn bộ export còn lại ngoài mục 12, gồm nhóm:

- MQTT / 4G / TCP server / NTP / Screen / Encryption / BLE / Gate / EAS / Whitelist / Access control  
- GPIO mở rộng, Relay2, Ant threshold/power nâng cao (trừ khi sau này cần chỉnh công suất: `SetRFPower`/`GetRFPower` có thể cân nhắc — **không bắt buộc Phase 2**)  
- `SelectOrSortGet` / `SelectOrSortSet` (dùng `SetSelectMask` theo Desk Reader)  
- `GetTagAndCustomUii*`  
- `SetDevicePara_J`, `GetMemoryData`, …  
- Mọi API **không có** trong binary x64 hiện tại (mục 1 ghi chú)

GB protocol / IAP / TEST macros trong header: **không** map sang DllImport Phase 2.

---

## Kết luận điều kiện chuyển Phase 2

- SDK x64 + `hidapi` + `CFApi.h` đủ tài liệu để viết Native Layer.  
- CallingConvention / CharSet / packing đã neo theo **sample C# đang dùng được**.  
- Không có callback; inventory = poll.  
- Không có `GetTagInfo` export — dùng `GetTagUii`.  

**Phase 1 đủ điều kiện chuyển Phase 2 (Native Layer).**

---

## 14. Phase 3 documentation (Driver readiness)

Phase 3 khóa Driver bằng tài liệu contract/ADR (không thay thế báo cáo SDK này):

- `docs/Architecture.md` — gate Phase 3 → Phase 4 (see also [archive/ArchitectureReadinessReview.md](archive/ArchitectureReadinessReview.md))  
- Living driver docs: [Driver.md](Driver.md); historical detail under `docs/archive/` (`DriverContract`, `ExceptionPolicy`, `MarshalGuideline`, …)  
- `docs/adr/ADR-001` … `ADR-005`  

Binary/header facts trong báo cáo này vẫn là nguồn sự thật cho Native Layer.
