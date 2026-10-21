shader_type spatial;
render_mode unshaded, cull_disabled, depth_draw_never;
void fragment() {
 float radius = length((UV - vec2(0.5)) * vec2(2.0, 2.0));
 ALBEDO = vec3(0.005, 0.005, 0.015);
 ALPHA = (1.0 - smoothstep(0.12, 0.95, radius)) * 0.6;
}
