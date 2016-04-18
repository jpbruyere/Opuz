#version 330
#extension GL_ARB_shader_subroutine : require
#extension GL_ARB_shader_texture_image_samples : require
#extension GL_ARB_sample_shading : require

precision highp float;

uniform sampler2DMS tex;
uniform sampler2DMS depthTex;

in vec2 texCoord;
out vec4 out_frag_color;

void main(void)
{
	int samples = textureSamples(tex);
	ivec2 texcoord = ivec2 (textureSize (tex) * texCoord);
	int i = gl_SampleID;

	//float depth = texelFetch (depthTex, texcoord, i).x;
	vec4 c = texelFetch (tex, texcoord, i);
	//if (c.a == 0.0)
	//	discard;

	out_frag_color = vec4(c.rgb,1.0);
}

