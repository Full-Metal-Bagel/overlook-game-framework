# Overlook Analyzers - Diagnostic IDs

This document describes the unified diagnostic ID scheme used by Overlook analyzers.

## Naming Convention

All diagnostic IDs follow the pattern `OVL###` where:
- `OVL` stands for "Overlook" 
- `###` is a three-digit sequential number

## Current Diagnostic IDs

| ID     | Analyzer                   | Description                                                                 |
|--------|----------------------------|-----------------------------------------------------------------------------|
| OVL001 | OptionalInit               | Missing initialization - Property must be initialized in struct            |
| OVL002 | DuplicatedGuidAnalyzer     | Duplicate TypeGuid detected - Each type should have a unique GUID          |
| OVL003 | DuplicatedGuidAnalyzer     | Duplicate MethodGuid detected - Each method should have a unique GUID      |
| OVL004 | DisallowDefaultConstructor | Struct instantiation without parameters - Struct must be instantiated with parameters |

## Adding New Diagnostics

When adding new diagnostic descriptors:
1. Use the next available `OVL###` number in sequence
2. Update this documentation file
3. Ensure all tests filter by the `OVL` prefix
4. Follow the existing pattern for diagnostic severity and categories 