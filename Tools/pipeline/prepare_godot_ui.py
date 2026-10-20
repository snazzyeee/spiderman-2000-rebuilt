"""Convert locally extracted menu/comic BMPs to PNG without executing game code."""
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[2]


def main():
    source = ROOT/'Game/Assets/_LocalReference/Frontend/Art'
    output = ROOT/'Godot/assets/ui'
    output.mkdir(parents=True, exist_ok=True)
    files = sorted(source.glob('*.bmp'))
    if not files:
        raise FileNotFoundError('Run Tools/Reference/prepare_frontend_assets.py first')
    for path in files:
        subprocess.run(['ffmpeg', '-hide_banner', '-loglevel', 'error', '-y', '-i', str(path),
                        '-frames:v', '1', str(output/(path.stem+'.png'))], check=True, timeout=30)
    print(f'Converted {len(files)} private menu/comic textures')


if __name__ == '__main__':
    main()
