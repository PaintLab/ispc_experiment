/*x15*///AUTOGEN,2020-06-19T15:52:13
/*x16*///Tools: ispc and BridgeBuilder
/*x17*///Src: D:\projects\ispc_experiment\TestLoadLib\bin\Debug/temp/callback_test_ispc.h
/*x18*/
/*x19*/using System;
/*x20*/using System.Runtime.InteropServices;
/*x21*/
/*x22*/
using uint8_t = System.Byte;
using int8_t = System.SByte;
using uint16_t = System.UInt16;
using int16_t = System.Int16;
using int32_t = System.Int32;
using uint32_t = System.UInt32;

/*x23*/namespace callback_test_ispc{
/*x24*/public static unsafe class NativeMethods{
/*x25*/const string LIB_NAME="callback_test.dll";
/*x26*/[DllImport(LIB_NAME,EntryPoint ="my_clear")]
public static extern void clear(int32_t* v_in,int32_t newValue,int32_t count)/*x27*/;
/*x28*/}
/*x29*/}
