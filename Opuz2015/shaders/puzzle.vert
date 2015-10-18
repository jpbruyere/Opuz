#version 330

precision highp float;

uniform mat4 Projection;
uniform mat4 ModelView;
uniform mat4 Model;
uniform mat4 Normal;
uniform vec2 ImgSize;

in vec3 in_position;
in vec2 in_tex;

out vec2 texCoord;


void main(void)
{	
	texCoord = vec2(in_position) / ImgSize;
	gl_Position = Projection * ModelView * Model * vec4(in_position, 1);
}
