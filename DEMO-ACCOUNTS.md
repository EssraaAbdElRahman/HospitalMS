# Development Demo Accounts

These fictional accounts are created by `DemoDataSeeder` only when the application runs in the `Development` environment. They are intended for local exploration and must never be used in production or exposed on a publicly reachable deployment.

**Shared development-only password:** `HospitalDemo!2026`

| Role | Login emails |
|---|---|
| SuperAdmin | `admin@demo.hospital.test` |
| Doctor | `doctor1@demo.hospital.test` through `doctor8@demo.hospital.test` |
| Receptionist | `receptionist1@demo.hospital.test` through `receptionist3@demo.hospital.test` |
| Patient | `patient1@demo.hospital.test`, `patient2@demo.hospital.test` |

## Local use

1. Configure a local SQL Server connection for `DefaultConnection` using user secrets or an environment-specific settings file.
2. Set `ASPNETCORE_ENVIRONMENT=Development` for the local process.
3. Run the application and open its login page. Startup applies migrations, seeds Identity roles and permission claims, and then creates demo accounts and business records.
4. Sign in with any email above and the shared development-only password.

The seeder does not reset an existing account's password. On an existing database, use the password already associated with that account or recreate the local development database if you need the fresh-seed credentials.

Do not deploy with `ASPNETCORE_ENVIRONMENT=Development`. Production administrator provisioning must use a separate secure process; these demo credentials are not production credentials.
