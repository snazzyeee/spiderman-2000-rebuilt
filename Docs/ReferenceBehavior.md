# Reference behavior boundaries

The first playable laboratory uses newly authored code, primitive meshes,
materials, UI, and collision boxes. Nothing from the original game archives is
needed to load it, run it, test it, or build it.

The initial movement speeds, jump arc, acceleration, camera distance and pitch,
coyote time, and jump buffer are deliberate prototype choices. No claim of
original-game behavioral parity has been established.

Future fidelity work should record a bounded question, the Windows PC reference,
evidence, observed values, uncertainty, and the chosen implementation. Examples:
roof and character scale, jump duration, camera transition rules, wall attachment
conditions, enemy state transitions, trigger meaning, and mission progression.
Avoid inferring gameplay from static GLB geometry alone.

REA is optional tooling for later static reverse engineering of specific
functions/data. It does not provide the missing source or recreate a Unity game.
Keep any original/derived binaries and proprietary asset exports in the ignored
LocalReference/ tree. No original executable or installer is run by this
project's conversion/build/test workflow.
