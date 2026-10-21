shader_type canvas_item;
uniform vec2 drift = vec2(0.0);
void fragment() {
 vec2 p = FRAGCOORD.xy + drift;
 float dots = step(0.80, length(fract(p / 8.0) - vec2(0.5)));
 float line = step(0.94, fract(p.y / 64.0));
 vec3 ink = mix(vec3(0.063,0.055,0.106), vec3(0.105,0.083,0.16), dots * 0.35 + line * 0.12);
 COLOR = vec4(ink, 1.0);
}
