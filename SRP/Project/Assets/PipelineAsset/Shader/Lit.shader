Shader "My Pipeline/Lit"
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

#pragma vertex LitPassVertex
#pragma fragment LitPassFragment

#include "../ShaderLibrary/Lit.hlsl"

            ENDHLSL
        }
    }
}
