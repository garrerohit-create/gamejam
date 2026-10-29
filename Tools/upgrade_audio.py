"""Build the game's replacement SFX from Kenney CC0 recordings. pip install numpy soundfile"""
import io, json, pathlib, urllib.request
from concurrent.futures import ThreadPoolExecutor
import numpy as np
import soundfile as sf

ROOT = pathlib.Path(__file__).resolve().parents[1]
CACHE = ROOT / 'Temp/AudioUpgrade'
CACHE.mkdir(parents=True, exist_ok=True)
BASE = 'https://raw.githubusercontent.com/GeorgeQLe/assets-sfx-combat/main/'
SR = 44100
# name: (pack, recording, playback rate, gain, delay seconds)
R, I, S = 'rpg-audio', 'impact-sounds', 'sci-fi-sounds'
def layer(pack, name, rate=1, gain=1, delay=0): return (pack,name,rate,gain,delay)
recipes = {
 'Combat/Sword_Swing_Light1': [layer(R,'knifeSlice',.9)],
 'Combat/Sword_Swing_Light2': [layer(R,'knifeSlice2',1.05)],
 'Combat/Sword_Swing_Heavy': [layer(R,'knifeSlice2',.68),layer(R,'cloth3',.8,.3)],
 'Combat/Parry_Clash': [layer(I,'impactMetal_light_002',1,.8),layer(I,'impactBell_heavy_001',1.6,.22)],
 'Combat/Sword_Clang': [layer(I,'impactMetal_light_000')],
 'Combat/Armor_Clang': [layer(I,'impactMetal_medium_001',.9)],
 'Combat/Gauntlet_Strike': [layer(I,'impactPunch_medium_001'),layer(I,'impactMetal_light_001',1,.25)],
 'Combat/Flesh_Hit': [layer(I,'impactPunch_medium_000'),layer(R,'chop',1,.35)],
 'Combat/Flesh_Hit_Heavy': [layer(I,'impactPunch_heavy_001',.85),layer(R,'chop',.85,.45)],
 'Combat/Blood_Splatter': [layer(S,'slime_000',1.3,.5)],
 'Combat/Heavy_Impact': [layer(I,'impactPunch_heavy_000',.72),layer(I,'impactPlate_heavy_000',.8,.35)],
 'Movement/Dodge_Whoosh': [layer(R,'cloth1',1.2)],
 'Movement/Roll_Slide': [layer(R,'cloth3',.85),layer(R,'dropLeather',1,.4,.1)],
 'Movement/Jump': [layer(R,'footstep02',1.15),layer(R,'cloth2',1.2,.3)],
 'Movement/Land_Thud': [layer(I,'footstep_concrete_001',.85),layer(R,'clothBelt',1,.35)],
 'Boss/Boss_Axe_Slash': [layer(R,'knifeSlice',.55),layer(R,'cloth4',.7,.5)],
 'Boss/Boss_Lightning_Slam': [layer(S,'explosionCrunch_000',.8),layer(I,'impactPlate_heavy_002',.65,.55)],
 'Boss/Boss_Shockwave_Launch': [layer(S,'lowFrequency_explosion_000',1,.8),layer(R,'knifeSlice2',.6,.4)],
 'Environment/Gate_Unlock': [layer(R,'metalLatch',.65,.7),layer(R,'doorOpen_1',.65,.65,.18),layer(I,'impactBell_heavy_000',.75,.3,.4)],
}

def download(item):
 pack,name=item; path=CACHE/(pack+'_'+name+'.ogg')
 if not path.exists(): path.write_bytes(urllib.request.urlopen(BASE+f'assets/kenney/{pack}/Audio/{name}.ogg',timeout=30).read())
 return item,path
sources=dict(ThreadPoolExecutor(8).map(download,sorted({(p,n) for layers in recipes.values() for p,n,*_ in layers})))
credits=ROOT/'Assets/Audio/Licenses';credits.mkdir(exist_ok=True)
for pack in (R,I,S):
 (credits/(pack+'.txt')).write_bytes(urllib.request.urlopen(BASE+'LICENSES/kenney-'+pack+'-license.txt').read())

def resample(x,rate):
 return np.interp(np.arange(0,len(x),rate),np.arange(len(x)),x)
def read(path,rate=1):
 x,sr=sf.read(path);x=x.mean(axis=1) if x.ndim==2 else x
 x=resample(x,rate*sr/SR)
 active=np.flatnonzero(abs(x)>.003)
 return x[max(0,active[0]-100):min(len(x),active[-1]+800)] if len(active) else x
def master(x,peak=.57):
 x=x-np.mean(x)
 # Gentle high-frequency rolloff; retain impact transients without brittle hiss.
 f=np.fft.rfftfreq(len(x),1/SR)
 x=np.fft.irfft(np.fft.rfft(x)/(1+(f/7800)**4),n=len(x))
 x*=peak/max(np.max(abs(x)),1e-8)
 fade=min(220,len(x)//4);x[:fade]*=np.linspace(0,1,fade);x[-fade:]*=np.linspace(1,0,fade)
 return x
for target,layers in recipes.items():
 chunks=[(int(delay*SR),read(sources[p,n],rate)*gain) for p,n,rate,gain,delay in layers]
 out=np.zeros(max(start+len(x) for start,x in chunks))
 for start,x in chunks:out[start:start+len(x)]+=x
 peak=.38 if target.startswith('Movement/') else (.30 if 'Blood' in target else .62)
 sf.write(ROOT/'Assets/Audio'/(target+'.wav'),master(out,peak),SR,subtype='PCM_16')
# Preserve the musical/vocal identity of these bespoke cues, with a softer mix.
for target in ['Combat/Sword_Charge','Movement/Heal_Cast','Movement/Knight_Hurt','Boss/Boss_Roar','Boss/Boss_Hurt','Boss/Boss_Death','Environment/Victory_Fanfare']:
 path=ROOT/'Assets/Audio'/(target+'.wav');backup=CACHE/(path.stem+'_original.wav')
 if not backup.exists():backup.write_bytes(path.read_bytes())
 sf.write(path,master(read(backup),.42 if 'Boss' not in target else .55),SR,subtype='PCM_16')
(credits/'SOURCES.md').write_text('Kenney CC0 audio: https://kenney.nl/assets/rpg-audio, https://kenney.nl/assets/impact-sounds, https://kenney.nl/assets/sci-fi-sounds\nDownloaded from GeorgeQLe/assets-sfx-combat mirror. Recipes in Tools/upgrade_audio.py.\n19 effects replaced with layered recordings; 7 existing musical/vocal cues remastered.\n')
for p in (ROOT/'Assets/Audio').glob('*/*.wav'):
 x,sr=sf.read(p); assert np.isfinite(x).all() and max(abs(x))<.99 and len(x)>100
print('Updated and validated all 26 effects: 19 replacements, 7 remasters; preserved Unity asset GUIDs.')
