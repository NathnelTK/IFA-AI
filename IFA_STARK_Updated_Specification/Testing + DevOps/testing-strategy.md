# Testing Strategy

## Backend

- unit tests for domain/application logic
- integration tests for PostgreSQL/API
- endpoint tests for request/response contracts
- authentication/authorization tests

## AI

Test structured outputs for:

- learner profiles
- research package normalization
- course blueprint
- module content
- quiz schema
- tutor responses

AI tests should verify schema and required fields rather than expecting identical natural-language output.

## End-to-end

Test the complete learner loop from onboarding through assessment and progress update.
