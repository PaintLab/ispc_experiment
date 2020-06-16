/*x28*///AUTOGEN,2020-06-16T19:06:32
/*x29*/using System;
/*x30*/using System.Runtime.InteropServices;
/*x31*/using int32_t = System.Int32;
/*x32*/namespace simple_ispc{
/*x33*/public static unsafe class NativeMethods{
/*x34*/[DllImport("simple.dll",EntryPoint ="my_clear")]
public static extern void clear(int32_t* v_in,int32_t newValue,int32_t count)/*x35*/;
/*x36*/[DllImport("simple.dll",EntryPoint ="my_flipY_and_swap")]
public static extern void flipY_and_swap(int32_t* v_in,int32_t* v_out,int32_t width,int32_t height)/*x37*/;
/*x38*/[DllImport("simple.dll",EntryPoint ="my_simple")]
public static extern void simple(float* vin,float* vout,int32_t count)/*x39*/;
/*x40*/[DllImport("simple.dll",EntryPoint ="my_simple2")]
public static extern void simple2(int32_t* v_in,int32_t* v_out,int32_t count)/*x41*/;
/*x42*/[DllImport("simple.dll",EntryPoint ="my_swap_rb")]
public static extern void swap_rb(int32_t* v_in,int32_t count)/*x43*/;
/*x44*/}
/*x45*/}
