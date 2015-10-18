#version 330
precision highp float;

in vec2 texCoord;
out vec4 out_frag_color;

void main()
{
	float w = 0.01;
	if (texCoord.s > w && 
		texCoord.s < 1.0-w &&
		texCoord.t > w && 
		texCoord.t < 1.0-w)
		discard;
	out_frag_color = vec4(1.0,0.0,0.0,0.4);
}

