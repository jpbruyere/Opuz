#version 330

precision highp float;

uniform mat4 mvp;
uniform mat4 model;
uniform vec2 imgSize;

in vec3 in_position;
in vec2 in_tex;

out vec2 texCoord;

void main(void)
{	
	texCoord = vec2(in_position) / imgSize;
	gl_Position = mvp * model * vec4(in_position, 1);
}
