shader_type canvas_item;
uniform vec2 drift = vec2(0.0);
uniform float motion_time = 0.0;
void fragment() {
 vec2 p = FRAGCOORD.xy + drift + vec2(motion_time * 1.5, 0.0);
 float dots = 1.0 - step(0.18, length(fract(p / 8.0) - vec2(0.5)));
 float block = floor((UV.x + drift.x * 0.001) * 22.0);
 float height = 0.16 + fract(sin(block * 73.71) * 957.13) * 0.25;
 float skyline = step(1.0-height, UV.y);
 float window = step(0.75, fract(p.x / 17.0)) * step(0.72, fract(p.y / 23.0)) * skyline;
 vec3 ink = mix(vec3(0.063,0.055,0.106), vec3(0.12,0.09,0.23), dots * 0.5 + skyline * 0.32);
 ink += vec3(0.09,0.055,0.04) * window;
 COLOR = vec4(ink, 1.0);
}
