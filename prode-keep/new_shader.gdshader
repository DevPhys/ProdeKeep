shader_type canvas_item;

uniform float glitch_intensity = 0.7;
uniform float glitch_speed = 25.0;
uniform float block_size = 0.03;
uniform float color_shift_amount = 0.2;

// Настройки периодичности
uniform float min_interval = 5.0; // 10 минут
uniform float max_interval = 10.0; // 15 минут
uniform float glitch_duration = 4.0; // Более длительный глитч для хоррора

// Хоррор-эффекты
uniform float darkening = 0.3; // Затемнение во время глитча
uniform float red_tint = 0.2; // Красноватый оттенок
uniform float distortion_wave = 0.1; // Волновые искажения

float random(vec2 st) {
    return fract(sin(dot(st.xy, vec2(12.9898, 78.233))) * 43758.5453123);
}

float hash(float n) {
    return fract(sin(n) * 43758.5453123);
}

float get_glitch_active(float time) {
    float cycle = floor(time / max_interval);
    float offset = hash(cycle) * (max_interval - min_interval);
    float glitch_time = time - (cycle * max_interval + offset);
    
    // Более резкое появление для испуга
    float active = smoothstep(0.0, 0.1, glitch_time) * 
                   (1.0 - smoothstep(glitch_duration - 0.3, glitch_duration, glitch_time));
    
    return active * step(0.0, glitch_time) * step(glitch_time, glitch_duration);
}

void fragment() {
    vec2 uv = UV;
    float glitch_active = get_glitch_active(TIME);
    
    float time = TIME * glitch_speed;
    
    // Базовые глитч-эффекты
    float block_y = floor(uv.y / block_size);
    float glitch_block = step(0.6, random(vec2(block_y, floor(time)))) * glitch_active;
    float glitch_block2 = step(0.75, random(vec2(block_y * 3.0, floor(time * 2.0)))) * glitch_active;
    
    // Сильные искажения
    vec2 glitch_uv = uv;
    glitch_uv.x += (random(vec2(block_y, floor(time))) - 0.5) * glitch_intensity * glitch_block;
    glitch_uv.y += (random(vec2(block_y * 2.0, floor(time * 1.5))) - 0.5) * glitch_intensity * glitch_block2 * 0.5;
    
    // Волновые искажения для жуткого эффекта
    float wave = sin(uv.y * 30.0 + time * 5.0) * distortion_wave * glitch_active;
    glitch_uv.x += wave;
    
    // Цветовой сдвиг с красным оттенком
    float color_shift = (random(vec2(block_y, floor(time * 1.5))) - 0.5) * color_shift_amount * glitch_block;
    
    vec4 glitch_color;
    glitch_color.r = texture(TEXTURE, glitch_uv + vec2(color_shift * 1.5, 0.0)).r;
    glitch_color.g = texture(TEXTURE, glitch_uv).g;
    glitch_color.b = texture(TEXTURE, glitch_uv - vec2(color_shift, 0.0)).b;
    glitch_color.a = texture(TEXTURE, uv).a;
    
    // Оригинальная текстура
    vec4 original_color = texture(TEXTURE, uv);
    
    // Смешиваем
    vec4 color = mix(original_color, glitch_color, glitch_active);
    
    // Затемнение для атмосферы
    color.rgb *= 1.0 - (darkening * glitch_active);
    
    // Красноватый оттенок во время глитча
    color.r += red_tint * glitch_active * 0.3;
    color.g *= 1.0 - red_tint * glitch_active * 0.2;
    color.b *= 1.0 - red_tint * glitch_active * 0.2;
    
    // Случайные "вспышки" для скримеров
    float scare_flash = step(0.98, random(vec2(floor(TIME * 40.0), 0.0))) * glitch_active;
    color.rgb += scare_flash * 0.5;
    
    // Эффект "слежения" - случайные тени
    float shadow = step(0.85, random(vec2(floor(uv.y * 20.0), floor(TIME * 10.0)))) * glitch_active;
    color.rgb *= 1.0 - shadow * 0.4;
    
    COLOR = color;
}