# Owner courts implementation plan

Goal: Manage child courts within an owner's sport complex using existing entities and Bootstrap Owner layout.

1. Add scoped list and form models; validate lengths, defined court status, safe image URLs and existing sport type. Offer active types plus the court's current inactive type so unrelated edits remain possible.
2. Add Owner CourtsController. Require an active approved owner and validate both complex ownership and court membership for every GET/POST. Assign parent from the authorized complex, never from posted SportComplexId. Use antiforgery and an allowlisted form model.
3. Add list/search/status filter/pagination and create/edit views. Link from each complex and retain complex context on forms, pagination and redirects. Manage retirement with Inactive status to preserve existing bookings.
4. Build Razor and run HTTP/SQL regression using disposable LocalDB: CRUD/status updates, ownership and parent tampering, invalid sport types/status/URLs, inactive current types, deleted courts, menu links and approval checks.

No migration. Time-slot and price editing are separate from court metadata; new courts have no slots until configured. Existing public booking logic already rejects Maintenance/Inactive courts.
