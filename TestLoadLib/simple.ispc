/*
  Copyright (c) 2010-2011, Intel Corporation
  All rights reserved.

  Redistribution and use in source and binary forms, with or without
  modification, are permitted provided that the following conditions are
  met:

    * Redistributions of source code must retain the above copyright
      notice, this list of conditions and the following disclaimer.

    * Redistributions in binary form must reproduce the above copyright
      notice, this list of conditions and the following disclaimer in the
      documentation and/or other materials provided with the distribution.

    * Neither the name of Intel Corporation nor the names of its
      contributors may be used to endorse or promote products derived from
      this software without specific prior written permission.


   THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS
   IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED
   TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A
   PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER
   OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
   EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
   PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
   PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
   LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
   NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
   SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/


export void simple(uniform float vin[], uniform float vout[],
                   uniform int count) {
    foreach (index = 0 ... count) {
        // Load the appropriate input value for this program instance.
        float v = vin[index];
      
        // Do an arbitrary little computation, but at least make the
        // computation dependent on the value being processed
        if (v < 3.)
            v = v * v;
        else
            v = sqrt(v);

        // And write the result to the output array.
        vout[index] = v;      
    } 
}
export void simple2(uniform int v_in[], uniform int v_out[], uniform int count){
     foreach (index = 0 ... count) {
        // Load the appropriate input value for this program instance.
        int v = v_in[index]; 

        // And write the result to the output array.
        v_out[index] = v;
    }      
}
export void swap_rb(uniform int v_in[], uniform int count){
     foreach (index = 0 ... count) {
        // Load the appropriate input value for this program instance.
        int src = v_in[index];
        v_in[index]= (((((src >> 24) & 0xFF)) << 24) |
                                                ((((src >> 0) & 0xFF)) << 16) |
                                                ((((src >> 8) & 0xFF)) << 8) |
                                                (((src >> 16) & 0xFF)));
    }      
}

export void clear(uniform int v_in[],uniform int newValue,uniform int count){
     foreach (index = 0 ... count) { 
        v_in[index]= newValue;
    }    
}
export void flipY_and_swap(uniform int v_in[],
                           uniform int v_out[],
                           uniform int width,
                           uniform int height){
                             
   int hh=0;
   for(uniform int iy=height-1;iy>=0;--iy) {
      int src_index= iy *width;
      int target_index= hh *width;
     
      foreach (index = 0 ... width) {       
          // Load the appropriate input value for this program instance.
          int src= v_in[src_index+index];
          v_out[target_index + index]= (((((src >> 24) & 0xFF)) << 24) |
                                                  ((((src >> 0) & 0xFF)) << 16) |
                                                  ((((src >> 8) & 0xFF)) << 8) |
                                                  (((src >> 16) & 0xFF)));
      }      
      hh++;
    }
}