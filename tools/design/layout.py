"""Validate authored layout geometry and audit untrusted private Gemini proposals."""
import argparse
import json
import math
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
STYLES = ('classic', 'enhanced')

def validate(document):
    if document.get('schema_version') != 1 or not document.get('screens'):
        raise ValueError('Unsupported or empty layout')
    ids = [s['id'] for s in document['screens']]
    if len(ids) != len(set(ids)): raise ValueError('Duplicate screen')
    for screen in document['screens']:
        if set(screen['styles']) != set(STYLES): raise ValueError('Missing style')
        for style in STYLES:
            page = screen['styles'][style]
            width, height = page['canvas']
            if width <= 0 or height <= 0: raise ValueError('Invalid canvas')
            elements = page['elements']
            element_ids = [e['id'] for e in elements]
            if len(element_ids) != len(set(element_ids)): raise ValueError('Duplicate element')
            if any(i not in element_ids for i in page['focus_order']): raise ValueError('Unknown focus target')
            if page['back'] not in ids + ['none', 'gameplay']: raise ValueError('Unknown back target')
            for e in elements:
                for key in ('type','rect','anchor','z','overlay','asset','font','font_size','colours','evidence','confidence'):
                    if key not in e: raise ValueError('Missing element field: '+key)
                x,y,w,h=e['rect']
                if not all(isinstance(n,(float,int)) and math.isfinite(n) for n in (x,y,w,h)) or min(x,y)<0 or min(w,h)<=0 or x+w>width+.001 or y+h>height+.001:
                    raise ValueError('Element escapes canvas: '+e['id'])
                if not e['evidence'] or e['confidence'] not in ('verified','recovered','authored','Gemini proposal (unverified)'): raise ValueError('Missing provenance')
                if e['asset'] and not e['asset'].startswith('res://assets/'): raise ValueError('Asset path outside private assets')
            for i,a in enumerate(elements):
                for b in elements[i+1:]:
                    if a['z']!=b['z'] or a['overlay'] or b['overlay']: continue
                    ax,ay,aw,ah=a['rect']; bx,by,bw,bh=b['rect']
                    if min(ax+aw,bx+bw)>max(ax,bx)+.001 and min(ay+ah,by+bh)>max(ay,by)+.001:
                        raise ValueError('Unmarked overlap: '+screen['id']+'/'+style+'/'+a['id']+'/'+b['id'])
    return document

def audit_proposal(document, root=ROOT):
    errors=[]; rows=[]
    if document.get('schema_version')!=1 or not isinstance(document.get('screens'),list):
        return {'valid':False,'errors':['schema/version/screens'],'elements':[]}
    for screen in document['screens']:
        for e in screen.get('elements',[]):
            issues=[]
            for key in ('id','type','evidence','confidence','classic_rect','enhanced_rect'):
                if key not in e or e[key] in (None,[], ''): issues.append('missing '+key)
            for style in STYLES:
                r=e.get(style+'_rect',[])
                if len(r)!=4 or not all(isinstance(x,(int,float)) and math.isfinite(x) for x in r) or min(r)<0 or r[2]<=0 or r[3]<=0 or r[0]+r[2]>1.00001 or r[1]+r[3]>1.00001: issues.append('invalid '+style+' rect')
            asset=e.get('asset')
            asset_status='not specified'
            if asset:
                asset_status='exists' if (root/asset).is_file() else 'unresolved path or archive/resource identity'
                if asset_status!='exists': issues.append('unresolved asset')
            row=dict(screen=screen['id'],element=e.get('id'),evidence=e.get('evidence',[]),proposal_confidence=e.get('confidence'),grade='Gemini proposal (unverified)',asset=asset,asset_status=asset_status,issues=issues)
            rows.append(row); errors.extend(screen['id']+'/'+str(e.get('id'))+': '+i for i in issues)
    return dict(valid=not errors,errors=errors,elements=rows)

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--audit',action='store_true');args=parser.parse_args()
    validate(json.loads((ROOT/'src/ui/layout/screens.json').read_text()))
    if args.audit:
        reports={}
        for folder,name in [('T9_menu','menu_spec.json'),('T10_ui','ui_spec.json')]:
            reports[folder]=audit_proposal(json.loads((ROOT/'staging/gemini'/folder/name).read_text()))
        out=ROOT/'staging/validation/menu/proposal-audit.json';out.parent.mkdir(parents=True,exist_ok=True);out.write_text(json.dumps(reports,indent=2))
        print({k:dict(elements=len(v['elements']),issues=len(v['errors'])) for k,v in reports.items()})
    print('Layout geometry/provenance valid')
if __name__=='__main__':main()
