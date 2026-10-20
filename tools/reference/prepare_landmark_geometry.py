"""Convert explicitly referenced following-area banks for private diagnostics only."""
import hashlib
import json
from pathlib import Path
import re
import shutil
import struct
import subprocess
from pinned_tools import CONVERTER_SHA256
from decode_psx_regions import MAX_BYTES, decode

ROOT=Path(__file__).resolve().parents[2]


def collect(graphs):
    references={}
    for level,graph in graphs.items():
        for node in graph['nodes']:
            for command in node.get('commands',[])+node.get('script',[]):
                args=command.get('args',[])
                if command.get('name') in ('SpoolIn','SpoolEnv') and args and isinstance(args[0],str) and re.fullmatch(r'[A-Za-z0-9]+_[GOL]',args[0],re.I):
                    references.setdefault(args[0].upper(),[]).append(dict(level=level,node=node['index'],command=command['name']))
    return references


def main():
    root=ROOT
    start=json.loads((root/'levels/L1A1/trg_graph.json').read_text())
    graphs={'L1A1':start}
    for node in start['nodes']:
        for command in node.get('commands',[])+node.get('script',[]):
            args=command.get('args',[])
            if command.get('name')=='LoadNewTrg' and args and isinstance(args[0],str) and re.fullmatch(r'[A-Za-z0-9]+_T',args[0]):
                level=args[0][:-2].upper();path=root/'levels'/level/'trg_graph.json'
                if path.exists():graphs[level]=json.loads(path.read_text())
    references=collect(graphs)
    converter=root/'tools/third_party/neversoft-multitool-1.3.4/NeversoftMultitool-Windows-CLI-x64/NeversoftMultitool.exe'
    if hashlib.sha256(converter.read_bytes()).hexdigest()!=CONVERTER_SHA256:
        raise ValueError('Converter fingerprint mismatch')
    sources=root/'staging/raw_extracted/Extracted/data/data'
    available={p.stem.upper():p for p in sources.glob('*.psx')}
    output=root/'staging/reference/landmark_geometry';raw=output/'raw';raw.mkdir(parents=True,exist_ok=True)
    records=[]
    for name,refs in sorted(references.items()):
        source=available.get(name)
        if source is None:
            records.append(dict(bank=name,references=refs,status='missing source'));continue
        if source.stat().st_size>MAX_BYTES:raise ValueError('PSX input exceeds audit budget')
        blob=source.read_bytes();magic,meta,count=struct.unpack_from('<III',blob)
        if magic not in (0x20004,0x20006) or count>65536 or 16+count*36>len(blob):raise ValueError('Invalid PSX table')
        models,=struct.unpack_from('<I',blob,12+36*count)
        record=dict(bank=name,references=refs,source_sha256=hashlib.sha256(blob).hexdigest(),models=models)
        if not models:
            record['status']='no models';records.append(record);continue
        decoded=decode(blob)
        record['decoded_objects']=len(decoded['objects'])
        target=raw/source.name;shutil.copyfile(source,target)
        record['input_file']=str(target.relative_to(root)).replace('\\','/')
        records.append(record)
    for record in records:
        if 'input_file' not in record:continue
        folder=output/record['bank'].lower();folder.mkdir(exist_ok=True)
        with (folder/'converter.log').open('w') as log:
            subprocess.run([str(converter),'psx-mesh',str(root/record['input_file']),'-o',str(folder)],stdout=log,stderr=subprocess.STDOUT,check=True,timeout=180)
        path=next(p for p in folder.glob('*.glb') if p.stem.upper()==record['bank'])
        record.update(status='converted',file='res://'+path.relative_to(root).as_posix(),glb_sha256=hashlib.sha256(path.read_bytes()).hexdigest())
    report=dict(converter_sha256=CONVERTER_SHA256,records=records,
                interpretation='References include transitions, not proof of simultaneous geometry residency')
    (output/'manifest.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    print(json.dumps(dict(records=len(records),converted=sum(r.get('status')=='converted' for r in records),dispositions=[(r['bank'],r['status']) for r in records])))


if __name__=='__main__':main()
