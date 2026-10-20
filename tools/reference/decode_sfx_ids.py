"""Bounded .sfx/KAT identity metadata decode. Audio and original code are never run."""
import argparse
import hashlib
import json
from pathlib import Path
import struct

MAX_KAT_BYTES=256*1024*1024


def decode_sfx(blob, global_bank=False):
    if not 4 <= len(blob) <= 2048:
        raise ValueError('SFX size out of bounds')
    rows=[];aliases=set();cursor=0
    while cursor+4 <= len(blob):
        control,=struct.unpack_from('<I',blob,cursor)
        if control==0xffffffff:
            if any(blob[cursor+4:]):raise ValueError('Nonzero SFX trailing padding')
            return rows
        if len(rows)>=64 or cursor+16>len(blob):raise ValueError('SFX record/table escapes bounds')
        control,parameters,alias,reserved=struct.unpack_from('<4I',blob,cursor)
        if control&255 not in (0,254) or reserved or alias>0x3fff:
            raise ValueError('Unsupported SFX control/alias/reserved fields')
        if not global_bank and alias in aliases:raise ValueError('Duplicate level alias')
        aliases.add(alias)
        identity=len(rows)+1 if global_bank else 0x8000|alias
        if global_bank and identity>=64:raise ValueError('Global SFX ID outside playback range')
        rows.append(dict(record=len(rows),native_sfx_id=identity,alias=alias,
                         sample_index=(control>>8)&127,control=control,parameters=parameters))
        cursor+=16
    raise ValueError('SFX terminator missing')


def decode_kat(blob):
    if not 4 <= len(blob) <= MAX_KAT_BYTES:raise ValueError('KAT size out of bounds')
    count,=struct.unpack_from('<I',blob)
    table_end=4+count*44
    if not 0<count<148 or table_end>len(blob):raise ValueError('KAT table escapes bounds')
    entries=[]
    for i in range(count):
        channels,offset,size,rate,loop,bits=struct.unpack_from('<6I',blob,4+i*44)
        if channels!=1 or bits not in (4,8,16) or not 99<=rate<=999999 or offset+size>len(blob) or (size and offset<table_end):
            raise ValueError('Unsupported KAT sample or escaping payload')
        entries.append(dict(index=i,offset=offset,bytes=size,sample_rate=rate,channels=channels,bits=bits,loop=loop))
    return entries


def build_catalog(directory):
    banks={p.stem.casefold():p for p in directory.glob('*.kat')}
    records=[];dispositions=[]
    for source in sorted(directory.glob('*.sfx')):
        blob=source.read_bytes() if source.stat().st_size<=2048 else b''
        rows=decode_sfx(blob,source.stem.casefold()=='spidey')
        kat=banks.get(source.stem.casefold())
        if kat is None:
            dispositions.append(dict(bank=source.stem,status='missing KAT'));continue
        if kat.stat().st_size>MAX_KAT_BYTES:raise ValueError('KAT input budget exceeded')
        kat_blob=kat.read_bytes();assets=decode_kat(kat_blob)
        invalid=[row['record'] for row in rows if row['sample_index']>=len(assets)]
        if invalid:
            if source.stem.casefold()=='spidey':raise ValueError('Global SFX index outside KAT table')
            dispositions.append(dict(bank=source.stem,status='rejected: sample outside KAT table',records=invalid))
            continue
        for row in rows:
            asset=assets[row['sample_index']]
            records.append(dict(bank=source.stem.casefold(),source_sfx_sha256=hashlib.sha256(blob).hexdigest(),
                source_kat_sha256=hashlib.sha256(kat_blob).hexdigest(),source_suffix=f'{kat.name}/{row["sample_index"]:03}.wav',
                payload_present=asset['bytes']>0,**row))
    return dict(schema_version=1,format='bounded_pc_sfx_kat_identity_v1',records=records,dispositions=dispositions)


def main():
    root=Path(__file__).resolve().parents[2]
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--directory',type=Path,default=root/'staging/raw_extracted/Extracted/data/data')
    parser.add_argument('--output',type=Path,default=root/'staging/reference/native_sfx_catalog.json')
    args=parser.parse_args();report=build_catalog(args.directory)
    args.output.parent.mkdir(parents=True,exist_ok=True)
    args.output.write_text(json.dumps(report,indent=2),encoding='utf-8')
    print(json.dumps(dict(records=len(report['records']),dispositions=report['dispositions'])))


if __name__=='__main__':main()
