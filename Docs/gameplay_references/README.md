# Gameplay algorithms retained from the pre-migration implementation

These 57 C# files preserve the authored runtime algorithms, scene assembly math
and regression tests byte for byte. They are source references, not an active
Unity or .NET project. The pushed archive/unity-pre-migration branch retains the
complete former project history. No proprietary game source was recovered.

Useful translation pairs include SwingSolver/PlayerStateMachine and the Godot
player motor, camera collision/damping and SpringArm3D, mission lifecycle and
intro ownership, dialogue routing, enemy states, health, checkpoints and saves.
Runtime/, Editor/ and Tests/ retain the original source directory organization.
Unity-authored Inspector/animation fields were captured separately as private
ordinary JSON before deleting their serialized engine files.

The private migration ledger records SHA-256 hashes of the reference files and
all preserved original art/data. The Godot project ignores this docs directory;
these C# files are never compiled into the runtime.
