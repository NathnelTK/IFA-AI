# Error Model

API errors should be consistent and machine-readable.

External AI/tool errors should be mapped to application errors such as:

- validation_failed
- unauthorized
- forbidden
- not_found
- external_service_unavailable
- ai_generation_failed
- research_failed
- generation_timeout

Do not expose provider secrets or internal stack traces to clients.
