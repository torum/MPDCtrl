sampler2D input : register(s0);


/// <summary>The second input texture.</summary>
/// <defaultValue>c:\examplefolder\examplefile.jpg</defaultValue> 
sampler2D randomInput : register(s1);

/// <summary>Change the ratio between the two Textures. 0 is 100% input source, 1 is full noise</summary>
/// <minValue>0/minValue>
/// <maxValue>1</maxValue>
/// <defaultValue>.5</defaultValue>
float Ratio : register(C0);
float4 main(float2 uv : TEXCOORD) : COLOR 
{ 
 float4 inputTex = tex2D(input, uv); 
 float4 randomTex = tex2D(randomInput, uv); 
 float4 noisedTex = inputTex + (inputTex * randomTex*Ratio);
 return noisedTex;
}
