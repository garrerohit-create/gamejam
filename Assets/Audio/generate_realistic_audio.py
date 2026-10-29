import os
import wave
import numpy as np

SR = 44100

BASE_DIR = r"c:\Users\garre\OneDrive\Desktop\asdasd\gamjam\Assets\Audio"
COMBAT_DIR = os.path.join(BASE_DIR, "Combat")
MOVE_DIR = os.path.join(BASE_DIR, "Movement")
BOSS_DIR = os.path.join(BASE_DIR, "Boss")
ENV_DIR = os.path.join(BASE_DIR, "Environment")

for d in [COMBAT_DIR, MOVE_DIR, BOSS_DIR, ENV_DIR]:
    os.makedirs(d, exist_ok=True)

def save_wav(filepath, data, target_peak=0.92):
    max_val = np.max(np.abs(data))
    if max_val > 1e-5:
        data = data / max_val * target_peak
    int16_data = np.clip(data * 32767, -32767, 32767).astype(np.int16)
    with wave.open(filepath, 'w') as wf:
        wf.setnchannels(1)
        wf.setsampwidth(2)
        wf.setframerate(SR)
        wf.writeframes(int16_data.tobytes())
    print(f"Generated: {os.path.basename(filepath)} ({len(data)/SR:.2f}s)")

def make_noise(n):
    return np.random.uniform(-1, 1, n)

def lowpass_fft(signal, cutoff_hz):
    n = len(signal)
    fft_vals = np.fft.rfft(signal)
    freqs = np.fft.rfftfreq(n, 1.0 / SR)
    # Smooth roll-off filter
    gain = 1.0 / (1.0 + (freqs / max(cutoff_hz, 1.0)) ** 4)
    filtered = np.fft.irfft(fft_vals * gain, n)
    return filtered

def bandpass_fft(signal, low_hz, high_hz):
    n = len(signal)
    fft_vals = np.fft.rfft(signal)
    freqs = np.fft.rfftfreq(n, 1.0 / SR)
    center = (low_hz + high_hz) * 0.5
    bw = max((high_hz - low_hz) * 0.5, 10.0)
    gain = np.exp(-0.5 * ((freqs - center) / bw) ** 2)
    filtered = np.fft.irfft(fft_vals * gain, n)
    return filtered

def metal_plate_resonance(dur, f0, mode_ratios, mode_decays, mode_amps, beat_spread=0.005):
    """Synthesizes physical inharmonic modal resonance with authentic acoustic beating."""
    n = int(SR * dur)
    t = np.linspace(0, dur, n, False)
    out = np.zeros(n)
    
    for r, d, a in zip(mode_ratios, mode_decays, mode_amps):
        f = f0 * r
        # Acoustic modal beating pair
        f_b = f * (1.0 + beat_spread)
        mode_wave = 0.5 * (np.sin(2 * np.pi * f * t) + np.sin(2 * np.pi * f_b * t))
        out += a * mode_wave * np.exp(-t * d)
        
    return out

def armor_rattle(dur, intensity=1.0, clinks=6):
    """Simulates realistic plate armor / chainmail jingle transient rattle."""
    n = int(SR * dur)
    t = np.linspace(0, dur, n, False)
    out = np.zeros(n)
    np.random.seed(42)
    
    for _ in range(clinks):
        start_t = np.random.uniform(0.01, min(dur * 0.5, 0.15))
        idx = int(start_t * SR)
        if idx >= n:
            continue
        clink_dur = 0.04
        clink_n = min(int(SR * clink_dur), n - idx)
        ct = t[:clink_n]
        cf = np.random.uniform(3200, 7500)
        clink_wave = (np.sin(2 * np.pi * cf * ct) + 0.3 * np.sin(2 * np.pi * cf * 1.4 * ct)) * np.exp(-ct * 80.0)
        out[idx:idx + clink_n] += clink_wave * np.random.uniform(0.2, 0.5) * intensity
        
    return out

# ==============================================================================
# 1. COMBAT SOUND EFFECTS (METAL BANGING & REALISTIC CLASHES)
# ==============================================================================

def generate_parry_clash():
    """Authentic high-tensile steel sword clash: razor spark, explosive transient, singing ring-out."""
    dur = 1.05
    n = int(SR * dur)
    t = np.linspace(0, dur, n, False)
    
    # 1. High-Tensile Steel Blade Modes (Inharmonic transverse beam modes)
    f0 = 2180.0  # High ringing blade fundamental
    ratios = [0.22, 0.48, 1.0, 1.76, 2.62, 3.85, 5.20]
    decays = [4.5,  6.0,  3.2, 8.5,  15.0, 24.0, 38.0]
    amps   = [0.65, 0.85, 1.0, 0.55, 0.35, 0.22, 0.12]
    steel_ring = metal_plate_resonance(dur, f0, ratios, decays, amps, beat_spread=0.007)
    
    # 2. Hard Steel Impact Transient (Instantaneous hammer / blade collision crack)
    crack_noise = make_noise(n)
    crack_filt = bandpass_fft(crack_noise, 1500, 9000)
    crack_env = np.exp(-t * 110.0)
    transient = np.tanh(crack_filt * crack_env * 5.0) * 1.4
    
    # 3. Steel Edge Friction Scrape (Immediate contact friction 0-25ms)
    scrape_n = int(SR * 0.035)
    scrape_t = t[:scrape_n]
    scrape_noise = bandpass_fft(make_noise(scrape_n), 3500, 11000)
    scrape = scrape_noise * np.sin(scrape_t / 0.035 * np.pi) * 0.8
    transient[:scrape_n] += scrape
    
    # 4. Low-end blade core punch (Physical steel mass)
    thud_wave = np.sin(2 * np.pi * (320.0 * np.exp(-t * 22.0)) * t) * np.exp(-t * 16.0)
    
    combined = steel_ring * 0.9 + transient * 1.1 + thud_wave * 0.45
    return combined

def generate_heavy_impact():
    """Massive iron greatsword slamming into ground/armor: anvil clang + seismic sub-bass."""
    dur = 0.95
    n = int(SR * dur)
    t = np.linspace(0, dur, n, False)
    
    # 1. Heavy Anvil / Iron Block Clang (Heavy hollow steel modes)
    f0 = 380.0
    ratios = [0.45, 1.0, 2.12, 3.45, 5.10, 7.80]
    decays = [3.8,  4.5, 9.0,  16.0, 28.0, 42.0]
    amps   = [0.9,  1.0, 0.65, 0.40, 0.25, 0.15]
    iron_clang = metal_plate_resonance(dur, f0, ratios, decays, amps, beat_spread=0.009)
    
    # 2. Seismic Sub-Bass Ground Thump (Pitch drops 80Hz -> 35Hz)
    sub_pitch = 85.0 * np.exp(-t * 7.5) + 32.0
    sub_phase = np.cumsum(2 * np.pi * sub_pitch / SR)
    sub_thump = np.sin(sub_phase) * np.exp(-t * 5.5) * 1.3
    
    # 3. Crushing Impact Blast & Stone Debris
    crunch_noise = make_noise(n)
    crunch_filt = bandpass_fft(crunch_noise, 400, 5500)
    crunch = np.tanh(crunch_filt * np.exp(-t * 35.0) * 4.0) * 1.1
    
    # 4. Ringing blade vibration
    blade_ring = np.sin(2 * np.pi * 640.0 * t) * np.exp(-t * 6.0) * 0.35
    
    combined = iron_clang * 0.85 + sub_thump * 1.2 + crunch * 0.95 + blade_ring
    return combined

def generate_gauntlet_strike():
    """Solid iron gauntlet punch into steel plate armor: hollow breastplate bang + chain rattle."""
    dur = 0.55
    n = int(SR * dur)
    t = np.linspace(0, dur, n, False)
    
    # 1. Cuirass Cavity Resonance (Hollow metal banging)
    f0 = 240.0
    ratios = [1.0, 1.95, 3.10, 4.85, 7.2]
    decays = [8.0, 14.0, 22.0, 36.0, 50.0]
    amps   = [1.0, 0.75, 0.45, 0.25, 0.15]
    breastplate = metal_plate_resonance(dur, f0, ratios, decays, amps, beat_spread=0.012)
    
    # 2. Hard punch transient & fist impact thud
    transient = np.tanh(bandpass_fft(make_noise(n), 800, 4500) * np.exp(-t * 65.0) * 4.5) * 1.1
    punch_thud = np.sin(2 * np.pi * (140.0 * np.exp(-t * 18.0)) * t) * np.exp(-t * 14.0) * 0.8
    
    # 3. Chainmail / plate rivet rattle
    rattle = armor_rattle(dur, intensity=0.65, clinks=8)
    
    combined = breastplate * 1.0 + transient * 0.9 + punch_thud * 0.75 + rattle * 0.6
    return combined

def generate_flesh_hit(is_heavy=False):
    """Blade cutting into armored guard: steel slicing clank + wet tissue crunch."""
    dur = 0.65 if is_heavy else 0.42
    n = int(SR * dur)
    t = np.linspace(0, dur, n, False)
    
    # 1. Armor plate deflection / metal cut sound
    f0 = 460.0 if not is_heavy else 320.0
    ratios = [1.0, 2.15, 3.65, 5.4]
    decays = [10.0, 18.0, 30.0, 45.0]
    amps   = [1.0, 0.65, 0.35, 0.18]
    metal_clank = metal_plate_resonance(dur, f0, ratios, decays, amps, beat_spread=0.008)
    
    # 2. Wet visceral flesh chop
    noise = make_noise(n)
    wet_cut = bandpass_fft(noise, 300, 3200) * np.exp(-t * (16.0 if is_heavy else 24.0))
    wet_cut = np.tanh(wet_cut * (4.5 if is_heavy else 3.2)) * 1.2
    
    # 3. Bone snap transient
    snap_env = np.exp(-t * 90.0)
    snap = bandpass_fft(noise, 1800, 6500) * snap_env * 0.85
    
    # 4. Low body impact thud
    body_thud = np.sin(2 * np.pi * (180.0 * np.exp(-t * 20.0)) * t) * np.exp(-t * 12.0) * (0.8 if is_heavy else 0.5)
    
    combined = metal_clank * 0.75 + wet_cut * 1.1 + snap * 0.7 + body_thud
    return combined

def generate_blood_splatter():
    """Visceral arterial blood squirt, splatter and drops."""
    dur = 0.55
    n = int(SR * dur)
    t = np.linspace(0, dur, n, False)
    
    noise = make_noise(n)
    # Squirt / burst
    burst = bandpass_fft(noise, 400, 2600) * np.exp(-t * 22.0)
    burst = np.tanh(burst * 3.5)
    
    # Wet spray droplets
    drips = bandpass_fft(noise, 1200, 5500) * np.exp(-t * 14.0) * (1.0 + 0.4 * np.sin(2 * np.pi * 32.0 * t))
    
    combined = burst * 0.9 + drips * 0.6
    return combined

def generate_sword_swing(variation=1):
    """High-velocity blade aerodynamic whoosh with whistling edge shear and blade flex."""
    dur = 0.30 if variation == 1 else 0.26
    n = int(SR * dur)
    t = np.linspace(0, dur, n, False)
    
    # Bell-shaped envelope peaking at 40% duration
    env = np.sin(np.pi * (t / dur) ** 0.85) ** 2.2
    
    # 1. Aerodynamic Air Shear (Whistling edge sweep)
    f_center = 1800.0 if variation == 1 else 2300.0
    f_sweep = f_center + 1200.0 * np.sin(np.pi * t / dur)
    noise = make_noise(n)
    
    # Instantaneous phase for the whistling blade edge
    whistle_phase = np.cumsum(2 * np.pi * f_sweep / SR)
    blade_whistle = np.sin(whistle_phase) * 0.35 + 0.65 * bandpass_fft(noise, f_center - 400, f_center + 800)
    
    # 2. Steel blade flexing flutter
    blade_hum = np.sin(2 * np.pi * (440.0 if variation == 1 else 520.0) * t) * 0.25
    
    # 3. Air displacement low rumble
    low_wind = bandpass_fft(noise, 90, 320) * 0.85
    
    combined = (blade_whistle * 0.9 + blade_hum + low_wind) * env
    return combined

def generate_heavy_swing():
    """Massive colossal greatsword cleave: deep sub rumble + terrifying blade hiss."""
    dur = 0.45
    n = int(SR * dur)
    t = np.linspace(0, dur, n, False)
    
    env = np.sin(np.pi * (t / dur) ** 0.75) ** 2.5
    noise = make_noise(n)
    
    # Low displacement rumble (massive blade mass pushing air)
    low_gust = bandpass_fft(noise, 60, 220) * 1.3
    
    # Cutting blade shear
    f_sweep = 900.0 + 1800.0 * np.sin(np.pi * t / dur)
    phase = np.cumsum(2 * np.pi * f_sweep / SR)
    edge_shear = (np.sin(phase) * 0.4 + 0.6 * bandpass_fft(noise, 1200, 4200)) * 0.85
    
    combined = (low_gust * 1.1 + edge_shear) * env
    return combined

def generate_sword_charge():
    """Tense vibrating steel blade resonance with rising pitch and harmonic sheen."""
    dur = 0.85
    n = int(SR * dur)
    t = np.linspace(0, dur, n, False)
    
    # Rising tension pitch (85Hz -> 240Hz)
    pitch = 85.0 + 155.0 * (t / dur) ** 1.4
    phase = np.cumsum(2 * np.pi * pitch / SR)
    
    # Steel fundamental + vibrating octaves
    blade_vibe = (np.sin(phase) + 0.5 * np.sin(2 * phase) + 0.25 * np.sin(3 * phase))
    # Tremolo modulation speeding up
    tremolo = 1.0 + 0.35 * np.sin(2 * np.pi * (12.0 + 20.0 * (t / dur)) * t)
    
    # High blade friction sheen
    sheen = bandpass_fft(make_noise(n), 3200, 7500) * (t / dur) ** 1.8 * 0.45
    
    envelope = (t / dur) ** 0.8
    combined = (blade_vibe * tremolo * 0.8 + sheen) * envelope
    return combined

# ==============================================================================
# 2. PLAYER MOVEMENT & HEALTH SOUNDS
# ==============================================================================

def generate_dodge_whoosh():
    """Fast tactical cloth + armored dodge whoosh."""
    dur = 0.32
    n = int(SR * dur)
    t = np.linspace(0, dur, n, False)
    env = np.sin(np.pi * (t / dur)) ** 2.0
    whoosh = bandpass_fft(make_noise(n), 200, 1800) * env * 1.1
    rattle = armor_rattle(dur, intensity=0.35, clinks=4)
    return whoosh + rattle * 0.4

def generate_roll_slide():
    """Heavy armored knight rolling and scraping across stone ground."""
    dur = 0.48
    n = int(SR * dur)
    t = np.linspace(0, dur, n, False)
    env = np.sin(np.pi * (t / dur)) ** 1.4
    
    # Gritty stone friction
    friction = bandpass_fft(make_noise(n), 350, 4200) * env * 1.0
    # Armor plate clatter
    rattle = armor_rattle(dur, intensity=0.85, clinks=10)
    # Low body roll weight
    roll_weight = bandpass_fft(make_noise(n), 80, 240) * env * 0.9
    
    return friction * 0.8 + rattle * 0.7 + roll_weight * 0.6

def generate_jump():
    """Armored knight jumping: powerful boot spring + chain armor jingle."""
    dur = 0.25
    n = int(SR * dur)
    t = np.linspace(0, dur, n, False)
    env = np.sin(np.pi * (t / dur) ** 0.7) ** 2.0
    spring = bandpass_fft(make_noise(n), 120, 900) * env * 1.1
    rattle = armor_rattle(dur, intensity=0.6, clinks=4)
    return spring + rattle * 0.5

def generate_land_thud():
    """Heavy plate armored landing: solid stone thud + metallic armor clatter."""
    dur = 0.38
    n = int(SR * dur)
    t = np.linspace(0, dur, n, False)
    
    # Heavy stone boot slam
    thud_pitch = 160.0 * np.exp(-t * 25.0) + 45.0
    thud = np.sin(np.cumsum(2 * np.pi * thud_pitch / SR)) * np.exp(-t * 14.0) * 1.3
    # Armor plates clattering together on impact
    rattle = armor_rattle(dur, intensity=1.2, clinks=9)
    # Greaves / sabaton plate clank
    f0 = 520.0
    plate_clank = metal_plate_resonance(dur, f0, [1.0, 2.3, 4.1], [16.0, 28.0, 45.0], [0.8, 0.4, 0.2]) * 0.6
    
    return thud * 1.1 + rattle * 0.8 + plate_clank

def generate_knight_hurt():
    """Knight taking damage: heavy plate armor dent/clang + pained grunt."""
    dur = 0.42
    n = int(SR * dur)
    t = np.linspace(0, dur, n, False)
    
    # 1. Heavy Armor Dent Clang
    f0 = 290.0
    ratios = [1.0, 1.85, 3.2, 5.1]
    decays = [8.0, 15.0, 26.0, 40.0]
    amps   = [1.0, 0.7, 0.4, 0.2]
    armor_dent = metal_plate_resonance(dur, f0, ratios, decays, amps, beat_spread=0.01) * 1.0
    
    # 2. Chest impact punch
    punch = np.sin(2 * np.pi * (160.0 * np.exp(-t * 20.0)) * t) * np.exp(-t * 15.0) * 0.9
    
    # 3. Visceral grunt / breath expulsion
    grunt_f = 140.0 * np.exp(-t * 8.0) + 60.0
    grunt = np.sin(np.cumsum(2 * np.pi * grunt_f / SR)) * np.exp(-t * 9.0) * 0.65
    grunt = bandpass_fft(grunt + make_noise(n) * 0.15, 90, 750)
    
    return armor_dent * 0.95 + punch * 0.7 + grunt * 0.8

def generate_heal_cast():
    """Mystical vital healing aura: resonant bell chime + sparkling harmonic shimmer."""
    dur = 0.95
    n = int(SR * dur)
    t = np.linspace(0, dur, n, False)
    
    # Resonant harmonic glass/bell chime (A major 9th chord)
    freqs = [440.0, 554.37, 659.25, 830.61, 1108.73, 1661.22]
    chime = np.zeros(n)
    for i, f in enumerate(freqs):
        chime += (1.0 / (i + 1) ** 0.8) * np.sin(2 * np.pi * f * t) * np.exp(-t * (3.0 + i * 0.8))
        
    # Shimmering vitality rise
    shimmer_env = np.sin(np.pi * (t / dur) ** 0.6) ** 1.8
    shimmer = bandpass_fft(make_noise(n), 2400, 6500) * shimmer_env * (1.0 + 0.4 * np.sin(2 * np.pi * 16.0 * t)) * 0.35
    
    return chime * 0.85 + shimmer

# ==============================================================================
# 3. BOSS SOUNDS (INFERNAL EXECUTIONER)
# ==============================================================================

def generate_boss_roar(is_death=False):
    """Deep, terrifying infernal executioner roar."""
    dur = 1.75 if is_death else 1.35
    n = int(SR * dur)
    t = np.linspace(0, dur, n, False)
    
    env = np.sin(np.pi * (t / dur) ** (0.5 if not is_death else 0.4)) ** 1.5
    
    # Low demonic pitch drop
    pitch_base = 75.0 if not is_death else 90.0
    pitch_end = 35.0 if not is_death else 28.0
    pitch = pitch_base - (pitch_base - pitch_end) * (t / dur) ** 1.2
    phase = np.cumsum(2 * np.pi * pitch / SR)
    
    # Beast roar harmonics
    vocal = np.sin(phase) + 0.7 * np.sin(2 * phase) + 0.5 * np.sin(3 * phase) + 0.3 * np.sin(4 * phase)
    # Throat growl modulation
    growl_mod = 1.0 + 0.5 * np.sin(2 * np.pi * 28.0 * t) + 0.3 * np.sin(2 * np.pi * 42.0 * t)
    # Cavernous roar resonance
    throat_noise = bandpass_fft(make_noise(n), 120, 1800) * 0.85
    
    roar = (vocal * growl_mod * 0.7 + throat_noise * 0.8) * env
    return roar

def generate_boss_axe_slash():
    """Colossal executioner battleaxe cleave: massive air vortex + razor-sharp iron hiss."""
    dur = 0.52
    n = int(SR * dur)
    t = np.linspace(0, dur, n, False)
    
    env = np.sin(np.pi * (t / dur) ** 0.7) ** 2.2
    noise = make_noise(n)
    
    # 1. Giant iron blade whoosh
    low_gust = bandpass_fft(noise, 50, 180) * 1.4
    
    # 2. Razor blade shear
    f_sweep = 700.0 + 2200.0 * np.sin(np.pi * t / dur)
    phase = np.cumsum(2 * np.pi * f_sweep / SR)
    shear = (np.sin(phase) * 0.4 + 0.6 * bandpass_fft(noise, 1500, 5200)) * 1.0
    
    # 3. Dark iron blade resonance
    iron_hum = np.sin(2 * np.pi * 210.0 * t) * 0.4
    
    combined = (low_gust * 1.2 + shear + iron_hum) * env
    return combined

def generate_boss_lightning_slam():
    """Cataclysmic metal axe smash into ground: seismic blast + ringing iron clang."""
    dur = 0.98
    n = int(SR * dur)
    t = np.linspace(0, dur, n, False)
    
    # 1. Seismic Sub Blast (30Hz earthquake)
    sub = np.sin(np.cumsum(2 * np.pi * (90.0 * np.exp(-t * 6.5) + 28.0) / SR)) * np.exp(-t * 4.5) * 1.5
    
    # 2. Heavy Iron Axehead Clang (Massive metal impact on stone)
    f0 = 260.0
    ratios = [1.0, 1.95, 3.25, 4.90, 7.10]
    decays = [4.0, 7.5,  14.0, 22.0, 35.0]
    amps   = [1.0, 0.75, 0.50, 0.30, 0.18]
    axe_clang = metal_plate_resonance(dur, f0, ratios, decays, amps, beat_spread=0.01) * 1.1
    
    # 3. Ground fissure & electrical arc crack
    crack = np.tanh(bandpass_fft(make_noise(n), 450, 6500) * np.exp(-t * 28.0) * 4.5) * 1.2
    
    combined = sub * 1.2 + axe_clang * 0.95 + crack * 1.0
    return combined

def generate_boss_shockwave():
    """Crescent hellfire shockwave slicing ground."""
    dur = 0.55
    n = int(SR * dur)
    t = np.linspace(0, dur, n, False)
    
    env = np.sin(np.pi * (t / dur) ** 0.65) ** 1.8
    # Ground grinding sound
    grind = bandpass_fft(make_noise(n), 200, 2400) * (1.0 + 0.3 * np.sin(2 * np.pi * 45.0 * t)) * 1.1
    # Plasma whoosh
    f_sweep = 1100.0 * np.exp(-t * 3.5) + 300.0
    plasma = np.sin(np.cumsum(2 * np.pi * f_sweep / SR)) * 0.5
    
    combined = (grind + plasma) * env
    return combined

def generate_boss_hurt():
    """Boss taking heavy cleave damage: heavy metal dent crunch + roar of rage."""
    dur = 0.48
    n = int(SR * dur)
    t = np.linspace(0, dur, n, False)
    
    # Heavy dark metal plate smash
    f0 = 195.0
    ratios = [1.0, 1.9, 3.1, 4.8]
    decays = [6.0, 12.0, 22.0, 38.0]
    amps   = [1.0, 0.7, 0.4, 0.2]
    dent = metal_plate_resonance(dur, f0, ratios, decays, amps, beat_spread=0.012) * 1.2
    
    # Low guttural roar
    roar = np.sin(2 * np.pi * (95.0 * np.exp(-t * 8.0) + 40.0) * t) * np.exp(-t * 8.0) * 0.8
    crunch = np.tanh(bandpass_fft(make_noise(n), 500, 3500) * np.exp(-t * 30.0) * 3.5) * 0.8
    
    return dent * 0.9 + roar * 0.7 + crunch * 0.75

# ==============================================================================
# 4. ENVIRONMENT SOUNDS
# ==============================================================================

def generate_gate_unlock():
    """Heavy ancient iron gate unlocking: massive grinding gears, chain clank, and resonant gong."""
    dur = 1.25
    n = int(SR * dur)
    t = np.linspace(0, dur, n, False)
    
    # 1. Deep Cathedral / Temple Gong
    f0 = 135.0
    ratios = [1.0, 2.05, 3.15, 4.40, 6.20]
    decays = [2.2, 3.8,  6.5,  11.0, 18.0]
    amps   = [1.0, 0.8,  0.55, 0.35, 0.20]
    gong = metal_plate_resonance(dur, f0, ratios, decays, amps, beat_spread=0.006) * 1.1
    
    # 2. Heavy iron chains dropping & gear ratchet
    chains = armor_rattle(dur, intensity=1.4, clinks=14)
    gears = bandpass_fft(make_noise(n), 450, 1800) * np.exp(-t * 6.0) * (1.0 + 0.5 * np.sin(2 * np.pi * 30.0 * t)) * 0.7
    
    return gong * 0.95 + chains * 0.85 + gears * 0.6

def generate_victory_fanfare():
    """Atmospheric dark-fantasy victory chime & bell toll."""
    dur = 2.2
    n = int(SR * dur)
    t = np.linspace(0, dur, n, False)
    
    # Toll bell in D minor
    chord_freqs = [293.66, 349.23, 440.0, 587.33, 880.0]
    bell = np.zeros(n)
    for i, f in enumerate(chord_freqs):
        bell += (1.0 / (i + 1) ** 0.7) * np.sin(2 * np.pi * f * t) * np.exp(-t * (1.8 + i * 0.4))
        
    shimmer = bandpass_fft(make_noise(n), 1800, 7500) * np.exp(-t * 2.0) * 0.25
    return bell * 0.9 + shimmer

# ==============================================================================
# MAIN GENERATION EXECUTION
# ==============================================================================

def main():
    print("=== Synthesizing Realistic Combat & Metallic Audio ===")
    
    # Combat
    save_wav(os.path.join(COMBAT_DIR, "Parry_Clash.wav"), generate_parry_clash())
    save_wav(os.path.join(COMBAT_DIR, "Heavy_Impact.wav"), generate_heavy_impact())
    save_wav(os.path.join(COMBAT_DIR, "Gauntlet_Strike.wav"), generate_gauntlet_strike())
    save_wav(os.path.join(COMBAT_DIR, "Flesh_Hit.wav"), generate_flesh_hit(is_heavy=False))
    save_wav(os.path.join(COMBAT_DIR, "Flesh_Hit_Heavy.wav"), generate_flesh_hit(is_heavy=True))
    save_wav(os.path.join(COMBAT_DIR, "Blood_Splatter.wav"), generate_blood_splatter())
    save_wav(os.path.join(COMBAT_DIR, "Sword_Swing_Light1.wav"), generate_sword_swing(variation=1))
    save_wav(os.path.join(COMBAT_DIR, "Sword_Swing_Light2.wav"), generate_sword_swing(variation=2))
    save_wav(os.path.join(COMBAT_DIR, "Sword_Swing_Heavy.wav"), generate_heavy_swing())
    save_wav(os.path.join(COMBAT_DIR, "Sword_Charge.wav"), generate_sword_charge())
    
    # Movement
    save_wav(os.path.join(MOVE_DIR, "Dodge_Whoosh.wav"), generate_dodge_whoosh())
    save_wav(os.path.join(MOVE_DIR, "Roll_Slide.wav"), generate_roll_slide())
    save_wav(os.path.join(MOVE_DIR, "Jump.wav"), generate_jump())
    save_wav(os.path.join(MOVE_DIR, "Land_Thud.wav"), generate_land_thud())
    save_wav(os.path.join(MOVE_DIR, "Knight_Hurt.wav"), generate_knight_hurt())
    save_wav(os.path.join(MOVE_DIR, "Heal_Cast.wav"), generate_heal_cast())
    
    # Boss
    save_wav(os.path.join(BOSS_DIR, "Boss_Roar.wav"), generate_boss_roar(is_death=False))
    save_wav(os.path.join(BOSS_DIR, "Boss_Death.wav"), generate_boss_roar(is_death=True))
    save_wav(os.path.join(BOSS_DIR, "Boss_Axe_Slash.wav"), generate_boss_axe_slash())
    save_wav(os.path.join(BOSS_DIR, "Boss_Lightning_Slam.wav"), generate_boss_lightning_slam())
    save_wav(os.path.join(BOSS_DIR, "Boss_Shockwave_Launch.wav"), generate_boss_shockwave())
    save_wav(os.path.join(BOSS_DIR, "Boss_Hurt.wav"), generate_boss_hurt())
    
    # Environment
    save_wav(os.path.join(ENV_DIR, "Gate_Unlock.wav"), generate_gate_unlock())
    save_wav(os.path.join(ENV_DIR, "Victory_Fanfare.wav"), generate_victory_fanfare())
    
    print("=== All Realistic Metallic Sound Effects Generated Successfully! ===")

if __name__ == "__main__":
    main()
