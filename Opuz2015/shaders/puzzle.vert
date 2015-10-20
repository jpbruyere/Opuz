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
out vec3 vLight;


void main(void)
{	
	texCoord = vec2(in_position) / ImgSize;
	vLight = vec3(ModelView * vec4(-1.0,-1.0,-1.0,1.0));
	gl_Position = Projection * ModelView * Model * vec4(in_position, 1);
}
