#version 330
precision highp float;

uniform vec4 color;
uniform float colMult;

uniform sampler2D tex;
uniform sampler2D texBords;

in vec2 texCoord;
out vec4 out_frag_color;

//kernel gaussian blur
const float[9] kgb = float[]  (1.,2.,1.,
                                2., 4., 2.,
                                1., 2., 1.);
//kenel emboos
const float[9] kem = float[]  ( 2., 1., 0.,
                          1., 1., -1.,
                          0., -1., -2.);

const ivec2[9] offsets = ivec2[]
(
	ivec2(-1,-1),
	ivec2( 0,-1),
	ivec2( 1,-1),
	ivec2(-1, 0),
	ivec2( 0, 0),
	ivec2( 1, 0),
	ivec2(-1,-1),
	ivec2( 0,-1),
	ivec2( 1,-1));

void main(void)
{
	vec4 accum = vec4(0.0,0.0,0.0,0.0);		
	accum += textureOffset( texBords, texCoord, offsets[0]) * kem[0];
	accum += textureOffset( texBords, texCoord, offsets[1]) * kem[1];
	accum += textureOffset( texBords, texCoord, offsets[2]) * kem[2];
	accum += textureOffset( texBords, texCoord, offsets[3]) * kem[3];
	accum += textureOffset( texBords, texCoord, offsets[4]) * kem[4];
	accum += textureOffset( texBords, texCoord, offsets[5]) * kem[5];
	accum += textureOffset( texBords, texCoord, offsets[6]) * kem[6];
	accum += textureOffset( texBords, texCoord, offsets[7]) * kem[7];
	accum += textureOffset( texBords, texCoord, offsets[8]) * kem[8];

	accum /= 8.0;

	out_frag_color = texture( tex, texCoord) * color * colMult - (accum.r * vec4(0.2,0.2,0.2,0.2));
}
