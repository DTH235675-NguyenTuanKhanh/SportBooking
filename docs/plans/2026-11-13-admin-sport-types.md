# Admin sport types implementation plan

Goal: Admin manages the existing SportTypes catalog through authorized MVC pages.

Architecture: Reuse the existing entity, database constraints, Bootstrap layout and pagination. Use a dedicated input model to prevent overposting. No schema change required.

1. Add validated input and list models matching database length limits.
2. Add Admin-only list/create/edit/delete actions with antiforgery validation. Normalize names, detect duplicates and handle unique constraint races. Check all courts including soft-deleted records before deletion; handle foreign-key races.
3. Add responsive list and form views, inline validation, deletion confirmation and navigation links.
4. Build Razor views and run HTTP/SQL regression coverage against a disposable LocalDB database for CRUD, validation, authorization and referenced deletion.

Design decision: Reuse full-page forms rather than modals or a separate SPA to match existing Admin pages and avoid additional dependencies. Inactive types retain court associations and disappear from existing public catalog queries.
