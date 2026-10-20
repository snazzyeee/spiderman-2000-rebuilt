# Third-party tools

Downloaded tools and models belong in the ignored `tools/third_party/` tree.
No original game program is executed. Retain upstream license files locally.

| Tool | Pin and setup | License |
|---|---|---|
| Godot Windows editor | 4.7.2-stable; `tools/pipeline/setup_godot.ps1`; ZIP SHA-256 `731980f9608d61333e5baf54a2ef17210acc7a538446c0cb9969f002aca1e953` | [MIT and bundled notices](https://github.com/godotengine/godot/blob/4.7.2-stable/LICENSE.txt) |
| Godot Linux x86_64 editor, used headlessly in CI | 4.7.2-stable; `tools/pipeline/setup_godot_linux.py`; ZIP SHA-256 `cadd3204e728a35d3f13adb7fd0d7902636b79f6b95c40c265eb73b6c35329e4` | Same Godot license; official release metadata supplies the digest |
| Neversoft Multitool | Existing executable SHA-256 `b83b49333dc70a58a7ff730548287b0a719800eb03408823255f68f3e330b95a`; shared pin `tools/reference/pinned_tools.py` | [MIT](https://github.com/slfx77/neversoft-multitool) |
| spidey-decomp | Read-only reference at `f8a039248908e085f8fba8106769aea7f8f9de67`; `tools/reference/setup_decomp.ps1` | No upstream license; never built, executed or redistributed |

CI uses immutable commit pins for checkout, setup-python and upload-artifact
(each MIT). The hosted runner provides Python and PowerShell. Test dependencies
have exact versions and published universal-wheel SHA-256 pins in
`tools/pipeline/ci-requirements.txt`: Requests (Apache-2.0), Beautiful Soup (MIT),
certifi (MPL-2.0), charset-normalizer (MIT), idna (BSD-3-Clause), urllib3 (MIT),
Soup Sieve (MIT), and typing-extensions (PSF-2.0).

No upscaler, font tracer or enhancement model has been downloaded by this
milestone. Existing private audio tool installations predate this inventory;
their provenance still needs consolidation before new download/setup changes.
