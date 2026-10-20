"""Convert locally extracted menu/comic BMPs to PNG without executing game code."""
from pathlib import Path
import subprocess
from convert_characters import extract_member, inspect

ROOT = Path(__file__).resolve().parents[2]


def main():
    archive = ROOT/'staging/raw_extracted/PC/app/data.pkr'
    source = ROOT/'staging/raw_extracted/GodotMigration/ui'
    output = ROOT/'assets/ui'
    output.mkdir(parents=True, exist_ok=True)
    wanted = {'title.bmp', 'shell_bg.bmp'} | {f'cb{i:02}.bmp' for i in range(32)}
    files = []
    with archive.open('rb') as stream:
        for entry in inspect(archive)['entries']:
            if entry['path'].lower() in {'data/'+name for name in wanted}:
                files.append(extract_member(stream, entry, source))
    if len(files) != len(wanted):
        raise ValueError('Missing original menu/comic archive members')
    for path in files:
        subprocess.run(['ffmpeg', '-hide_banner', '-loglevel', 'error', '-y', '-i', str(path),
                        '-frames:v', '1', str(output/(path.stem.lower()+'.png'))], check=True, timeout=30)
    print(f'Converted {len(files)} private menu/comic textures')


if __name__ == '__main__':
    main()
