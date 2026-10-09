shader_type spatial;

uniform vec3 bloodColor : source_color;
uniform float time_scale : hint_range(0.0, 10.0, 0.25) = 1.0;
uniform float uv_scale : hint_range(0.1, 100.0, 0.1) = 10.0; // Add this!
uniform sampler2D bloodNormalNoise : repeat_enable; // Ensure repeat is on

void fragment() {
    // 1. Scale the UVs first
    vec2 _uv = UV * uv_scale;
    
    // 2. Add the flow (Time-based movement)
    vec2 flow = vec2(TIME * time_scale * 0.1, TIME * time_scale * 0.1);
    _uv += flow;
    
    // 3. Apply your original wobble
    _uv.x += sin(TIME * time_scale + (_uv.x + _uv.y) * 25.0) * 0.01;
    _uv.y += cos(TIME * time_scale + (_uv.x - _uv.y) * 25.0) * 0.01;
    
    ALBEDO = bloodColor;
    NORMAL_MAP = texture(bloodNormalNoise, _uv).rgb;
    
    NORMAL_MAP_DEPTH = 4.0; 
    ROUGHNESS = 0.2;
}