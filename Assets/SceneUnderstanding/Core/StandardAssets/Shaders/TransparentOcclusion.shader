Shader "Unlit/TransparentOcclusion"
{
    Properties
    {
    }
    SubShader
    {
        Tags {"Queue" = "Geometry" }
        
        Pass {
            ZWrite On
            ColorMask 0
        }  
    }
}
