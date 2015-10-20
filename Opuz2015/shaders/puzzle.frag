#version 330
precision highp float;

uniform vec4 color;
uniform float colMult;
uniform sampler2D tex;

in vec2 texCoord;
out vec4 out_frag_color;

void main(void)
{
	out_frag_color = texture( tex, texCoord) * color * colMult;
}
