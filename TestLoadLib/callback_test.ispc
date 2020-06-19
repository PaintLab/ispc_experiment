//MIT, 2020, WinterDev

extern "C"  void appFunc(uniform int activeLanes);

export void clear(uniform int v_in[],uniform int newValue,uniform int count){
    foreach (index = 0 ... count) { 
        v_in[index]= newValue;
    }    
    appFunc(lanemask());
}
 