#version 330
precision highp float;

in vec2 texCoord;
out vec4 out_frag_color;

void main()
{
	/*float w = 0.001;
	if (gl_FragCoord.s > w && 
		gl_FragCoord.s < 1.0-w &&
		gl_FragCoord.t > w && 
		gl_FragCoord.t < 1.0-w)
		discard;*/


	out_frag_color = vec4(1.0,0.0,0.0,1.0);
}

