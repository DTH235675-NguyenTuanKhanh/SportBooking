# Owner area

Goal: Provide an Owner landing page and consistent navigation using existing Bootstrap styles.

Design: Reuse existing booking workflows. A dedicated nested layout provides active navigation for dashboard and bookings, with a root-area profile link. Dashboard shows owner-scoped complex/court counts, today's booking count and five booking rows, plus upcoming pending counter payments. Compute today in Vietnam time. Require an active, approved Owner for dashboard access, matching BookingsController.

Implementation: Add dashboard input-free view model, read-only controller and Razor view; add Owner ViewStart and layout; link the dashboard from the account dropdown and default login redirect. Explicitly set the root area on public links in the shared layout to prevent ambient Owner/Admin route values from redirecting navigation incorrectly.

Validation: Build with Razor compilation and run `python -X utf8 tests/regression.py Regression.test_owner_dashboard_and_menu`. Verify approved owner access, menu routes, login redirect and rejection of other roles or unapproved owners. No database migration required.
