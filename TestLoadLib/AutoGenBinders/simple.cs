/*x30*///AUTOGEN,2020-06-16T20:46:42
/*x31*///Tools: ispc and BridgeBuilder
/*x32*///Src: D:\projects\ispc_experiment\TestLoadLib\bin\Debug/temp/simple_ispc.h
/*x33*/
/*x34*/using System;
/*x35*/using System.Runtime.InteropServices;
/*x36*/using int32_t = System.Int32;
/*x37*/namespace simple_ispc{
/*x38*/public static unsafe class NativeMethods{
/*x39*/const string LIB_NAME="simple.dll";
/*x40*/[DllImport(LIB_NAME,EntryPoint ="my_clear")]
public static extern void clear(int32_t* v_in,int32_t newValue,int32_t count)/*x41*/;
/*x42*/[DllImport(LIB_NAME,EntryPoint ="my_flipY_and_swap")]
public static extern void flipY_and_swap(int32_t* v_in,int32_t* v_out,int32_t width,int32_t height)/*x43*/;
/*x44*/[DllImport(LIB_NAME,EntryPoint ="my_simple")]
public static extern void simple(float* vin,float* vout,int32_t count)/*x45*/;
/*x46*/[DllImport(LIB_NAME,EntryPoint ="my_simple2")]
public static extern void simple2(int32_t* v_in,int32_t* v_out,int32_t count)/*x47*/;
/*x48*/[DllImport(LIB_NAME,EntryPoint ="my_swap_rb")]
public static extern void swap_rb(int32_t* v_in,int32_t count)/*x49*/;
/*x50*/}
/*x51*/}
