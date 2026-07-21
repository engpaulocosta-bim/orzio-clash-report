# Responsibility and Authorship Decision

## Status

Deferred.

Step 27 freezes the product decision that clash responsibility and element authorship are
not part of the internal preview release.

## Decision

Autodesk sign-in is not automatic responsibility evidence.

Creator, Owner, and LastChangedBy metadata are not equivalent to clash responsibility.
Element authorship, model stewardship, and clash assignment are separate concepts and must
not be collapsed into one field.

Responsibility must not be added to `ModelIdentity`. `ModelIdentity` remains revision-free
model identity, not a person, team, assignment, or accountability record.

Source clash GUID must not become persistent assignment identity. It remains evidence only
until stability is proven across real sequential exports, and even then it would not imply
responsibility by itself.

Future responsibility is operational and human-governed state. It requires explicit
workflow rules, review, privacy controls, and project governance.

## Potential Future Sources

Potential future responsibility sources include:

- Navisworks Assigned To fields.
- An explicit Orzio mapping maintained by project governance.
- Autodesk Construction Cloud Issues.

These sources may conflict or be unavailable. Future implementation must preserve the
difference between evidence, stewardship, and assignment.

## Privacy

Personal identity output must be optional and privacy-controlled. Reports and release
artifacts must not expose personal names, email addresses, or account identifiers by
default.

## Consequences

The internal preview does not provide automatic clash responsibility, discipline owner
assignment, model-author attribution, or issue assignment.

Implementation is deferred to a later human-governance stage.
