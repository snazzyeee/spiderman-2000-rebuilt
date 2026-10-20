"""Private diagnostic join: bank-origin evidence, TRG references and gap groups.

Never changes geometry, positions, hypotheses, thresholds or gate results.
"""
import argparse
from collections import defaultdict
import json
from pathlib import Path
import statistics
from decode_psx_regions import decode, compare_glb, MAX_BYTES
from psx_units import METERS_PER_MODEL_UNIT


def attribute_candidates(comparison, banks):
    objects = [(name,obj) for name,bank in banks.items() for obj in bank['objects']]
    if len(comparison['comparisons'])*len(objects) > 10000000:
        raise ValueError('Bank comparison work budget exceeded')
    result = []
    for candidate in comparison['comparisons']:
        if max(map(abs,candidate['translation_error'])) < 0.01:
            continue
        matches = []
        for name,obj in objects:
            expected = [v*s/2.25 for v,s in zip(obj['origin_world'],(1,-1,-1))]
            if max(abs(a-b) for a,b in zip(expected,candidate['actual_translation'])) < 0.01:
                matches.append(dict(bank=name,object=obj['index'],model=obj['model']))
        result.append(dict(exported_model=candidate['model'],translation=candidate['actual_translation'],matches=matches))
    return result


def enrich(report, graph, candidates):
    details = report['details']
    gaps = defaultdict(list)
    near = 0
    for row in details['per_landmark']:
        origin = row['nearest_chunk_origin_m']
        matched = [c for c in candidates if max(abs(a-b*METERS_PER_MODEL_UNIT) for a,b in zip(origin,c['translation'])) < 0.001]
        row['nearest_chunk_origin_candidates'] = [m for c in matched for m in c['matches']]
        row['near_former_unmatched'] = bool(matched and row['nearest_chunk_distance_m'] <= 2.0 and not row['supported'])
        near += row['near_former_unmatched']
        if row['classification'] == 'over_geometry_unsupported' and row['nearest_surface_vertical_gap_m'] is not None:
            gaps[row['type']+'/'+row['subtype']].append(row['nearest_surface_vertical_gap_m'])
    details['classification_counts']['near_former_unmatched'] = near
    details['gap_observations'] = {key:dict(count=len(values),min=min(values),max=max(values),median=statistics.median(values),
                                           status='Observation only; no fitted correction permitted') for key,values in gaps.items()}
    references = []
    for node in graph['nodes']:
        for command in node.get('commands',[])+node.get('script',[]):
            if command.get('name') in ('SpoolIn','SpoolEnv','BackgroundCreate','LoadNewTrg'):
                args=command.get('args',[])
                references.append(dict(node=node['index'],command=command['name'],args=args,
                    argument_status='unresolved_control_character' if any(isinstance(a,str) and any(ord(c)<32 or 127<=ord(c)<160 for c in a) for a in args) else 'decoder_observation'))
    details['geometry_references'] = references
    details['former_unmatched_bank_audit'] = candidates
    return report


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root',type=Path,default=Path(__file__).resolve().parents[2])
    args=parser.parse_args();root=args.root
    base=root/'staging/raw_extracted/GodotMigration/raw/data'
    banks={}
    for key in ('G','O'):
        with (base/f'L1A1_{key}.psx').open('rb') as stream:
            banks[key]=decode(stream.read(MAX_BYTES+1))
    comparison=compare_glb(root/'assets/environments/l1a1_g.glb',banks['G'])
    candidates=attribute_candidates(comparison,banks)
    report_path=root/'staging/validation/units-landmark.json'
    report=json.loads(report_path.read_text())
    graph=json.loads((root/'levels/L1A1/trg_graph.json').read_text())
    result=enrich(report,graph,candidates)
    report_path.write_text(json.dumps(result,indent=2),encoding='utf-8')
    print(json.dumps(dict(gate_passed=result['passed'],classification=result['details']['classification_counts'],bank_attributions=candidates)))


if __name__=='__main__':
    main()
