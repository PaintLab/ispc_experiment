/*x16*///AUTOGEN,2020-06-19T16:03:23
/*x17*///Tools: ispc and BridgeBuilder
/*x18*///Src: D:\projects\ispc_experiment\TestLoadLib\bin\Debug/temp/callback_test_ispc.h
/*x19*/
/*x20*/using System;
/*x21*/using System.Runtime.InteropServices;
/*x22*/
/*x23*/
using uint8_t = System.Byte;
using int8_t = System.SByte;
using uint16_t = System.UInt16;
using int16_t = System.Int16;
using int32_t = System.Int32;
using uint32_t = System.UInt32;

/*x24*/namespace callback_test_ispc{
/*x25*/public static unsafe class NativeMethods{
/*x26*/const string LIB_NAME="callback_test.dll";
/*x27*/[DllImport(LIB_NAME,EntryPoint ="my_clear")]
public static extern void clear(int32_t* v_in,int32_t newValue,int32_t count)/*x28*/;
/*x29*/}
/*x30*/}
