// Dear future players of a timeline when this game eventually came out,
// I am not good with shaders. There is no vertex shader, since the game
// started out as some lousy raycaster written in C#. The zbuffer is set
// as a parameter calculated while rendering the scene, and a projection
// buffer translates screen coordinates to world coordinates. Finally,
// a lightmap is used to calculate tile-wise lighting.

// Projects screen coordinates into world coordinates
Texture2D Projection : register(t1);
sampler ProjSampler : register(s1)
{
	Texture = <Projection>;
	Filter = Point;
}; 
// Maps world coordinates to light color on that tile
Texture2D LightMap : register(t2);
sampler LightSampler : register(s2)
{
	Texture = <LightMap>;
	Filter = Point;
};
// Screen sampler
sampler s0;
// Fog color used to shade distant objects
float4 SkyColor = float4(0, 0, 0, 1);
// Maximum view distance, over which everything will be colored SkyColor
float ViewDistance = 1;
// Ambient light: dims the scene at night or indoors. If pure white, other light cannot be seen.
float AmbientLightIntensity = 1;
// The normalized world position of the player
float2 PlayerPosition = float2(0, 0);

float4 main(float2 texCoord : TEXCOORD0) : COLOR0
{
	// Look up world coordinates at texCoord
	float4 ptex = tex2D(ProjSampler, texCoord);
	float2 proj = ptex.rg;
	// Look up light value at proj
	float4 light = tex2D(LightSampler, proj) * ptex.a;
	float ali = max(0, AmbientLightIntensity - .15) * (1 - ptex.b) + .15;

	float4 ambient = float4(ali, ali, ali, 1);
	light = clamp(light + ambient, 0, 1);

	// Return base pixel multiplied by light linearly interpolated with SkyColor over dist 
	float4 c = tex2D(s0, texCoord) * light;
	return lerp(c, lerp(SkyColor * ambient, c, ali * ViewDistance + ptex.b), distance(PlayerPosition, proj) / ViewDistance * (1-(light.r * light.g * light.b) / 1.5));
}
technique Technique1
{
	pass Lighting
	{
		PixelShader = compile ps_3_0 main();
	}
}