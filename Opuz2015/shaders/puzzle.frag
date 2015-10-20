#version 330
precision highp float;

uniform vec4 color;
uniform float colMult;
//uniform float kernel[9];
//uniform vec2 offsets[9];
uniform sampler2D tex;
uniform sampler2D texBords;

in vec2 texCoord;
in vec3 vLight;
out vec4 out_frag_color;



void main(void)
{
	//kernel gaussian blur
	float[9] kgb = float[]  (1.,2.,1.,
                                    2., 4., 2.,
                                    1., 2., 1.);
    //kenel emboos
	float[9] kem = float[]  ( 2., 1., 0.,
                              1., 1., -1.,
                              0., -1., -2.);
/*	float[9] kem = float[]  ( -1., -1., 0.,
                              -1., 2., 1.,
                              0., 1., 1.);*/
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
	//if (accum.r < 0.5)
	//	accum.r = 1.0;

	out_frag_color = texture( tex, texCoord) * color * colMult - (accum.r * vec4(0.2,0.2,0.2,0.2));
	//out_frag_color = texture( tex, texCoord) * color * colMult * accum.r;
}
