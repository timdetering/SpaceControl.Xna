string description = "MultiTexturing Shader";

Texture xMainTexture : Diffuse
<
	string ResourceName = "DepolymentBase.png";
>;

float4x4 xView;
float4x4 xProjection;
float4x4 xWorld;

sampler MainTextureSampler = sampler_state
{
	texture = <xMainTexture>;
	MagFilter =linear;
	MipFilter=linear;
	MinFilter=linear;
	AddressU=wrap;
	AddressV=wrap;
};

struct vertexToPixel
{
	float4 position : POSITION0;
	float4 color : COLOR0;
	float4 TextureCoord : TEXCOORD0;
};

struct PixelToScreen
{
	float4 color : COLOR0;
};
vertexToPixel VS_TransformAndTexture(float4 inPos : POSITION0,
									 float4 inTexCoord : TEXCOORD0,
									 float4 inColor : COLOR0)
{
	vertexToPixel OUT = (vertexToPixel)0;
	
	float4x4 preViewNormal = mul(xView, xProjection);
	float4x4 preWorldViewProj = mul(xWorld, preViewNormal);
	
	OUT.position = mul(inPos, preWorldViewProj);
	OUT.TextureCoord = inTexCoord;
	OUT.color = inColor;
	
	return OUT;
}


PixelToScreen PS_Textured(vertexToPixel IN)
{
	PixelToScreen finalColor = (PixelToScreen)0;

	//get texture coord between 0 and 1 for the strip color
	float texX = clamp((IN.TextureCoord.x), -1, 10);
	
	float texZeroToOne = ceil(texX) - texX;
	texZeroToOne = clamp(texZeroToOne, .5, 1);
	texZeroToOne = (texZeroToOne - .5f) * 20;
	texZeroToOne = clamp(texZeroToOne, 0, 1);
	float4 highlightColor = IN.color;
	float4 baseColor = tex2D(MainTextureSampler, IN.TextureCoord);
	highlightColor.a = 0.0f;
	finalColor.color.rgb = (highlightColor.rgb * texZeroToOne)  + (baseColor.rgb * (1-texZeroToOne));
	finalColor.color.a = baseColor.a;
	return finalColor;
	
}
//-----------------------------------
technique textured
{
    pass p0 
    {		
		VertexShader = compile vs_1_1 VS_TransformAndTexture();
		PixelShader  = compile ps_2_0 PS_Textured();
    }
}