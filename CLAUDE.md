# Code context in this package

When gathering context or designing changes, **start with** **`Runtime/Abstractions`** in this module (and, if needed, abstractions in dependency modules).

Code under **`Runtime`** outside **`Abstractions`**—concrete types, **`Impl`**, SDK integrations, etc.—read **only when** the task needs the actual implementation (platform behavior, external API usage, fixing a provider bug), not merely the contract or extension surface.

If this package has **no** `Runtime/Abstractions`, find contracts and interfaces in the **base domain package** (the module without a vendor-specific suffix, or the primary assembly that owns the abstractions).

