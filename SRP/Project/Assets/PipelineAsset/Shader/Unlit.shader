﻿Shader "My Pipeline/Unlit"
{
    Properties
    {
        _Color("Color", Color) = (1, 1, 1, 1)
        [Toggle(test_pragma_flag)]_Flag("Flag", float) = 0
    }
    SubShader
    {
        Pass
        {
            HLSLPROGRAM

#pragma target 3.5
#pragma multi_compile_instancing

#pragma vertex UnlitPassVertex
#pragma fragment UnlitPassFragment

#include "../ShaderLibrary/Unlit.hlsl"

            ENDHLSL
        }
    }
}
