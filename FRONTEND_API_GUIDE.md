# Liike Backend API — Frontend Integration Guide

This document describes all 20 HTTP routes exposed by the Liike backend and the information a frontend needs to call them. There is no public account-registration endpoint.

## 1. Connect to the API

Use this Render base URL for every endpoint in this guide:

```text
https://liike-api.onrender.com
```

Append the route path to the base URL. For example, `GET https://liike-api.onrender.com/api/sport-events`. All endpoints use HTTPS. The deployed backend does not expose Swagger in production; Swagger UI is enabled only in the Development environment.

All API paths are under `/api`. Send and receive JSON (`Content-Type: application/json` for JSON request bodies). JSON property names are camelCase. Enum values are sent and returned as integers, not strings. Dates are ISO 8601 date-time values with an explicit timezone/offset (prefer UTC, for example `2030-06-01T10:00:00Z`).

### Browser CORS note

The backend's `NetlifyFrontend` CORS policy allows browser requests only from this frontend origin:

```text
https://liikeapp.netlify.app
```

The policy allows any HTTP method and request header, but does not enable cross-origin credentials. Send the JWT in the `Authorization: Bearer <accessToken>` header; cookie-based authentication with `credentials: "include"` is not supported by this policy. Localhost, preview, and other frontend origins are not allowed unless the backend CORS policy is changed. CORS is not an authentication mechanism.

## 2. Authentication

Public event browsing and login do not need a token. All other routes below require a JWT unless marked otherwise. Send the token in the HTTP header:

```http
Authorization: Bearer <accessToken>
```

### Sign in

`POST /api/auth/login`

```json
{
  "email": "organizer@example.com",
  "password": "<password>"
}
```

Both fields are required; email must be valid and password length must be 12–128 characters. On success (`200 OK`), the response is:

```json
{
  "accessToken": "<JWT>",
  "expiresAt": "2030-06-01T10:15:00+00:00",
  "role": "Organizer",
  "mustChangePassword": false
}
```

`role` is the string `Organizer` or `Admin` in this login response. A bad email/password or inactive organizer returns `401 Unauthorized`. The configured token lifetime is 15 minutes; use `expiresAt` as the source of truth. There is no refresh-token endpoint, so sign in again after expiry. The backend rechecks the account and organizer active status when validating a token, so deactivation/account changes can invalidate access before token expiry.

Accounts are provisioned by an administrator; the frontend cannot self-register. When `mustChangePassword` is `true`, send the user through the password-change flow before organizer-only operations. Organizer endpoints require the claim to be false.

### Change password

`POST /api/auth/change-password` — requires an authenticated Organizer or Admin token; accepts a token with `mustChangePassword: true`.

```json
{
  "currentPassword": "<current-password>",
  "newPassword": "<new-password>"
}
```

Both values must be 12–128 characters. A successful change returns `204 No Content`; an invalid current password returns `400 Bad Request`. A successful change clears `mustChangePassword`; sign in again if the client needs an updated token claim.

## 3. Endpoint inventory

Prepend `https://liike-api.onrender.com` to each path below. `{id}` is a GUID. Unless a body is shown, no request body is expected. Successful `204 No Content` responses have no JSON body.

### Authentication

| Method and path | Access | Success | Purpose |
|---|---|---|---|
| `POST /api/auth/login` | Public | `200` + login response | Authenticate and issue JWT. |
| `POST /api/auth/change-password` | Organizer or Admin | `204` | Change the current account password. |

### Public event browsing

| Method and path | Access | Query parameters | Success | Purpose |
|---|---|---|---|---|
| `GET /api/sport-events` | Public | `municipality`, `category`, `minimumPrice`, `maximumPrice`, `organizerType`, `tag`, `pageNumber` (default `1`), `pageSize` (default `20`) | `200` + event array | Search published events. |
| `GET /api/sport-events/all` | Public | `pageNumber` (default `1`), `pageSize` (default `20`) | `200` + event array | Browse published events without filters. |
| `GET /api/sport-events/nearby` | Public | `latitude`, `longitude`, optional `radiusKilometers` | `200` + nearby-event array | Find published, geocoded events around a point, ordered nearest first. |
| `GET /api/sport-events/{id}` | Public | — | `200` + event, or `404` | Get a published event. Unpublished/nonexistent IDs both return `404`. |

Search pagination requires `pageNumber >= 1` and `pageSize` from 1 through 100. Search results are ordered by start time. Municipality matching is exact after trimming. Price bounds must be non-negative and minimum cannot exceed maximum; if a price bound is supplied, events with no `price` are excluded. Nearby latitude must be -90 through 90, longitude -180 through 180, and optional radius must be non-negative. If radius is omitted, all geocoded published events are returned, nearest first. Invalid query values return `400`.

### Organizer self-service and events

| Method and path | Access | Success | Purpose |
|---|---|---|---|
| `GET /api/organizers/me` | Organizer | `200` + organizer profile | Get the signed-in organizer profile. |
| `PUT /api/organizers/me` | Organizer | `204` | Replace editable profile fields. |
| `DELETE /api/organizers/me` | Organizer | `204`, or `409` confirmation object | Permanently delete the organizer account and its events after confirmation. |
| `GET /api/sport-events/mine` | Organizer | `200` + event array | List the signed-in organizer's events. |
| `POST /api/sport-events` | Organizer | `201` + created event | Create an event. |
| `PUT /api/sport-events/{id}` | Organizer | `204` | Replace an owned event's editable fields. |
| `DELETE /api/sport-events/{id}` | Organizer | `204` | Delete an owned event. |

The `Organizer` access policy requires an active organizer account and `mustChangePassword: false`. These endpoints operate on the signed-in organizer's own data; an event belonging to another organizer is not accessible through these routes.

### Administrator organizer management

| Method and path | Access | Success | Purpose |
|---|---|---|---|
| `GET /api/organizers` | Admin | `200` + organizer array | List all organizers, including inactive ones. |
| `GET /api/organizers/{id}` | Admin | `200` + organizer, or `404` | Get an organizer. |
| `POST /api/organizers` | Admin | `201` + created organizer | Create an organizer account with a temporary password. |
| `PUT /api/organizers/{id}` | Admin | `204` | Update an organizer profile. |
| `DELETE /api/organizers/{id}` | Admin | `204`, `404`, or `409` | Delete an organizer only when they have no events. |
| `PATCH /api/organizers/{id}/active` | Admin | `204` | Activate or deactivate an organizer. |
| `POST /api/organizers/{id}/temporary-password` | Admin | `204` | Set a temporary password; the organizer must change it at next sign-in. |

## 4. Request and response shapes

### Event object

Event reads return `SportEventDto` objects with these camelCase fields:

```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "organizerId": "00000000-0000-0000-0000-000000000000",
  "organizerName": "Example Sports Club",
  "organizerType": 2,
  "title": "Community run",
  "description": "An open run for all levels.",
  "sport": "Running",
  "category": 3,
  "tags": [1, 3],
  "startTime": "2030-06-01T10:00:00Z",
  "endTime": "2030-06-01T11:00:00Z",
  "isRecurring": false,
  "isPublished": true,
  "municipality": "Helsinki",
  "address": "Mannerheimintie 1, Helsinki",
  "latitude": 60.1699,
  "longitude": 24.9384,
  "price": 0,
  "priceDescription": "Free",
  "registrationInfo": null,
  "equipmentInfo": null,
  "arrivalInfo": null,
  "additionalInfo": null
}
```

`latitude`, `longitude`, `price`, and the optional information fields may be `null`. Create/update event requests contain the editable fields only: `title`, `description`, `sport`, `category`, `tags`, `startTime`, `endTime`, `isRecurring`, `municipality`, `address`, `price`, `priceDescription`, `registrationInfo`, `equipmentInfo`, `arrivalInfo`, and `additionalInfo`. Do not send server-owned fields such as IDs, organizer data, publication state, or coordinates.

Required event fields: title (max 200 chars), description, sport (max 100), category, startTime, endTime, municipality (max 120), and address (max 300). `tags` may be an empty array. Price is optional, must not be negative, and may be at most 9999999999.99. End time must be later than start time; category must be a defined nonzero value. Unknown/unspecified tags are invalid. Update is a full replacement of editable values, not a partial patch; send all required values and the complete desired tag list.

A nearby event result wraps the full event object:

```json
{
  "event": { "id": "00000000-0000-0000-0000-000000000000" },
  "distanceKilometers": 2.4
}
```

### Organizer object

Organizer reads return:

```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "name": "Example Sports Club",
  "type": 2,
  "email": "organizer@example.com",
  "phoneNumber": "+358401234567",
  "website": "https://example.com",
  "isActive": true
}
```

`phoneNumber` and `website` may be `null`. Organizer profile create/update requests use `name`, `email`, `phoneNumber`, and `website`; admin creation additionally requires `type` and `temporaryPassword`. Names are limited to 200 characters, emails to 320, phone numbers to 32, websites to 2048. Email must be valid. Temporary passwords must be 12–128 characters.

### Administrator-specific request bodies

Create organizer (`POST /api/organizers`):

```json
{
  "name": "Example Sports Club",
  "type": 2,
  "email": "organizer@example.com",
  "temporaryPassword": "<temporary-password>",
  "phoneNumber": "+358401234567",
  "website": "https://example.com"
}
```

Set active state (`PATCH /api/organizers/{id}/active`):

```json
{ "isActive": true }
```

Set temporary password (`POST /api/organizers/{id}/temporary-password`):

```json
{ "temporaryPassword": "<temporary-password>" }
```

## 5. Enum values

Send these numeric values in JSON and query strings. The API does not use enum-name strings in JSON.

- `OrganizerType`: `0` Unspecified, `1` Student, `2` Company, `3` StudentOrganization, `4` Municipality.
- `SportCategory`: `0` Unspecified (not valid for event creation/update), `1` BallSports, `2` RacketSports, `3` Running, `4` Gym, `5` CombatSports, `6` Dance, `7` WaterSports, `8` WinterSports, `9` OutdoorActivities, `10` Other.
- `EventTag`: `0` Unspecified (not valid on an event), `1` BeginnerFriendly, `2` Indoor, `3` Outdoor, `4` TeamSport, `5` IndividualSport, `6` OlympicSport, `7` StudentEvent, `8` FreeTrial, `9` EquipmentProvided.

## 6. Deletion confirmation flow

`DELETE /api/organizers/me` deletes associated events too, so the frontend must explicitly confirm the count. First call without a query parameter. The API always responds with `409 Conflict` and a confirmation object, including when the event count is zero:

```json
{
  "eventCount": 3,
  "message": "Are you sure? This permanently deletes your organizer account and its 3 events. Retry with ?confirmEventCount=3 to confirm.",
  "confirmationRequired": true
}
```

After user confirmation, repeat the DELETE with the exact count: `DELETE /api/organizers/me?confirmEventCount=3`. If the count changes between calls, the API returns a new `409` confirmation object; show the updated count and confirm again. A negative count returns `400`.

By contrast, `DELETE /api/organizers/{id}` is an Admin-only operation and does not delete existing events: it returns `409 Conflict` if that organizer still has events.

## 7. Errors, limits, and frontend handling

- `400 Bad Request`: invalid request data, event query/coordinate values, or incorrect current password. With `[ApiController]`, model validation failures may include validation details; several explicit controller errors have an empty body.
- `401 Unauthorized`: missing/invalid/expired token, or unsuccessful login.
- `403 Forbidden`: valid identity without the required role/policy; organizer routes also require the password-change flag to be cleared.
- `404 Not Found`: missing resource, or a public request for an unpublished event.
- `409 Conflict`: duplicate organizer email, deletion constraints, or account-deletion confirmation.
- `422 Unprocessable Entity`: event address could not be geocoded.
- `429 Too Many Requests`: rate limit reached. Login is limited to 5 requests per minute per client IP; public event search is limited to 100 per minute per client IP; organizer/account operations are limited to 30 per minute per account (or client IP when unauthenticated).
- `503 Service Unavailable`: geocoding provider unavailable during event creation/update.

Use the HTTP status code as the primary success/error signal. Do not assume all non-success responses contain the same JSON error shape. Unhandled server errors are returned as generic Problem Details and do not expose internal details.

## 8. Render request examples

```sh
# Public event search
curl "https://liike-api.onrender.com/api/sport-events?pageNumber=1&pageSize=20&category=3"

# Sign in
curl -X POST "https://liike-api.onrender.com/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"organizer@example.com","password":"<password>"}'

# Authenticated organizer profile
curl "https://liike-api.onrender.com/api/organizers/me" \
  -H "Authorization: Bearer <accessToken>"

# Nearby events around Helsinki, within 25 km
curl "https://liike-api.onrender.com/api/sport-events/nearby?latitude=60.1699&longitude=24.9384&radiusKilometers=25"
```
