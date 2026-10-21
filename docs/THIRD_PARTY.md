# Third-party tools

Downloaded tools and models belong in the ignored `tools/third_party/` tree.
No original game program is executed. Retain upstream license files locally.

| Tool | Pin and setup | License |
|---|---|---|
| Godot Windows editor | 4.7.2-stable; `tools/pipeline/setup_godot.ps1`; ZIP SHA-256 `731980f9608d61333e5baf54a2ef17210acc7a538446c0cb9969f002aca1e953` | [MIT and bundled notices](https://github.com/godotengine/godot/blob/4.7.2-stable/LICENSE.txt) |
| Godot Linux x86_64 editor, used headlessly in CI | 4.7.2-stable; `tools/pipeline/setup_godot_linux.py`; ZIP SHA-256 `cadd3204e728a35d3f13adb7fd0d7902636b79f6b95c40c265eb73b6c35329e4` | Same Godot license; official release metadata supplies the digest |
| Neversoft Multitool | Existing executable SHA-256 `b83b49333dc70a58a7ff730548287b0a719800eb03408823255f68f3e330b95a`; shared pin `tools/reference/pinned_tools.py` | [MIT](https://github.com/slfx77/neversoft-multitool) |
| spidey-decomp | Read-only reference at `f8a039248908e085f8fba8106769aea7f8f9de67`; `tools/reference/setup_decomp.ps1` | No upstream license; never built, executed or redistributed |

CI uses immutable commit pins for checkout and upload-artifact (each MIT).
The hosted runner provides Python and PowerShell; no extra interpreter is
downloaded. Test dependencies
have exact versions and published universal-wheel SHA-256 pins in
`tools/pipeline/ci-requirements.txt`: Requests (Apache-2.0), Beautiful Soup (MIT),
certifi (MPL-2.0), charset-normalizer (MIT), idna (BSD-3-Clause), urllib3 (MIT),
Soup Sieve (MIT), and typing-extensions (PSF-2.0).

Font conversion dependencies are fetched by `tools/pipeline/setup_font_tools.py`
into `tools/third_party/font-tools/`, with the exact wheel SHA-256 values in that
script: Pillow 11.3.0 (HPND), fontTools 4.60.1 (MIT), Capstone 5.0.6 (BSD-3-Clause).
Pillow supports private visual QA; fontTools builds outlines/fonts; Capstone is
used only for bounded static analysis of the fingerprinted executable. Wheel
metadata includes upstream license notices. No original font or glyph is public.

No upscaler or enhancement model has been downloaded by this milestone.
Existing private audio tool installations predate this inventory;
their provenance still needs consolidation before new download/setup changes.

Frontend validation uses the desktop's already bundled dependency runtime
26.1015.12040 (no download): Sharp under the bundled Node.js packages, Apache-2.0,
https://github.com/lovell/sharp/blob/main/LICENSE . Exact installed version is
recorded in private staging/validation/menu/overlay-manifest.json. It rasterizes
the generated SVGs, crops their canvas, applies 40% alpha and produces private
comparison sheets. Runtime art decoding continues to use the existing SHA-pinned
MIT Neversoft Multitool; no new executable or model was downloaded.
