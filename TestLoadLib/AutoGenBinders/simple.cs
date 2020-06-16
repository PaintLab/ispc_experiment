/*x28*///AUTOGEN,2020-06-16T19:57:36
/*x29*///Tools: ispc/BridgeBuilder
/*x30*///Src: D:\projects\ispc_experiment\TestLoadLib\bin\Debug/temp/simple_ispc.h
/*x31*/
/*x32*/using System;
/*x33*/using System.Runtime.InteropServices;
/*x34*/using int32_t = System.Int32;
/*x35*/namespace simple_ispc{
/*x36*/public static unsafe class NativeMethods{
/*x37*/[DllImport("simple.dll",EntryPoint ="my_clear")]
public static extern void clear(int32_t* v_in,int32_t newValue,int32_t count)/*x38*/;
/*x39*/[DllImport("simple.dll",EntryPoint ="my_flipY_and_swap")]
public static extern void flipY_and_swap(int32_t* v_in,int32_t* v_out,int32_t width,int32_t height)/*x40*/;
/*x41*/[DllImport("simple.dll",EntryPoint ="my_simple")]
public static extern void simple(float* vin,float* vout,int32_t count)/*x42*/;
/*x43*/[DllImport("simple.dll",EntryPoint ="my_simple2")]
public static extern void simple2(int32_t* v_in,int32_t* v_out,int32_t count)/*x44*/;
/*x45*/[DllImport("simple.dll",EntryPoint ="my_swap_rb")]
public static extern void swap_rb(int32_t* v_in,int32_t count)/*x46*/;
/*x47*/}
/*x48*/}
