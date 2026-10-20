"""Check synthetic CI evidence and explicitly report the unavailable private suite."""
import json
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[2]


def validate(report, logs):
    suites = {'camera': 56, 'swing': 13, 'wallcrawl': 21, 'zip': 14, 'webbing': 41}
    if (report.get('passed') is not True or len(report.get('checks', [])) < 150
            or report.get('display_driver') != 'headless'
            or report.get('renderer') != 'forward_plus'
            or set(report.get('suites', {})) != set(suites)):
        raise ValueError('Synthetic traversal result incomplete or failed')
    for name, minimum in suites.items():
        suite = report['suites'][name]
        if suite.get('passed') is not True or suite.get('checks', 0) < minimum:
            raise ValueError('Synthetic suite incomplete: ' + name)
    if any(row.get('passed') is not True for row in report['checks']):
        raise ValueError('Failed traversal assertion')
    for log in logs:
        if re.search(r'^(?:ERROR:|SCRIPT ERROR:|Shader ERROR:)', log, re.MULTILINE):
            raise ValueError('Engine/script error in CI log')


def validate_fonts(report):
    if (report.get('passed') is not True or len(report.get('checks', [])) < 10
            or report.get('display_driver') != 'headless' or report.get('renderer') != 'forward_plus'
            or any(row.get('passed') is not True for row in report['checks'])):
        raise ValueError('Font fallback/variant suite incomplete or failed')


def validate_menu_audio(report):
    if (report.get('passed') is not True or len(report.get('checks', [])) < 17
            or report.get('display_driver') != 'headless' or report.get('renderer') != 'forward_plus'
            or any(row.get('passed') is not True for row in report['checks'])):
        raise ValueError('Menu audio suite incomplete or failed')


if __name__ == '__main__':
    evidence = ROOT / 'staging/validation'
    report = json.loads((evidence / 'traversal-tests.json').read_text())
    validate(report, [(evidence / name).read_text() for name in ('ci-import.log', 'ci-traversal.log', 'ci-fonts.log', 'ci-menu-audio.log')])
    fonts = json.loads((evidence / 'font-tests.json').read_text())
    validate_fonts(fonts)
    menu_audio = json.loads((evidence / 'menu-audio-tests.json').read_text())
    validate_menu_audio(menu_audio)
    result = {'synthetic_traversal': {'passed': True, 'checks': len(report['checks'])},
              'font_checks': {'passed': True, 'checks': len(fonts['checks']), 'skips': fonts.get('skips', [])},
              'menu_audio_checks': {'passed': True, 'checks': len(menu_audio['checks']), 'skips': menu_audio.get('skips', [])},
              'private_asset_smoke': {'status': 'skipped', 'minimum_checks': 44,
                  'reason': 'CI has no original/derived game assets; run the unchanged smoke suite locally on all three targets.'}}
    (evidence / 'ci-summary.json').write_text(json.dumps(result, indent=2) + '\n')
    print(json.dumps(result, indent=2))
