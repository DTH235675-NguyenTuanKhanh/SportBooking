# Complete Owner court management implementation plan

Goal: Owner can configure bookable courts including safe time-slot and price management, following BUSINESS_RULES.md.

Architecture: Keep existing court metadata forms and add scoped time-slot list/create/edit pages. Serialize schedule changes and booking creation using a transaction-owned SQL application lock per complex. Re-read current court/slot data under the lock before creating bookings. Preserve booking time/price snapshots.

1. Add Vietnam clock and schedule lock helper, input models and Owner time-slot controller with ownership, hours, overlap, price and pending booking checks.
2. Add responsive slot forms/list and links from court list and edit page. Provide deactivate/reactivate through edit, not destructive deletion.
3. Integrate transaction lock into booking, court and complex updates; enforce consecutive selection, stale price confirmation and opening hours. Use booking snapshots in customer/Owner views.
4. Add HTTP/SQL regression for slot CRUD/ownership, overlap, hours, future booking locks, price changes, history and race conditions. Run existing regression suite and review final implementation.

No schema migration. Daytime slots only, whole positive VND weekday/weekend prices, no holiday pricing. Slots with the same start use the existing unique index: edit/reactivate the existing record instead of duplicating it.
