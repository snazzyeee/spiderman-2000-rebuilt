# Level push-offset files

## Recovered layout

Evidence `pkr:data/offsets:crc-audit-20261021`, confidence **recovered**:
the archive contains nine `.off` members (14–118 bytes), including L1A3_G and
L1A4_G but no L1A1_G. Each is ASCII decimal text: a nonnegative row count,
followed by exactly that many triples, separated by whitespace. Negative values
occur in the second and third columns. Private source hashes, CRCs, decoded
records and corresponding geometry counts belong in
`staging/reference/level-offsets.json`, never in public fixtures.

Evidence `decomp:DXinit.cpp#LoadPushOffsets@f8a0392 (@NotOk)`, confidence
**partial**: the loader selects `offsets/<current region filename>.off`. It
allocates one eight-byte record per declared row. The first column is stored
as a 32-bit integer; the second goes to the signed 16-bit field at byte six;
the third goes to the signed 16-bit field at byte four. This is a keyed pair
of short parameters, not a decoded three-component translation.

Evidence `decomp:DXinit.h#SPushOffset@f8a0392` and
`decomp:DXinit.cpp#validate_SPushOffset@f8a0392`, confidence **partial**:
the structure has one 32-bit field and two 16-bit fields at those offsets.
Evidence `decomp:DXinit.cpp#getNextNumber@f8a0392 (@Ok)`, confidence
**partial for signed tokens**: the helper scans whitespace-delimited decimal
tokens, but its assertion only admits digits while the data contains minus
signs. The decoder follows the observed signed decimal grammar and does not
claim that assertion proves signed-value semantics.

Evidence `remake:offset-object-range-audit`, confidence **recovered observation**:
all first-column values in the nine files fall within their corresponding G
object/model counts. Object and model counts are equal in these banks, so this
does not distinguish which index space, if either, the key denotes.

## Unknowns and placement decision

The pinned decomp exposes allocation/freeing and the text loader but no use of
the loaded records in its available C++ bodies (search for the pointer and
count globals). The name suggests a rendering adjustment; **its meaning,
units, consumer and coordinate axes remain unknown**. It is not evidence for
changing a mesh origin or TRG position. No XYZ offset, scalar scale or axis
assignment may be inferred from the file suffix or fitted to landmarks.

L1A1 has no offset member, and its seven former unmatched G-name candidates
already match O-bank objects (see units.md). These files provide no correction
for that mismatch. Applying L1A3/L1A4 records as geometric transforms, or
claiming a with/without landmark improvement, is blocked until the consumer
and parameter meanings are independently recovered. The original 21-check
L1A1 gate continues unchanged and TRG placement stays unverified.

## Decoder and test contract (authored bounds)

`tools/reference/decode_level_offsets.py` reads only archive metadata and the
selected bounded members, checks compressed-stream termination, length and CRC,
then parses at most 4 KiB / 256 records. Unknown characters, malformed counts,
extra/truncated tokens, integer overflow, duplicate/negative keys and oversized
members fail closed. The first field must fit a nonnegative signed 32-bit
integer; both short parameters must fit signed 16 bits. These are safety limits,
not claims about the original engine's complete supported input range.

Synthetic tests cover signed records and their byte-field order, malformed and
trailing input, overflow, duplicate keys, compressed CRC/length/budget failures.
Optional bounded G-file audits report key range agreement without applying any
transform. Public fixtures contain invented numbers only.
