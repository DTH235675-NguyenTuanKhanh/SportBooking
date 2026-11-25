# Owner complex management implementation plan

Goal: Approved, active owners can list, create and edit their own sport complexes.

Architecture: Reuse SportComplex and existing Owner layout. Scope every query and update by authenticated owner ID; bind a dedicated form model, never OwnerId or timestamps. Preserve booking history by allowing deactivation rather than hard deletion (the existing schema cascades complex deletion to courts).

1. Add form/list models with database length limits and contact, safe HTTP(S) image URL and same-day opening-hours validation.
2. Add Owner-only controller with approval checks, scoped list/search/status/pagination and antiforgery-protected create/edit.
3. Add Bootstrap list/form views, validation feedback and Owner menu/dashboard links.
4. Build Razor and run HTTP/SQL integration tests against disposable LocalDB: CRUD, validation, CSRF, ownership tampering, foreign IDs and unapproved access; rerun Owner navigation regression.

No schema migration required. Opening hours describe the complex; they do not modify existing child-court time slots.
